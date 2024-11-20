using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;

public class DialogueNode : Node
{
    public string GUID;
    public string DialogueText;
    public string DialogueTextPreview;
    public bool EntryPoint = false;
    public Vector2 Position;

    public TextField DialogueTextField;
    public TextField PreviewTextField;

    public Color NodeColor = Color.gray;
    public ColorField NodeColorField; 
}
