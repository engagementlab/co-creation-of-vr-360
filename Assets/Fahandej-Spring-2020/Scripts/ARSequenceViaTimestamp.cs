using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

public class ARSequenceViaTimestamp : MonoBehaviour
{
    public ARClip[] Clips;
    public RawImage _imageObject1;
    public RawImage _imageObject2;
    public RawImage _imageObject3;
    public RawImage _imageObject4;
    public RawImage _imageObject5;
    public RawImage _imageObject6;
    public int startAtClip;
    
    private AudioSource _audio;
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
        public float startTime;
        public TimedImage Image1;
        public TimedImage Image2;
        public TimedImage Image3;
        public TimedImage Image4;
        public TimedImage Image5;
        public TimedImage Image6;
        public VideoClip VideoFile;
        public string PolyID;
        public float PolyScale;
        public Vector3 PolyPositionAdjust;
        public PolyRotation PolyRotation;
    }

    [System.Serializable]
    public struct TimedImage
    {
        public Texture2D imageFile;
        public float imageStartTime;
    }
    
    void Awake()
    {
        // Save some references.
        _audio = GetComponentInChildren<AudioSource>();
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
            // This means you're done!  Reset to the beginning,
            // but wait until the next AR activation to replay.
            Debug.Log("Reset to beginning!");
            _currentClip = 0;
            LoadCurrentClip();
            _isPlaying = false;
        }
        else
        {
            // Get the current audio clip timestamp, and compare it to the timestamp value for the next clip.
            float currentAudioTime = _audio.time;
                
            // If you're NOT on the last clip, check to see if you can switch clips.
            if (_currentClip < Clips.Length - 1) {                
                float nextClipTime = Clips[_currentClip + 1].startTime;

                if (currentAudioTime > nextClipTime) {
                    Debug.Log("Playing next clip...");
                    _currentClip++;
                    LoadCurrentClip();
                    PlayCurrentClip();
                }
            }

            // Either way - every frame, check to see if we're enabling or disabling based on the timestamps.
            if (_imageObject1.texture != null && Clips[_currentClip].Image1.imageStartTime < currentAudioTime) { _imageObject1.enabled = true; }
            if (_imageObject2.texture != null && Clips[_currentClip].Image2.imageStartTime < currentAudioTime) { _imageObject2.enabled = true; }
            if (_imageObject3.texture != null && Clips[_currentClip].Image3.imageStartTime < currentAudioTime) { _imageObject3.enabled = true; }
            if (_imageObject4.texture != null && Clips[_currentClip].Image4.imageStartTime < currentAudioTime) { _imageObject4.enabled = true; }
            if (_imageObject5.texture != null && Clips[_currentClip].Image5.imageStartTime < currentAudioTime) { _imageObject5.enabled = true; }
            if (_imageObject6.texture != null && Clips[_currentClip].Image6.imageStartTime < currentAudioTime) { _imageObject6.enabled = true; }
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
        _imageObject1.texture = Clips[_currentClip].Image1.imageFile;
        _imageObject2.texture = Clips[_currentClip].Image2.imageFile;
        _imageObject3.texture = Clips[_currentClip].Image3.imageFile;
        _imageObject4.texture = Clips[_currentClip].Image4.imageFile;
        _imageObject5.texture = Clips[_currentClip].Image5.imageFile;
        _imageObject6.texture = Clips[_currentClip].Image6.imageFile;
        if (_video != null) { _video.clip = Clips[_currentClip].VideoFile; }

        // Toggle image & video renderers
        // Images always load disabled now!
        _imageObject1.enabled = false;
        _imageObject2.enabled = false;
        _imageObject3.enabled = false;
        _imageObject4.enabled = false;
        _imageObject5.enabled = false;
        _imageObject6.enabled = false;
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
        // Quick sanity check... if no audio, this is premature; wait a frame and try again.
        if (_audio == null) {
            Debug.Log("Caught sanity check, waiting a frame...");
            StartCoroutine(WaitAFrameThenPlayCurrentClip());
            return;
        }

        if (!_audio.isPlaying){
            _audio.Play();
            // Fix for "startAtClip"
            if (startAtClip > 0)
            {
                float newStartTime = Clips[_currentClip].startTime;
                _audio.time = newStartTime;
            }
        }
        
        // Show the video clip...
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
        // Pause the audio!
        _audio.Pause();
        
        // Hide the images!
        _imageObject1.enabled = false;
        _imageObject2.enabled = false;
        _imageObject3.enabled = false;
        _imageObject4.enabled = false;
        _imageObject5.enabled = false;
        _imageObject6.enabled = false;

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