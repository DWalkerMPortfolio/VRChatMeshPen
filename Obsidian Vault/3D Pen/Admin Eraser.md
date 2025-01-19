---
tags:
  - Documentation
  - Documentation/Meeting
---
## Why this Exists
### Desired Change to Player Experience
This eraser allows admins to erase all lines drawn by a given pen. This can be used to remove disruptive drawings or declutter the world faster than a normal eraser.
### Developer Rationale
There are currently 3 separate erasers with different functionality ([[3D Pen#Erasers|see here]]). These may be merged into a single object at some point in the future.
## Aesthetic Inspiration
The eraser resembles a typical pink pencil eraser despite the pen resembling a paintbrush. This is because there is no universally-identifiable object used for erasing paint. The admin eraser additionally has a gold band around it, making it visually distinct from the standard eraser.
## Explanation of Functionality
### Player Perspective
Only admins can spawn or hold the admin eraser. Holding the admin eraser near a pen line highlights it in blue. Pressing the use button then erases the entire line and all other lines drawn by the same pen.
#### VR Specific
N/A
#### Keyboard/Mouse Specific
N/A
### Developer Perspective
While held, the eraser queries every line in the scene to determine which (if any) to target. This collision checking is expensive and is therefore not performed every frame. The collision checking code also avoids the use of square root operations in an attempt to improve performance. The marking is performed using the marking `LineRenderer` of the line ([[3D Pen#Line Renderers|see here]]).

The regular eraser and admin eraser both use the same script. The admin eraser has the `Clear All` property of the `Mesh 3D Pen Eraser` script selected while the normal eraser does not. The admin eraser also has an `Admin Only Pickup` script attached to it.
## How to Tweak Behavior
### To add an admin eraser to the scene:
1. Add the `Assets/Prefabs/Work/Admin Mesh Pen Eraser` prefab to the scene.
2. Assign the `Pen Line Holder` property on the eraser's `Mesh 3D Pen Eraser` script to the line holder in the scene