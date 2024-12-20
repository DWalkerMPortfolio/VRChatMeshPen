using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using VRC.Udon.Common;
using System;
using static VRC.Dynamics.CollisionShapes;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Unity.Burst.Intrinsics;
using System.Linq;
using BestHTTP.JSON;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Mesh3DPenLine : UdonSharpBehaviour
{
    #region Variables
    [Tooltip("The pen associated with this line")]
    public Mesh3DPen pen;
    [Tooltip("The component that holds all the lines in the scene")]
    public Mesh3DPenLineHolder lineHolder;

    [Tooltip("The late join mesh 3D pen line sync associated with this line")]
    [SerializeField] LateJoinMesh3DPenLineSync lateJoinSync;
    [Tooltip("The minimum distance the pen must move before adding a new point to the line")]
    [SerializeField] float minMoveDistance;
    [Tooltip("The tolerance to use when simplifying a finished line")]
    [SerializeField] float simplifyTolerence;
    [Tooltip("The time to wait between syncing")]
    [SerializeField] float timeBetweenSyncs;
    [Tooltip("The line renderer used for the current line")]
    [SerializeField] LineRenderer currentLineRenderer;
    [Tooltip("The line renderer used for smoothing out drawing by remote players")]
    [SerializeField] LineRenderer remoteCurrentLineRenderer;
    [Tooltip("The line renderer used for marking lines")]
    [SerializeField] LineRenderer markingLineRenderer;
    [Tooltip("The main line renderer used for displaying all drawn lines")]
    [SerializeField] LineRenderer mainLineRenderer;
    [Tooltip("The list of colors this line can be")]
    [SerializeField] Color[] colors;
    [Tooltip("The minimum distance a pixel eraser must move before updating line positions")]
    [SerializeField] float pixelEraserMinMoveDistance = 0.1f;
    [Tooltip("The size of the shader points buffer to use when pixel erasing. Must match the value on the material")]
    [SerializeField] int pixelEraseShaderPointsBufferSize = 25;
    [Tooltip("The index in the buffer at which to apply the line data changes. Used as a buffer to allow time for network syncing")]
    [SerializeField] int pixelEraseShaderPointsBufferSyncIndex = 15;
    [Tooltip("The material parameter used to pass pixel erase points")]
    [SerializeField] string pixelErasePointsParameter = "_Points";
    [Tooltip("The material parameter used for the pixel eraser position")]
    [SerializeField] string pixelEraserPositionParameter = "_EraserPosition";
    [Tooltip("The material parameter used for the pixel erase radius")]
    [SerializeField] string pixelEraseRadiusParameter = "_EraseRadius";
    [Tooltip("The material parameter used for the pixel erase marking radius")]
    [SerializeField] string pixelEraseMarkRadiusParameter = "_MarkRadius";
    [Tooltip("The radius around the end of newly received current line positions within which to switch from the remote current line renderer to the actual positions")]
    [SerializeField] float remoteCurrentLineSwapRadius = 0.25f;

    [Tooltip("The current color of this line")]
    [SerializeField] [UdonSynced] int colorIndex = 0;

    [UdonSynced] Vector3[] newCurrentLinePositions = new Vector3[0]; //The new positions of the line currently being drawn
    [UdonSynced] bool finished = false; //Set to true for one network update when a serialization marks a finished line
    [UdonSynced] bool erased = false; //Set to true for one network update when a serialization marks a line being erased
    [UdonSynced] int erasedIndex = -1; //Set to the index of the line being erased (-1 if none)
    [UdonSynced] bool cleared = false; //Set to true for one network update when all lines have been cleared
    [UdonSynced] bool pixelEraseFinished = false; //Set to true for one network update when a pixel erase has been finished
    [UdonSynced] bool isDrawing; //Whether this pen is currently drawing
    [UdonSynced] string pixelEraseSyncDataJson; //Used to sync pixelEraseSyncData
    [UdonSynced] bool pixelErasing = false; //Whether a pixel erase is currently ongoing

    [Header("Debug")]
    [Tooltip("The line renderer to use for displaying debug values")]
    [SerializeField] LineRenderer debugLineRenderer;
    [Tooltip("The index of the line to debug")]
    [SerializeField] int debugLineIndex;

    Transform penTip; //The transform representing the tip of the pen associated with this line
    Vector3 lastPointPosition; //The position at which we started drawing or the last point was drawn
    float lastSyncTime; //The last time the line currently being drawn was synced
    int currentLineLastSyncCount; //The number of points on the current line last time it was synced
    Vector3 lastPixelEraserPosition = Vector3.positiveInfinity; //The position of the pixel eraser the last time line positions were updated
    Vector4[] pixelEraseShaderPoints; //The positions array passed to the line shader when pixel erasing
    int pixelEraseShaderPointsIndex = 0; //The index of the shader position to be updated next when pixel erasing
    bool bufferingNewCurrentLinePositions; //Whether new current line positions are currently being buffered
    Vector3[] bufferedNewCurrentLinePositions; //A buffer of the most recently received new current line positions
    bool startedRemotePixelErase = false; //Set to true when a remote pixel erase is started, then set back to false as soon as the actual pixelErasing bool is synced to true. Used to start updating shader parameters immediately when a remote erase starts
    Vector3 lineBreakPosition = new Vector3(0, -10000, 0); // The position set on the main line renderer to indicate a line break and clip the associated quads
    
    DataList pixelEraseSyncData = new DataList(); //The data sent to sync a pixel erase. DataList of DataDictionaries. Dictionary keys: nullClear (bool), lineIndex (int), segmentAIndex (int), positionA_X (double), positionA_Y (double), positionA_Z (double), segmentBIndex (int), positionB_X (double), positionB_Y (double), positionB_Z (double)

    DataList linePositions; //A list of lists of positions for all of this object's lines. Points are stored as 3 sequential floats x,y,z
    DataList lineColorIndices; //A list of color indices for all of this object's lines
    #endregion

    #region Unity Functions
    private void Start()
    {
        //Get pen tip
        penTip = pen.GetTip();

        //Initialize data containers
        linePositions = new DataList();
        lineColorIndices = new DataList();

        //Initialize the pen model color
        int startingColorIndex = Mathf.Max(0, lineHolder.GetLineIndex(this));
        startingColorIndex %= colors.Length;
        pen.SetPenModelColor(colors[startingColorIndex % colors.Length]);
        
        if (Networking.IsOwner(gameObject))
        {
            colorIndex = startingColorIndex;
            RequestSerialization();
        }

        //Initialize pixel erase shader parameters
        pixelEraseShaderPoints = new Vector4[pixelEraseShaderPointsBufferSize];
        for (int i = 0; i < pixelEraseShaderPointsBufferSize; i++)
            pixelEraseShaderPoints[i] = Vector4.positiveInfinity;
        mainLineRenderer.material.SetFloat(pixelEraseRadiusParameter, 0);
    }

    private void Update()
    {
        if (isDrawing)
        {
            if (Networking.IsOwner(gameObject))
            {
                //Has the pen moved enough?
                if (VectorSquareMagnitude(penTip.position - lastPointPosition) >  minMoveDistance * minMoveDistance)
                {
                    //Increase points in Line
                    currentLineRenderer.positionCount++;

                    //Make sure color matches
                    currentLineRenderer.startColor = colors[colorIndex];
                    currentLineRenderer.endColor = colors[colorIndex];

                    //Add point to Line
                    currentLineRenderer.SetPosition(currentLineRenderer.positionCount - 1, penTip.position);
                    lastPointPosition = penTip.position;
                }

                //Send position data
                if (Time.time - lastSyncTime >= timeBetweenSyncs)
                {
                    SyncCurrentLine();
                    lastSyncTime = Time.time;
                }
            }
            else
            {
                //Has the pen moved enough
                if (VectorSquareMagnitude(penTip.position - lastPointPosition) > minMoveDistance * minMoveDistance)
                {
                    //If the remote current line renderer is empty, add a point that matches the end of the current line to connect more seamlessly
                    if (remoteCurrentLineRenderer.positionCount == 0 && currentLineRenderer.positionCount > 0)
                    {
                        remoteCurrentLineRenderer.positionCount = 1;
                        remoteCurrentLineRenderer.SetPosition(0, currentLineRenderer.GetPosition(currentLineRenderer.positionCount - 1));
                    }

                    //Make sure remote current line renderer color matches
                    remoteCurrentLineRenderer.startColor = colors[colorIndex];
                    remoteCurrentLineRenderer.endColor = colors[colorIndex];

                    //Increase points in remote line renderer
                    remoteCurrentLineRenderer.positionCount++;

                    //Add point to remote line renderer
                    remoteCurrentLineRenderer.SetPosition(remoteCurrentLineRenderer.positionCount - 1, penTip.position);
                    lastPointPosition = penTip.position;
                }

                //Have positions been buffered and is the pen close to the end of the newest received batch of line positions
                if (bufferingNewCurrentLinePositions)
                {
                    if (VectorSquareMagnitude(penTip.position - bufferedNewCurrentLinePositions[bufferedNewCurrentLinePositions.Length - 1]) < remoteCurrentLineSwapRadius)
                    {
                        //Debug.Log("Updating buffer due to proximity");
                        UpdateCurrentLineRenderer(bufferedNewCurrentLinePositions);
                    }
                }
            }
        }
    }
    #endregion

    #region VRChat Functions
    public override void OnPreSerialization()
    {
        base.OnPreSerialization();

        //Serialize pixelEraseSyncData for network syncing
        if (VRCJson.TrySerializeToJson(pixelEraseSyncData, JsonExportType.Minify, out DataToken result))
            pixelEraseSyncDataJson = result.String;
        else
            Debug.LogError("Failed to serialize pixel erase sync data: " + result.Error);
    }

    /// <summary>
    /// Called after sending network data
    /// </summary>
    /// <param name="result">The result of the network transfer</param>
    public override void OnPostSerialization(SerializationResult result)
    {
        base.OnPostSerialization(result);

        //If finished, clear the current line points
        if (finished)
            newCurrentLinePositions = new Vector3[0];

        //Reset bools so they're only sent for one network update
        ResetSyncBools();

        //Reset pixel erase sync data to avoid sending duplicate data on the next network sync
        pixelEraseSyncData = new DataList();
        pixelEraseSyncDataJson = "";
    }

    /// <summary>
    /// Called when new network data is received
    /// </summary>
    public override void OnDeserialization()
    {
        base.OnDeserialization();

        Debug.Log("There are " + linePositions.Count + " lines present in the scene");

        if (!Networking.IsOwner(gameObject))
        {
            //Check if new current line points have been received
            if (newCurrentLinePositions.Length > 0)
            {
                //If we're already buffering positions, apply them immediately before buffering more
                if (bufferingNewCurrentLinePositions)
                {
                    //Debug.Log("Updating buffer due to new data");
                    UpdateCurrentLineRenderer(bufferedNewCurrentLinePositions);
                }

                bufferingNewCurrentLinePositions = true;

                //Get the end point of the buffered positions
                bufferedNewCurrentLinePositions = newCurrentLinePositions;
            }

            //Update current line renderer color
            currentLineRenderer.startColor = colors[colorIndex];
            currentLineRenderer.endColor = colors[colorIndex];

            //Update the pen model's color
            pen.SetPenModelColor(colors[colorIndex]);

            //A line has been finished
            if (finished)
            {
                //Update the current line renderer
                //Debug.Log("Updating buffer due to finished line");
                UpdateCurrentLineRenderer(newCurrentLinePositions);

                //Generate line mesh
                AddLine(currentLineRenderer);

                //Reset line renderers
                currentLineRenderer.positionCount = 0;
            }

            //A line has been erased
            if (erased)
            {
                EraseLine(erasedIndex);
                Debug.Log("Told to erase line at index: " + erasedIndex + " there are now" + linePositions.Count + " lines present in the scene");
                currentLineRenderer.positionCount = 0;
            }

            //All lines have been cleared
            if (cleared)
            {
                linePositions = new DataList();
                lineColorIndices = new DataList();
                currentLineRenderer.positionCount = 0;
                mainLineRenderer.positionCount = 0;
            }

            //Deserialize and apply pixel erase data
            if (VRCJson.TryDeserializeFromJson(pixelEraseSyncDataJson, out DataToken result))
            {
                pixelEraseSyncData = result.DataList;

                if (pixelEraseSyncData.Count > 0)
                {
                    //Apply the pixel erase to the data
                    for (int i = 0; i < pixelEraseSyncData.Count; i++)
                    {
                        DataDictionary segmentErase = pixelEraseSyncData[i].DataDictionary;

                        if (!segmentErase["nullClear"].Boolean)
                        {
                            //This entry in the sync data is a pixel erase
                            int lineIndex = (int)segmentErase["lineIndex"].Double;
                            Vector3 positionA = new Vector3((float)segmentErase["positionA_X"].Double, (float)segmentErase["positionA_Y"].Double, (float)segmentErase["positionA_Z"].Double);
                            Vector3 positionB = new Vector3((float)segmentErase["positionB_X"].Double, (float)segmentErase["positionB_Y"].Double, (float)segmentErase["positionB_Z"].Double);
                            PixelEraseLinePortion(lineIndex, (int)segmentErase["segmentAIndex"].Double, positionA, (int)segmentErase["segmentBIndex"].Double, positionB, false);
                        }
                        else
                        {
                            //This entry in the sync data is a null clear
                            ClearNullLines(false);
                        }
                    }
                    ApplyPixelErase(false, pixelEraseFinished);
                }
                else if (pixelEraseFinished) //It's possible for a pixel erase to finish on a serialization with no new pixel erase data, make sure shader parameters are still cleared in that case
                    ClearPixelEraseShaderParameters();
            }
            else
                Debug.LogError("Failed to deserialize pixel erase sync data: " + result.Error);
        }
    }

    /// <summary>
    /// Called when ownership of this object changed
    /// </summary>
    /// <param name="player">The player that now owns this object</param>
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        base.OnOwnershipTransferred(player);

        if (player == Networking.LocalPlayer)
        {
            //Reset bools on ownership transfer
            ResetSyncBools();

            //Reset current line buffering
            bufferingNewCurrentLinePositions = false;
            bufferedNewCurrentLinePositions = new Vector3[0];
            newCurrentLinePositions = new Vector3[0];
        }
    }
    #endregion

    #region Public Functions
    /// <summary>
    /// Called by the Mesh3DPen to ensure this line is owned by the same player as its pen
    /// </summary>
    public void UpdateOwner(VRCPlayerApi owner)
    {
        Networking.SetOwner(owner, gameObject);
        Networking.SetOwner(owner, lateJoinSync.gameObject);
    }

    /// <summary>
    /// Called by the Mesh3DPen when interaction starts
    /// </summary>
    public void StartDrawing()
    {
        //Reset variables
        isDrawing = true;
        lastPointPosition = penTip.position;
        lastSyncTime = Time.time;

        //Initialize line with 2 points at pen tip
        currentLineRenderer.positionCount = 2;
        currentLineRenderer.SetPosition(0, penTip.position);
        currentLineRenderer.SetPosition(1, penTip.position);
        currentLineRenderer.startColor = colors[colorIndex];
        currentLineRenderer.endColor = colors[colorIndex];

        RequestSerialization();
    }

    /// <summary>
    /// Called by the Mesh3DPen when interaction stops
    /// </summary>
    public void StopDrawing()
    {
        if (Networking.IsOwner(gameObject))
        {
            //Finish drawing
            isDrawing = false;

            //Send network data
            if (VRCPlayerApi.GetPlayerCount() > 1)
            {
                finished = true;
                SyncCurrentLine();
            }

            //Generate line mesh
            AddLine(currentLineRenderer);

            //Clear the line renderer
            currentLineRenderer.positionCount = 0;

            //Clear the last sync count
            currentLineLastSyncCount = 0;
        }
    }

    /// <summary>
    /// Called by the LateJoinMesh3DPenLineSync to get the linePositions data list
    /// </summary>
    /// <returns>The linePositions data list</returns>
    public DataList GetLinePositions()
    {
        return linePositions;
    }

    /// <summary>
    /// Called by the LateJoinMesh3DPenLineSync to get the lineColorIndices data list
    /// </summary>
    /// <returns>The lineColorIndices data list</returns>
    public DataList GetLineColorInidices()
    {
        return lineColorIndices;
    }

    /// <summary>
    /// Called by the LateJoinMesh3DPenLinesync to set line data and regenerate the mesh
    /// </summary>
    /// <param name="positions">The new linePositions data list</param>
    /// <param name="colorIndices">The new colorIndices data list</param>
    public void SetLineData(DataList positions, DataList colorIndices)
    {
        linePositions = positions;
        lineColorIndices = colorIndices;
        RegenerateLines();
    }

    /// <summary>
    /// Checks for a line and erases it if found
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="radius">The radius around the position to check</param>
    public void CheckEraseLine(Vector3 position, float radius)
    {
        //Skip if there is already a pending unsent erase
        if (erased)
        {
            Debug.Log("Skipping erase due to pending unsent erase");
            return;
        }

        //Skip if a pixel erase is ongoing
        if (pixelErasing)
        {
            Debug.Log("Skipping erase due to ongoing pixel erase");
            return;
        }

        //Debug.Log("Checking to erase line");
        ClearMark();
        int eraseIndex = CheckSphereIntersectionLine(position, radius);
        if (eraseIndex != -1)
        {
            //Debug.Log("Found intersecting line at index: "+eraseIndex);
            EraseLine(eraseIndex);
        }
    }

    /// <summary>
    /// Marks the areas of lines that would be pixel erased with the given eraser position and radius
    /// </summary>
    /// <param name="position">The position of the pixel eraser to mark around</param>
    /// <param name="radius">The radius around the pixel eraser to mark</param>
    public void MarkPixelEraseLine(Vector3 position, float radius)
    {
        mainLineRenderer.material.SetVector(pixelEraserPositionParameter, position);
        mainLineRenderer.material.SetFloat(pixelEraseMarkRadiusParameter, radius);
    }

    /// <summary>
    /// Checks for part of a line and pixel erases it if found. Only updates the actual line data if run by the owner of this line
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="radius">The radius around the position to check</param>
    public void CheckPixelEraseLine(Vector3 position, float radius)
    {
        //Debug.Log("Checking pixel erase line");

        if (Networking.IsOwner(gameObject))
        {
            //Skip if there is a pending unsent erase
            if (erased)
            {
                Debug.Log("Skipping pixel erase due to pending unsent erase");
                return;
            }
           
            PixelEraseLine(position, radius, true);
        }
        else if (pixelErasing || startedRemotePixelErase) //Make sure not to do this if the host is no longer pixel erasing
        {
            if (pixelErasing && startedRemotePixelErase)
                startedRemotePixelErase = false; //startedRemotePixelErase is only used to update shader parameters at the start of the pixel erase. Reset it as soon as pixelErasing is synced to be true

            PixelEraseLine(position, radius, false);
        }
    }

    /// <summary>
    /// Called when starting a pixel erase
    /// </summary>
    public void StartPixelErase()
    {
        if (Networking.IsOwner(gameObject))
        {
            pixelErasing = true;
            RequestSerialization();
        }
        else
        {
            startedRemotePixelErase = true;
        }
    }

    /// <summary>
    /// Finishes any ongoing pixel erase, applying it. Should only be called on the owner of this line
    /// </summary>
    /// <param name="position">The position the erase finished</param>
    /// <param name="radius">The radius around the position to erase</param>
    public void FinishPixelErase(Vector3 position, float radius)
    {
        PixelEraseUpdateLinePositions(position, radius);
        pixelErasing = false;
        pixelEraseFinished = true;
        ApplyPixelErase(true, true);
    }

    /// <summary>
    /// Checks for a line and clears all lines if found
    /// </summary>
    /// <param name="position">The position to check</param>
    /// <param name="radius">The radius around the position to check</param>
    public void CheckClearLines(Vector3 position, float radius)
    {
        ClearMark();
        int eraseIndex = CheckSphereIntersectionLine(position, radius);
        if (eraseIndex != -1)
            Clear();
    }

    /// <summary>
    /// Checks for a line to mark. Clears any existing marks first
    /// </summary>
    /// <param name="position">The position to check for a line to mark</param>
    /// <param name="radius">The radius around the position to check</param>
    public void CheckMarkLine(Vector3 position, float radius)
    {
        ClearMark();
        int markIndex = CheckSphereIntersectionLine(position, radius);
        if (markIndex != -1)
        {
            MarkLine(markIndex);
        }
    }

    /// <summary>
    /// Clears the line mark if one exists
    /// </summary>
    public void ClearMark()
    {
        markingLineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Clears the points of the current line renderer
    /// </summary>
    public void ClearCurrentLine()
    {
        currentLineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Clears the lines. Called on all clients by the LineHolder
    /// </summary>
    public void Clear()
    {
        mainLineRenderer.positionCount = 0;
        linePositions = new DataList();
        lineColorIndices = new DataList();
        currentLineRenderer.positionCount = 0;
        newCurrentLinePositions = new Vector3[0];
        currentLineLastSyncCount = 0;

        if (Networking.IsOwner(gameObject))
        {
            cleared = true;
            RequestSerialization();
        }
    }

    /// <summary>
    /// Increment the color to draw
    /// </summary>
    public void IncrementColor()
    {
        colorIndex++;
        colorIndex %= colors.Length;
        pen.SetPenModelColor(colors[colorIndex]);
        RequestSerialization();
    }

    /// <summary>
    /// Sets the current color to draw
    /// </summary>
    /// <param name="value">The index of the new color</param>
    public void SetColor(int value)
    {
        colorIndex = value;
        pen.SetPenModelColor(colors[colorIndex]);
        RequestSerialization();
    }
    #endregion

    #region Private Functions
    /// <summary>
    /// Returns a position on a line as a Vector3
    /// </summary>
    /// <param name="lineDataList">The data list representing the line to get a position from</param>
    /// <param name="positionIndex">The index of the x coordinate of the position to get</param>
    /// <returns>The position as a Vector3</returns>
    Vector3 GetLinePosition(DataList lineDataList, int positionIndex)
    {
        return new Vector3((float)lineDataList[positionIndex].Double, (float)lineDataList[positionIndex + 1].Double, (float)lineDataList[positionIndex + 2].Double);
    }

    /// <summary>
    /// Resets the status bools that are only sent for one network update
    /// </summary>
    void ResetSyncBools()
    {
        finished = false;
        erased = false;
        cleared = false;
        pixelEraseFinished = false;
    }

    /// <summary>
    /// Syncs the line currently being drawn
    /// </summary>
    void SyncCurrentLine()
    {
        currentLineRenderer.Simplify(simplifyTolerence);
        newCurrentLinePositions = new Vector3[currentLineRenderer.positionCount - Mathf.Max(0, currentLineLastSyncCount - 1)]; //This line sometimes results in an arithmetic overflow somehow. I haven't yet managed to figure out why
        for (int i=Mathf.Max(0, currentLineLastSyncCount - 1); i<currentLineRenderer.positionCount; i++)
            newCurrentLinePositions[i - Mathf.Max(0, currentLineLastSyncCount - 1)] = currentLineRenderer.GetPosition(i);
        currentLineLastSyncCount = currentLineRenderer.positionCount;
        RequestSerialization();
    }

    /// <summary>
    /// Updates the current line renderer to match buffered data, clearing the remote current line renderer
    /// </summary>
    void UpdateCurrentLineRenderer(Vector3[] positions)
    {
        //Debug.Log("Updating current line renderer");

        //Update the current line renderer
        //Update length. The sync starts with the last position from the last sync if possible so account for that here
        int currentLineRendererCurrentLength = currentLineRenderer.positionCount;
        currentLineRenderer.positionCount += positions.Length;
        if (currentLineRendererCurrentLength > 0)
            currentLineRenderer.positionCount -= 1;
        //Update positions
        int positionIndex = Mathf.Max(0, currentLineRendererCurrentLength - 1);
        for (int i = 0; i < positions.Length; i++)
            currentLineRenderer.SetPosition(positionIndex + i, positions[i]);

        //Clear the remote current line renderer
        remoteCurrentLineRenderer.positionCount = 0;

        //Clear the buffer
        bufferingNewCurrentLinePositions = false;
    }

    /// <summary>
    /// Adds a finished line to the line positions list and bakes it into the mesh
    /// </summary>
    /// <param name="lineRenderer">The line renderer holding the line data</param>
    /// <param name="save">Whether to save the points and color in the data lists</param>
    void AddLine(LineRenderer lineRenderer, bool save = true, int colorIndexOverride = -1)
    {
        //Simplify line renderer. This makes sure all clients are fully simplified before adding the line so they all have the same number of points
        //lineRenderer.Simplify(simplifyTolerence);

        int newLineStartingIndex = mainLineRenderer.positionCount;
        mainLineRenderer.positionCount += lineRenderer.positionCount + 1;
        mainLineRenderer.SetPosition(newLineStartingIndex, lineBreakPosition);
        for (int i=0; i<lineRenderer.positionCount; i++)
        {
            mainLineRenderer.SetPosition(newLineStartingIndex + i + 1, lineRenderer.GetPosition(i));
        }

        // Mesh method
        /*
        // Get mesh from line renderer
        Mesh finishedLineMesh = new Mesh();
        lineRenderer.BakeMesh(finishedLineMesh, useTransform: true);

        // Set vertex colors on new line mesh
        int vertexColorIndex = colorIndexOverride != -1 ? colorIndexOverride : colorIndex;
        Color[] vertexColors = new Color[finishedLineMesh.vertexCount];
        for (int i = 0; i < vertexColors.Length; i++)
            vertexColors[i] = colors[vertexColorIndex];
        finishedLineMesh.colors = vertexColors;

        // Create combine instances
        CombineInstance[] combineInstances = new CombineInstance[2];
        
        // Existing meshes combine instance
        combineInstances[0] = new CombineInstance();
        combineInstances[0].mesh = meshFilter.mesh;
        combineInstances[0].transform = Matrix4x4.identity;

        // New line combine instance
        combineInstances[1] = new CombineInstance();
        combineInstances[1].mesh = finishedLineMesh;
        combineInstances[1].transform = Matrix4x4.identity;
        */

        // Save points
        if (save)
        {
            //Get points from line renderer
            Vector3[] finishedLinePoints = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(finishedLinePoints);

            //Save to data list
            DataList lineDataList = new DataList();
            foreach (Vector3 point in finishedLinePoints)
            {
                lineDataList.Add(point.x);
                lineDataList.Add(point.y);
                lineDataList.Add(point.z);
            }
            linePositions.Add(lineDataList);
            Debug.Log("Adding line with " + lineDataList.Count + " points. New line count: " + linePositions.Count);

            //Save colors
            lineColorIndices.Add(colorIndex);
        }
        else
            Debug.Log("Regenerating line. Line count: " + linePositions.Count);

        // Combine meshes
        /*
        Mesh lineMesh = new Mesh();
        lineMesh.CombineMeshes(combineInstances);
        Destroy(meshFilter.mesh);
        meshFilter.mesh = lineMesh;*/
    }

    /// <summary>
    /// Checks whether a sphere intersects any line on this object
    /// </summary>
    /// <param name="position">The position to check for a line</param>
    /// <param name="radius">The radius to search around the position</param>
    /// <returns>The index of the line found (or -1 if none)</returns>
    private int CheckSphereIntersectionLine(Vector3 position, float radius)
    {
        //Check if there are any lines to intersect
        if (linePositions == null)
            return -1;

        //Check if the position is within the mesh bounds
        // TODO: Replace this with a bounds check that doesn't use the mesh
        /*if (!meshFilter.mesh.bounds.Intersects(new Bounds(position, Vector3.one * radius)))
            return -1;*/

        //Iterate over all lines drawn by this gameObject to find the closest intersecting line
        bool lineIntersects = false;
        int nearestIntersectionIndex = -1;
        float nearestIntersectionDistanceSquared = Mathf.Infinity;
        for (int i = 0; i < linePositions.Count; i++)
        {
            //Get line points list
            DataToken lineDataToken = linePositions[i];
            DataList lineDataList = lineDataToken.DataList;

            //Skip if line only has one point somehow
            if (lineDataList.Count < 6)
            {
                Debug.Log("There's a line with only one point somehow. Index: " + i);
                continue;
            }

            //Check collision between eraser sphere and each segment of the line
            Vector3 previousPointPosition = GetLinePosition(lineDataList, 0);
            for (int j = 3; j < lineDataList.Count; j += 3)
            {
                Vector3 pointPosition = GetLinePosition(lineDataList, j);

                if (SegmentSphereIntersectionCheck(position, radius, previousPointPosition, pointPosition))
                {

                    lineIntersects = true;
                    float intersectionDistanceSquared = VectorSquareMagnitude(pointPosition - position);
                    if (intersectionDistanceSquared < nearestIntersectionDistanceSquared)
                    {
                        nearestIntersectionDistanceSquared = intersectionDistanceSquared;
                        nearestIntersectionIndex = i;
                    }
                }
            }
        }

        if (lineIntersects)
            return nearestIntersectionIndex;

        return -1;
    }

    /// <summary>
    /// Erase the line at the given index
    /// </summary>
    /// <param name="index">The index of the line to erase</param>
    void EraseLine(int index)
    {
        Debug.Log("Erasing line with index: " + index + ". There are now " + (linePositions.Count - 1) + " lines present in the scene");
        linePositions.RemoveAt(index);
        lineColorIndices.RemoveAt(index);
        RegenerateLines();

        //Sync erased line
        if (Networking.IsOwner(gameObject))
        {
            if (VRCPlayerApi.GetPlayerCount() > 1)
            {
                //Debug.Log("Owner, syncing line erase");
                erased = true;
                erasedIndex = index;
                RequestSerialization();
            }
        }
    }

    /// <summary>
    /// Pixel erases part of a line. Can only be used to update actual line data if run by the owner of this line
    /// </summary>
    /// <param name="position">The position of the sphere to erase</param>
    /// <param name="radius">The radius of the sphere to erase</param>
    /// <param name="updateLineData">Whether to actually update the line data or to only update the shader visuals</param>
    void PixelEraseLine(Vector3 position, float radius, bool updateLineData)
    {
        //Debug.Log("Pixel erasing line");

        // Check if there are any lines to intersect
        if (linePositions == null)
            return;

        //Check if the position is within the mesh bound
        // TODO: Replace this with a bounds check that doesn't use the mesh
        /*if (!meshFilter.mesh.bounds.Intersects(new Bounds(position, Vector3.one * radius)))
            return;*/

        //Update pixel eraser position and radius in shader
        mainLineRenderer.material.SetFloat(pixelEraseRadiusParameter, radius);
        mainLineRenderer.material.SetVector(pixelEraserPositionParameter, position);

        //Check if the pixel eraser has moved enough to update line positions
        if (VectorSquareMagnitude(position - lastPixelEraserPosition) > pixelEraserMinMoveDistance * pixelEraserMinMoveDistance)
        {
            lastPixelEraserPosition = position;
            
            //Pass a new point to the shader
            pixelEraseShaderPoints[pixelEraseShaderPointsIndex] = position;
            mainLineRenderer.material.SetVectorArray(pixelErasePointsParameter, pixelEraseShaderPoints);
            pixelEraseShaderPointsIndex++;

            if (updateLineData)
            {
                //Update the actual line data
                PixelEraseUpdateLinePositions(position, radius);

                //Apply and sync the pixel erase
                if (pixelEraseShaderPointsIndex >= pixelEraseShaderPointsBufferSyncIndex)
                {
                    ApplyPixelErase(true, true);
                }
            }
            else
            {
                //Failsafe: reset pixel erase shader points index if it exceeds the size of the buffer
                if (pixelEraseShaderPointsIndex >= pixelEraseShaderPointsBufferSize)
                {
                    Debug.Log("Resetting pixel erase shader points index because it exceeded the size of the buffer");
                    pixelEraseShaderPointsIndex = 0;
                }
            }
        }
    }

    /// <summary>
    /// Modifies the actual line position data in response to a pixel eraser moving sufficiently
    /// </summary>
    /// <param name="position">The position of the pixel eraser</param>
    /// <param name="radius">The radius around the pixel eraser to erase</param>
    void PixelEraseUpdateLinePositions(Vector3 position, float radius)
    {
        //Iterate over all lines in this object
        bool erased = false;
        for (int i = 0; i < linePositions.Count; i++)
        {
            //Get line points list
            DataToken lineDataToken = linePositions[i];
            DataList lineDataList = lineDataToken.DataList;

            //Skip if line only has one point somehow
            if (lineDataList.Count < 6)
            {
                Debug.Log("There's a line with only one point somehow. Index: " + i);
                continue;
            }

            //Get initial points
            Vector3 p1;
            Vector3 p2 = GetLinePosition(lineDataList, 0);

            //Initialize intersection carry
            bool carryingIntersection = false;
            int intersectionCarrySegment = -1; //The index of the x position of a carried intersection segment
            Vector3 intersectionCarryPosition = Vector3.zero;

            //Check if the line starts inside the sphere and set an initial carry if it does
            if (VectorSquareMagnitude(p2 - position) < radius * radius)
            {
                carryingIntersection = true;
                intersectionCarrySegment = -1;
            }

            //Iterate over all segments of this line
            for (int j = 0; j < lineDataList.Count - 5; j += 3)
            {
                //Get the positions
                p1 = p2;
                p2 = GetLinePosition(lineDataList, j + 3);

                //Check for intersection
                if (SegmentSphereIntersectionPositions(position, radius, p1, p2, out Vector3 intersectionA, out Vector3 intersectionB, out bool intersectedA, out bool intersectedB))
                {
                    //Debug.Log("Intersected with line: " + i);

                    //Check for first possible intersection
                    if (intersectedA)
                    {
                        //Debug.Log("Intersected on intersection A");
                        //We have a carried intersection position, erase from it to intersection A
                        if (carryingIntersection)
                        {
                            PixelEraseLinePortion(i, intersectionCarrySegment, intersectionCarryPosition, j, intersectionA, true);
                            carryingIntersection = false;
                            erased = true;
                            break;
                        }
                        else
                        {
                            //Carry the intersection until another one is found
                            carryingIntersection = true;
                            intersectionCarrySegment = j;
                            intersectionCarryPosition = intersectionA;
                        }
                    }

                    //Check for second possible intersection
                    if (intersectedB)
                    {
                        //Debug.Log("Intersected on intersection B");
                        if (carryingIntersection)
                        {
                            PixelEraseLinePortion(i, intersectionCarrySegment, intersectionCarryPosition, j, intersectionB, true);
                            carryingIntersection = false;
                            erased = true;
                            break;
                        }
                        else
                        {
                            //Carry the intersection until another one is found
                            carryingIntersection = true;
                            intersectionCarrySegment = j;
                            intersectionCarryPosition = intersectionB;
                        }
                    }
                }
            }

            //If there is still a carry when reaching the end of the line, erase from the carry to the end
            if (carryingIntersection)
            {
                PixelEraseLinePortion(i, intersectionCarrySegment, intersectionCarryPosition, -1, Vector3.zero, true);
                carryingIntersection = false;
                erased = true;
            }
        }

        //Remove all null lines if anything was erased
        if (erased)
        {
            //Debug.Log("Pixel erased");
            ClearNullLines(true);
        }
    }

    /// <summary>
    /// Pixel erases a portion of a line, moving the part after the erased portion to a new index at the end of the list. Must clear null lines once all portion erases have executed
    /// </summary>
    /// <param name="line">The index of the line to pixel erase a portion of</param>
    /// <param name="segmentA">The index of the x coordinate of the first point in the segment of the first intersection (negative if this erase is at the beginning of the line)</param>
    /// <param name="positionA">The position of the first intersection</param>
    /// <param name="segmentB">The index of the x coordinate of the first point in the segment of the second intersection (negative if this erase is at the end of the line)</param>
    /// <param name="positionB">The position of the second intersection</param>
    /// <param name="saveDataToSync">Whether to save this erase to sync to other clients</param>
    void PixelEraseLinePortion(int line, int segmentA, Vector3 positionA, int segmentB, Vector3 positionB, bool saveDataToSync)
    {
        //Debug.Log("Pixel erasing line portion.");

        //Debug.Log("Pixel erasing a portion of the line at index " + line + ". There are " + linePositions.Count + " lines in the scene.");

        if (saveDataToSync)
        {
            //Add this segment erase to the pixel erase sync data
            DataDictionary segmentEraseData = new DataDictionary();
            segmentEraseData["nullClear"] = false;
            segmentEraseData["lineIndex"] = line;
            segmentEraseData["segmentAIndex"] = segmentA;
            segmentEraseData["positionA_X"] = positionA.x;
            segmentEraseData["positionA_Y"] = positionA.y;
            segmentEraseData["positionA_Z"] = positionA.z;
            segmentEraseData["segmentBIndex"] = segmentB;
            segmentEraseData["positionB_X"] = positionB.x;
            segmentEraseData["positionB_Y"] = positionB.y;
            segmentEraseData["positionB_Z"] = positionB.z;
            pixelEraseSyncData.Add(segmentEraseData);
        }

        //Print errors to the console if they occur here
        if (linePositions[line].TokenType == TokenType.Error)
            Debug.Log("Error data token when pixel erasing a line portion. Error: " + linePositions[line].Error);

        //Get the line positions (each point is 3 floats/doubles stored sequentially)
        DataList lineDataList = linePositions[line].DataList;

        if (segmentB >= 0)
        {
            //Get the data list for the part after the erase
            DataList postErasePortion = new DataList();
            postErasePortion.Add(positionB.x); //Add position B to the start of the list
            postErasePortion.Add(positionB.y);
            postErasePortion.Add(positionB.z);
            postErasePortion.AddRange(lineDataList.GetRange(segmentB + 3, lineDataList.Count - (segmentB + 3))); //Copy the remaining list data

            //Append the post-erase part to the end of the lines list
            linePositions.Add(postErasePortion);
            lineColorIndices.Add(lineColorIndices[line]); //Append color index for new line
        }

        if (segmentA >= 0)
        {
            //Remove everything past the erase from the original data list
            lineDataList.RemoveRange(segmentA + 3, lineDataList.Count - (segmentA + 3));
            lineDataList.Add(positionA.x); //Add position A to the end of the list
            lineDataList.Add(positionA.y);
            lineDataList.Add(positionA.z);
        }
        else
        {
            //This erase is at the start of the line, mark it for removal
            linePositions[line] = new DataToken((DataList)null);
        }
    }

    /// <summary>
    /// Applies a pixel erase to the mesh, clears shader parameters, and optionally syncs it to all clients
    /// </summary>
    /// <param name="sync">Whether or not to sync this apply to other clients</param>
    /// <param name="clearShaderParameters">Whether to clear the pixel erase shader parameters when applying this erase</param>
    void ApplyPixelErase(bool sync, bool clearShaderParameters)
    {
        Debug.Log("Applying pixel erase. Sync: " + sync);

        //Regenerate line mesh
        RegenerateLines();

        //Clear shader parameters
        if (clearShaderParameters)
            ClearPixelEraseShaderParameters();

        //Network sync
        if (sync)
            RequestSerialization();
    }

    /// <summary>
    /// Called when a pixel erase is applied to reset the shader parameters
    /// </summary>
    /// <param name="finishedPixelErase">Whether this shader clear is because a pixel erase was finished</param>
    private void ClearPixelEraseShaderParameters()
    {
        Debug.Log("Clearing pixel erase shader parameters");
        
        //This clear is occuring at the end of a pixel erase
        for (int i = 0; i < pixelEraseShaderPoints.Length; i++)
            pixelEraseShaderPoints[i] = Vector4.positiveInfinity;
        mainLineRenderer.material.SetVectorArray(pixelErasePointsParameter, pixelEraseShaderPoints);

        mainLineRenderer.material.SetFloat(pixelEraseRadiusParameter, 0); //Set radius parameter to 0 to avoid clipping points until the next pixel erase starts

        pixelEraseShaderPointsIndex = 0;
    }

    /// <summary>
    /// Removes all null lines from the line positions list. Null lines can be generated by PixelEraseLinePortion()
    /// </summary>
    /// <param name="saveForSync">Whether to save this null clear in the pixel erase sync data</param>
    private void ClearNullLines(bool saveForSync)
    {
        //Clear all null lines
        for (int i = linePositions.Count - 1; i >= 0; i--)
        {
            if (linePositions[i].IsNull)
            {
                //Debug.Log("Removing null line");
                linePositions.RemoveAt(i);
                lineColorIndices.RemoveAt(i);
            }
        }

        //Save null clear for sync
        if (saveForSync)
        {
            DataDictionary nullClearDictionary = new DataDictionary();
            nullClearDictionary["nullClear"] = true;
            pixelEraseSyncData.Add(nullClearDictionary);
        }
    }

    /// <summary>
    /// Mark the line at the given index
    /// </summary>
    /// <param name="index">The index of the line to mark</param>
    void MarkLine(int index)
    {
        //Debug.Log("Marked for erase");
        DataList lineDataList = linePositions[index].DataList;
        markingLineRenderer.positionCount = lineDataList.Count / 3;
        int pointIndex = 0;
        for (int i=0; i<lineDataList.Count; i+=3)
        {
            Vector3 point = GetLinePosition(lineDataList, i);
            markingLineRenderer.SetPosition(pointIndex, point);
            pointIndex++;
        }
    }

    /// <summary>
    /// Regenerate the mesh to match a modified line positions data list
    /// </summary>
    void RegenerateLines()
    {
        // Clear the main line renderer
        mainLineRenderer.positionCount = 0;

        // Iterate over all lines drawn by this gameObject
        int pointIndex = 0;
        for (int i = 0; i < linePositions.Count; i++)
        {
            // Get line points list
            DataToken lineDataToken = linePositions[i];
            DataList lineDataList = lineDataToken.DataList;

            mainLineRenderer.positionCount += lineDataList.Count / 3 + 1;

            // Add gap before next line
            mainLineRenderer.SetPosition(pointIndex, lineBreakPosition);
            pointIndex++;

            // Extract points from points list and apply them to the main line renderer
            for (int j=0; j<lineDataList.Count; j+=3)
            {
                mainLineRenderer.SetPosition(pointIndex, GetLinePosition(lineDataList, j));
                pointIndex++;
            }
        }
    }
    #endregion

    #region Intersection Functions
    //Based on https://stackoverflow.com/questions/2062286/testing-whether-a-line-segment-intersects-a-sphere
    /// <summary>
    /// Checks for intersection between a line segment and a sphere
    /// </summary>
    /// <param name="p">The position of the sphere</param>
    /// <param name="r">The radius of the sphere</param>
    /// <param name="a">One end of the line segment</param>
    /// <param name="b">The other end of the line segment</param>
    /// <returns>True if intersects, false otherwise</returns>
    bool SegmentSphereIntersectionCheck(Vector3 p, float r, Vector3 a, Vector3 b)
    {
        Vector3 A = a - p;
        Vector3 B = b - p;
        Vector3 C = a - b;
        float rSquared = r * r;

        //Avoid using square roots
        if (VectorSquareMagnitude(A) < rSquared || VectorSquareMagnitude(B) < rSquared)
            return true;

        if (Vector3.Angle(A, B) < 90)
            return false;

        float hSquared = VectorSquareMagnitude(A) * (1 - Mathf.Pow(Vector3.Dot(A.normalized, C.normalized), 2));
        if (hSquared < rSquared)
            return true;
        return false;
    }

    //Based on https://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
    /// <summary>
    /// Checks for intersection between a line segment and a sphere and returns the intersection points
    /// </summary>
    /// <param name="p">The center of the sphere</param>
    /// <param name="r">The radius of the sphere</param>
    /// <param name="a">One end of the line segment</param>
    /// <param name="b">The other end of the line segment</param>
    /// <param name="intersectionA">Returns the first intersection point (infinity if no intersection)</param>
    /// <param name="intersectionB">Returns the second intersection point (infinity if no intersection)</param>
    /// <param name="intersectionPositionBuffer">A buffer to add to the intersection positions to ensure they are outside the sphere in case of a second intersection check</param>
    /// <returns></returns>
    bool SegmentSphereIntersectionPositions(Vector3 p, float r, Vector3 a, Vector3 b, out Vector3 intersectionA, out Vector3 intersectionB, out bool intersectedA, out bool intersectedB, float intersectionPositionBuffer = 0.01f)
    {
        //Initial output data
        intersectionA = Vector3.positiveInfinity;
        intersectionB = Vector3.positiveInfinity;
        intersectedA = false;
        intersectedB = false;

        //Get ray data
        float rayLength = (b - a).magnitude;
        Vector3 rayDirection = (b - a) / rayLength;

        //solve for tc
        Vector3 L = p - a;
        float LMagnitude = L.magnitude;
        float tc = Vector3.Dot(L, rayDirection);

        float d2 = (LMagnitude * LMagnitude) - (tc * tc);

        float radius2 = r * r;
        if (d2 > radius2)
        {
            return false;
        }

        //solve for t1c
        float t1c = Mathf.Sqrt(radius2 - d2);

        //solve for intersection t-values
        float t1 = tc - t1c;
        float t2 = tc + t1c;

        //Get the intersection points
        if (t1 > 0 && t1 < rayLength)
        {
            //Debug.Log("Intersected A");
            intersectionA = a + rayDirection * (t1 - intersectionPositionBuffer);
            intersectedA = true;
        }
        if (t2 > 0 && t2 < rayLength)
        {
            //Debug.Log("Intersected B");
            intersectionB = a + rayDirection * (t2 + intersectionPositionBuffer);
            intersectedB = true;
        }

        return intersectedA || intersectedB;
    }

    /// <summary>
    /// Returns the square of a vector's magnitude (faster than getting the magnitude)
    /// </summary>
    /// <param name="v">The vector to find the square of the magnitude of</param>
    /// <returns>The square of the vector's magnitude</returns>
    float VectorSquareMagnitude(Vector3 v)
    {
        return v.x * v.x + v.y * v.y + v.z * v.z;
    }
    #endregion

    #region Debug Functions
    [ContextMenu("Debug Display Line Data")]
    void DebugDisplayLineData()
    {
        DataList lineDataList = linePositions[debugLineIndex].DataList;

        debugLineRenderer.positionCount = lineDataList.Count / 3;
        for (int i=0; i<lineDataList.Count; i+=3)
        {
            debugLineRenderer.SetPosition(i/3, new Vector3((float)lineDataList[i].Double, (float)lineDataList[i+1].Double, (float)lineDataList[i+2].Double));
        }
    }
    #endregion
}