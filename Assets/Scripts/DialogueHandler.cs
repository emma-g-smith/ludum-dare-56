using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject panel;
    [SerializeField] private float textSpeed = 1;

    // enums
    public enum Dialogues
    {
        Intro,
        MeetingMouse,
        MeetingBat,
        HintFly,
        HintSmall,
        HintLock,
        Letter,
        Final,
        None,
    }

    // event and delegate
    public delegate void OnDialogueStart(Dialogues dialogue);
    public static OnDialogueStart onDialogueStart;

    public delegate void OnDialogueEnd();
    public static event OnDialogueEnd onDialogueEnd;

    // private variables
    private Dictionary<Dialogues, DialogueInformation> dialogueInformations;
    private string[] lines;
    private int index;

    // Start is called before the first frame update
    void Start()
    {
        dialogueInformations = new Dictionary<Dialogues, DialogueInformation>();
        dialogueInformations[Dialogues.Intro] = new DialogueInformation(new string[] { 
            "Witch: You are always so forgetful. Your controls are: [WASD] to move [e] to interact [123] to swap",
            "Witch: sigh...",
            "Witch: Being immortal is so lonely. The last time i saw my husband, he asked me to go see the world for him.",
            "Witch: He knew he�d be gone by the time I returned. He had grown quite old. It was his last wish.", 
            "Witch: I would give anything to see him again. My husband� where is he?"
        });
        dialogueInformations[Dialogues.MeetingMouse] = new DialogueInformation(new string[] {
            "Mouse: Thanks for the assist! I have been stuck in that pumpking for a while now.",
            "Mouse: How about I tag along with you?",
            "You: I wouldn't mind the company."
        });
        dialogueInformations[Dialogues.MeetingBat] = new DialogueInformation(new string[] {
            "Bat: It's Morbing TIME.",
            "You: Huh?"
        });
        dialogueInformations[Dialogues.HintFly] = new DialogueInformation(new string[] {
            "You: I can�t cross the water... if only i had a way to fly...?"
        }, noFill:true);
        dialogueInformations[Dialogues.HintSmall] = new DialogueInformation(new string[] {
            "You: I�m not small enough to crawl through"
        }, noFill: true);
        dialogueInformations[Dialogues.HintLock] = new DialogueInformation(new string[] {
            "You: It�s locked"
        }, noFill: true);
        dialogueInformations[Dialogues.Letter] = new DialogueInformation(new string[] {
            "Letter: I knew you would find me!"
        }, noFill: true);
        dialogueInformations[Dialogues.Final] = new DialogueInformation(new string[] {
            "Witch: You found him? Words cannot express my gratitude!",
            "Witch: Oh, how i�ve missed you!"
        });


        // Set up text variables
        text.text = string.Empty;
        panel.SetActive(false);
        onDialogueStart += StartDialogue;
        onDialogueEnd += EndDialogue;

        // call intro dialog
        onDialogueStart?.Invoke(Dialogues.Intro);
    }

    // Update is called once per frame
    void Update()
    {
        if(panel.activeSelf && Input.GetKeyDown(KeyCode.E))
        {
            if(text.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                text.text = lines[index];
            }
        }
    }

    private void StartDialogue(Dialogues dialogue)
    {
        lines = dialogueInformations[dialogue].Lines;
        panel.SetActive(true);
        index = 0;
        StartCoroutine(TypeLine());
    }

    private void EndDialogue()
    {
        text.text = string.Empty;
        panel.SetActive(false);
        index = 0;
    }

    private IEnumerator TypeLine()
    {
        foreach(char character in lines[index].ToCharArray())
        {
            text.text += character;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            text.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            onDialogueEnd?.Invoke();
        }
    }
    
    // Dataclass to hardcode different dialogues with
    private class DialogueInformation
    {
        private string[] lines;
        private bool noFill;

        public string[] Lines { get { return lines; } }
        public bool NoFill { get { return noFill; } }

        public DialogueInformation(string[] lines, bool noFill = false)
        {
            this.lines = lines;
            this.noFill = noFill;
        }
    }
}
