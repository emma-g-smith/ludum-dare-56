using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public Sound[] musicSounds;
    public AudioSource musicSource;
    // Start is called before the first frame update
    void Start()
    {
        PlayMusic();
    }

    public void PlayMusic()
    {
        Sound s = musicSounds[0];

        musicSource.clip = s.clip;
        musicSource.Play();
    }
}
