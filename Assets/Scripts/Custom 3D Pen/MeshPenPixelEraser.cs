
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MeshPenPixelEraser : UdonSharpBehaviour
{
    [Tooltip("The mesh 3D pen line holder in the scene")]
    [SerializeField] Mesh3DPenLineHolder lineHolder;
    [Tooltip("The radius around this eraser to erase")]
    [SerializeField] float eraseRadius;

    [UdonSynced] bool erasing = false; //Whether the pixel eraser is currently erasing

    bool held; //Whether the pixel eraser is currently held by the local player

    private void Update()
    {
        if (erasing)
        {
            foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
            {
                line.CheckPixelEraseLine(transform.position, eraseRadius); //Owner of line actually updates line data, other clients just update shader parameters
            }
        }
        
        if (held)
        {
            foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
            {
                line.MarkPixelEraseLine(transform.position, eraseRadius);
            }
        }
    }

    //Called on all clients when starting erasing
    public void StartErasing()
    {
        if (erasing)
            return;

        erasing = true;

        foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
        {
            line.StartPixelErase();
        }
    }

    //Called on all clients when stopping erasing
    public void StopErasing()
    {
        //Skip if erasing is already false
        if (!erasing)
            return;

        erasing = false;

        foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
        {
            if (Networking.IsOwner(line.gameObject))
                line.FinishPixelErase(transform.position, eraseRadius); //Owner of line finishes pixel erase, then clears shader parameters on other instances
        }
    }

    public void Used()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(StartErasing));
        StartErasing();
    }
    
    public void StoppedUsing()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(StopErasing));
        StopErasing();
    }

    public void PickedUp()
    {
        held = true;
    }

    public void Dropped()
    {
        held = false;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(StopErasing));
        StopErasing();
    }
}
