
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class HideInVR : UdonSharpBehaviour
{

    [Tooltip("Whether to hide in desktop mode instead")]
    [SerializeField] bool hideInDesktop;
    [Tooltip("The object to hide")]
    [SerializeField] GameObject objectToHide;

    void OnEnable()
    {
        if (hideInDesktop)
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                objectToHide.SetActive(true);
            }
            else
            {
                objectToHide.SetActive(false);
            }
        }
        else
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                objectToHide.SetActive(false);
            }
            else
            {
                objectToHide.SetActive(true);
            }
        }
    }
}
