# DFU-DungeonTextureReplacer
Helper classes for replacing Daggerfall Unity dungeon textures.

Provides functions to register global textures, specific texture and door textures.

**Global Textures**

These are replacments for ALL dungeon textures, including props. If you don't want that use specific textures.

**Specific Textures**

Are registered with `AddArchiveRule()`. Replaces a specific record from a DFU archive. Can replace all textures of an archive or just exactly one.

You can use the `DebugDumpTextures(@"C:\Temp\DTRDump")` method to dump all textures of a specific dungeon in runtime. This way you can see what archive textures are used.

**Door Textures**

Are registered with `RegisterDoorTextures(params)`. Door textures get replaced randomly inside the dungeon.

## Usage

For a full example see the `TestMod` folder. Clone this whole repo into `Assets/Game/Mods/` in your Unity project.

## Idea

This is a base class to enable modders to replace dungeon textures on the fly. In the future I will probably add a height based texture blending mode, meaning you can register up to 3 texture sets, which blend, based on their position in the dungeon. E.G. lower floors get sewer sets, mid, normal stone, and top marbel textures.

You can define a texture pack via code and apply it to any dungeon or specific dungeons. Someone could create a mod, that allows to add more dungeon texture archives to be used for any dungeon or modify a specific main story dungeon only with a custom tailored texture pack.

## Contribute

Feel free to optimize or add your own ideas as a PR



