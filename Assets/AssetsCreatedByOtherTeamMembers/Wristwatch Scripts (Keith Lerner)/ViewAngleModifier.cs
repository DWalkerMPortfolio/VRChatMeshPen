using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

public class ViewAngleModifier : UdonSharpBehaviour
{
    public GameObject[] ModifiedGameObjects;
    //public float MaxAngle = 90;

    public float enableAngle = 60;
    public float disableAngle = 89;
    public float headDistance = 2;

    // Update is called once per frame
    // Since this is designed to be used for UI, Update is preferred over FixedUpdate
    private void Update()
    {
        if (!Networking.LocalPlayer.IsUserInVR()) return;
        
        // Get the observer's rotation
        Quaternion observerRot = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        Vector3 observerDist = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 handDist = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        
        // Loop through each modified gameObject
        foreach (GameObject go in ModifiedGameObjects)
        {
            /*
            // Determine if the object should be active or not based on the angle between
            // the observer's forward and go's backwards
            bool active = Vector3.Angle(observerRot * Vector3.forward, go.transform.forward) < MaxAngle;
            
            // Change go's active in hierarchy value 
            go.SetActive(active);
            */

            if (Vector3.Distance(observerDist, handDist) < headDistance)
            {
                if (go.activeInHierarchy) // For disabling
                {
                    // Determine if the object should be deactivated based on the angle between
                    // the observer's forward and go's backwards
                    if (Vector3.Angle(observerRot * Vector3.forward, go.transform.forward) > disableAngle)
                    {
                        go.SetActive(false);
                    }

                }
                else // For enabling
                {
                    // Determine if the object should be activated based on the angle between
                    // the observer's forward and go's backwards
                    if (Vector3.Angle(observerRot * Vector3.forward, go.transform.forward) < enableAngle)
                    {
                        go.SetActive(true);
                    }
                }
            }
            else
            {
                go.SetActive(false);
            }
        }
    }
}
