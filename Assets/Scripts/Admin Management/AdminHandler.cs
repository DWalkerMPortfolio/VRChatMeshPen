
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

public class AdminHandler : UdonSharpBehaviour
{
    DataList adminlist = new DataList(); //Contains all players currently set as admins
    [UdonSynced] string json; //The string used to sync the admins list

    private void Start()
    {
        Networking.LocalPlayer.SetPlayerTag("role", "user");
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        UpdateFallbackAdmins();
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        UpdateFallbackAdmins();
    }

    void UpdateFallbackAdmins()
    {
        //Current instance master is treated as an admin
        if (Networking.LocalPlayer.isMaster && Networking.LocalPlayer.GetPlayerTag("role") != "admin")
            Networking.LocalPlayer.SetPlayerTag("role", "owner");
        else if (Networking.LocalPlayer.GetPlayerTag("role") != "admin" && !Networking.LocalPlayer.isMaster)
            Networking.LocalPlayer.SetPlayerTag("role", "user");
    }

    public void AddAdmin(string name)
    {
        Debug.Log("Adding admin: " + name);
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        //Add a new admin to the list
        if (!adminlist.Contains(name))
            adminlist.Add(name);

        //Check whether the local player is now an admin (failsafe for 1-player instance (no network data is serialized))
        if (name == Networking.LocalPlayer.displayName)
            Networking.LocalPlayer.SetPlayerTag("role", "admin");

        RequestSerialization();
    }

    public void RemoveAdmin(string name)
    {
        Debug.Log("Removing admin: " + name);
        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        //Remove an admin from the list
        adminlist.Remove(name);

        //Check whether the local player is no longer an admin (failsafe for 1-player instance (no network data is serialized))
        if (name == Networking.LocalPlayer.displayName)
        {
            if (Networking.LocalPlayer.isMaster)
                Networking.LocalPlayer.SetPlayerTag("role", "owner");
            else
                Networking.LocalPlayer.SetPlayerTag("role", "user");
        }

        RequestSerialization();
    }

    public bool IsAdmin(string name)
    {
        return adminlist.Contains(name);
    }

    //Called when network data is being sent
    public override void OnPreSerialization()
    {
        //Serialize playerTimes for network syncing
        if (VRCJson.TrySerializeToJson(adminlist, JsonExportType.Minify, out DataToken result))
            json = result.String;
        else
            Debug.LogError("Failed to serialize admin list");
    }

    //Called when new network data is received
    public override void OnDeserialization()
    {
        //Deserialize playerTimes after network syncing
        if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            adminlist = result.DataList;
        else
            Debug.LogError("Failed to deserialize admin list");

        //Check whether the local player is admin
        if (adminlist.Contains(Networking.LocalPlayer.displayName))
            Networking.LocalPlayer.SetPlayerTag("role", "admin");
        else
        {
            if (Networking.LocalPlayer.isMaster)
                Networking.LocalPlayer.SetPlayerTag("role", "owner");
            else
                Networking.LocalPlayer.SetPlayerTag("role", "user");
        }
    }

}