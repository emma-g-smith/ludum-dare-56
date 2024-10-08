using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private float interactSpeed = 0.25f;
    [SerializeField] private Animator catAnimator;
    [SerializeField] private Animator mouseAnimator;
    [SerializeField] private Animator batAnimator;

    [SerializeField] private float catSize = 1.2f;
    [SerializeField] private float mouseSize = 0.6f;
    [SerializeField] private float batSize= 0.8f;

    [SerializeField] private float cameraSpeed = 5f;

    [SerializeField] private StateSaveObject state;
    [SerializeField] private GameObject mouseCollectable;
    [SerializeField] private GameObject pumpkinFace;
    [SerializeField] private SpriteRenderer vinesImage;
    [SerializeField] private Sprite vinesCutImage;
    [SerializeField] private PolygonCollider2D vineCollider;

    private Dictionary<Characters, CharacterInformation> charachterInformations;
    private Dictionary<Characters, Animator> charachterAnimators;
    private Dictionary<Scenes, CameraInformation> cameraPresets;
    private Dictionary<string, ColliderInformation> colliderInformations;
    private HashSet<Characters> unlockedCharacters;
    private HashSet<Characters> playableCharacters;
    private HashSet<Characters> inventory;
    private Characters currentCharacter;
    private Scenes currentScene;
    private bool wasInteracting;
    private bool justTeleported;
    private bool controlsEnabled;
    private bool dialogueFinished;
    private GameObject lastFriendAquired;
    private float timeElapsed;

    private enum Characters
    {
        Cat,
        Mouse,
        Bat,
        Key,
        Bones,
        None,
        All
    }

    private enum Scenes
    {
        Inside,
        House,
        Maze,
        Tree,
        Garden,
        PumkinCarving,
        CaveOutside,
        CaveInside,
        BoneRoom,
    }

    private enum Layers
    {
        AlwaysWall = 1 << 3,
        SometimesWall = 1 << 6,
        Neverwall = 1 << 7,
    }


    // Start is called before the first frame update
    void Start()
    {
        charachterInformations = new Dictionary<Characters, CharacterInformation>();
        charachterInformations[Characters.Cat] = new CharacterInformation(true, false, false, catSize, "CatImage");
        charachterInformations[Characters.Mouse] = new CharacterInformation(false, true, false, mouseSize, "MouseImage");
        charachterInformations[Characters.Bat] = new CharacterInformation(false, false, true, batSize, "BatImage");

        charachterAnimators = new Dictionary<Characters, Animator>();
        charachterAnimators[Characters.Cat] = catAnimator;
        charachterAnimators[Characters.Mouse] = mouseAnimator;
        charachterAnimators[Characters.Bat] = batAnimator;

        cameraPresets = new Dictionary<Scenes, CameraInformation>();
        cameraPresets[Scenes.Inside] = new CameraInformation(16, -16, 27, 9, new List<Scenes> { Scenes.House });
        cameraPresets[Scenes.House] = new CameraInformation(16, -16, 9, -9, new List<Scenes> { Scenes.Maze, Scenes.Garden, Scenes.Inside });
        cameraPresets[Scenes.Maze] = new CameraInformation(-16, -48, 9, -9, new List<Scenes> { Scenes.House, Scenes.Tree });
        cameraPresets[Scenes.Tree] = new CameraInformation(-48, -80, 17.5f, -0.5f, new List<Scenes> { Scenes.Maze });
        cameraPresets[Scenes.Garden] = new CameraInformation(46.8f, 14.8f, 5, -13, new List<Scenes> { Scenes.House, Scenes.CaveOutside, Scenes.PumkinCarving });
        cameraPresets[Scenes.PumkinCarving] = new CameraInformation(46.8f, 14.8f, -46, -64, new List<Scenes> { Scenes.Garden });
        cameraPresets[Scenes.CaveOutside] = new CameraInformation(77.8f, 45.8f, 9, -9, new List<Scenes> { Scenes.Garden, Scenes.CaveInside });
        cameraPresets[Scenes.CaveInside] = new CameraInformation(109.8f, 77.8f, 9, -9, new List<Scenes> { Scenes.CaveOutside, Scenes.BoneRoom });
        cameraPresets[Scenes.BoneRoom] = new CameraInformation(141.8f, 109.8f, 9, -9, new List<Scenes> { Scenes.CaveInside });

        colliderInformations = new Dictionary<string, ColliderInformation>();
        colliderInformations["Wall"] = new ColliderInformation(stopMovement:true);
        colliderInformations["Water"] = new ColliderInformation(stopMovement:true, hasDialogue: true, character:Characters.Bat, dialogue: DialogueHandler.Dialogues.HintFly);
        colliderInformations["MouseHole"] = new ColliderInformation(stopMovement:true, hasDialogue: true, character:Characters.Mouse, dialogue: DialogueHandler.Dialogues.HintSmall);
        colliderInformations["Vine"] = new ColliderInformation(stopMovement:true, canBreak:true, hasDialogue: true, character:Characters.Cat, dialogue: DialogueHandler.Dialogues.HintVines);
        colliderInformations["Door"] = new ColliderInformation(stopMovement:true, canBreak:true, character:Characters.Key);
        colliderInformations["Key"] = new ColliderInformation(canPickup:true, character:Characters.Key);
        colliderInformations["MouseAcquire"] = new ColliderInformation(canPickup: true, character:Characters.Mouse);
        colliderInformations["BatAcquire"] = new ColliderInformation(canPickup: true, character: Characters.Bat);
        colliderInformations["Bones"] = new ColliderInformation(stopMovement:true, canPickup: true, hasDialogue: true, character: Characters.Bones, dialogue: DialogueHandler.Dialogues.Letter);
        colliderInformations["Pumpkin"] = new ColliderInformation(canInteract: true, stopMovement: true, character: Characters.Cat, targetSceneName:"PumpkinGame");
        colliderInformations["Witch"] = new ColliderInformation(stopMovement: true);
        
        // teleporters
        colliderInformations["HouseOutside"] = new ColliderInformation(canTeleport: true, teleportName:"TeleporterHouseInside");
        colliderInformations["HouseInside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterHouseOutside");
        colliderInformations["CaveOutside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterCaveInside");
        colliderInformations["CaveInside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterCaveOutside");
        colliderInformations["BonesOutside"] = new ColliderInformation(canTeleport: true, character: Characters.Key, teleportName: "TeleporterBonesInside");
        colliderInformations["BonesInside"] = new ColliderInformation(canTeleport: true, character: Characters.Bones, teleportName: "TeleporterBonesOutside");

        unlockedCharacters = new HashSet<Characters>();
        unlockedCharacters.Add(Characters.Cat);

        playableCharacters = new HashSet<Characters>();
        playableCharacters.Add(Characters.Cat);
        playableCharacters.Add(Characters.Mouse);
        playableCharacters.Add(Characters.Bat);

        inventory = new HashSet<Characters>();

        currentCharacter = Characters.Cat;

        currentScene = Scenes.Inside;

        swapCharacter(currentCharacter);

        wasInteracting = false;
        justTeleported = false;
        controlsEnabled = false;
        dialogueFinished = false;

        timeElapsed = 0;
        mouseCollectable.SetActive(state.PumpkinCarved);
        pumpkinFace.SetActive(state.PumpkinCarved);

        // load the save state
        if (state.PumpkinCarved)
        {
            transform.position = state.PlayerPosition;
            justTeleported = true;
        }
        if (state.VinesCut)
        {
            vinesImage.sprite = vinesCutImage;
            vineCollider.enabled = false;
        }

        // hooking up events and delegates
        DialogueHandler.onDialogueStart += dialogueStarted;
        DialogueHandler.onDialogueEnd += dialogueEnded;
    }

    // Update is called once per frame
    void Update()
    {
        if (controlsEnabled)
        {
            if (dialogueFinished)
            {
                dialogueFinished = false;
            }
            else
            {
                movementControl();
            }

            characterControl();

            cameraControl();

            // only have custom interaction code if cat
            if (currentCharacter == Characters.Cat)
            {
                wasInteracting = catAnimator.GetCurrentAnimatorStateInfo(0).IsName("cat_claw");
            }
            else
            {
                wasInteracting = timeElapsed < interactSpeed;

                timeElapsed += Time.deltaTime;
            }
        }
    }

    private void dialogueStarted(DialogueHandler.Dialogues dialogue)
    {
        controlsEnabled = false;
    }
    private void dialogueEnded()
    {
        controlsEnabled = true;
        dialogueFinished = true;

        if (lastFriendAquired != null && unlockedCharacters.Contains(Characters.Mouse))
        {
            lastFriendAquired.SetActive(false);
        }
        if(unlockedCharacters.Contains(Characters.Bat))
        {
            lastFriendAquired.SetActive(false);
        }
    }

    private void movementControl()
    {
        Vector3 moveVector = new Vector3(0, 0);
        bool interacting = false;

        if (Input.GetKey(KeyCode.W))
        {
            moveVector.y += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveVector.y -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveVector.x += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveVector.x -= 1;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            interacting = true;
            timeElapsed = 0;
        }

        charachterAnimators[currentCharacter].SetBool("IsInteracting", interacting);

        // add custom interacting timing for the cat
        if (currentCharacter == Characters.Cat)
        {
            interacting = wasInteracting && !charachterAnimators[currentCharacter].GetCurrentAnimatorStateInfo(0).IsName("cat_claw");
        }
        else
        {
            interacting = wasInteracting && (timeElapsed >= interactSpeed);
        }

        moveVector = moveVector.normalized * Time.deltaTime * movementSpeed;

        Vector3 wallMove = findGreatestPossibleMove(moveVector, Layers.AlwaysWall);
        Vector3 permiableMove = findGreatestPossibleMove(moveVector, Layers.SometimesWall);

        float signX = Math.Sign(moveVector.x);
        float signY = Math.Sign(moveVector.y);

        moveVector.x = signX * Math.Min(Math.Abs(wallMove.x), Math.Abs(permiableMove.x));
        moveVector.y = signY * Math.Min(Math.Abs(wallMove.y), Math.Abs(permiableMove.y));

        //flip animation based on direction - this might not work when switching characters. Maybe solution is to flip us and not the images
        if (moveVector.x > 0)
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        if (moveVector.x < 0)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        charachterAnimators[currentCharacter].SetBool("IsMoving", moveVector.magnitude != 0);

        if (interacting)
        {
            interact(Layers.AlwaysWall);
            interact(Layers.SometimesWall);
            interact(Layers.Neverwall);
        }

        transform.position += moveVector;
    }


    private Vector3 findGreatestPossibleMove(Vector3 potentialMove, Layers layer)
    {
        Vector3 calculatedMoveVector = moveCalculations(potentialMove, layer);

        // if the move is diagonal and cannot make move, see if individual x adn y moves work
        if (potentialMove.x != 0 && potentialMove.y != 0)
        {
            // only do extra calculation if move has been cut
            if (calculatedMoveVector != potentialMove)
            {
                Vector3 xMove = new Vector3(potentialMove.x, 0, 0);
                Vector3 yMove = new Vector3(0, potentialMove.y, 0);

                xMove = moveCalculations(xMove, layer);
                yMove = moveCalculations(yMove, layer);

                calculatedMoveVector.x = xMove.x;
                calculatedMoveVector.y = yMove.y;
            }
        }

        // final check to ensure move is valid
        if (potentialMove.x != 0 || potentialMove.y != 0)
        {
            Vector3 checkMove = moveCalculations(calculatedMoveVector, layer, transform.position + calculatedMoveVector, false);

            if (checkMove == Vector3.zero)
            {
                calculatedMoveVector = Vector3.zero;
            }
        }

        return calculatedMoveVector;
    }
    private Vector3 moveCalculations(Vector3 potentialMove, Layers layer, Vector3? location = null, bool useBigBox=true) //RaycastHit2D interactionHitInformation, bool interacting)
    {
        Vector3 lookPoint;
        if (location == null)
        {
            lookPoint = transform.position;
        }
        else
        {
            lookPoint = (Vector3)location;
        }
        
        // BoxCast only for colliders on the specified layer according to hitbox
        float bigBoxSize = charachterInformations[currentCharacter].HitBoxScale;
        float smallBoxSize = bigBoxSize * 0.9f;

        RaycastHit2D bigHitInformation = Physics2D.BoxCast(lookPoint, new Vector2(bigBoxSize, bigBoxSize), 0, potentialMove, raycastDistance, (int)layer);
        RaycastHit2D smallHitInformation = Physics2D.BoxCast(lookPoint, new Vector2(smallBoxSize, smallBoxSize), 0, potentialMove, raycastDistance, (int)layer);

        bool goingToCollide = potentialMove.magnitude > bigHitInformation.distance;
        bool movingTowardsCollision = smallHitInformation.distance - bigHitInformation.distance < (bigBoxSize - smallBoxSize) / 2;
        bool raysCollided = bigHitInformation.collider != null && smallHitInformation.collider != null;
        bool stopMovement = false;

        // look at specified box
        RaycastHit2D hitInformation;
        if (useBigBox)
        {
            hitInformation = bigHitInformation;
        }
        else
        {
            hitInformation = smallHitInformation;
        }

        // only stop movement for specific cases
        if (hitInformation.collider != null)
        {
            ColliderInformation information = colliderInformations[hitInformation.collider.tag];

            // stop movement
            if (currentCharacter != information.Character || information.CanBreak || information.CanInteract)
            {
                stopMovement = information.StopMovement;
            }
        }

        if(goingToCollide && movingTowardsCollision && raysCollided && stopMovement)
        {
            return potentialMove / potentialMove.magnitude * hitInformation.distance;
        }
        else
        {
            return potentialMove;
        }
    }
    private void interact(Layers layer)
    {
        float hitBoxScale = charachterInformations[currentCharacter].HitBoxScale;
        RaycastHit2D interactionHitInformation = Physics2D.BoxCast(transform.position, new Vector2(interactionRadius * hitBoxScale, interactionRadius * hitBoxScale), 0, Vector2.zero, raycastDistance, (int) layer);
        
        // only interact with things if this hits something
        if (interactionHitInformation.collider != null)
        {
            ColliderInformation information = colliderInformations[interactionHitInformation.collider.tag];

            // block break
            if (information.CanBreak && (currentCharacter == information.Character || inventory.Contains(information.Character)))
            {
                if (interactionHitInformation.collider.tag == "Vine")
                {
                    vinesImage.sprite = vinesCutImage;
                    vineCollider.enabled = false;
                }
                else
                {
                    interactionHitInformation.collider.gameObject.SetActive(false);
                }
            }

            // pick up
            if (information.CanPickup)
            {
                if (playableCharacters.Contains(information.Character))
                {
                    unlockedCharacters.Add(information.Character);

                    // call specific dialogues when characters are unlocked
                    if (information.Character == Characters.Mouse)
                    {
                        DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.MeetingMouse); 
                    }
                    if (information.Character == Characters.Bat)
                    {
                        DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.MeetingBat); 
                    }

                    lastFriendAquired = interactionHitInformation.collider.gameObject;
                }
                else
                {
                    inventory.Add(information.Character);
                    interactionHitInformation.collider.gameObject.SetActive(false);
                }
            }

            // teleport
            if (information.CanTeleport)
            {
                // make sure if character is specified, that item is in inventory
                if (inventory.Contains(information.Character) || information.Character == Characters.All)
                {
                    Vector3 targetPosition = GameObject.Find(information.TeleportName).transform.position;
                    targetPosition.z = transform.position.z;

                    transform.position = targetPosition;
                    justTeleported = true;
                }
                
                // if not show dialogue message
                if(!inventory.Contains(information.Character))
                {
                    if (information.Character == Characters.Key)
                    {
                        DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.HintLock);
                    }
                    if (information.Character == Characters.Bones)
                    {
                        DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.HintBones);
                    }
                }
            }

            // pumpkin teleport
            if (information.CanInteract && !state.PumpkinCarved)
            {
                state.PlayerPosition = transform.position;
                state.VinesCut = true;

                // hard coded to get working
                transform.position = new Vector3(31, -58, transform.position.z);
                justTeleported = true;
                controlsEnabled = false;
            }

            // dialogue
            if (information.HasDialogue)
            {
                // make sure if character is specified, that it is excluded
                if (currentCharacter != information.Character || information.Character == Characters.All)
                {
                    DialogueHandler.onDialogueStart?.Invoke(information.Dialogue);
                }
            }

            // custom witch code
            if (interactionHitInformation.collider.tag == "Witch")
            {
                if (inventory.Contains(Characters.Bones))
                {
                    DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.Final);
                }
                else
                {
                    DialogueHandler.onDialogueStart?.Invoke(DialogueHandler.Dialogues.Intro);
                }
            }
        }
    }

    public void carvingComplete()
    {
        controlsEnabled = true;

        state.VinesCut = false;
        transform.position = state.PlayerPosition;
        justTeleported = true;

        mouseCollectable.SetActive(state.PumpkinCarved);
        pumpkinFace.SetActive(state.PumpkinCarved);
    }

    private void characterControl()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            swapCharacter(Characters.Cat);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            swapCharacter(Characters.Mouse);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            swapCharacter(Characters.Bat);
        }
    }
    private void swapCharacter(Characters targetCharacter)
    {
        bool changeCharacter = false;
        
        // Don't allow change if character is not unlocked
        if (unlockedCharacters.Contains(targetCharacter))
        {
            changeCharacter = true;
        }

        // make it so that characters can only be swapped in good places
        if (changeCharacter)
        {
            float newHitboxSize = charachterInformations[targetCharacter].HitBoxScale;

            RaycastHit2D cast = Physics2D.BoxCast(transform.position, new Vector2(newHitboxSize, newHitboxSize), 0, Vector2.zero, raycastDistance, (int)Layers.AlwaysWall);

            if (cast.collider == null)
            {
                cast = Physics2D.BoxCast(transform.position, new Vector2(newHitboxSize, newHitboxSize), 0, Vector2.zero, raycastDistance, (int)Layers.SometimesWall);

                if (cast.collider == null)
                {
                    changeCharacter = true;
                }
                else
                {
                    ColliderInformation information = colliderInformations[cast.collider.tag];

                    if (!information.StopMovement || information.Character == targetCharacter)
                    {
                        changeCharacter = true;
                    }
                }
            }
        }

        // change character if it is appropriate to do so
        if (changeCharacter)
        {
            currentCharacter = targetCharacter;
        }


        foreach (Characters character in playableCharacters)
        {
            if (character == currentCharacter)
            {
                charachterInformations[character].Image.SetActive(true);
            }
            else
            {
                charachterInformations[character].Image.SetActive(false);
            }
        }

        
    }

    private void cameraControl()
    {
        CameraInformation information = cameraPresets[currentScene];
        
        // see if scene needs to be changed
        if (!characterInScene(currentScene))
        {
            foreach (Scenes scene in information.Neighbors)
            {
                if(characterInScene(scene))
                {
                    currentScene = scene;
                    break;
                }
            }
        }


        // move camera towards correct position for scene if pan and jump cut if not
        if (justTeleported)
        {
            information = cameraPresets[currentScene];
            Camera.main.transform.position = information.CameraTarget;
            justTeleported = false;
        }
        else
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, information.CameraTarget, cameraSpeed * Time.deltaTime);
        }
    }
    private bool characterInScene(Scenes scene)
    {
        CameraInformation information = cameraPresets[scene];

        float x_value = transform.position.x;
        float y_value = transform.position.y;

        return x_value > information.LeftBound && x_value < information.RightBound && y_value > information.BottomBound && y_value < information.TopBound;
    }

    private class CharacterInformation
    {
        // Variables
        private bool canSlash;
        private bool isSmall;
        private bool canFly;
        private float hitBoxScale;
        private GameObject image;

        // Public Fields
        public bool CanSlash { get { return canSlash; } }
        public bool IsSmall{ get { return isSmall; } }
        public bool CanFly { get { return canFly; } }
        public float HitBoxScale { get { return hitBoxScale; } }
        public GameObject Image { get { return image; } }

        public CharacterInformation(bool canSlash, bool isSmall, bool canFly, float hitBoxScale, string imageObjectName)
        {
            this.canSlash = canSlash;
            this.isSmall = isSmall;
            this.canFly = canFly;
            this.hitBoxScale = hitBoxScale;
            this.image = GameObject.Find(imageObjectName);
        }
    }
    private class ColliderInformation
    {
        // Variables
        private bool stopMovement;
        private bool canPickup;
        private bool canBreak;
        private bool canInteract;
        private bool canTeleport;
        private Characters character;
        private string targetSceneName;
        private string teleportName;
        private bool hasDialogue;
        private DialogueHandler.Dialogues dialogue;

        // Public Fields
        public bool StopMovement { get { return stopMovement; } }
        public bool CanPickup { get { return canPickup; } }
        public bool CanBreak { get { return canBreak; } }
        public bool CanInteract { get { return canInteract; } }
        public bool CanTeleport { get { return canTeleport; } }
        public Characters Character { get { return character; } }
        public string TargetSceneName { get { return targetSceneName; } }
        public string TeleportName { get { return teleportName; } }
        public bool HasDialogue { get { return hasDialogue; } }
        public DialogueHandler.Dialogues Dialogue { get { return dialogue; } }

        public ColliderInformation(bool stopMovement = false, bool canPickup = false, bool canBreak = false, bool canInteract = false, bool canTeleport = false, Characters character = Characters.All, string targetSceneName="", string teleportName = "", bool hasDialogue = false, DialogueHandler.Dialogues dialogue = DialogueHandler.Dialogues.None)
        {
            this.stopMovement = stopMovement;
            this.canPickup = canPickup;
            this.canBreak = canBreak;
            this.canInteract = canInteract;
            this.canTeleport = canTeleport;
            this.character = character;
            this.targetSceneName = targetSceneName;
            this.teleportName = teleportName;
            this.hasDialogue = hasDialogue;
            this.dialogue = dialogue;
        }
    }
    private class CameraInformation
    {
        private float rightBound;
        private float leftBound;
        private float topBound;
        private float bottomBound;
        private List<Scenes> neighbors;
        private Vector3 cameraTarget;

        public float RightBound { get { return rightBound; } }
        public float LeftBound { get { return leftBound; } }
        public float TopBound { get { return topBound; } }
        public float BottomBound { get { return bottomBound; } }
        public List<Scenes> Neighbors { get { return neighbors; } }
        public Vector3 CameraTarget { get { return cameraTarget; } }

        public CameraInformation(float rightBound, float leftBound, float topBound, float bottomBound, List<Scenes> neighbors)
        {
            this.rightBound = rightBound;
            this.leftBound = leftBound;
            this.topBound = topBound;
            this.bottomBound = bottomBound;
            this.neighbors = neighbors;

            this.cameraTarget = new Vector3((rightBound + leftBound) / 2, (topBound + bottomBound) / 2, Camera.main.transform.position.z);
        }
    }

}
