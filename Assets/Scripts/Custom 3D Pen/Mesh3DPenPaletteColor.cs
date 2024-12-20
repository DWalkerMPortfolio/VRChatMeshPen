
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPenPaletteColor : UdonSharpBehaviour
{
    [Tooltip("The color index this pen palette color applies")]
    public int colorIndex;

    private void OnTriggerEnter(Collider other)
    {
        Mesh3DPen pen = other.GetComponentInParent<Mesh3DPen>();
        if (pen != null)
        {
            pen.SetColor(colorIndex);
        }
    }
}
