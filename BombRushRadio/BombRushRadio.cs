﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Reptile;
using UnityEngine;
using UnityEngine.Networking;

namespace BombRushRadio
{
    [BepInPlugin("kade.bombrushradio", "Bomb Rush Radio!", "1.4")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]

    public class BombRushRadio : BaseUnityPlugin
    {
        public static ConfigEntry<bool> CacheAudios = null!;
        public static MusicPlayer mInstance;
        public static List<MusicTrack> audios = new();
        public static Dictionary<string, string> filePaths = new();
        public int shouldBeDone;
        public int done;

        private static List<String> loaded = new();
        
        public static bool inMainMenu = false;
        public static bool loading;

        private readonly string songFolder = Path.Combine(Application.streamingAssetsPath, "Mods", "BombRushRadio", "Songs");
        private readonly string cachePath = Path.Combine(Paths.CachePath, "BombRushRadio");

        public void SanitizeSongs()
        {
            if (Core.Instance == null || Core.Instance.audioManager == null)
                return;

            if (Core.Instance.audioManager.musicPlayer != null)
            {
                List<MusicTrack> toRemove = new List<MusicTrack>();

                foreach (MusicTrack tr in audios)
                {
                    if (loaded.FirstOrDefault(l => l == Helpers.FormatMetadata(new string[] { tr.Artist, tr.Title }, "dash")) == null)
                    {
                        Logger.LogInfo("[BRR] Removed " + tr.Title);
                        mInstance.musicTrackQueue.currentMusicTracks.Remove(tr);
                        toRemove.Add(tr);
                    }
                    else if (!mInstance.musicTrackQueue.currentMusicTracks.Contains(tr))
                    {
                        Logger.LogInfo("[BRR] Added " + tr.Title);
                        Core.Instance.audioManager.musicPlayer.AddMusicTrack(tr);
                    }
                }

                foreach (MusicTrack tr in toRemove)
                {
                    filePaths.Remove(Helpers.FormatMetadata(new string[] { tr.Artist, tr.Title }, "dash"));
                    audios.Remove(tr);
                    tr.AudioClip.UnloadAudioData();
                }

                mInstance.musicTrackQueue.currentMusicTracks.Sort((m,m2) => String.Compare(m.Title, m2.Title, StringComparison.Ordinal));
            }
        }

        public IEnumerator LoadAudioFile(string filePath, AudioType type)
        {
            string[] metadata = Helpers.GetMetadata(filePath, false);

            string songName = Helpers.FormatMetadata(metadata, "dash");
            string validName = String.Concat(songName.Split(Path.GetInvalidFileNameChars()));

            string cacheFile = Path.Combine(cachePath, validName + ".cache");
            string tagFile = Path.Combine(cachePath, validName + ".tag");

            if (!filePaths.ContainsKey(songName))
                filePaths.Add(songName, cacheFile + "," + tagFile);

            if (CacheAudios.Value)
            {
                if (File.Exists(cacheFile)) // cache
                {
                    MusicTrack t = ScriptableObject.CreateInstance<MusicTrack>();
                    t.AudioClip = null;
                    t.Artist = metadata[0];
                    t.Title = metadata[1];
                    t.isRepeatable = false;

                    audios.Add(t);
                    done++;
                    loaded.Add(songName);
                    Logger.LogInfo("[BRR] Cache found for " + Helpers.FormatMetadata(metadata, "dash"));
                    yield break;
                }
            }

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
                    done++;

                    MusicTrack t = ScriptableObject.CreateInstance<MusicTrack>();
                    t.AudioClip = null;
                    t.Artist = metadata[0];
                    t.Title = metadata[1];
                    t.isRepeatable = false;
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www); // this has preloadAudioData on it, which is bad.
                    myClip.name = songName;

