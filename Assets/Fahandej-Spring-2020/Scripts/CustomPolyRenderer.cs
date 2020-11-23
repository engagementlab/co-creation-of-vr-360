using System.Collections.Generic;
using UnityEngine;
using PolyToolkit;
using UnityEngine.UI;

public class CustomPolyRenderer : MonoBehaviour
{
    public Text statusText;
    protected Dictionary<string,GameObject> importedObjectsDictionary;
    protected GameObject lastActivatedObject;

    public bool DownloadStarted = false;
    public int ActiveDownloads;
    
    // Called by the ARSequence to download all the things.
    public virtual void LoadPolyAssetsFromStringArray(string[] polyIDs)
    {
        // Instantiate a data structure to hold your objects.
        importedObjectsDictionary = new Dictionary<string, GameObject>();
        
        // Just do all the import calls, all at once.
        //PolyApi.GetAsset("assets/" + "dJ7nZJQU9Dn", GetAssetCallback);
        foreach (string s in polyIDs)
        {
            // Keep track of how many are waiting to finish.
            ActiveDownloads++;
            
            // The very first time this is called, flip the DownloadStarted bool.
            DownloadStarted = true;

            // Start downloading...
            PolyApi.GetAsset("assets/" + s, GetAssetCallback);
            statusText.text = "Downloading...";
        }
        
        // TODO - Consider switching to actually downloading these onto the device to avoid janky load time.
    }
        
    // These two just turn the objects on and off.
    // We probably could have just used Transform.find, but I think it doesn't work on inactive objects.
    public void ActivatePolyObjectWithID(string polyID)
    {
        // If there was already one active, deactivate it.
        if (lastActivatedObject != null) {
            lastActivatedObject.SetActive(false);
        }
        
        // Grab the new one.
        GameObject go = importedObjectsDictionary["assets/" + polyID];
        
        // Activate it, and save it!
        go.SetActive(true);
        lastActivatedObject = go;
    }

    public void DeactivateLastPolyObject()
    {
        if (lastActivatedObject != null) {
            lastActivatedObject.SetActive(false);
        }
    }
    
    // Callback invoked when the featured assets results are returned.
    private void GetAssetCallback(PolyStatusOr<PolyAsset> result) {
        if (!result.Ok) {
            string errorString = "Failed to download poly model. Reason: " + result.Status;
            Debug.LogError(errorString);
            statusText.text = errorString;
            return;
        }
        Debug.Log("Successfully downloaded asset, name = " + result.Value.name);

        // Set the import options.
        PolyImportOptions options = PolyImportOptions.Default();
        
        // We want to rescale the imported mesh to a specific size.
        options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
        // The specific size we want assets rescaled to.
        options.desiredSize = 0.20f;
        // We want the imported assets to be recentered such that their centroid coincides with the origin:
        options.recenter = true;

        // Immediately move to import.
        PolyApi.Import(result.Value, options, ImportAssetCallback);
    }

    // Callback invoked when an asset has just been imported.
    private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result) {
        if (!result.Ok) {
            string errorString = "Failed to import poly model. Reason: " + result.Status;
            Debug.LogError(errorString);
            statusText.text = errorString;
            return;
        }
        Debug.Log("Successfully imported asset, name = " + asset.name + ", ActiveDownloads = " + ActiveDownloads);
        
        // Mark this one as completed!
        ActiveDownloads--;

        // If we're at zero, should be safe to clear the text.
        if (ActiveDownloads == 0) {
            statusText.text = "";
        }

        // Keep a reference to this new gameobject!
        importedObjectsDictionary.Add(asset.name, result.Value.gameObject);
        
        // Rename the object so we can find it later!
        result.Value.gameObject.name = asset.name;
        
        // Move it here, into the hierarchy!
        result.Value.gameObject.transform.SetParent(transform);
        result.Value.gameObject.transform.localPosition = Vector3.zero;
        result.Value.gameObject.transform.localRotation = Quaternion.identity;
        
        // Immediately set it inactive.
        result.Value.gameObject.SetActive(false);
    }
}