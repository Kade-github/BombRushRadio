using HarmonyLib;
using Reptile;

namespace BombRushRadio;

[HarmonyPatch(typeof(MusicPlayerBuffer), nameof(MusicPlayerBuffer.UnloadMusicPlayerData))]
public class MusicPlayerBuffer_Patches
{
    static bool Prefix(MusicPlayerData musicPlayerData) // the the game to not unload our files please lol
    {
        if (BombRushRadio.audios.Find(m => musicPlayerData.Artist == m.Artist && musicPlayerData.Title == m.Title))
            return false;
        return true;
    }
}