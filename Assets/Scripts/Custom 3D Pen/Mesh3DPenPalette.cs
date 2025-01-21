
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPenPalette : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("The root gameObject of the palette")]
    [SerializeField] GameObject paletteRoot;
    [Tooltip("The FollowWrist component for the palette")]
    [SerializeField] FollowWrist followWrist;
    #endregion

    #region Public Methods
    public void PenPickedUp(VRC_Pickup.PickupHand hand)
    {
        if (Networking.LocalPlayer.IsUserInVR())
        {
            paletteRoot.SetActive(true);
            followWrist.rightHand = (hand == VRC_Pickup.PickupHand.Left);
            followWrist.UpdatePosition();
        }
    }

    public void PenDropped()
    {
        paletteRoot.SetActive(false);
    }
    #endregion
}
