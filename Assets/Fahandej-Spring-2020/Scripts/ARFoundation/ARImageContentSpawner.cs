// Sam was here - suppressing "never assigned" warnings in private struct
#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageContentSpawner : MonoBehaviour {

    private ARTrackedImageManager imageTargetManager;

    [SerializeField]
    private ImageTargetContent[] targetsContent = null;

    private readonly Dictionary<string, (GameObject prefab, bool maintainAspect)> contentLookup = new Dictionary<string, (GameObject, bool)>();
    private readonly Dictionary<string, GameObject> contentReferences = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, UnityEngine.XR.ARSubsystems.TrackingState> previousStates = new Dictionary<string, UnityEngine.XR.ARSubsystems.TrackingState>();
    
    private void Awake()
    {
        imageTargetManager = GetComponent<ARTrackedImageManager>();

        foreach(var targetContent in targetsContent)
        {
            if (!string.IsNullOrEmpty(targetContent.name) && targetContent.content != null)
            {
                if (contentLookup.ContainsKey(targetContent.name))
                {
                    Debug.LogError("[AR Image Content Spawner] Duplicate keys in array!");
                }
                else
                {
                    contentLookup.Add(targetContent.name, (targetContent.content, targetContent.maintainAspect));
                }
            }
        }

        Debug.Log("ARImageContentSpawner Awake...!");
    }

    private void OnEnable()
    {
        imageTargetManager.trackedImagesChanged += ImageTargetManager_trackedImagesChanged;
    }

    private void OnDisable()
    {
        imageTargetManager.trackedImagesChanged -= ImageTargetManager_trackedImagesChanged;
    }

    private void ImageTargetManager_trackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs) {
        //Debug.Log("ImageTargetManager_trackedImagesChanged!");

        // "ADDED"
        (GameObject prefab, bool maintainAspect) spawnContent;
        foreach (var trackedImage in eventArgs.added)
        {
            /*
            Renderer imagePreview = trackedImage.GetComponentInChildren<Renderer>();
            if (imagePreview != null)
            {
                imagePreview.material.SetTexture("_MainTex", trackedImage.referenceImage.texture);
            }
            */

            // Hide literally every other piece of content!
            foreach (GameObject otherContent in contentReferences.Values) {
                Debug.Log("IN ADDED, HIDING: " + otherContent.name);
                ARTriggers triggers = otherContent.GetComponent<ARTriggers>();
                triggers.Deactivate();                
                //otherContent.SetActive(false);
            }
            
            // Spawn this content!
            if (contentLookup.TryGetValue(trackedImage.referenceImage.name, out spawnContent))
            {
                GameObject content = Instantiate(spawnContent.prefab, trackedImage.transform);
                ARTriggers triggers = content.GetComponent<ARTriggers>();
                triggers.Activate();
                
                // Keep track of it for later!
                contentReferences.Add(trackedImage.referenceImage.name, content);
            }
            
            // Resize the content!
            trackedImage.transform.localScale = spawnContent.maintainAspect ?
                (new Vector3(trackedImage.size.x, 1f, trackedImage.size.y)) :
                Vector3.one;
            
            // Save the tracking state!
            previousStates[trackedImage.referenceImage.name] =
                UnityEngine.XR.ARSubsystems.TrackingState.Tracking;

            Debug.Log("ADDED: " + trackedImage.name);
        }
        
        // "UPDATED"
        foreach (var trackedImage in eventArgs.updated)
        {
            /*
            bool wasTracking = (
                (previousStates[trackedImage.referenceImage.name] == UnityEngine.XR.ARSubsystems.TrackingState.Limited) ||
                (previousStates[trackedImage.referenceImage.name] == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            );
            bool isTrackingNow = (
                (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Limited) ||
                (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            );
            */

            bool wasTracking = (previousStates[trackedImage.referenceImage.name] == UnityEngine.XR.ARSubsystems.TrackingState.Tracking);
            bool isTrackingNow = (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking);
            
            // If we were tracking, but aren't now... deactivate!
            if (wasTracking && !isTrackingNow) {
                Debug.Log("IN UPDATED/NO-TRACKING, HIDING: " + trackedImage.referenceImage.name);
                GameObject content = contentReferences[trackedImage.referenceImage.name];
                ARTriggers triggers = content.GetComponent<ARTriggers>();
                triggers.Deactivate();
                //content.SetActive(false);

                Debug.Log("UPDATED & DEACTIVATING: " + trackedImage.name);
            }
            
            // If we're just starting tracking... activate!
            else if (!wasTracking && isTrackingNow) {

                // Hide literally every other piece of content!
                foreach (GameObject otherContent in contentReferences.Values) {
                    Debug.Log("IN UPDATED/TRACKING, HIDING: " + otherContent.name);
                    ARTriggers otherTriggers = otherContent.GetComponent<ARTriggers>();
                    otherTriggers.Deactivate();
                    //otherContent.SetActive(false);
                }
 
                // Turn this one on!
                GameObject content = contentReferences[trackedImage.referenceImage.name];
                //content.SetActive(true);
                ARTriggers triggers = content.GetComponent<ARTriggers>();
                triggers.Activate();
                Debug.Log("UPDATED & ACTIVATING: " + trackedImage.name);
            }
            
            // Save the new state!
            previousStates[trackedImage.referenceImage.name] = trackedImage.trackingState;
        }
                
        // "REMOVED"
        // Not sure this ever happens anyway!!
        foreach (var trackedImage in eventArgs.removed)
        {
            /*
            GameObject content = contentReferences[trackedImage.referenceImage.name];
            content.GetComponent<ARTriggers>().Deactivate();
            content.SetActive(false);
             */

            Debug.Log("REMOVED: " + trackedImage.name);
        }
    }

    [System.Serializable]
    private struct ImageTargetContent
    {
        public string name;
        public GameObject content;
        public bool maintainAspect;
    }
}