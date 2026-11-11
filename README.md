# BOXROOM Quick Fill

A MelonLoader mod that fills the shelf you are looking at with media that is not already spawned anywhere in BOXROOM.

## Use

1. Open **Settings > Mod Settings > BOXROOM Quick Fill**.
2. Choose **What Auto Fill (Media Type)**.
3. Look directly at a shelf and press **O**.

Quick Fill keeps existing shelf contents, fills only empty space, and places eligible media in A-Z order. It ignores media already on a shelf, placed loose, held by the player, or reserved by a source box.

The media selector includes Games, Albums, Movies / Videos, Books, and installed BR-MediaAPI types. **All shelf-compatible media** is the default.

## Build

Build `BoxroomQuickFill.csproj` in Release mode. The mod targets .NET Standard 2.1 and requires ModsPanel.
