using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BombRushRadio;

[HarmonyPatch(typeof(AppMusicPlayer), nameof(AppMusicPlayer.OnAppInit))]
public class AppMusicPlayer_Patches
{
    public static AppMusicPlayer instance;
    static void Postfix(AppMusicPlayer __instance)
    {
        instance = __instance;
        
        if (MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentChapter != null)
            MusicPlayer_Patches_RefreshMusicQueueForStage.Refresh((MusicPlayer)__instance.GameMusicPlayer, MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentChapter, MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentStage);

    }
}

[HarmonyPatch(typeof(AppMusicPlayer), nameof(AppMusicPlayer.OnAppDisable))]
public class AppMusicPlayerDisable_Patches
{
    static void Prefix(AppMusicPlayer __instance)
    {
        AppMusicPlayer_Patches.instance = null;
    }
}

[HarmonyPatch(typeof(AppHomeScreen), nameof(AppHomeScreen.AwakeAnimation))]
public class AppMusicPlayerRefresh_Patches
{
    static void Prefix(AppHomeScreen __instance)
    {
        if (MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentChapter != null)
            MusicPlayer_Patches_RefreshMusicQueueForStage.Refresh(BombRushRadio.mInstance, MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentChapter, MusicPlayer_Patches_RefreshMusicQueueForStage.CurrentStage);
    }
}

[HarmonyPatch(typeof(MusicPlayer), MethodType.Constructor, new Type[] { typeof(AudioSource), typeof(MusicTrackQueue)})]
public class MusicPlayer_Patches
{
    static void Postfix(MusicPlayer __instance, AudioSource audioSource, MusicTrackQueue musicTrackQueue) // constructor
    {
        BombRushRadio.mInstance = __instance;
    }
}

[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.RefreshMusicQueueForStage))]
public class MusicPlayer_Patches_RefreshMusicQueueForStage
{
    public static ChapterMusic CurrentChapter;
    public static Stage CurrentStage;

    public static void Refresh(MusicPlayer __instance, ChapterMusic chapterMusic, Stage stage)
    {
         __instance.musicTrackQueue.ClearTracks();

        Story.ObjectiveInfo currentObjectiveInfo = Story.GetCurrentObjectiveInfo();
        if (stage == Stage.hideout)
        {
            MusicTrack musicTrackByID = Core.Instance.AudioManager.MusicLibraryPlayer.GetMusicTrackByID(MusicTrackID.Hideout_Mixtape);
            __instance.AddMusicTrack(musicTrackByID);
            Debug.Log("[BRR] [BASE-GAME] Added " + musicTrackByID.Title + " to the total list.");
        }
        else
        {
            MusicTrack chapterMusic2 = chapterMusic.GetChapterMusic(currentObjectiveInfo.chapter);
            __instance.AddMusicTrack(chapterMusic2);
            Debug.Log("[BRR] [BASE-GAME] Added " + chapterMusic2.Title + " to the total list.");
        }
        AUnlockable[] unlockables = WorldHandler.instance.GetCurrentPlayer().phone.GetAppInstance<AppMusicPlayer>().Unlockables;
        for (int i = 0; i < unlockables.Length; i++)
        {
            MusicTrack musicTrack = unlockables[i] as MusicTrack;
            if (Core.Instance.Platform.User.GetUnlockableSaveDataFor(musicTrack).IsUnlocked)
            {
                musicTrack.isRepeatable = false;
                __instance.AddMusicTrack(musicTrack);
                Debug.Log("[BRR] [BASE-GAME] Added " + musicTrack.Title + " to the total list.");
            }
        }
                    
        foreach (MusicTrack track in BombRushRadio.audios)
        {
            if (__instance.musicTrackQueue.currentMusicTracks.Find(m => m.Title == track.Title && m.Artist == track.Artist) != null)
                continue;

            __instance.musicTrackQueue.currentMusicTracks.Add(track);
            Debug.Log("[BRR] [CUSTOM] Added " + track.Title + " to the total list.");
        }
        
        Debug.Log("[BRR] Finished checking for songs to add...");
        
        if (AppMusicPlayer_Patches.instance != null)
        {
            Debug.Log("[BRR] Refreshing songs on the app...");

            AppMusicPlayer_Patches.instance.RefreshList();
        }
    }
    
