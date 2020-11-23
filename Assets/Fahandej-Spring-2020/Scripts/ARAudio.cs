using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ARAudio : MonoBehaviour
{
    public AudioClip[] soundFiles;
    private AudioSource _audio;
    private int _currentSoundFile;
    private bool _isARActive;

    void Start()
    {
        // Save our audio source reference.
        _audio = GetComponent<AudioSource>();
        
        // Set the correct clip, or show an error.
        if (soundFiles.Length > 0) {
            _audio.clip = soundFiles[0];
        }
        else
        {
            Debug.LogError("No sound file found!  Make sure a file has been added to the AR Audio component.");
        }
    }

    void Update()
    {
        //AudioClip currentClip = soundFiles[_currentSoundFile];
        //Debug.Log("time = " + _audio.time + ", length = " + currentClip.length);

        // While AR is active, check to see if the sound file is finished.
        if (_isARActive && !_audio.isPlaying) {
            // If there are more to play, move to the next one.
            if (_currentSoundFile < soundFiles.Length - 1)
            {
                Debug.Log("Playing next sound...");
                _currentSoundFile++;
                _audio.clip = soundFiles[_currentSoundFile];
                _audio.Play();
            }
            // If we're at the last one, reset to the beginning,
            // but wait until the next AR activation to replay.
            else
            {
                Debug.Log("Reset to beginning!");
                _currentSoundFile = 0;
                _audio.clip = soundFiles[_currentSoundFile];
                _isARActive = false;
            }
        }
    }
    
    public void Play()
    {
        _audio.Play();
        _isARActive = true;
    }

    public void Pause()
    {
        _audio.Pause();
        _isARActive = false;
    }
}
