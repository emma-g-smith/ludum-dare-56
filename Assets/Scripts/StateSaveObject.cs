using UnityEngine;

[CreateAssetMenu(fileName = "SaveState", menuName = "Save Objects/Create SaveState")]
public class StateSaveObject : ScriptableObject
{
    [SerializeField] private Vector3 playerPosition;
    [SerializeField] private bool pumpkinCarved;
    [SerializeField] private bool canCarve;

    public Vector3 PlayerPosition { get { return playerPosition; } set { playerPosition = value; } }
    public bool PumpkinCarved { get { return pumpkinCarved; } set { pumpkinCarved = value; } }
    public bool CanCarve { get { return canCarve; } set { canCarve = value; } }

    void OnEnable()
    {
        playerPosition = new Vector3(0, 0, 0);

        pumpkinCarved = false;
        canCarve = false;
    }
}
