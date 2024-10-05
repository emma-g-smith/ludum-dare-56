using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private int layerMask = 0;
    [SerializeField] private float distance = 5f;

    private Dictionary<Characters, CharacterInformation> charachterInformations;
    private Dictionary<string, ColliderInformation> colliderInformations;
    private HashSet<Characters> unlockedCharacters;
    private HashSet<Characters> playableCharacters;
    private HashSet<Characters> inventory;
    private Characters currentCharacter;

    private enum Characters
    {
        Cat,
        Mouse,
        Bat,
        Key,
        None,
        All
    }


    // Start is called before the first frame update
    void Start()
    {
        charachterInformations = new Dictionary<Characters, CharacterInformation>();
        charachterInformations[Characters.Cat] = new CharacterInformation(true, false, false, "CatImage");
        charachterInformations[Characters.Mouse] = new CharacterInformation(false, true, false, "MouseImage");
        charachterInformations[Characters.Bat] = new CharacterInformation(false, false, true, "BatImage");

        // TODO: Add door (unlocked with key)
        colliderInformations = new Dictionary<string, ColliderInformation>();
        colliderInformations["Wall"] = new ColliderInformation(stopMovement:true);
        colliderInformations["Water"] = new ColliderInformation(stopMovement:true, character:Characters.Bat);
        colliderInformations["MouseHole"] = new ColliderInformation(stopMovement:true, character:Characters.Mouse);
        colliderInformations["Vine"] = new ColliderInformation(stopMovement:true, canBreak:true, character:Characters.Cat);
        colliderInformations["Door"] = new ColliderInformation(stopMovement:true, canBreak:true, character:Characters.Key);
        colliderInformations["Key"] = new ColliderInformation(canPickup:true, character:Characters.Key);
        colliderInformations["MouseAcquire"] = new ColliderInformation(canPickup: true, character:Characters.Mouse);
        colliderInformations["BatAcquire"] = new ColliderInformation(canPickup: true, character: Characters.Bat);

        unlockedCharacters = new HashSet<Characters>();
        unlockedCharacters.Add(Characters.Cat);

        playableCharacters = new HashSet<Characters>();
        playableCharacters.Add(Characters.Cat);
        playableCharacters.Add(Characters.Mouse);
        playableCharacters.Add(Characters.Bat);

        inventory = new HashSet<Characters>();

        currentCharacter = Characters.Cat;

        RaycastHit2D interactionHitInformation = Physics2D.BoxCast(transform.position, new Vector2(interactionRadius, interactionRadius), 0, Vector2.zero);
        swapCharacter(currentCharacter, interactionHitInformation);
    }

    // Update is called once per frame
    void Update()
    {
        // Make it so characters can't be changed in bad places.
        // Add sliding on walls so movement feels nicer <- do a raycast in each direction, and alter the movement vector accordingly to where you can go
        // Need pumpkin minigame and pumpkin block

        RaycastHit2D interactionHitInformation = Physics2D.BoxCast(transform.position, new Vector2(interactionRadius, interactionRadius), 0, Vector2.zero);

        movementControl(interactionHitInformation);

        characterControl(interactionHitInformation);
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
        if (Input.GetKey(KeyCode.E))
        {
            interacting = true;
        }

        float bigBoxSize = 1f;
        float smallBoxSize = 0.9f;

        moveVector = moveVector.normalized * Time.deltaTime * movementSpeed;
        RaycastHit2D bigHitInformation = Physics2D.BoxCast(transform.position, new Vector2(bigBoxSize, bigBoxSize), 0, moveVector);
        RaycastHit2D smallHitInformation = Physics2D.BoxCast(transform.position, new Vector2(smallBoxSize, smallBoxSize), 0, moveVector);

        RaycastHit2D test = Physics2D.BoxCast(transform.position, new Vector2(smallBoxSize, smallBoxSize), 0, moveVector, distance, layerMask);

        bool goingToCollide = moveVector.magnitude > bigHitInformation.distance;
        bool movingTowardsCollision = smallHitInformation.distance - bigHitInformation.distance < bigBoxSize - smallBoxSize;
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
            if (currentCharacter != information.Character || information.CanBreak)
            {
                stopMovement = information.StopMovement;
            }
                        
            // break block
            if (interacting && information.CanBreak && (currentCharacter == information.Character || inventory.Contains(information.Character)))
            {
                bigHitInformation.collider.gameObject.SetActive(false);
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
        }

        if (goingToCollide && movingTowardsCollision && raysCollided && stopMovement && inWall)
        {
            moveVector = moveVector / moveVector.magnitude * bigHitInformation.distance;
        }

        transform.position += moveVector;
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

    private class CharacterInformation
    {
        // Variables
        private bool canSlash;
        private bool isSmall;
        private bool canFly;
        private GameObject image;

        // Public Fields
        public bool CanSlash { get { return canSlash; } }
        public bool IsSmall{ get { return isSmall; } }
        public bool CanFly { get { return canFly; } }
        public GameObject Image { get { return image; } }

        public CharacterInformation(bool canSlash, bool isSmall, bool canFly, string imageObjectName)
        {
            this.canSlash = canSlash;
            this.isSmall = isSmall;
            this.canFly = canFly;
            this.image = GameObject.Find(imageObjectName);
        }
    }


    private class ColliderInformation
    {
        // Variables
        private bool stopMovement;
        private bool canPickup;
        private bool canBreak;
        private Characters character;

        // Public Fields
        public bool StopMovement { get { return stopMovement; } }
        public bool CanPickup { get { return canPickup; } }
        public bool CanBreak { get { return canBreak; } }
        public Characters Character { get { return character; } }

        public ColliderInformation(bool stopMovement = false, bool canPickup = false, bool canBreak = false, Characters character = Characters.All)
        {
            this.stopMovement = stopMovement;
            this.canPickup = canPickup;
            this.canBreak = canBreak;
            this.character = character;
        }
    }
}
