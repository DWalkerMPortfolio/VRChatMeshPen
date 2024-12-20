
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using TMPro;

public class AdminOnlyPickup : UdonSharpBehaviour
{

    [Tooltip("The VRCPickup component to restrict to the host")]
    [SerializeField] VRCPickup pickup;

    private void Update()
    {
        pickup.pickupable = Networking.LocalPlayer.GetPlayerTag("role") != "user";
    }
}
