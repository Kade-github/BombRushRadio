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

        foreach (MusicTrack track in BombRushRadio.audios)
        {
            if (__instance.musicTrackQueue.currentMusicTracks.Find(m =>
                    m.Title == track.Title && m.Artist == track.Artist))
                continue;
            
            __instance.musicTrackQueue.currentMusicTracks.Add(track);
        }
        
        __instance.musicTrackQueue.BufferTracksInQueue();

        Debug.Log("[BRR] Line up: ");
        int i = 0;
        foreach (MusicTrack track in __instance.musicTrackQueue.currentMusicTracks)
        {
            i++;
            Debug.Log("[BRR] #" + i + " " + track.Title + " by " + track.Artist);
        }
    }
}

[HarmonyPatch(typeof(MusicTrackQueue), nameof(MusicTrackQueue.HasMusicTrack))]
public class MusicTrackQueue_Patches
{
    static bool Prefix(MusicTrack musicTrack) // ignore unlocking for custom stuff
    {
        if (BombRushRadio.audios.Find(m => musicTrack.Artist == m.Artist && musicTrack.Title == m.Title))
            return false;
        return true;
    }
}