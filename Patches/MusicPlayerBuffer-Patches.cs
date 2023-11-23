using HarmonyLib;
using Reptile;
using UnityEngine;

namespace BombRushRadio;

[HarmonyPatch(typeof(MusicPlayerBuffer), nameof(MusicPlayerBuffer.BufferMusicTrack))]
public class MusicPlayerBuffer_BufferMusicTrack_Patches
{
    static bool Prefix(MusicPlayerBuffer __instance, MusicTrack musicTrackToLoad) // the the game to not unload our files please lol
    {
        if (musicTrackToLoad == null || musicTrackToLoad.AudioClip == null)
            return false;

        MusicPlayerData musicPlayerData = __instance.FindMusicPlayerDataByMusicTrack(musicTrackToLoad);

        if (musicPlayerData == null)
            musicPlayerData = __instance.CreateNewMusicPlayerDataObject(musicTrackToLoad);

        __instance.BufferMusicPlayerData(musicPlayerData);
        return false;
    }
}

[HarmonyPatch(typeof(MusicPlayerBuffer), nameof(MusicPlayerBuffer.UnloadMusicPlayerData))]
public class MusicPlayerBuffer_Patches
{
    static bool Prefix(MusicPlayerData musicPlayerData) // the the game to not unload our files please lol
    {
        MusicTrack t = BombRushRadio.Audios.Find(m => musicPlayerData.Artist == m.Artist && musicPlayerData.Title == m.Title);

        if (t != null)
        {
            if (BombRushRadio.CacheAudios.Value && !BombRushRadio.PreloadCache.Value)
            {
                t.AudioClip.UnloadAudioData();
                t.AudioClip = null;
                Debug.Log("[BRR] Unloaded cache for " + t.Title);
            }

            return false;
        }

        return true;
    }
}