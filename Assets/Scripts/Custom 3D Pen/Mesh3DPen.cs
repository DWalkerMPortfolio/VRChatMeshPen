using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPen : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("This pen's line")]
    public Mesh3DPenLine line;

    [Tooltip("The pen palette in the scene")]
    [SerializeField] Mesh3DPenPalette palette;
    [Tooltip("The VRCPickup for this pen")]
    [SerializeField] VRCPickup pickup;
    [Tooltip("This pen's tip")]
    [SerializeField] Transform tip;
    [Tooltip("The MeshRenderer of this pen")]
    [SerializeField] MeshRenderer meshRenderer;
    [Tooltip("The color property on the pen's material")]
    [SerializeField] string colorProperty;
    #endregion

    #region Unity Methods
    private void Start()
    {
        UpdateLineOwnership();
    }

    private void Update()
    {
        if (pickup.IsHeld && pickup.currentPlayer == Networking.LocalPlayer)
        {
            //Change color
            if (Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("Changing color");
                line.IncrementColor();
            }
        }
    }
    #endregion

    #region VRChat Methods
    /// <summary>
    /// Called when the pen is picked up
    /// </summary>
    public override void OnPickup()
    {
        base.OnPickup();

        if (palette != null)
            palette.PenPickedUp(pickup.currentHand);
    }

    /// <summary>
    /// Called when the pen is dropped
    /// </summary>
    public override void OnDrop()
    {
        base.OnDrop();

        if (palette != null)
            palette.PenDropped();
    }

    /// <summary>
    /// Called when the interact button is pressed while the pen is held
    /// </summary>
    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();

        if (Networking.IsOwner(gameObject))
            line.StartDrawing();
    }

    /// <summary>
    /// Called when the interact button is released while the pen is held
    /// </summary>
    public override void OnPickupUseUp()
    {
        base.OnPickupUseUp();

        if (Networking.IsOwner(gameObject))
            line.StopDrawing();
    }

    /// <summary>
    /// Called when ownership of the pen is transfered to another player
    /// </summary>
    /// <param name="player">The player taking ownership</param>
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        UpdateLineOwnership();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the color the pen is currently set to draw
    /// </summary>
    /// <param name="value">The index of the color to set</param>
    public void SetColor(int value)
    {
        line.SetColor(value);
    }

    /// <summary>
    /// Returns the transform marking the tip of the pen
    /// </summary>
    /// <returns>The transform marking the tip of the pen</returns>
    public Transform GetTip()
    {
        return tip;
    }

    /// <summary>
    /// Sets the color of the paint on the tip of the pen's model
    /// </summary>
    /// <param name="color">The color to set</param>
    public void SetPenModelColor(Color color)
    {
        meshRenderer.material.SetColor(colorProperty, color);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Updates ownership of this pen's associated line to ensure both are always owned by the same player
    /// </summary>
    void UpdateLineOwnership()
    {
        // Make sure line has same owner as pen
        if (Networking.IsOwner(gameObject))
        {
            line.UpdateOwner(Networking.LocalPlayer);
        }
    }
    #endregion
}