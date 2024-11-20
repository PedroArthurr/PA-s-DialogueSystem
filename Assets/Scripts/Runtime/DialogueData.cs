using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueData
{
    public string NodeGUID;
    public string DialogueText;
    public string DialogueTextPreview;
    public List<ChoiceData> Choices = new();
    public Vector2 Position;
    public Color NodeColor;
    public bool EntryPoint;
}

[System.Serializable]
public class ChoiceData
{
    public string ChoiceText;
    public string TargetNodeGUID;
}
