
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Mesh3DPenPaletteColor : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("The color index this pen palette color applies")]
    public int colorIndex;
    #endregion

    #region Unity Methods
    private void OnTriggerEnter(Collider other)
    {
        Mesh3DPen pen = other.GetComponentInParent<Mesh3DPen>();
        if (pen != null)
        {
            pen.SetColor(colorIndex);
        }
    }
    #endregion
}
