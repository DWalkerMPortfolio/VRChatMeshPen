
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class FollowWrist : UdonSharpBehaviour
{
    [Tooltip("Whether to use the right hand instead of the left")]
    public bool rightHand;
    
    [Tooltip("The offset from the player's wrist position at which this object should be placed")]
    [SerializeField] Vector3 positionOffset;

    private void Update()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        //if (!Networking.LocalPlayer.IsUserInVR()) return;
        var tData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);

        //Get player wrist tracking data
        if (rightHand) tData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

        var headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

        if (tData.position == null || tData.rotation == null) return;

        transform.position = tData.position + positionOffset;

        Vector3 headPos = headData.position;

        transform.LookAt(headPos);

        //transform.rotation = tData.rotation;
    }
}
