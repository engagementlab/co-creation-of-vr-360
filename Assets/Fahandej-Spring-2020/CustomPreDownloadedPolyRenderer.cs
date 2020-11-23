// Sam was here - at the last minute, create a version of this script
// that doesn't actually pull things from the internet!
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomPreDownloadedPolyRenderer : CustomPolyRenderer
{
    
    // Added 9/28/2020 - Grab the status text via tag!
    void Start() {
        if (statusText == null) {
            GameObject statusTextGO = GameObject.FindWithTag("StatusText");
            if (statusTextGO == null) {
                Debug.LogWarning("Failed to find status text in CustomPreDownloadedPolyRenderer.cs!");
            } else {
                statusText = statusTextGO.GetComponent<Text>();
            }
        }
    }
    
    // Called by the ARSequence to download all the things.
    public override void LoadPolyAssetsFromStringArray(string[] polyIDs)
    {
        // Just flip this bool to avoid calling this multiple times.
        DownloadStarted = true;

        // Instantiate a data structure to hold your objects.
        importedObjectsDictionary = new Dictionary<string, GameObject>();
        
        // Just do all the import calls, all at once.
        //PolyApi.GetAsset("assets/" + "dJ7nZJQU9Dn", GetAssetCallback);
        foreach (string s in polyIDs)
        {
            // Do we find it...?
            bool connectedPoly = false;
            
            // Instead of using the poly API, FIND the existing transform underneath us.
            foreach (Transform t in transform) {
                if (t.name.IndexOf(s) > -1) {
                    // Name is found!
                    importedObjectsDictionary.Add("assets/" + s, t.gameObject);
                    Debug.Log("Connected Poly object " + s + "!");
                    connectedPoly = true;
                    break;
                }
            }
            if (!connectedPoly) {
                Debug.LogWarning("Failed to find Google Poly object named " + s + "!");
            }
        }        
    }
}