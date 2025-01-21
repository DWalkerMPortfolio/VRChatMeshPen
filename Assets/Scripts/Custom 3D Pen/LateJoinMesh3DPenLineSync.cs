
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LateJoinMesh3DPenLineSync : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("The Mesh3DPenLine to sync")]
    [SerializeField] Mesh3DPenLine line;
    [Tooltip("Whether the owner of this object should request syncs too")]
    [SerializeField] bool ownerSyncs = false;

    [UdonSynced] string positionsJson;
    [UdonSynced] string colorIndicesJson;

    bool requestedSync = false; //Whether this client requested for the line to be synced
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (ownerSyncs || !Networking.IsOwner(gameObject))
        {
            //We are a late joiner, request for this line to be synced
            requestedSync = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SyncLine));
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Called by late joiners on the owner to request that the line be synced
    /// </summary>
    public void SyncLine()
    {
        RequestSerialization();
    }
    #endregion

    #region VRChat Methods
    /// <summary>
    /// Called right before network data is sent
    /// </summary>
    public override void OnPreSerialization()
    {
        base.OnPreSerialization();

        //Serialize line positions
        if (VRCJson.TrySerializeToJson(line.GetLinePositions(), JsonExportType.Minify, out DataToken positionsResult))
            positionsJson = positionsResult.String;
        else
        {
            Debug.LogError("Failed to serialize late join line positions list");
            positionsJson = "";
        }

        //Serialize line color indices
        if (VRCJson.TrySerializeToJson(line.GetLineColorInidices(), JsonExportType.Minify, out DataToken colorIndicesResult))
            colorIndicesJson = colorIndicesResult.String;
        else
        {
            Debug.LogError("Failed to serialize late join line positions list");
            colorIndicesJson = "";
        }
    }

    /// <summary>
    /// Called when new network data is received
    /// </summary>
    public override void OnDeserialization()
    {
        base.OnDeserialization();

        //Skip if this is the owner
        if (Networking.IsOwner(gameObject))
            return;

        if (requestedSync)
        {
            //Deserialize line positions
            if (positionsJson != "" && colorIndicesJson != "")
            {
                if (VRCJson.TryDeserializeFromJson(positionsJson, out DataToken positionsResult))
                {
                    if (VRCJson.TryDeserializeFromJson(colorIndicesJson, out DataToken colorIndicesResult))
                    {
                        Debug.Log("Late join line syncing. There are " + positionsResult.DataList.Count + " lines in the scene with " + colorIndicesResult.DataList.Count + " color indices saved");
                        line.SetLineData(positionsResult.DataList, colorIndicesResult.DataList);
                        line.ClearCurrentLine();
                    }
                    else
                        Debug.LogError("Failed to deserialize late join line color indices list");
                }
                else
                    Debug.LogError("Failed to deserialize late join line positions list");
            }

            requestedSync = false;
        }
    }
    #endregion
}
