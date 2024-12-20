using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Components;

public class GenerateLinesForMesh3DPens : MonoBehaviour
{
    [Tooltip("The pool that holds the pens")]
    [SerializeField] VRCObjectPool penPool;
    [Tooltip("The Mesh 3D Pen Line Holder to hold the lines")]
    [SerializeField] Mesh3DPenLineHolder lineHolder;
    [Tooltip("The Mesh 3D Pen Line prefab")]
    [SerializeField] GameObject linePrefab;

    [ContextMenu("Generate")]
    void Generate()
    {
        //Clear previous lines
        foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
            DestroyImmediate(line.gameObject);

        //Initialize pen holder array
        lineHolder.mesh3DPenLines = new Mesh3DPenLine[penPool.Pool.Length];

        //Generate new ones
        for (int i=0; i<penPool.Pool.Length; i++)
        {
            Mesh3DPen pen = penPool.Pool[i].GetComponentInChildren<Mesh3DPen>();
            GameObject lineGO = Instantiate(linePrefab, lineHolder.transform);
            lineGO.name = linePrefab.name + " (" + i + ")";
            Mesh3DPenLine line = lineGO.GetComponentInChildren<Mesh3DPenLine>();
            pen.line = line;
            line.pen = pen;
            line.lineHolder = lineHolder;
            lineHolder.mesh3DPenLines[i] = line;
        }
    }
}
