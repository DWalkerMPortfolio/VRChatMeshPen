
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Mesh3DPenEraser : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("The VRCPickup for this eraser")]
    [SerializeField] VRCPickup pickup;
    [Tooltip("The Mesh 3D Pen Line Holder in the scene")]
    [SerializeField] Mesh3DPenLineHolder penLineHolder;
    [Tooltip("The radius around this GameObject to erase")]
    [SerializeField] float eraseRadius;
    [Tooltip("How frequently to check which line is being targeted for erase")]
    [SerializeField] float checkEraseFrequency = 0.5f;
    [Tooltip("Whether this eraser clears all lines created by the same pen as the selected line")]
    [SerializeField] bool clearAll;

    float lastCheckEraseTime = -Mathf.Infinity; //The last time this eraser checked which line to erase
    bool clearedMarks = false; //Whether this pen's marks have been cleared after it was dropped
    #endregion

    #region Unity Methods
    private void Update()
    {
        if (pickup.IsHeld && pickup.currentPlayer == Networking.LocalPlayer)
        {
            if (Time.time - lastCheckEraseTime > checkEraseFrequency)
            {
                Mark();
                lastCheckEraseTime = Time.time;
                clearedMarks = false;
            }
        }
        else if (!clearedMarks)
        {
            clearedMarks = true;
            ClearMarks();
        }
    }
    #endregion

    #region VRChat Methods
    /// <summary>
    /// Called when the interact button is pressed while the eraser is held
    /// </summary>
    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();

        Debug.Log("Sending erase event");

        //Send event to all instances of self so that client that owns the line can handle the erase
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Erase));
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Called on all instances of this eraser so each one can communicate directly with the owner of each line
    /// </summary>
    public void Erase()
    {
        Debug.Log("Received erase event");

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            if (!penLine.gameObject.activeSelf)
                continue;

            if (Networking.IsOwner(penLine.gameObject))
            {
                //Debug.Log("Own a line, checking intersection");
                if (clearAll)
                    penLine.CheckClearLines(transform.position, eraseRadius);
                else
                    penLine.CheckEraseLine(transform.position, eraseRadius);
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Mark the line that is currently targeted for erasal
    /// </summary>
    void Mark()
    {
        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            if (!penLine.gameObject.activeSelf)
                continue;

            penLine.CheckMarkLine(transform.position, eraseRadius);
        }
    }

    /// <summary>
    /// Clear all marks currently on lines
    /// </summary>
    void ClearMarks()
    {
        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            if (!penLine.gameObject.activeSelf)
                continue;

            penLine.ClearMark();
        }
    }
    #endregion
}
