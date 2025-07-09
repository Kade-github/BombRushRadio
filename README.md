<p align="center">
  <img src="https://github.com/Kade-github/BombRushRadio/assets/26305836/0ffccadb-8004-437f-8543-5040c219fff2">
</p>

# BombRushRadio
A Bomb Rush Cyberfunk mod that lets you add custom music into the game!

## How to Use

Launch the game, go to your game's local files, navigate to **"Bomb Rush Cyberfunk_Data/StreamingAssets/Mods/BombRushRadio/Songs"** (which would've been created by the mod), then drag your songs into that folder. Make sure it's a supported audio format.

### Supported Audio Formats
- AIF(F)
- IT
- MOD
- MP2
- MP3
- OGG
- S3M
- WAV
- XM
- FLAC

### Folder & Song Formatting

Songs can be stored in the folder itself, or can be put into subfolders for organization. The mod will search through all folders stored in the Songs folder.

BombRushRadio will automatically detect the metadata from the song file. If metadata is not present, the file name will be displayed instead. You can still display the artist separately in the radio app by naming the file with the following format: `Song Name-Artist Name`.

## Reloading

Want to reload songs on the fly? Make some changes (additions, removals, and modifications) in the song folder and press **F1** in-game.

The keybind can be configured in the mod's config.

## Config

```
## Settings file was created by plugin BombRushRadio v1.7
## Plugin GUID: BombRushRadio
[Settings]

## Whether to stream audio from disk or load at runtime (Streaming is faster but more CPU intensive)
# Setting type: Boolean
# Default value: true
Stream Audio = true

## Keybind used for reloading songs.
# Setting type: KeyCode
# Default value: F1
Reload Key = F1

```

## Installation

You can install BombRushRadio via [r2modman](https://thunderstore.io/c/bomb-rush-cyberfunk/p/ebkr/r2modman/), [GaleModManager](https://thunderstore.io/c/bomb-rush-cyberfunk/p/Kesomannen/GaleModManager/), or any other mod manager that integrates Thunderstore.

## Building

Please follow this step in the SlopCrew building file (as we use the exact same method to find the Assembly-CSharp.dll) [here](https://github.com/SlopCrew/SlopCrew/blob/main/docs/Developer%20Guide.md#building-slop-crew), and then open the .csproj.

Make sure to add "https://nuget.bepinex.dev/v3/index.json" as a NuGet source. (in rider here's how it looks)

![image](https://github.com/Kade-github/BombRushRadio/assets/26305836/e128d6c4-debd-4d02-a51b-85b7f8b21517)

Then just build it.
