using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class ActivateDirector : MonoBehaviour
{
    public float delay = 2;
    float timer;
    bool hasPlayed = false;

    [SerializeField] public PlayableDirector director;

    // Start is called before the first frame update
    void Start()
    {
       

    }

    // Update is called once per frame
    void Update()
    {
        if (hasPlayed == false)
        {
            timer += Time.deltaTime;
            if (timer > delay)
            {
                director.Play();
                hasPlayed = true;
            }
        }
    }
}