    static void Prefix(MusicPlayer __instance, ChapterMusic chapterMusic, MusicTrack trackToPlay, Stage stage)
    {
        CurrentChapter = chapterMusic;
        CurrentStage = stage;
        
        Refresh(__instance, chapterMusic, stage);
        
        __instance.musicTrackQueue.UpdateMusicQueueForStage(trackToPlay);
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

[HarmonyPatch(typeof(MusicTrackQueue), nameof(MusicTrackQueue.SelectNextTrack))]
public class MusicTrackQueue_Patches_SelectNextTrack
{
    static bool Prefix(MusicTrackQueue __instance)
    {
        Debug.Log("[BRR] Finding next track. Amount: "  + __instance.AmountOfTracks);
        return true;
    }
}

[HarmonyPatch(typeof(MusicTrackQueue), nameof(MusicTrackQueue.EvaluateNextTrack))]
public class MusicTrackQueue_Patches_EvaluateNextTrack
{
    static bool Prefix(MusicTrackQueue __instance, int nextTrackIndex)
    {
        Debug.Log("[BRR] Next Track!");
        MusicTrack t = __instance.currentMusicTracks[nextTrackIndex];
        if (BombRushRadio.CacheAudios.Value && !BombRushRadio.PreloadCache.Value)
        {

            if (BombRushRadio.audios.Find(m => m.Title == t.Title && m.Artist == t.Artist) && t.AudioClip == null)
            {
                string cacheName = Helpers.FormatMetadata(new string[] { t.Artist, t.Title }, "dash");

                string[] sp = BombRushRadio.filePaths[cacheName].Split(',');
                t.AudioClip = Helpers.LoadACFromCache(sp[0], sp[1]);
                Debug.Log("[BRR] Loaded cache for " + t.Title + ". Length: " + t.AudioClip.length);
            }
        }

        __instance.currentTrackIndex = nextTrackIndex;
        __instance.BufferTrackAtIndex(nextTrackIndex);
        
        Debug.Log("[BRR] Playing track: " + t.Title + " by " + t.Artist + " (id: " + nextTrackIndex + ")");
        
        return false;
    }
}


[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.UpdateIsPlayingMusic))]
public class MusicPlayer_Patches_UpdateIsPlayingMusic
{
    static void Prefix(MusicPlayer __instance)
    {
    }
}

[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.HandleNoMusicPlayerDataFound))]
public class MusicPlayer_Patches_NoMusicFound
{
    static void Prefix(MusicPlayer __instance)
    {
        Debug.Log("[BRR] Music not found... skipping to next track.");
    }
}


[HarmonyPatch(typeof(MusicPlayer), nameof(MusicPlayer.PlayFrom))]
public class MusicPlayer_Patches_PlayFrom
{
    static void Prefix(MusicPlayer __instance, int index, int playbackSamples = 0)
    {
        __instance.ForcePaused();
        MusicTrack t = __instance.musicTrackQueue.currentMusicTracks[index];
        if (BombRushRadio.CacheAudios.Value && !BombRushRadio.PreloadCache.Value)
        {

            if (BombRushRadio.audios.Find(m => m.Title == t.Title && m.Artist == t.Artist) && t.AudioClip == null)
            {
                string cacheName = Helpers.FormatMetadata(new string[] { t.Artist, t.Title }, "dash");

                string[] sp = BombRushRadio.filePaths[cacheName].Split(',');
                t.AudioClip = Helpers.LoadACFromCache(sp[0], sp[1]);
                Debug.Log("[BRR] Loaded cache for " + t.Title + ". Length: " + t.AudioClip.length);
            }
        }
    }
}
