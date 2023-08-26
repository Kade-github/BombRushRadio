using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
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

        foreach (MusicTrack track in BombRushRadio.audios)
        {
            if (__instance.musicTrackQueue.currentMusicTracks.Find(m =>
                    m.Title == track.Title && m.Artist == track.Artist))
                continue;
            
            __instance.musicTrackQueue.currentMusicTracks.Add(track);
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
        if (BombRushRadio.CacheAudios.Value)
        {
            MusicTrack t = __instance.musicTrackQueue.currentMusicTracks[index];
            if (BombRushRadio.audios.Find(m => m.Title == t.Title && m.Artist == t.Artist))
            {
                string[] sp = BombRushRadio.filePaths[t.Artist + "-" + t.Title].Split(',');
                t.AudioClip = Helpers.LoadACFromCache(sp[0], sp[1]);
                Debug.Log("[BRR] Loaded cache for " + t.Title);
            }
        }
    }
}
