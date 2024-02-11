using HarmonyLib;
using Reptile;
using System;
using UnityEngine;

namespace BombRushRadio;

[HarmonyPatch(typeof(StageManager), nameof(StageManager.GetCurrentNonChapterTrackToPlay))]
public class StageManager_Patches_NonChapterTrack
{
    private static void Postfix(int playbackSamples, ref int playbackSamplesToPlay, StageManager __instance, ref MusicTrack __result) {
        if (__result == null) {
            foreach (MusicTrack track in BombRushRadio.Audios) {
                if (track.GetInstanceID() == __instance.baseModule.lastPlayingSongOnStageExistInstanceID) {
                    __result = track;
                    playbackSamplesToPlay = __instance.baseModule.lastPlayingSongOnStageExistSamples;
                    break;
                }
            }
        }
    }
}