
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AdminOnlyObject : UdonSharpBehaviour
{
    [Tooltip("The GameObject to set active or not")]
    [SerializeField] public GameObject adminObject;

    private void Update()
    {
        bool localPlayerIsAdmin = Networking.LocalPlayer.GetPlayerTag("role") != "user";

        if (adminObject != null)
        {
            if (adminObject.activeSelf != localPlayerIsAdmin)
                adminObject.SetActive(localPlayerIsAdmin);
        }
        else
        {
            if (gameObject.activeSelf != localPlayerIsAdmin)
                gameObject.SetActive(localPlayerIsAdmin);
        }
    }
}
