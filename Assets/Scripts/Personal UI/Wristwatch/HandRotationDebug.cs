
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;
public class HandRotationDebug : UdonSharpBehaviour
{
    //Color to indicate if the rotations are within a specified range
    public Color activeColor;
    public Color inactiveColor;

    [SerializeField]
    LineRenderer leftHandLine;
    [SerializeField]
    LineRenderer rightHandLine;

    //For the debug visualization text
    [SerializeField] TextMeshProUGUI rightHandTmp;
    [SerializeField] TextMeshProUGUI leftHandTmp;

    //Specified Range of rotations (how one would have to rotate thier hand to activate wristwatch)
    [SerializeField] float minPitchRotation;
    [SerializeField] float maxPitchRotation;

    [SerializeField] float minYawRotation;
    [SerializeField] float maxYawRotation;

    [SerializeField] float minRollRotation;
    [SerializeField] float maxRollRotation;

    [SerializeField] LineRenderer xAxisLine;
    [SerializeField] LineRenderer yAxisLine;
    [SerializeField] LineRenderer zAxisLine;
    [SerializeField] LineRenderer xRotationLine;
    [SerializeField] LineRenderer yRotationLine;
    [SerializeField] LineRenderer zRotationLine;
    void Update()
    {
        //This is the tracking data from the hands
        var headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        var leftData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        var rightData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

        Quaternion leftRotation = leftData.rotation;
        Quaternion rightRotation = rightData.rotation;
        Quaternion headRotation = Networking.LocalPlayer.GetRotation();

        //null check (note: GetBoneRotation will return Quaternion identity of nothing is found)
        if (leftData.position == null || rightData.position == null) return;

        float xrot = 0;
        float yrot = 0;
        float zrot = 0;

        //Display the rounded values

        //Axis Position
        xAxisLine.SetPosition(0, rightData.position);
        yAxisLine.SetPosition(0, rightData.position);
        zAxisLine.SetPosition(0, rightData.position);

        xAxisLine.SetPosition(1, rightData.position + headRotation * Vector3.right);
        yAxisLine.SetPosition(1, rightData.position + headRotation * Vector3.up);
        zAxisLine.SetPosition(1, rightData.position + headRotation * Vector3.forward);
        
        //Axis Rotation
        xRotationLine.SetPosition(0, rightData.position);
        yRotationLine.SetPosition(0, rightData.position);
        zRotationLine.SetPosition(0, rightData.position);

        Vector3 xRotationEnd = rightData.position + rightRotation * Vector3.right;
        Vector3 yRotationsEnd = rightData.position + rightRotation * Vector3.up;
        Vector3 zRotationsEnd = rightData.position + rightRotation * Vector3.forward;

        xRotationLine.SetPosition(1, xRotationEnd);
        yRotationLine.SetPosition(1, yRotationsEnd);
        zRotationLine.SetPosition(1, zRotationsEnd);

        Vector3 rightHandRight = rightData.position - xRotationEnd;
        Vector3 rightHandUp = rightData.position - yRotationsEnd;
        Vector3 rightHandForward = rightData.position - zRotationsEnd;

        xrot = Vector3.Angle(rightHandRight, Vector3.up); ;
        yrot = Vector3.Angle(rightHandUp, Vector3.up);
        zrot = Vector3.Angle(rightHandForward, Vector3.forward);

        rightHandTmp.text = $"Pitch (X): {xrot}\nYaw (Y): {yrot}\nRoll (Z) : {zrot}";

        //Tracking Data Hands
        xrot = leftData.rotation.eulerAngles.x;
        yrot = leftData.rotation.eulerAngles.y;
        zrot = leftData.rotation.eulerAngles.z;

        leftHandTmp.text = $"Pitch (X): {xrot}\nYaw (Y): {yrot}\nRoll (Z) : {zrot}";
        
    }
}
