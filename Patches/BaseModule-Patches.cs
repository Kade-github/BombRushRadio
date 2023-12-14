using HarmonyLib;
using Reptile;

namespace BombRushRadio;

[HarmonyPatch(typeof(BaseModule), nameof(BaseModule.UnloadMainMenuScene))]
public class UnloadMainMenuScenePatch
{
    static void Prefix()
    {
        BombRushRadio.InMainMenu = false;
    }
}

[HarmonyPatch(typeof(BaseModule), nameof(BaseModule.LoadMainMenuScene))]
public class LoadMainMenuScenePatch
{
    static void Prefix()
    {
        BombRushRadio.InMainMenu = true;
    }
}