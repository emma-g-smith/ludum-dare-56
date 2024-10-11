using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionBoxManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject panel;

    private string leaveText;
    private string enterText;

    // event and delegate
    public delegate void OnCanTeleport(bool willEnter);
    public static OnCanTeleport onCanTeleport;

    public delegate void OnCannotTeleport();
    public static OnCannotTeleport onCannotTeleport;

    // Start is called before the first frame update
    void Start()
    {
        leaveText = "[E] to Leave";
        enterText = "[E] to Enter";

        panel.SetActive(false);
        text.text = "";

        onCanTeleport += canTeleport;
        onCannotTeleport += cannotTeleport;
    }

    private void canTeleport(bool willEnter)
    {
        if (willEnter)
        {
            text.text = enterText;
        }
        else
        {
            text.text = leaveText;
        }
        
        panel.SetActive(true);
    }

    private void cannotTeleport()
    {
        panel.SetActive(false);
    }
}
