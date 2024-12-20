using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPen : UdonSharpBehaviour
{
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

    public override void OnPickup()
    {
        base.OnPickup();

        if (palette != null)
            palette.PenPickedUp(pickup.currentHand);
    }

    public override void OnDrop()
    {
        base.OnDrop();

        if (palette != null)
            palette.PenDropped();
    }

    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();

        if (Networking.IsOwner(gameObject))
            line.StartDrawing();
    }

    public override void OnPickupUseUp()
    {
        base.OnPickupUseUp();

        if (Networking.IsOwner(gameObject))
            line.StopDrawing();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        UpdateLineOwnership();
    }

    public void SetColor(int value)
    {
        line.SetColor(value);
    }

    void UpdateLineOwnership()
    {
        // Make sure line has same owner as pen
        if (Networking.IsOwner(gameObject))
        {
            line.UpdateOwner(Networking.LocalPlayer);
        }
    }

    public Transform GetTip()
    {
        return tip;
    }

    public void SetPenModelColor(Color color)
    {
        meshRenderer.material.SetColor(colorProperty, color);
    }
}