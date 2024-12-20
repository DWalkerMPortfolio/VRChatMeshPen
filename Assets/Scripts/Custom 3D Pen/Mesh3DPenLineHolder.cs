
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPenLineHolder : UdonSharpBehaviour
{
    [Tooltip("All Mesh 3D Pen lines in the scene")]
    public Mesh3DPenLine[] mesh3DPenLines;

    //Called by the clear all lines button
    public void ClearAllLines()
    {
        foreach (Mesh3DPenLine line in mesh3DPenLines)
        {
            line.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Mesh3DPenLine.Clear));
        }
    }

    /// <summary>
    /// Gets the index of a given line. Called by the Mesh3DPenLine to help set its starting color index
    /// </summary>
    /// <param name="line">The line to find the index of</param>
    /// <returns>The index of the line, or -1 if it is not held by this holder</returns>
    public int GetLineIndex(Mesh3DPenLine line)
    {
        for (int i = 0; i < mesh3DPenLines.Length; i++)
        {
            if (mesh3DPenLines[i] == line)
                return i;
        }

        return -1;
    }
}
