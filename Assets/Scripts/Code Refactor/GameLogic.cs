using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public enum Characters
    {
        None,
        Cat,
        Mouse,
        Bat,
    }

    public enum Items
    {
        None,
        Key,
        Bones,
    }

    public enum LayerIndex
    {
        AlwaysWall = 3,
        SometimesWall = 6,
        Neverwall = 7,
    }

    public enum Layers
    {
        AlwaysWall = 1 << 3,
        SometimesWall = 1 << 6,
        Neverwall = 1 << 7,
    }

    public enum Scenes
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public class CharacterInformation
    {
        // Variables
        private float hitBoxScale;
        private GameObject image;

        // Public Fields
        public float HitBoxScale { get { return hitBoxScale; } }
        public GameObject Image { get { return image; } }

        public CharacterInformation(float hitBoxScale, string imageObjectName)
        {
            this.hitBoxScale = hitBoxScale;
            this.image = GameObject.Find(imageObjectName);
        }
    }

    public class CameraInformation
    {
        private Vector3 position;
        private float xBound;
        private float yBound;
        private List<Scenes> neighbors;

        public float RightBound { get { return position.x + xBound; } }
        public float LeftBound { get { return position.x - xBound; } }
        public float TopBound { get { return position.y + yBound; } }
        public float BottomBound { get { return position.y - yBound; } }
        public List<Scenes> Neighbors { get { return neighbors; } }
        public Vector3 Position { get { return position; } }

        public CameraInformation(Vector3 position, List<Scenes> neighbors)
        {
            this.position = position;
            this.neighbors = neighbors;

            // constants
            xBound = 16;
            yBound = 9;
        }
    }
}
