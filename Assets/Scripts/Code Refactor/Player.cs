using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private float interactSpeed = 0.25f;
    [SerializeField] private Animator catAnimator;
    [SerializeField] private Animator mouseAnimator;
    [SerializeField] private Animator batAnimator;

    [SerializeField] private float catSize = 1.4f;
    [SerializeField] private float mouseSize = 0.6f;
    [SerializeField] private float batSize = 0.8f;

    [SerializeField] private float cameraSpeed = 5f;

    [SerializeField] private StateSaveObject state;

    private Dictionary<GameLogic.Characters, GameLogic.CharacterInformation> charachterInformations;
    private Dictionary<GameLogic.Characters, Animator> charachterAnimators;
    private Dictionary<GameLogic.Scenes, GameLogic.CameraInformation> cameraPresets;
    private HashSet<GameLogic.Characters> unlockedCharacters;
    private HashSet<GameLogic.Items> inventory;
    private HashSet<GameLogic.Layers> collideLayers;
    private HashSet<GameLogic.Layers> interactLayers;
    private CustomizableCollider lastTouchedCollider;
    private GameLogic.Characters currentCharacter;
    private GameLogic.Scenes currentScene;
    private bool justTeleported;
    private bool controlsEnabled;
    private bool dialogueFinished;
    private bool wasInteracting;
    private float timeElapsed;
    private Vector3 lastPosition;

    public delegate void OnUnlockCharacter(GameLogic.Characters character);
    public static OnUnlockCharacter onUnlockCharacter;

    public delegate void OnAddItem(GameLogic.Items item);
    public static OnAddItem onAddItem;

    public delegate void OnTeleport(Vector2 position);
    public static OnTeleport onTeleport;

    // Start is called before the first frame update
    void Start()
    {
        // Setting up character information
        charachterInformations = new Dictionary<GameLogic.Characters, GameLogic.CharacterInformation>();
        charachterInformations[GameLogic.Characters.Cat] = new GameLogic.CharacterInformation(catSize, "CatImage");
        charachterInformations[GameLogic.Characters.Mouse] = new GameLogic.CharacterInformation(mouseSize, "MouseImage");
        charachterInformations[GameLogic.Characters.Bat] = new GameLogic.CharacterInformation(batSize, "BatImage");

        // Setting Animators
        charachterAnimators = new Dictionary<GameLogic.Characters, Animator>();
        charachterAnimators[GameLogic.Characters.Cat] = catAnimator;
        charachterAnimators[GameLogic.Characters.Mouse] = mouseAnimator;
        charachterAnimators[GameLogic.Characters.Bat] = batAnimator;

        // Setting Camera infomration
        cameraPresets = new Dictionary<GameLogic.Scenes, GameLogic.CameraInformation>();
        float cameraZ = Camera.main.gameObject.transform.position.z;
        cameraPresets[GameLogic.Scenes.Inside] = new GameLogic.CameraInformation(new Vector3(0, 18, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.House });
        cameraPresets[GameLogic.Scenes.House] = new GameLogic.CameraInformation(new Vector3(0, 0, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.Maze, GameLogic.Scenes.Garden, GameLogic.Scenes.Inside });
        cameraPresets[GameLogic.Scenes.Maze] = new GameLogic.CameraInformation(new Vector3(-32, 0, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.House, GameLogic.Scenes.Tree });
        cameraPresets[GameLogic.Scenes.Tree] = new GameLogic.CameraInformation(new Vector3(-64, 8.5f, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.Maze });
        cameraPresets[GameLogic.Scenes.Garden] = new GameLogic.CameraInformation(new Vector3(30.8f,-4, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.House, GameLogic.Scenes.CaveOutside, GameLogic.Scenes.PumkinCarving });
        cameraPresets[GameLogic.Scenes.PumkinCarving] = new GameLogic.CameraInformation(new Vector3(30.8f, -55, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.Garden });
        cameraPresets[GameLogic.Scenes.CaveOutside] = new GameLogic.CameraInformation(new Vector3(61.8f, 0, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.Garden, GameLogic.Scenes.CaveInside });
        cameraPresets[GameLogic.Scenes.CaveInside] = new GameLogic.CameraInformation(new Vector3(93.8f, 0, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.CaveOutside, GameLogic.Scenes.BoneRoom });
        cameraPresets[GameLogic.Scenes.BoneRoom] = new GameLogic.CameraInformation(new Vector3(125.8f, 0, cameraZ), new List<GameLogic.Scenes> { GameLogic.Scenes.CaveInside });

        // Making unlocked Character List
        unlockedCharacters = new HashSet<GameLogic.Characters>();
        unlockedCharacters.Add(GameLogic.Characters.Cat);
        mouseAnimator.gameObject.SetActive(false);
        batAnimator.gameObject.SetActive(false);

        // Setting Clear inventory
        inventory = new HashSet<GameLogic.Items>();

        // Setting Collide Layers
        collideLayers = new HashSet<GameLogic.Layers>();
        collideLayers.Add(GameLogic.Layers.AlwaysWall);
        collideLayers.Add(GameLogic.Layers.SometimesWall);
        
        // Setting Interact Layers
        interactLayers = new HashSet<GameLogic.Layers>();
        interactLayers.Add(GameLogic.Layers.AlwaysWall);
        interactLayers.Add(GameLogic.Layers.SometimesWall);
        interactLayers.Add(GameLogic.Layers.Neverwall);

        // Updating non-set variables
        currentCharacter = GameLogic.Characters.Cat;
        currentScene = GameLogic.Scenes.Inside;
        justTeleported = false;
        controlsEnabled = false;
        dialogueFinished = false;
        wasInteracting = false;
        timeElapsed = 0;
        lastPosition = new Vector3();

        // Settings event and deligate interactions
        DialogueHandler.onDialogueStart += dialogueStarted;
        DialogueHandler.onDialogueEnd += dialogueEnded;
        onUnlockCharacter += unlockCharacter;
        onAddItem += addItem;
        onTeleport += teleport;

        // Calling specific update methods
        swapCharacter(currentCharacter);
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

            cameraControl();

            if (currentCharacter == GameLogic.Characters.Cat)
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

    // Delegate methods
    private void dialogueStarted(DialogueHandler.Dialogues dialogue)
    {
        controlsEnabled = false;
    }
    private void dialogueEnded()
    {
        controlsEnabled = true;
        dialogueFinished = true;
    }
    private void unlockCharacter(GameLogic.Characters character)
    {
        unlockedCharacters.Add(character);
    }
    private void addItem(GameLogic.Items item)
    {
        inventory.Add(item);
    }
    private void teleport(Vector2 position)
    {
        lastPosition = transform.position;
        justTeleported = true;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
    public void carvingComplete()
    {
        controlsEnabled = true;

        state.CanCarve = false;
        transform.position = lastPosition;
        justTeleported = true;
    }

    // Helper Methods
    // Character Control
    private void swapCharacter(GameLogic.Characters targetCharacter)
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

            RaycastHit2D cast = Physics2D.BoxCast(transform.position, new Vector2(newHitboxSize, newHitboxSize), 0, Vector2.zero, raycastDistance, (int)GameLogic.Layers.AlwaysWall);

            if (cast.collider == null)
            {
                cast = Physics2D.BoxCast(transform.position, new Vector2(newHitboxSize, newHitboxSize), 0, Vector2.zero, raycastDistance, (int)GameLogic.Layers.SometimesWall);

                if (cast.collider == null)
                {
                    changeCharacter = true;
                }
                else
                {
                    //ColliderInformation information = colliderInformations[cast.collider.tag];

                    //if (!information.StopMovement || information.Character == targetCharacter)
                    //{
                    //    changeCharacter = true;
                    //}
                }
            }
        }

        // change character if it is appropriate to do so
        if (changeCharacter)
        {
            currentCharacter = targetCharacter;
        }


        foreach (GameLogic.Characters character in unlockedCharacters)
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

    // Movement Control
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            swapCharacter(GameLogic.Characters.Cat);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            swapCharacter(GameLogic.Characters.Mouse);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            swapCharacter(GameLogic.Characters.Bat);
        }

        charachterAnimators[currentCharacter].SetBool("IsInteracting", interacting);

        // add custom interacting timing for the cat
        if (currentCharacter == GameLogic.Characters.Cat)
        {
            interacting = wasInteracting && !charachterAnimators[currentCharacter].GetCurrentAnimatorStateInfo(0).IsName("cat_claw");
        }
        else
        {
            interacting = wasInteracting && (timeElapsed >= interactSpeed);
        }

        moveVector = moveVector.normalized * Time.deltaTime * movementSpeed;

        Vector3 wallMove = findGreatestPossibleMove(moveVector, GameLogic.Layers.AlwaysWall);
        Vector3 permiableMove = findGreatestPossibleMove(moveVector, GameLogic.Layers.SometimesWall);

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

        interact(interacting);

        transform.position += moveVector;
    }


    private Vector3 findGreatestPossibleMove(Vector3 potentialMove, GameLogic.Layers layer)
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
    private Vector3 moveCalculations(Vector3 potentialMove, GameLogic.Layers layer, Vector3? location = null, bool useBigBox = true)
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
            CustomizableCollider information = hitInformation.collider.gameObject.GetComponent<CustomizableCollider>();

            // stop movement
            if (currentCharacter != information.StopsMotionEnum)
            {
                stopMovement = information.StopsMotion;
            }
        }

        if (goingToCollide && movingTowardsCollision && raysCollided && stopMovement)
        {
            return potentialMove / potentialMove.magnitude * hitInformation.distance;
        }
        else
        {
            return potentialMove;
        }
    }
    private void interact(bool userInteracting)
    {
        // Perform logic to see if interaction should occur
        HashSet<CustomizableCollider> interactCollisions = new HashSet<CustomizableCollider>();

        foreach (GameLogic.Layers layer in interactLayers)
        {
            float hitBoxScale = charachterInformations[currentCharacter].HitBoxScale;
            RaycastHit2D hit = Physics2D.BoxCast(transform.position, new Vector2(interactionRadius * hitBoxScale, interactionRadius * hitBoxScale), 0, Vector2.zero, raycastDistance, (int)layer);

            if (hit.collider != null)
            {
                interactCollisions.Add(hit.collider.gameObject.GetComponent<CustomizableCollider>());
            }
        }

        if (!interactCollisions.Contains(lastTouchedCollider) || justTeleported)
        {
            lastTouchedCollider = null;
        }

        foreach (CustomizableCollider collider in interactCollisions)
        {            
            // Interact if you are supposed to
            if (userInteracting)
            {
                collider.Interact(currentCharacter, inventory);
            }

            if (collider.InteractsOnTouch && lastTouchedCollider != collider)
            {
                collider.Interact(currentCharacter, inventory);
                lastTouchedCollider = collider;
            }
        }
    }

    // Camera Control
    private void cameraControl()
    {
        GameLogic.CameraInformation information = cameraPresets[currentScene];

        // see if scene needs to be changed
        if (!characterInScene(currentScene))
        {
            foreach (GameLogic.Scenes scene in information.Neighbors)
            {
                if (characterInScene(scene))
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
            Camera.main.transform.position = information.Position;
            justTeleported = false;
        }
        else
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, information.Position, cameraSpeed * Time.deltaTime);
        }
    }
    private bool characterInScene(GameLogic.Scenes scene)
    {
        GameLogic.CameraInformation information = cameraPresets[scene];

        float x_value = transform.position.x;
        float y_value = transform.position.y;

        return x_value > information.LeftBound && x_value < information.RightBound && y_value > information.BottomBound && y_value < information.TopBound;
    }
}
