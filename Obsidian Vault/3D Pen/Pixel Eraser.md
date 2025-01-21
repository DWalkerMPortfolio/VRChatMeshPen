---
tags:
  - Documentation
  - Documentation/Meeting
---
## Why this Exists
### Desired Change to Player Experience
The pixel eraser allows players to erase only part of a line rather than entire lines at a time. This allows for a more precise and more familiar method of erasing.
### Developer Rationale
This feature was requested directly by the research team. 

There are currently 3 separate erasers with different functionality ([[3D Pen#Erasers|see here]]). These may be merged into a single object at some point in the future.
## Aesthetic Inspiration
The eraser resembles a typical pink pencil eraser despite the pen resembling a paintbrush. This is because there is no universally-identifiable object used for erasing paint. The pixel eraser additionally has a transparent sphere surrounding it, making it visually distinct from the standard eraser and indicating the radius around it that it will erase.
## Explanation of Functionality
### Player Perspective
Holding the pixel eraser near a line highlights the portion of the line that will be erased. Pressing or holding the use button erases that portion of the line. The use button may be held down as the eraser is moved around to erase larger portions of the line.
#### VR Specific
N/A
#### Keyboard/Mouse Specific
N/A
### Developer Perspective
This eraser is responsible for the majority of the complexity of the `Mesh3DPenLine` script. It functions generally as follows:
1. When the use button is pressed, a pixel erase is started. This is synced to all clients.
2. As the eraser moves during a pixel erase, its positions over time are passed to the shader for the line. This shader clips any pixels within the erase radius of any of these points. Remote clients also pass points to their local copy of the shader based on the approximate synced position of the pixel eraser.
3. Whenever a point is passed to the shader, sphere-line segment intersection checks are performed between the pixel eraser's sphere of influence and each line segment on every line in the scene (after rough bounds checks are performed). The positions list of the lines are then modified to erase the portions inside the sphere of influence, splitting lines as needed. The position and radius of the erase are added to a queue (the `pixelEraseSyncData` variable of the `Mesh3DPenLine` script) that will later be synced to remote clients.
4. Once the buffer of points passed to the shader is filled to the index specified by the `Pixel Erase Shader Points Buffer Sync Index` property of the `Mesh 3D Pen Line` component, the shader buffer is cleared, the mesh is regenerated from the new positions list, and the queue is synced.
5. Once remote clients receive the queue, they perform the same pixel erases on their local copy of the line positions list. Remote clients' shader queues are used as a circular buffer that wraps back to the start when filled. The queue is only cleared when a pixel erase is finished.
## How to Tweak Behavior
### To add a pixel eraser to the scene
1. Add the `Assets/Prefabs/Work/Mesh Pen Pixel Eraser` prefab to the scene.
2. Assign the `Line Holder` property on the eraser's Pixel Eraser child's `Mesh 3D Pen Pixel Eraser` script to the line holder in the scene.