using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class DialogueGraphView : GraphView
{
    public DialogueGraphView()
    {
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Resources/NodeStyle.uss");
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }
        else
        {
            Debug.LogError("StyleSheet 'NodeStyle.uss' not found. Make sure the file is in 'Assets/Resources/'.");
        }

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(GenerateEntryPointNode());
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode
        {
            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "ENTRYPOINT",
            EntryPoint = true
        };

        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
        node.outputContainer.Add(generatedPort);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        // Apply styles to the entry node
        node.AddToClassList("entry-node");

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(100, 200, 150, 200));

        return node;
    }

    public void CreateNode(string nodeName)
    {
        AddElement(CreateDialogueNode(nodeName, Vector2.zero));
    }

    public DialogueNode CreateDialogueNode(string nodeName, Vector2 position)
    {
        var node = new DialogueNode
        {
            title = nodeName,
            DialogueText = "",
            DialogueTextPreview = "",
            GUID = Guid.NewGuid().ToString()
        };

        var inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        node.inputContainer.Add(inputPort);

        var titleContainer = node.titleContainer;
        var colorField = new ColorField()
        {
            value = node.NodeColor,
            name = "nodeColorField",
            style =
            {
                width = 30,
                height = 20,
                marginLeft = 5
            }
        };
        colorField.RegisterValueChangedCallback(evt =>
        {
            node.NodeColor = evt.newValue;
            ApplyNodeColor(node, node.NodeColor);
        });
        titleContainer.Add(colorField);

        node.NodeColorField = colorField;

        ApplyNodeColor(node, node.NodeColor);

        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("node-content-container");

        var previewTextField = new TextField("Preview Text")
        {
            multiline = true
        };
        previewTextField.RegisterValueChangedCallback(evt =>
        {
            node.DialogueTextPreview = evt.newValue;
            node.title = evt.newValue; 
        });
        previewTextField.SetValueWithoutNotify(node.DialogueTextPreview);
        previewTextField.AddToClassList("node-text-field");
        previewTextField.style.whiteSpace = WhiteSpace.Normal;
        previewTextField.style.flexGrow = 1;
        previewTextField.style.height = StyleKeyword.Auto;
        contentContainer.Add(previewTextField);

        node.PreviewTextField = previewTextField;

        var textField = new TextField("Dialogue Text")
        {
            multiline = true
        };
        textField.RegisterValueChangedCallback(evt =>
        {
            node.DialogueText = evt.newValue;
        });
        textField.SetValueWithoutNotify(node.DialogueText);
        textField.AddToClassList("node-text-field");
        textField.style.whiteSpace = WhiteSpace.Normal;
        textField.style.flexGrow = 1;
        textField.style.height = StyleKeyword.Auto;
        contentContainer.Add(textField);

        node.DialogueTextField = textField;

        node.mainContainer.Add(contentContainer);

        var button = new Button(() => { AddChoicePort(node); });
        button.text = "Add Choice";
        node.titleButtonContainer.Add(button);

        node.AddToClassList("dialogue-node");

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, new Vector2(300, 250)));

        return node;
    }

    public void AddChoicePort(DialogueNode node, string overriddenPortName = "")
    {
        int outputPortCount = node.outputContainer.Query("connector").ToList().Count;
        if (outputPortCount >= 4)
        {
            EditorUtility.DisplayDialog("Option Limit", "A node cannot have more than 4 options.", "OK");
            return;
        }

        var generatedPort = GeneratePort(node, Direction.Output);

        var portName = string.IsNullOrEmpty(overriddenPortName) ? $"Option {outputPortCount + 1}" : overriddenPortName;
        generatedPort.portName = portName;

        var textField = new TextField
        {
            name = string.Empty,
            value = portName
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label("  "));
        generatedPort.contentContainer.Add(textField);

        var deleteButton = new Button(() => RemovePort(node, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);

        node.outputContainer.Add(generatedPort);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    private void RemovePort(DialogueNode node, Port port)
    {
        var targetEdge = edges.ToList().FirstOrDefault(edge =>
            edge.output.portName == port.portName && edge.output.node == port.node);

        if (targetEdge != null)
        {
            targetEdge.input.Disconnect(targetEdge);
            RemoveElement(targetEdge);
        }
        node.outputContainer.Remove(port);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    public void ClearGraph()
    {
        nodes.ToList().ForEach(node => RemoveElement(node));
        edges.ToList().ForEach(edge => RemoveElement(edge));

        AddElement(GenerateEntryPointNode());
    }

    public void ApplyNodeColor(DialogueNode node, Color color)
    {
        node.style.backgroundColor = color;
    }
}
