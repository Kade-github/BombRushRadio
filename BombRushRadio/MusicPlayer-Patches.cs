using HarmonyLib;
using Reptile;
using UnityEngine;

namespace BombRushRadio;

[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.StartMusicPlayer))]
public class MusicPlayer_Patches
{
    static void Prefix(MusicPlayer __instance)
    {
        if (BombRushRadio.inMainMenu)
            return; // don't do it in the mainmenu
        Debug.Log("[BRR] Adding custom music to the game...");
        foreach (MusicTrack track in BombRushRadio.audios)
        {
            __instance.musicTrackQueue.currentMusicTracks.Add(track);
        }

        Debug.Log("[BRR] Line up: ");
        foreach (MusicTrack track in __instance.musicTrackQueue.currentMusicTracks)
        {
            Debug.Log("[BRR] " + track.Title + " by " + track.Artist);
        }
    }
}