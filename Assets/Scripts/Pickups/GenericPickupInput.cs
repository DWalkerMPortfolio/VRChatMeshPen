
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GenericPickupInput : UdonSharpBehaviour
{

    [Tooltip("The target to call functions on")]
    [SerializeField] UdonSharpBehaviour target;
    [Tooltip("The function to call when picked up")]
    [SerializeField] string pickUpFunction;
    [Tooltip("The function to call when dropped")]
    [SerializeField] string droppedFunction;
    [Tooltip("The function to call when the use button is pressed")]
    [SerializeField] string useFunction;
    [Tooltip("The function to call when the use button is released")]
    [SerializeField] string stopUsingFunction;
    [Tooltip("The function to call when ownership is transfered")]
    [SerializeField] string ownershipTransferedFunction;

    public override void OnPickup()
    {
        base.OnPickup();

        target.SendCustomEvent(pickUpFunction);
    }

    public override void OnDrop() 
    { 
        base.OnDrop();
    
        target.SendCustomEvent(droppedFunction);
    }

    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();

        target.SendCustomEvent(useFunction);
    }

    public override void OnPickupUseUp()
    {
        base.OnPickupUseUp();

        target.SendCustomEvent(stopUsingFunction);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        target.SendCustomEvent(ownershipTransferedFunction);
    }
}
