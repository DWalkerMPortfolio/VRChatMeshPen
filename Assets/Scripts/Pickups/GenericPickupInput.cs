
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GenericPickupInput : UdonSharpBehaviour
{
    #region Variables
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
    #endregion

    #region VRChat Methods
    /// <summary>
    /// Called when the pickup is picked up
    /// </summary>
    public override void OnPickup()
    {
        base.OnPickup();

        target.SendCustomEvent(pickUpFunction);
    }

    /// <summary>
    /// Called when the pickup is dropped
    /// </summary>
    public override void OnDrop() 
    { 
        base.OnDrop();
    
        target.SendCustomEvent(droppedFunction);
    }

    /// <summary>
    /// Called when the interact button is pressed while the pickup is held
    /// </summary>
    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();

        target.SendCustomEvent(useFunction);
    }

    /// <summary>
    /// Called when the interact button is released while the pickup is held
    /// </summary>
    public override void OnPickupUseUp()
    {
        base.OnPickupUseUp();

        target.SendCustomEvent(stopUsingFunction);
    }

    /// <summary>
    /// Called when ownership of the pickup is transfered to another player
    /// </summary>
    /// <param name="player">The player ownership was transfered to</param>
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        target.SendCustomEvent(ownershipTransferedFunction);
    }
    #endregion
}
