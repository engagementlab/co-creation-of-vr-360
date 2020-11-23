// Sam was here - suppressing "never assigned" warnings in Unity Events
#pragma warning disable 0649

using UnityEngine;

public class ARTriggers : MonoBehaviour
{
    [SerializeField]
    UnityEngine.Events.UnityEvent OnActivated;

    [SerializeField]
    UnityEngine.Events.UnityEvent OnDeactivated;

    [SerializeField]
    bool activateOnEnable = true;

    [SerializeField]
    bool deactivateOnDisable = true;

    bool activated = false;

    private void OnEnable()
    {
        if (activateOnEnable)
        {
            Activate();
        }
    }

    private void OnDisable()
    {
        if (deactivateOnDisable)
        {
            Deactivate();
        }
    }

    public void Activate()
    {
        //if (!activated)
        {
            Debug.Log("ACTIVATING ON TRIGGER: " + name);
            OnActivated.Invoke();
            activated = false;
        }
        
    }

    public void Deactivate()
    {
        //if (activated)
        {
            Debug.Log("DEACTIVATING ON TRIGGER: " + name);
            OnDeactivated.Invoke();
            activated = false;
        }
    }
}