                    if (CacheAudios.Value)
                    {
                        int lengthInSamples = myClip.samples * myClip.channels;
                        float[] samples = new float[lengthInSamples];
                        myClip.GetData(samples, 0);

                        File.WriteAllBytes(cacheFile, Helpers.ConvertFloatToByte(samples));
                        File.WriteAllText(tagFile, myClip.samples + "," + myClip.channels + "," + myClip.frequency);
                        myClip.UnloadAudioData();
                        Logger.LogInfo("[BRR] Cached " + Helpers.FormatMetadata(metadata, "dash"));
                    }
                    else
                    {
                        t.AudioClip = myClip;
                    }

                    audios.Add(t);

                    Logger.LogInfo($"[BRR] Loaded {Helpers.FormatMetadata(metadata, "by")} ({done}/{shouldBeDone})");
                    loaded.Add(songName);
                }
            }
        }

        public IEnumerator LoadFile(string f)
        {
            string extension = Path.GetExtension(f).ToLowerInvariant().Substring(1);

            if (extension is "cache" or "tag")
            {
                File.Delete(f); // Remove old cache files
                yield return null;
            }

            string[] metadata = Helpers.GetMetadata(f, false);

            if (audios.Find(m => m.Artist == metadata[0] && m.Title == metadata[1]))
            {
                string songName = Helpers.FormatMetadata(metadata, "dash");
                loaded.Add(songName);
                Logger.LogInfo("[BRR] " + songName + " is already loaded, skipping.");
            }
            else
            {
                AudioType type = AudioType.UNKNOWN;

                switch (extension)
                {
                    case "aif":
                    case "aiff":
                        type = AudioType.AIFF;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "it":
                        type = AudioType.IT;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "mod":
                        type = AudioType.MOD;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "mp2":
                    case "mp3":
                        type = AudioType.MPEG;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "ogg":
                        type = AudioType.OGGVORBIS;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "s3m":
                        type = AudioType.S3M;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "wav":
                        type = AudioType.WAV;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "xm":
                        type = AudioType.XM;
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                    case "flac":
                        shouldBeDone++;
                        StartCoroutine(LoadAudioFile(f, type));
                        break;
                }
            }

            yield return null;
        }

        public IEnumerator SearchDirectories(string path = "")
        {
            string p = path.Length == 0 ? songFolder : path;

            foreach (string f in Directory.GetDirectories(p))
            {
                Logger.LogInfo("[BRR] Searching directory " + f);
                StartCoroutine(SearchDirectories(f));
            }

            foreach(string f in Directory.GetFiles(p))
            {
                StartCoroutine(LoadFile(f));
            }

            yield return null;
        }

        public IEnumerator ReloadSongs()
        {
            loaded.Clear();
            loading = true;

            if (audios.Count > 0)
            {
                if (Core.Instance.audioManager.musicPlayer.IsPlaying && mInstance != null)
                    Core.Instance.audioManager.musicPlayer.ForcePaused();
            }

            Logger.LogInfo("[BRR] Loading songs...");
            shouldBeDone = 0;
            done = 0;

            yield return StartCoroutine(SearchDirectories());

            Logger.LogInfo("[BRR] Bomb Rush Radio has been loaded!");
            loading = false;

            SanitizeSongs();
        }

        private void Awake()
        {
            // setup mod directory
            if (!Directory.Exists(songFolder))
                Directory.CreateDirectory(songFolder);

            // bind to config
            CacheAudios = Config.Bind("Audio", "Caching", false,
                "Caches audios to disc (Pros: Memory is lowered significantly, Any startup load time after the first start is lowered significantly, Cons: Stutters on play (depending on audio size), Caching on disc can be expensive on storage (depending on audio size/format))");

            // setup cache directory
            if (CacheAudios.Value && !Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

            // load em
            StartCoroutine(ReloadSongs());

            var harmony = new Harmony("kade.bombrushradio");
            harmony.PatchAll();
            Logger.LogInfo("[BRR] Patched...");

            Core.OnUpdate += () =>
            {
                if (Input.GetKeyDown(KeyCode.F1) && !inMainMenu) // reload songs
                {
                    if (!CacheAudios.Value)
                        StartCoroutine(ReloadSongs());
                    else
                        Logger.LogWarning("[BRR] Reloading cached audios is not supported atm (im lazy)");
                }
            };
        }
    }
}