using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ArcLayoutGroup : UdonSharpBehaviour
{
    [SerializeField] [UnityEngine.Min(0.001f)] private float radius = 1;
    [SerializeField] [UnityEngine.Min(0)]private float arcLength = 1;
    
    private void Update()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == transform) continue;
            Transform trans = transforms[i];

            // trans.hideFlags = HideFlags.NotEditable;
            
            trans.localPosition =
                PointOnArc(radius, arcLength, (float)i / transforms.Length);
        }
    }

    public static Vector2 PointOnArc(float radius, float arcLength, float rawInput)
    {
        rawInput = Mathf.Clamp01(rawInput);
        
        float arcDegs = (arcLength / radius) * (180 / Mathf.PI);
        float arcHalfDegs = arcDegs / 2;
        float degsAtRawInput = rawInput * arcDegs - arcHalfDegs;
        float arcRads = Mathf.Deg2Rad * (degsAtRawInput + 90);

        float x = radius * Mathf.Cos(arcRads);
        float y = radius * Mathf.Sin(arcRads);

        return new Vector2(x, y);
    }

    // private void OnDisable()
    // {
    //     Transform[] transforms = transform.GetComponentsInChildren<Transform>();
    //     for (int i = 0; i < transforms.Length; i++)
    //     {
    //         if (transforms[i] == transform) continue;
    //
    //         transforms[i].hideFlags = HideFlags.None;
    //     }
    // }
    //  
    // #if UNITY_EDITOR
    // private void OnDrawGizmos()
    // {
    //     if (Application.isPlaying) return;
    //     
    //     Update();
    // }
    // #endif
}
