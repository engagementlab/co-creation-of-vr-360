using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARContentSpawner : MonoBehaviour
{

    private ARTrackedImageManager trackedImageManager;

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += TrackedImageManager_trackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= TrackedImageManager_trackedImagesChanged;
    }

    private void TrackedImageManager_trackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);

            UpdateInfo(trackedImage);
        }
        throw new System.NotImplementedException();
    }

    private void UpdateInfo(ARTrackedImage trackedImage)
    {
        GameObject contentParent = trackedImage.transform.GetChild(0).gameObject;
        GameObject imageView = contentParent.transform.GetChild(0).gameObject;

        if (trackedImage.trackingState != UnityEngine.XR.ARSubsystems.TrackingState.None)
        {
            contentParent.SetActive(true);

            // Tracked image size is only valid once image is tracked
            trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

            // Set image texture
            if (trackedImage.referenceImage.texture != null)
            {
                Material material = imageView.GetComponentInChildren<Renderer>().material;
                material.mainTexture = trackedImage.referenceImage.texture;
            }
            

        }
    }
}
