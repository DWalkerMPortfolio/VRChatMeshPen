---
tags:
  - Documentation
  - Documentation/Meeting
---
## Why this Exists
### Desired Change to Player Experience
The 3D pen allows users to communicate more effectively by drawing out ideas, marking things in the world, and taking simple notes.
### Developer Rationale
The 3D pen color changing uses a palette above the off-hand as that is an intuitive way to change the color of a paintbrush. Additionally, nearly every input on the Quest controllers is already mapped to something in VRChat.
## Aesthetic Inspiration
The 3D pen is a paintbrush as it is a more whimsical option than a simple pen. This also justifies switching colors by touching the "palette" above your off-hand.
## Explanation of Functionality
### Player Perspective
When a pen is spawned, it starts with a random color. When players pick up the pen, they can hold down the use button to draw a continuous line in 3D space until the button is released. Players can also switch the color of the pen by using an input specific to their platform. The paint on the tip of the pen model changes color to match the color of the line that will be drawn.
#### VR Specific
In VR, the pen color is swapped using a palette that appears above the hand not holding the pen. The palette consists of an arc of colored spheres. Touching the tip of the pen to one of the spheres changes the color of the pen to match the sphere.
#### Keyboard/Mouse Specific
On keyboard, the color of the pen can be cycled through the available options by pressing `F`.
### Developer Perspective
#### History
The 3D pen has gone through several iterations including the default provided VRChat pen (`Packages/VRChat SDK - Worlds/Samples/UdonExampleScene/SimplePenSystem`), the popular qvPen asset (`Assets/3rd Party/net.ureishi.qvpen-3.2.7`), and several custom solutions. We switched from the qvPen to a custom solution due to the difficultly of modifying the qvPen to meet specific requests from the research team.
#### Object Pool vs Mesh
Previous pen solutions used an object pool of GameObjects, each with a `LineRenderer` displaying one continuous line. This method had two issues:
1. It imposed a hard cap on the number of lines that could be present in the scene
2. More importantly, it introduced a network race condition where if two players started drawing at the same time, they would take the same line GameObject from the pool and both add points to it at the same time
The current solution uses a `LineRenderer` for lines as they are being drawn but then bakes them into a `Mesh` when the draw button is released. Each pen has one `MeshRenderer` that holds all lines it has drawn.
#### Scripts
All scripts involved in this system are located in `Assets/Scripts/Custom 3D Pen`. They are as follows:
- `Mesh3DPenLine`: Handles the majority of processing related to drawing and erasing lines. Holds the data for lines that have been drawn
- `Mesh3DPenLineHolder`: Holds a list of every `Mesh3DPenLine` script in the scene, allowing erasers to test for intersection against every line when used
- `LateJoinMesh3DPenLineSync`: Allows for syncing the entirety of existing line data for players who join the world late. The `Mesh3DPenLine` script only syncs changes to the line and assumes other clients know the state of the line before the change
- `Mesh3DPen`: Handles the pen itself, tells its associated `Mesh3DPenLine` when and where to add lines
- `Mesh3DPenPalette`: Handles the palette that appears above users' off-hand in VR, allowing them to change the pen color
- `Mesh3DPenPaletteColor`: Handles an individual color in the palette
- `Mesh3DPenEraser`: Handles both the normal and admin erasers
- `Mesh3DPenPixelEraser`: Handles the pixel eraser
- `GenerateLinesForMesh3DPens`: An editor-only utility script used to quickly assign lines to pens in an object pool
#### Line Renderers
Despite using a `Mesh` to display finished lines, the pen still makes use of several `LineRenderer`s. Each is located on a child of the pen prefab.
- Current Line: Displays the line that is currently being drawn. This is the `LineRenderer` that is baked into the mesh when the line is complete
- Remote Current Line: The position of the pen syncs to remote clients more frequently than the current line data does. This `LineRenderer` follows the position of the pen to display an approximate prediction of what a remote player is drawing, smoothing out the perspective of remote players
- Regeneration Line: The `Mesh` must be regenerated when line data changes. When this happens, this `LineRenderer` is (all within one frame) assigned the positions of each existing line and then used to bake each one into the new `Mesh`.
- Marking Line: Used to mark a line targeted by the Eraser or Admin Eraser. [[#To Assign Lines To A Populated Pool of Pens]].
#### Erasers
The 3 available erasers are responsible for the majority of the complexity of the pen system:
- [[Eraser]]
- [[Admin Eraser]] (uses the same script as the normal eraser)
- [[Pixel Eraser]]
## How to Tweak Behavior
### To add a pen to a scene:
1. Add the `Assets/Prefabs/Work/Mesh Pen` prefab to the scene.
2. Add the `Assets/Prefabs/Work/Mesh Pen Palette` prefab to the scene. There should only be one palette in the scene, regardless of how many pens are present.
3. Assign the `Palette` property on the mesh pen's `Mesh 3D Pen` script to the palette that was just added to the scene.
4. Add the `Assets/Prefab/Work/Mesh Pen Line` prefab to the scene.
5. Assign the `Line` property on the mesh pen's `Mesh 3D Pen` script to the newly added pen line. Assign the `Pen` property on the pen line's `Mesh 3D Pen Line` script to the mesh pen.
6. Place the newly-added mesh pen line `GameObject` under an empty parent. Add a `Mesh 3D Pen Line Holder` script to the parent.
7. Add the pen line to the `Mesh 3D Pen Lines` property on the `Pen Line Holder` script. Assign the `Line Holder` property on the pen line's `Mesh 3D Pen Line` script to the `Pen Line Holder` script on the parent.
Further pens must have an associated line added to the line holder with all references assigned. There should only be one palette and line holder in the scene regardless of the number of pens.
### To assign lines to a populated pool of pens
If you have an object pool of pens (ex: for player to spawn from the inventory), you can use the `GenerateLinesForMesh3DPens` script to quickly assign each pen a line. This will delete any existing lines in the line holder.
1. Add the `GenerateLinesForMesh3DPens` script to the line holder `GameObject`.
2. Assign the required references on the script.
3. In the context menu (3 dots at the top-right of the component), select "Generate"
Known issues:
- Sometime Unity is unable to delete existing pen lines and freezes up. If this occurs, reboot Unity and delete all existing lines in the line pool before generating.
- Sometimes Unity fails to assign references to some objects (usually prefab instances). If this occurs, simply assign the missing references manually.
- Sometimes Unity appears to have assigned references correctly but they will be set to null when the game runs. If this occurs, assign any missing references manually.
### To change available pen colors
1. Change the `Colors` property on the `Mesh 3D Pen Line` script of every line in the scene (or update the prefab and regenerate the lines).
2. Change the colors of/add/remove children of the arc layout group child of the palette. Ensure the colors, number, and order of the colors on the palette and lines match.
3. Make sure the `Color Index` property on each `Mesh 3D Pen Palette` color script in the arc layout's children matches the index in the array of its associated color in each line's `Colors` property.
### To debug line data
As the debugger cannot be used with UdonSharp, debugging the position data of existing lines can be tricky. There are debug properties on the `Mesh 3D Pen Line` script that can be used to visualize this data.
1. Assign a `LineRenderer` to the `Debug Line Renderer` property on the script. This line renderer should be easily identifiable when overlaid over a line. A duplicate of the marking line renderer works well for this purpose.
2. Run the game and create the lines you would like to visualize.
3. Assign the `Debug Line Index` property of the script to the index of the line you would like to visualize and select "Debug Display Line Data" from the component context menu.
The debug line renderer assigned should then be overlaid over the line with the selected index. Its positions list can then be examined.