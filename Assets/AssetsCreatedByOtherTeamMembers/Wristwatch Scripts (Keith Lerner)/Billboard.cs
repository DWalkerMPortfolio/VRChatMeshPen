using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Billboard : UdonSharpBehaviour
{   
    [Min(1)] public float turnSpeed = 32f;

    public bool horizontalOnly = false;

    private void Update()
    {
        Vector3 targetPos = Networking.LocalPlayer.GetTrackingData(
            VRCPlayerApi.TrackingDataType.Head).position;
        
        if (horizontalOnly) targetPos.y = transform.position.y;
        
        Quaternion rot = Quaternion.LookRotation(
            (targetPos - transform.position).normalized, 
            Vector3.up);
        
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
    }
}