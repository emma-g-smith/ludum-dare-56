using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeScript : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiGroup;
    [SerializeField] private bool fadeIn = false;
    [SerializeField] public float delay = 30;
    float timer;
    // Start is called before the first frame update
    void Start()
    {
        fadeIn = true;
        HideUI();
    }

    public void ShowUI()
    {
        uiGroup.alpha = 1;
    }

    public void HideUI()
    {
        uiGroup.alpha = 0;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > delay)
        {
            if (fadeIn)
            {
                if (uiGroup.alpha < 1)
                {
                    uiGroup.alpha += Time.deltaTime;
                    if (uiGroup.alpha >= 1)
                    {
                        fadeIn = false;
                    }
                }
            }
        }

        
    }
}
