using HarmonyLib;
using Reptile;
using UnityEngine;

namespace BombRushRadio;

[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.StartMusicPlayer))]
public class MusicPlayer_Patches
{
    static void Prefix(MusicPlayer __instance)
    {
        if (BombRushRadio.inMainMenu || BombRushRadio.loading)
            return; // don't do it in the mainmenu

        BombRushRadio.mInstance = __instance;

        Debug.Log("[BRR] Amount of tracks " + __instance.musicTrackQueue.AmountOfTracks);
        
        foreach (MusicTrack track in BombRushRadio.audios)
        {
            if (__instance.musicTrackQueue.currentMusicTracks.Find(m =>
                    m.Title == track.Title && m.Artist == track.Artist))
                continue;
            
            __instance.musicTrackQueue.currentMusicTracks.Add(track);
        }

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

[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.PlayFrom))]
public class MusicPlayer_Patches_PlayFrom
{
    static void Prefix(MusicPlayer __instance, int index, int playbackSamples = 0)
    {
        __instance.ForcePaused();
    }
}