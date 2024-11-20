using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class DialogueEditor : EditorWindow
{
    private DialogueGraphView _graphView;

    [MenuItem("Tools/Dialogue Editor")]
    public static void OpenDialogueEditor()
    {
        var window = GetWindow<DialogueEditor>();
        window.titleContent = new GUIContent("Dialogue Editor");
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView
        {
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        var saveButton = new Button(() => SaveData())
        {
            text = "Save"
        };
        toolbar.Add(saveButton);

        var loadButton = new Button(() => LoadData())
        {
            text = "Load"
        };
        toolbar.Add(loadButton);

        var nodeCreateButton = new Button(() => _graphView.CreateNode("Dialogue Node"))
        {
            text = "Create Node"
        };
        toolbar.Add(nodeCreateButton);

        rootVisualElement.Add(toolbar);
    }

    private void SaveData()
    {
        var dialogueContainer = new DialogueContainer();

        var nodes = _graphView.nodes.ToList().Cast<DialogueNode>().ToList();
        foreach (var node in nodes)
        {
            var dialogueData = new DialogueData();
            dialogueData.NodeGUID = node.GUID;
            dialogueData.DialogueText = node.DialogueText;
            dialogueData.DialogueTextPreview = node.DialogueTextPreview;
            dialogueData.Position = node.GetPosition().position;
            dialogueData.NodeColor = node.NodeColor;

            dialogueData.EntryPoint = node.EntryPoint;

            foreach (var port in node.outputContainer.Children())
            {
                var choicePort = port as Port;
                var connections = choicePort.connections;
                if (connections.Count() == 0) continue;

                var choiceData = new ChoiceData();
                choiceData.ChoiceText = choicePort.portName;

                var targetNode = connections.First().input.node as DialogueNode;
                choiceData.TargetNodeGUID = targetNode.GUID;

                dialogueData.Choices.Add(choiceData);
            }

            dialogueContainer.Nodes.Add(dialogueData);
        }

        string jsonData = JsonUtility.ToJson(dialogueContainer, true);

        string path = EditorUtility.SaveFilePanel("Save Dialogue", Application.dataPath + "/Resources/Dialogues", "NewDialogue", "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, jsonData);
        AssetDatabase.Refresh();
    }

    private void LoadData()
    {
        string path = EditorUtility.OpenFilePanel("Load Dialogue", Application.dataPath + "/Resources/Dialogues", "json");
        if (string.IsNullOrEmpty(path)) return;

        string jsonData = File.ReadAllText(path);
        var dialogueContainer = JsonUtility.FromJson<DialogueContainer>(jsonData);

        if (dialogueContainer == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load the dialogue file!", "OK");
            return;
        }

        _graphView.ClearGraph();

        foreach (var nodeData in dialogueContainer.Nodes)
        {
            DialogueNode tempNode;

            if (nodeData.EntryPoint)
            {
                tempNode = _graphView.GenerateEntryPointNode();
                tempNode.GUID = nodeData.NodeGUID;
                tempNode.SetPosition(new Rect(nodeData.Position, new Vector2(150, 200)));
            }
            else
            {
                tempNode = _graphView.CreateDialogueNode(nodeData.DialogueTextPreview, nodeData.Position);
                tempNode.GUID = nodeData.NodeGUID;
                tempNode.DialogueText = nodeData.DialogueText;
                tempNode.DialogueTextPreview = nodeData.DialogueTextPreview;
                tempNode.NodeColor = nodeData.NodeColor;

                tempNode.DialogueTextField.SetValueWithoutNotify(tempNode.DialogueText);
                tempNode.PreviewTextField.SetValueWithoutNotify(tempNode.DialogueTextPreview);

                tempNode.title = tempNode.DialogueTextPreview;

                tempNode.NodeColorField.SetValueWithoutNotify(tempNode.NodeColor);
                _graphView.ApplyNodeColor(tempNode, tempNode.NodeColor);
            }

            tempNode.EntryPoint = nodeData.EntryPoint;

            tempNode.RefreshExpandedState();
            tempNode.RefreshPorts();

            _graphView.AddElement(tempNode);
        }

        foreach (var nodeData in dialogueContainer.Nodes)
        {
            var baseNode = _graphView.nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == nodeData.NodeGUID);

            if (baseNode == null)
            {
                Debug.LogError($"Base node with GUID {nodeData.NodeGUID} not found.");
                continue;
            }

            foreach (var choice in nodeData.Choices)
            {
                var targetNode = _graphView.nodes.ToList().Cast<DialogueNode>().FirstOrDefault(x => x.GUID == choice.TargetNodeGUID);

                if (targetNode == null)
                {
                    Debug.LogError($"Target node with GUID {choice.TargetNodeGUID} not found.");
                    continue;
                }

                Port outputPort = null;
                if (baseNode.EntryPoint)
                {
                    outputPort = baseNode.outputContainer.Q<Port>();
                }
                else
                {
                    outputPort = baseNode.outputContainer.Children().FirstOrDefault(port => (port as Port).portName == choice.ChoiceText) as Port;
                }

                if (outputPort == null)
                {
                    _graphView.AddChoicePort(baseNode, choice.ChoiceText);
                    outputPort = baseNode.outputContainer.Children().FirstOrDefault(port => (port as Port).portName == choice.ChoiceText) as Port;
                }

                var inputPort = targetNode.inputContainer.Q<Port>();

                var tempEdge = new Edge
                {
                    output = outputPort,
                    input = inputPort
                };

                tempEdge?.input.Connect(tempEdge);
                tempEdge?.output.Connect(tempEdge);
                _graphView.Add(tempEdge);
            }
        }
    }
 
}
