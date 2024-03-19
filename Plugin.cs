using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Reptile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace BombRushRadio;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class BombRushRadio : BaseUnityPlugin
{
    public static ConfigEntry<KeyCode> ReloadKey;

    public static MusicPlayer MInstance;
    public static List<MusicTrack> Audios = new();
    public int ShouldBeDone;
    public int Done;

    private static readonly List<string> Loaded = new();

    public static bool InMainMenu = false;
    public static bool Loading;

    private readonly AudioType[] _trackerTypes = new[] { AudioType.IT, AudioType.MOD, AudioType.S3M, AudioType.XM };
    private readonly string _songFolder = Path.Combine(Application.streamingAssetsPath, "Mods", "BombRushRadio", "Songs");

    public void SanitizeSongs()
    {
        if (Core.Instance == null || Core.Instance.audioManager == null)
        {
            return;
        }

        if (Core.Instance.audioManager.musicPlayer != null)
        {
            var toRemove = new List<MusicTrack>();

            int idx = 0;

            foreach (MusicTrack tr in Audios)
            {
                if (MInstance.musicTrackQueue.currentMusicTracks.Contains(tr))
                {
                    MInstance.musicTrackQueue.currentMusicTracks.Remove(tr);
                }
                else
                {
                    Logger.LogInfo("[BRR] Adding " + tr.Title);
                }

                if (Loaded.FirstOrDefault(l => l == Helpers.FormatMetadata(new[] { tr.Artist, tr.Title }, "dash")) == null)
                {
                    Logger.LogInfo("[BRR] Removing " + tr.Title);
                    toRemove.Add(tr);
                }

                MInstance.musicTrackQueue.currentMusicTracks.Insert(1 + idx, tr);
                idx++;
            }

            foreach (MusicTrack tr in toRemove)
            {
                Audios.Remove(tr);
                tr.AudioClip.UnloadAudioData();
            }
        }
    }

    public IEnumerator LoadAudioFile(string filePath, AudioType type)
    {
        string[] metadata = Helpers.GetMetadata(filePath, false);

        string songName = Helpers.FormatMetadata(metadata, "dash");

        // Escape special characters so we don't get an HTML error when we send the request
        filePath = UnityWebRequest.EscapeURL(filePath);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, type))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Logger.LogError(www.error);
            }
            else
            {
                Done++;

                MusicTrack musicTrack = ScriptableObject.CreateInstance<MusicTrack>();
                musicTrack.AudioClip = null;
                musicTrack.Artist = metadata[0];
                musicTrack.Title = metadata[1];
                musicTrack.isRepeatable = false;

                var downloadHandler = (DownloadHandlerAudioClip) www.downloadHandler;
                downloadHandler.streamAudio = !_trackerTypes.Contains(type);

                AudioClip myClip = downloadHandler.audioClip;
                myClip.name = filePath;

                musicTrack.AudioClip = myClip;

                Audios.Add(musicTrack);

                Logger.LogInfo($"[BRR] Loaded {Helpers.FormatMetadata(metadata, "by")} ({Done}/{ShouldBeDone})");
                Loaded.Add(songName);
            }
        }
    }

    public IEnumerator LoadFile(string f)
    {
        string extension = Path.GetExtension(f).ToLowerInvariant().Substring(1);
        string[] metadata = Helpers.GetMetadata(f, false);

        if (Audios.Find(m => m.Artist == metadata[0] && m.Title == metadata[1]))
        {
            string songName = Helpers.FormatMetadata(metadata, "dash");
            Loaded.Add(songName);
            Logger.LogInfo("[BRR] " + songName + " is already loaded, skipping.");
        }
        else
        {
            AudioType type = extension switch
            {
                "aif" => AudioType.AIFF,
                "aiff" => AudioType.AIFF,
                "it" => AudioType.IT,
                "mod" => AudioType.MOD,
                "mp2" => AudioType.MPEG,
                "mp3" => AudioType.MPEG,
                "ogg" => AudioType.OGGVORBIS,
                "s3m" => AudioType.S3M,
                "wav" => AudioType.WAV,
                "xm" => AudioType.XM,
                "flac" => AudioType.UNKNOWN,
                _ => AudioType.UNKNOWN
            };

            ShouldBeDone++;
            StartCoroutine(LoadAudioFile(f, type));
        }

        yield return null;
    }

    public IEnumerator SearchDirectories(string path = "")
    {
        string p = path.Length == 0 ? _songFolder : path;

        foreach (string f in Directory.GetDirectories(p))
        {
            Logger.LogInfo("[BRR] Searching directory " + f);
            StartCoroutine(SearchDirectories(f));
        }

        foreach (string f in Directory.GetFiles(p))
        {
            StartCoroutine(LoadFile(f));
        }

        yield return null;
    }

    public IEnumerator ReloadSongs()
    {
        Loaded.Clear();
        Loading = true;

        if (Audios.Count > 0)
        {
            if (Core.Instance.audioManager.musicPlayer.IsPlaying && MInstance != null)
            {
                Core.Instance.audioManager.musicPlayer.ForcePaused();
            }
        }

        Logger.LogInfo("[BRR] Loading songs...");
        ShouldBeDone = 0;
        Done = 0;

        yield return StartCoroutine(SearchDirectories());

        Logger.LogInfo("[BRR] TOTAL SONGS LOADED: " + Audios.Count);

        Logger.LogInfo("[BRR] Bomb Rush Radio has been loaded!");
        Loading = false;

        Audios.Sort((t, t2) => string.Compare(t.AudioClip.name, t2.AudioClip.name, StringComparison.OrdinalIgnoreCase));

        SanitizeSongs();
    }

    private void Awake()
    {
        // setup mod directory
        if (!Directory.Exists(_songFolder))
        {
            Directory.CreateDirectory(_songFolder);
        }

        // bind to config
        ReloadKey = Config.Bind("Settings", "Reload Key", KeyCode.F1, "Keybind used for reloading songs.");

        // load em
        StartCoroutine(ReloadSongs());

        var harmony = new Harmony("kade.bombrushradio");
        harmony.PatchAll();
        Logger.LogInfo("[BRR] Patched...");

        Core.OnUpdate += () =>
        {
            if (Input.GetKeyDown(ReloadKey.Value) && !InMainMenu) // reload songs
            {
                StartCoroutine(ReloadSongs());
            }
        };
    }
}