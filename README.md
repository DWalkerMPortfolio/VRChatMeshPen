For a code sample, see [Mesh3DPenLine.cs](/Assets/Scripts/Custom%203D%20Pen/Mesh3DPenLine.cs).
For documentation, see [Obsidian Vault](/Obsidian%20Vault).

This repository is a fully-networked 3D pen system for VRChat developed in Unity. It features multiple colors, no cap on the number of lines drawn, and per-pixel erasing. It was originally developed for Meetspace VR, a project by Michigan State University's GEL and SPARTIE labs and is reproduced here with permission.

The most complex portion of this project is per-pixel erasing which allows portions of lines to be erased without erasing the entire line. At the time of writing, this feature is not present in any other free VRChat pen system. The code responsible for this system can be found starting at [Mesh3DPenLine.cs line 891](/Assets/Scripts/Custom%203D%20Pen/Mesh3DPenLine.cs#L891). Documentation explaining this system's technical functionality can be found in the [Developer Perspective section of Pixel Eraser.md](/Obsidian%20Vault/3D%20Pen/Pixel%20Eraser.md#developer-perspective).

The left and right sides of the GIF below are separate VRChat clients both running on the same local machine.
![VRChat Mesh Pen Demo](https://github.com/user-attachments/assets/2b75abc7-0074-49f9-85ca-3ba74b7f1630)

Credits:
- The contents of the Assets/AssetsCreatedByOtherTeamMembers folder were created by other members of the Meetspace VR development team.
- The contents of the Assets/UdonSharp and Assets/XR folders are from VRChat's SDK.
- The avatar used in the demo GIF above is a fallback avatar provided by VRChat.
- All other scripts, assets, and documentation were created by me.
