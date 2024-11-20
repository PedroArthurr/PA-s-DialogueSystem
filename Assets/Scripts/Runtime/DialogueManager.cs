using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private string fileName;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button[] optionButtons;

    private DialogueContainer dialogueContainer;
    private Dictionary<string, DialogueData> dialogueDictionary;
    private DialogueData currentNode;

    [SerializeField] private float typingSpeed = 0.05f;

    private bool isTyping = false;

    void Start()
    {
        LoadDialogue(fileName); 
        StartDialogue();
    }

    public void LoadDialogue(string fileName)
    {
        TextAsset targetFile = Resources.Load<TextAsset>($"Dialogues/{fileName}");
        if (targetFile == null)
        {
            Debug.LogError("Dialogue file not found!");
            return;
        }

        dialogueContainer = JsonUtility.FromJson<DialogueContainer>(targetFile.text);
        dialogueDictionary = new Dictionary<string, DialogueData>();
        foreach (var node in dialogueContainer.Nodes)
        {
            dialogueDictionary.Add(node.NodeGUID, node);
        }

        var startNode = dialogueContainer.Nodes.FirstOrDefault(node => node.EntryPoint == true);
        if (startNode != null && startNode.Choices.Count > 0)
        {
            var firstNodeGUID = startNode.Choices[0].TargetNodeGUID;
            currentNode = dialogueDictionary[firstNodeGUID];
        }
        else
        {
            Debug.LogError("Start node not found or has no connections!");
        }
    }

    public void StartDialogue()
    {
        DisplayCurrentNode();
    }

    public void DisplayCurrentNode()
    {
        StopAllCoroutines();
        isTyping = false;

        StartCoroutine(TypeSentence(currentNode.DialogueText));

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentNode.Choices.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                var choice = currentNode.Choices[i];
                var targetNode = dialogueDictionary[choice.TargetNodeGUID];

                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceText;

                string nextNodeGUID = choice.TargetNodeGUID;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => ProceedToNextNode(nextNodeGUID));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void ProceedToNextNode(string nextNodeGUID)
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentNode.DialogueText;
            isTyping = false;
            return;
        }

        if (dialogueDictionary.TryGetValue(nextNodeGUID, out var nextNode))
        {
            currentNode = nextNode;
            DisplayCurrentNode();
        }
        else
        {
            Debug.Log("End of dialogue.");
            foreach (var button in optionButtons)
            {
                button.gameObject.SetActive(false);
            }
            dialogueText.text = "End of dialogue.";
        }
    }
}
