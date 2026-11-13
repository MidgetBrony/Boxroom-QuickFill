# BOXROOM Quick Fill

A MelonLoader mod that fills the shelf you are looking at with media that is not already spawned anywhere in BOXROOM.

## Use

1. Open **Settings > Mod Settings > BOXROOM Quick Fill**.
2. Choose **What Auto Fill (Media Type)**.
3. When using **Games**, choose which Steam content to include and whether tools and utilities should be excluded, included, or selected on their own.
4. Look directly at a shelf and press **O**.

Quick Fill keeps existing shelf contents, fills only empty space, and places eligible media in A-Z order. It ignores media already on a shelf, placed loose, held by the player, or reserved by a source box.

The media selector includes Games, Albums, Movies / Videos, Books, and installed BR-MediaAPI types. **All shelf-compatible media** is the default.

When **Games** is selected, Quick Fill defaults to **Games only** and **Exclude tools and utilities**. The Steam content selector can include DLC or target DLC by itself. The tools selector recognizes Steam tools, dedicated servers, SDKs, editors, runtimes, and Proton utilities even when Steam labels them as games.

## Build

Build `BoxroomQuickFill.csproj` in Release mode. The mod targets .NET Standard 2.1 and requires ModsPanel.
