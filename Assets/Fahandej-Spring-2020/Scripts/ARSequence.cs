using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

public class ARSequence : MonoBehaviour
{
    public ARClip[] Clips;
    public int startAtClip;
    
    private AudioSource _audio;
    private RawImage _image;
    private VideoPlayer _video;
    private CustomPolyRenderer _poly;
    private Text _downloadingText;
    
    private int _currentClip;
    private bool _isARActive;
    private bool _isPlaying;

    private Vector3 _polyStartPosition;

    public enum PolyRotation{ Default, Rotate90, Rotate180, Rotate270,
        AxisSwap, AxisSwapRotate90, AxisSwapRotate180, AxisSwapRotate270}
    
    [System.Serializable]
    public struct ARClip
    {
        public Texture2D ImageFile;
        public VideoClip VideoFile;
        public AudioClip SoundFile;
        public string PolyID;
        public float PolyScale;
        public Vector3 PolyPositionAdjust;
        public PolyRotation PolyRotation;
    }
    
    void Awake()
    {
        // Save some references.
        _audio = GetComponentInChildren<AudioSource>();
        _image = GetComponentInChildren<RawImage>();
        _video = GetComponentInChildren<VideoPlayer>();
        _poly = GetComponentInChildren<CustomPolyRenderer>();
        _downloadingText = GetComponentInChildren<Text>();
        
        // Set the correct clip, or show an error.
        _currentClip = startAtClip;
        if (Clips.Length > 0) {
            LoadCurrentClip();
        } else {
            Debug.LogError("No AR clips found!  Make sure at least one clip with a sound file, has been added to the ARSequence component.");
        }
    }

    private void InitPolyForThisTarget()
    {
        // Save the poly start position.
        _polyStartPosition = _poly.transform.localPosition;
        
        // Ask our Poly renderer to load all the things.
        List<string> polyIdList = new List<string>();
        for (int i = 0; i < Clips.Length; i++)
        {
            // Quick sanity check - since I couldn't set an initializer in the struct,
            // let's overwrite "scale 0" with "scale 1" here.
            if (Clips[i].PolyScale.Equals(0f))
            {
                Clips[i].PolyScale = 1.0f;
            }
    
            // Now actually fill up our List of IDs.
            string polyID = Clips[i].PolyID;
            if (!string.IsNullOrEmpty(polyID))
            {
                polyIdList.Add(polyID);
            }
        }
        _poly.LoadPolyAssetsFromStringArray(polyIdList.ToArray());
    }

    void Update()
    {
        // Timing debug log!
        //AudioClip currentClip = arClips[_currentClip].soundFile;
        //Debug.Log("time = " + _audio.time + ", length = " + currentClip.length);

        // While AR is active, check to see if the sound file is finished.
        if (_isPlaying && !_audio.isPlaying) {
            // If there are more to play, move to the next one.
            if (_currentClip < Clips.Length - 1)
            {
                Debug.Log("Playing next clip...");
                _currentClip++;
                LoadCurrentClip();
                PlayCurrentClip();
            }
            // If we're at the last one, reset to the beginning,
            // but wait until the next AR activation to replay.
            else
            {
                Debug.Log("Reset to beginning!");
                _currentClip = 0;
                LoadCurrentClip();
                _isPlaying = false;
            }
        }
    }
    
    public void Play()
    {
        // AR is active, but we might not actually want to play yet.
        _isARActive = true;
        
        // Do a quick check to see if the Poly is done downloading.
        if (_poly != null && (!_poly.DownloadStarted || _poly.ActiveDownloads > 0))
        {
            // If  we haven't started yet, start the Poly stuff now.
            if (!_poly.DownloadStarted)
            {
                InitPolyForThisTarget();
            }
            
            // Hide everything with a "Pause".
            PauseCurrentClip();
            
            // Display the "Status text"!
            if (_downloadingText != null) {
                _downloadingText.enabled = true;
                _downloadingText.text = _poly.statusText.text;
            }
            
            // Start a looping coroutine to poll if the download's done.
            StartCoroutine(CheckForDownloadComplete());
        }
        else
        {
            // Go for it!
            PlayCurrentClip();
            _isPlaying = true;
        }
    }
    private IEnumerator CheckForDownloadComplete()
    {
        // Wait a sec, then if we're still in AR mode, try again!
        yield return new WaitForSeconds(1.0f);
        if (_isARActive) {
            Play();
        }        
    }

