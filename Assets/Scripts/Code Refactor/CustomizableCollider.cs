using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomizableCollider : MonoBehaviour
{
    // Stops Motion
    [SerializeField] private bool stopsMotion = false;
    [SerializeField] private GameLogic.Characters stopsMotionEnum = GameLogic.Characters.None;
    public bool StopsMotion { get { return stopsMotion; } }
    public GameLogic.Characters StopsMotionEnum { get { return stopsMotionEnum; } }

    // Unlocks Character
    [SerializeField] private bool addsCharacter = false;
    [SerializeField] private GameLogic.Characters addsCharacterEnum = GameLogic.Characters.None;

    // Adds Item to Inventory
    [SerializeField] private bool addsInventory = false;
    [SerializeField] private GameLogic.Items addsInventoryEnum = GameLogic.Items.None;

    // Breaks Current Block
    [SerializeField] private bool getsBroken = false;
    [SerializeField] private GameLogic.Characters getsBrokenCharacter = GameLogic.Characters.None;
    [SerializeField] private GameLogic.Items getsBrokenItem = GameLogic.Items.None;

    // Allows Teleportation
    [SerializeField] private bool allowsTeleportation = false;
    [SerializeField] private bool interactionHint = false;
    [SerializeField] private bool teleportationWillEnter = false;
    [SerializeField] private GameObject teleportationTargetObject = null;
    [SerializeField] private GameLogic.Items teleportRequiredItem = GameLogic.Items.None;
    public bool AllowsTeleportation { get { return allowsTeleportation; } }
    public bool InteractionHint { get { return interactionHint; } }
    public bool TeleportationWillEnter { get { return teleportationWillEnter; } }


    // Allows Pumpkin Carving Minigame
    [SerializeField] private bool allowsCarving = false;
    [SerializeField] private StateSaveObject state;

    // Interacts on Touch
    [SerializeField] private bool interactsOnTouch = false;
    public bool InteractsOnTouch { get { return interactsOnTouch; } }

    // Interacts once
    [SerializeField] private bool interactsOnce = false;
    private bool canInteract = true;

    // Addes dialogue when interacted with
    [SerializeField] private DialogueHandler.Dialogues dialogues = DialogueHandler.Dialogues.None;
    [SerializeField] private bool deleteAfterDialogue = false;
    [SerializeField] private GameLogic.Characters dialogueHintCharacter = GameLogic.Characters.None;
    [SerializeField] private GameLogic.Items dialogueHintItem = GameLogic.Items.None;

    // Enables/Disables Objects on interaction
    [SerializeField] private GameObject[] enableOnInteract = null;
    [SerializeField] private GameObject[] disableOnInteract = null;
    [SerializeField] private GameObject[] hides = null;

    // Start is called before the first frame update
    private void Start()
    {
        GameLogic.LayerIndex targetLayer = GameLogic.LayerIndex.AlwaysWall;
        
        // Set the layer of the current object to the correct layer
        if (!stopsMotion)
        {
            targetLayer = GameLogic.LayerIndex.Neverwall;
        }
        if (stopsMotionEnum != GameLogic.Characters.None)
        {
            targetLayer = GameLogic.LayerIndex.SometimesWall;
        }

        gameObject.layer = (int)targetLayer;


        if (hides != null)
        {
            foreach (GameObject gameObject in hides)
            {
                gameObject.SetActive(false);
            }
        }
    }

    // Method to perform interaction
    public void Interact(GameLogic.Characters currentCharacter, HashSet<GameLogic.Items> inventory)
    {
        // interact once
        if (!canInteract)
        {
            return;
        }
        if (interactsOnce)
        {
            canInteract = false;
        }

        // unlock character
        if (addsCharacter)
        {
            Player.onUnlockCharacter?.Invoke(addsCharacterEnum);
        }

        // add item to inventory
        if (addsInventory)
        {
            Player.onAddItem?.Invoke(addsInventoryEnum);
        }

        // block break
        if (getsBroken)
        {
            if (getsBrokenCharacter == GameLogic.Characters.None && getsBrokenItem == GameLogic.Items.None)
            {
                gameObject.SetActive(false);
            }
            if (currentCharacter == getsBrokenCharacter)
            {
                gameObject.SetActive(false);
            }
            if (inventory.Contains(getsBrokenItem))
            { 
                gameObject.SetActive(false);
            }
        }

        // teleport
        if (allowsTeleportation)
        {
            if (teleportRequiredItem == GameLogic.Items.None)
            {
                Vector2 targetLocation = new Vector2(teleportationTargetObject.gameObject.transform.position.x, teleportationTargetObject.gameObject.transform.position.y);

                Player.onTeleport?.Invoke(targetLocation);
            }
            if (inventory.Contains(teleportRequiredItem))
            {
                Vector2 targetLocation = new Vector2(teleportationTargetObject.gameObject.transform.position.x, teleportationTargetObject.gameObject.transform.position.y);

                Player.onTeleport?.Invoke(targetLocation);
            }
        }

        // allows carving minigame
        if (allowsCarving)
        {
            state.CanCarve = true;
        }

        // play dialogue
        if (dialogues != DialogueHandler.Dialogues.None)
        {
            if (dialogueHintCharacter == GameLogic.Characters.None && dialogueHintItem == GameLogic.Items.None)
            {
                DialogueHandler.onDialogueStart?.Invoke(dialogues);

                if (deleteAfterDialogue)
                {
                    DialogueHandler.onDialogueEnd += inactivateObject;
                }
            }
            else
            {
                if (dialogueHintCharacter != currentCharacter && dialogueHintItem == GameLogic.Items.None)
                {
                    DialogueHandler.onDialogueStart?.Invoke(dialogues);

                    if (deleteAfterDialogue)
                    {
                        DialogueHandler.onDialogueEnd += inactivateObject;
                    }
                }
                if (!inventory.Contains(dialogueHintItem) && dialogueHintCharacter == GameLogic.Characters.None)
                {
                    DialogueHandler.onDialogueStart?.Invoke(dialogues);

                    if (deleteAfterDialogue)
                    {
                        DialogueHandler.onDialogueEnd += inactivateObject;
                    }
                }
            }
        }

        // enable specified objects
        if (enableOnInteract != null)
        {
            foreach (GameObject gameObject in enableOnInteract)
            {
                gameObject.SetActive(true);
            }
        }

        // disable specified objects
        if (disableOnInteract != null)
        {
            foreach (GameObject gameObject in disableOnInteract)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void inactivateObject()
    {
        gameObject.SetActive(false);
    }
}
