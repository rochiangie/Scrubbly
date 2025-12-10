using UnityEngine;

public class MusicMuteToggle : MonoBehaviour
{
    public AudioSource musicSource;
    private bool isMuted = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMuted = !isMuted;
            musicSource.mute = isMuted;
        }
    }
}
