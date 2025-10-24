using UnityEngine;

public class PortalInstances : MonoBehaviour , IInteractable
{
    public void OnInteract()
    {
        // Teleport the player to another instances of the MapScene

        Debug.Log("Interacted with portal");
        //InstanceManager.Instance.RequestInstanceChange();
    }
}
