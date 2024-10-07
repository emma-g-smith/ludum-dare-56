using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private int layerMask = 0;
    [SerializeField] private float distance = 5f;
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

    private enum Characters
    {
        Cat,
        Mouse,
        Bat,
        Key,
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
        CaveOutside,
        CaveInside,
        BoneRoom,
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
        cameraPresets[Scenes.Garden] = new CameraInformation(46.8f, 14.8f, 5, -13, new List<Scenes> { Scenes.House, Scenes.CaveOutside });
        cameraPresets[Scenes.CaveOutside] = new CameraInformation(77.8f, 45.8f, 9, -9, new List<Scenes> { Scenes.Garden, Scenes.CaveInside });
        cameraPresets[Scenes.CaveInside] = new CameraInformation(109.8f, 77.8f, 9, -9, new List<Scenes> { Scenes.CaveOutside, Scenes.BoneRoom });
        cameraPresets[Scenes.BoneRoom] = new CameraInformation(46.8f, 14.8f, 9, -9, new List<Scenes> { Scenes.CaveInside });

        colliderInformations = new Dictionary<string, ColliderInformation>();
        colliderInformations["Wall"] = new ColliderInformation(stopMovement:true);
        colliderInformations["Water"] = new ColliderInformation(stopMovement:true, character:Characters.Bat);
        colliderInformations["MouseHole"] = new ColliderInformation(stopMovement:true, character:Characters.Mouse);
        colliderInformations["Vine"] = new ColliderInformation(stopMovement:true, canBreak:true, character:Characters.Cat);
        colliderInformations["Door"] = new ColliderInformation(stopMovement:true, canBreak:true, character:Characters.Key);
        colliderInformations["Key"] = new ColliderInformation(canPickup:true, character:Characters.Key);
        colliderInformations["MouseAcquire"] = new ColliderInformation(canPickup: true, character:Characters.Mouse);
        colliderInformations["BatAcquire"] = new ColliderInformation(canPickup: true, character: Characters.Bat);
        colliderInformations["Pumpkin"] = new ColliderInformation(canInteract: true, stopMovement: true, character: Characters.Cat, targetSceneName:"PumpkinGame");
        colliderInformations["HouseOutside"] = new ColliderInformation(canTeleport: true, teleportName:"TeleporterHouseInside");
        colliderInformations["HouseInside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterHouseOutside");
        // Make it so only the mouse can teleport
        colliderInformations["CaveOutside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterCaveInside");
        colliderInformations["CaveInside"] = new ColliderInformation(canTeleport: true, teleportName: "TeleporterCaveOutside");

        unlockedCharacters = new HashSet<Characters>();
        unlockedCharacters.Add(Characters.Cat);

        playableCharacters = new HashSet<Characters>();
        playableCharacters.Add(Characters.Cat);
        playableCharacters.Add(Characters.Mouse);
        playableCharacters.Add(Characters.Bat);

        inventory = new HashSet<Characters>();

        currentCharacter = Characters.Cat;

        currentScene = Scenes.House;

        swapCharacter(currentCharacter, getInteraction());

        wasInteracting = false;
        justTeleported = false;

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
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit2D interactionHitInformation = getInteraction();

        movementControl(interactionHitInformation);

        characterControl(interactionHitInformation);

        cameraControl();

        // only have custom interaction code if cat
        if (currentCharacter == Characters.Cat)
        {
            wasInteracting = catAnimator.GetCurrentAnimatorStateInfo(0).IsName("cat_claw");
        }
    }

    private RaycastHit2D getInteraction()
    {
        float hitBoxScale = charachterInformations[currentCharacter].HitBoxScale;
        return Physics2D.BoxCast(transform.position, new Vector2(interactionRadius * hitBoxScale, interactionRadius * hitBoxScale), 0, Vector2.zero);
    }

    private void movementControl(RaycastHit2D interactionHitInformation)
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
        }

        charachterAnimators[currentCharacter].SetBool("IsInteracting", interacting);

        // add custom interacting timing for the cat
        if (currentCharacter == Characters.Cat)
        {
            interacting = wasInteracting && !charachterAnimators[currentCharacter].GetCurrentAnimatorStateInfo(0).IsName("cat_claw");
        }

        moveVector = moveVector.normalized * Time.deltaTime * movementSpeed;

        Vector3 calculatedMoveVector = moveCalculations(moveVector, interactionHitInformation, interacting);

        // if the move is diagonal and cannot make move, see if individual x adn y moves work
        if(moveVector.x != 0 && moveVector.y != 0)
        {
            // only do extra calculation if move has been cut
            if (calculatedMoveVector != moveVector)
            {
                Vector3 xMove = new Vector3(moveVector.x, 0, 0);
                Vector3 yMove = new Vector3(0, moveVector.y, 0);

                xMove = moveCalculations(xMove, interactionHitInformation, interacting);
                yMove = moveCalculations(yMove, interactionHitInformation, interacting);

                calculatedMoveVector.x = xMove.x;
                calculatedMoveVector.y = yMove.y;
            }
        }
        
        moveVector = calculatedMoveVector;

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

        transform.position += moveVector;
    }
    private Vector3 moveCalculations(Vector3 potentialMove, RaycastHit2D interactionHitInformation, bool interacting)
    {
        float bigBoxSize = charachterInformations[currentCharacter].HitBoxScale;
        float smallBoxSize = bigBoxSize * 0.9f;

        RaycastHit2D bigHitInformation = Physics2D.BoxCast(transform.position, new Vector2(bigBoxSize, bigBoxSize), 0, potentialMove);
        RaycastHit2D smallHitInformation = Physics2D.BoxCast(transform.position, new Vector2(smallBoxSize, smallBoxSize), 0, potentialMove);

        RaycastHit2D test = Physics2D.BoxCast(transform.position, new Vector2(smallBoxSize, smallBoxSize), 0, potentialMove, distance, layerMask);

        bool goingToCollide = potentialMove.magnitude > bigHitInformation.distance;
        bool movingTowardsCollision = smallHitInformation.distance - bigHitInformation.distance < (bigBoxSize - smallBoxSize) / 2;
        bool inWall = true;
        bool raysCollided = bigHitInformation.collider != null && smallHitInformation.collider != null;
        bool stopMovement = false;

        // check if thing being interacted with is in the interaction radius
        interacting &= interactionHitInformation.collider != null;

        // Correcting for in wall edge case behavior
        if (bigHitInformation.collider != null && smallHitInformation.collider != null)
        {
            ColliderInformation bigInformation = colliderInformations[bigHitInformation.collider.tag];
            ColliderInformation smallInformation = colliderInformations[smallHitInformation.collider.tag];

            bool bigNoMove = bigInformation.StopMovement && currentCharacter != bigInformation.Character && bigHitInformation.distance == 0;
            bool smallCanMove = !(smallInformation.StopMovement && currentCharacter != smallInformation.Character) && smallHitInformation.distance == 0;
            bool movingAwayFromWall = test.collider == null;

            inWall = !(bigNoMove && smallCanMove && movingAwayFromWall);
        }

        // only stop movement for specific cases
        if (bigHitInformation.collider != null)
        {
            ColliderInformation information = colliderInformations[bigHitInformation.collider.tag];

            // stop movement
            if (currentCharacter != information.Character || information.CanBreak || information.CanInteract)
            {
                stopMovement = information.StopMovement;
            }

            // break block
            if (interacting && information.CanBreak && (currentCharacter == information.Character || inventory.Contains(information.Character)))
            {
                if (bigHitInformation.collider.tag == "Vine")
                {
                    vinesImage.sprite = vinesCutImage;
                    vineCollider.enabled = false;
                    state.VinesCut = true;
                }
                else
                {
                    bigHitInformation.collider.gameObject.SetActive(false);
                }
            }

            // pick up
            if (interacting && information.CanPickup)
            {
                bigHitInformation.collider.gameObject.SetActive(false);
                if (playableCharacters.Contains(information.Character))
                {
                    unlockedCharacters.Add(information.Character);
                }
                else
                {
                    inventory.Add(information.Character);
                }
            }

            // teleport
            if (interactionHitInformation.collider != null)
            {
                ColliderInformation interactionInformation = colliderInformations[interactionHitInformation.collider.tag];

                if (interacting && interactionInformation.CanTeleport)
                {
                    Vector3 targetPosition = GameObject.Find(interactionInformation.TeleportName).transform.position;
                    targetPosition.z = transform.position.z;

                    transform.position = targetPosition;
                    justTeleported = true;
                }
            }
            
            // change scene
            if (interacting && information.CanInteract)
            {
                state.PlayerPosition = transform.position;
                SceneManager.LoadScene(information.TargetSceneName);
            }
        }

        if(goingToCollide && movingTowardsCollision && raysCollided && stopMovement && inWall)
        {
            return potentialMove / potentialMove.magnitude * bigHitInformation.distance;
        }
        else
        {
            return potentialMove;
        }
    }

    private void characterControl(RaycastHit2D interactionHitInformation)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            swapCharacter(Characters.Cat, interactionHitInformation);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            swapCharacter(Characters.Mouse, interactionHitInformation);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            swapCharacter(Characters.Bat, interactionHitInformation);
        }
    }
    private void swapCharacter(Characters targetCharacter, RaycastHit2D interactionHitInformation)
    {
        bool changeCharacter = true;
        
        // Don't allow change if character is not unlocked
        if (!unlockedCharacters.Contains(targetCharacter))
        {
            changeCharacter = false;
        }

        // make it so that characters can only be swapped in good places
        if (interactionHitInformation.collider != null)
        {
            ColliderInformation information = colliderInformations[interactionHitInformation.collider.tag];

            if (information.StopMovement && information.Character != targetCharacter)
            {
                changeCharacter = false;
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

        // Public Fields
        public bool StopMovement { get { return stopMovement; } }
        public bool CanPickup { get { return canPickup; } }
        public bool CanBreak { get { return canBreak; } }
        public bool CanInteract { get { return canInteract; } }
        public bool CanTeleport { get { return canTeleport; } }
        public Characters Character { get { return character; } }
        public string TargetSceneName { get { return targetSceneName; } }
        public string TeleportName { get { return teleportName; } }

        public ColliderInformation(bool stopMovement = false, bool canPickup = false, bool canBreak = false, bool canInteract = false, bool canTeleport = false, Characters character = Characters.All, string targetSceneName="", string teleportName = "")
        {
            this.stopMovement = stopMovement;
            this.canPickup = canPickup;
            this.canBreak = canBreak;
            this.canInteract = canInteract;
            this.canTeleport = canTeleport;
            this.character = character;
            this.targetSceneName = targetSceneName;
            this.teleportName = teleportName;
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