    public void Pause()
    {
        PauseCurrentClip();
        _isARActive = false;
        _isPlaying = false;
    }

    private void LoadCurrentClip()
    {
        // Load in data files
        _audio.clip = Clips[_currentClip].SoundFile;
        _image.texture = Clips[_currentClip].ImageFile;
        if (_video != null) { _video.clip = Clips[_currentClip].VideoFile; }

        // Toggle image & video renderers
        _image.enabled = (_image.texture != null);
        if (_video != null) { _video.targetMaterialRenderer.enabled = (_video.clip != null); }
        
        // Whenever loading a clip, nuke the last poly object if you don't have one now.
        if (_poly != null) {
            string polyID = Clips[_currentClip].PolyID;
            if (string.IsNullOrEmpty(polyID)) {
                _poly.DeactivateLastPolyObject();
            }
        }
    }
    
    private void PlayCurrentClip()
    {
        Debug.Log("PLAYING CURRENT CLIP...");
        
        // Quick sanity check... if no audio, this is premature; wait a frame and try again.
        if (_audio == null) {
            Debug.Log("Caught sanity check, waiting a frame...");
            StartCoroutine(WaitAFrameThenPlayCurrentClip());
            return;
        }
        
        if (!_audio.isPlaying) {
            _audio.Play();
        }

        // Show the image and video clip...
        _image.enabled = (_image.texture != null);
        if (_video != null) {
            if (_video.clip != null) { _video.Play(); }
            _video.targetMaterialRenderer.enabled = (_video.clip != null);
        }
        
        // Hide the downloading text...
        if (_downloadingText != null) {
            _downloadingText.enabled = false;       
        }
        
        // Show the Poly object, if there is one
        if (_poly != null) {
            string polyID = Clips[_currentClip].PolyID;
            if (!string.IsNullOrEmpty(polyID)) {
                _poly.ActivatePolyObjectWithID(polyID);
                
                // Also reposition it based on the Clip's parameters!
                _poly.transform.localPosition = _polyStartPosition + Clips[_currentClip].PolyPositionAdjust;
                switch (Clips[_currentClip].PolyRotation)
                {
                    case PolyRotation.Default: _poly.transform.localRotation = Quaternion.identity; break;
                    case PolyRotation.Rotate90: _poly.transform.localRotation = Quaternion.Euler(0f, 90, 0f); break;
                    case PolyRotation.Rotate180: _poly.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); break;
                    case PolyRotation.Rotate270: _poly.transform.localRotation = Quaternion.Euler(0f, 270f, 0f); break;
                    case PolyRotation.AxisSwap: _poly.transform.localRotation = Quaternion.Euler(90f, 0f, 90f); break;
                    case PolyRotation.AxisSwapRotate90: _poly.transform.localRotation = Quaternion.Euler(90f, 90f, 90f); break;
                    case PolyRotation.AxisSwapRotate180: _poly.transform.localRotation = Quaternion.Euler(90f, 180f, 90f); break;
                    case PolyRotation.AxisSwapRotate270: _poly.transform.localRotation = Quaternion.Euler(90f, 270f, 90f); break;
                }
                _poly.transform.localScale = Vector3.one * Clips[_currentClip].PolyScale;
            }
        }

    }

    private IEnumerator WaitAFrameThenPlayCurrentClip() {
        yield return 0;
        PlayCurrentClip();
    }

    private void PauseCurrentClip()
    {
        Debug.Log("PAUSING CURRENT CLIP...");

        // Pause the audio!
        _audio.Pause();
        
        // Hide the image!
        _image.enabled = false;
                
        // Pause and hide the video!
        if (_video != null) {
            if (_video.clip != null) { _video.Pause(); }
            _video.targetMaterialRenderer.enabled = false;
        }
        
        // Hide the downloading text...
        if (_downloadingText != null) {
            _downloadingText.enabled = false;       
        }

        // Hide the latest poly object.
        if (_poly != null) {
            _poly.DeactivateLastPolyObject();
        }
    }
}