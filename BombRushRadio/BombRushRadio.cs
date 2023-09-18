using System;
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
        public static ConfigEntry<bool> PreloadCache = null!;
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

                int idx = 0;

                foreach (MusicTrack tr in audios)
                {
                    if (mInstance.musicTrackQueue.currentMusicTracks.Contains(tr))
                        mInstance.musicTrackQueue.currentMusicTracks.Remove(tr);
                    else
                        Logger.LogInfo("[BRR] Adding " + tr.Title);

                    if (loaded.FirstOrDefault(l => l == Helpers.FormatMetadata(new string[] { tr.Artist, tr.Title }, "dash")) == null)
                    {
                        Logger.LogInfo("[BRR] Removing " + tr.Title);
                        toRemove.Add(tr);
                    }

                    mInstance.musicTrackQueue.currentMusicTracks.Insert(1 + idx, tr);
                    idx++;
                }

                foreach (MusicTrack tr in toRemove)
                {
                    filePaths.Remove(Helpers.FormatMetadata(new string[] { tr.Artist, tr.Title }, "dash"));
                    audios.Remove(tr);

                    if (!CacheAudios.Value || (CacheAudios.Value && PreloadCache.Value))
                        tr.AudioClip.UnloadAudioData();
                }
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
                    t.AudioClip = PreloadCache.Value ? Helpers.LoadACFromCache(cacheFile, tagFile) : null;
                    t.Artist = metadata[0];
                    t.Title = metadata[1];
                    t.isRepeatable = false;

                    if (t.AudioClip != null)
                        t.AudioClip.name = filePath;

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
                    myClip.name = filePath;

                    if (CacheAudios.Value)
                    {
                        int lengthInSamples = myClip.samples * myClip.channels;
                        float[] samples = new float[lengthInSamples];
                        myClip.GetData(samples, 0);

                        File.WriteAllBytes(cacheFile, Helpers.ConvertFloatToByte(samples));
                        File.WriteAllText(tagFile, myClip.samples + "," + myClip.channels + "," + myClip.frequency);

                        if (PreloadCache.Value)
                            t.AudioClip = myClip;
                        else
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

            if (!CacheAudios.Value || (CacheAudios.Value && PreloadCache.Value))
                audios.Sort((t, t2) => String.Compare(t.AudioClip.name, t2.AudioClip.name, StringComparison.Ordinal));

            SanitizeSongs();
        }

        private void Awake()
        {
            // setup mod directory
            if (!Directory.Exists(songFolder))
                Directory.CreateDirectory(songFolder);

            // bind to config
            CacheAudios = Config.Bind("Audio", "Caching", false,
                "Caches audio to disk.\nPros: Memory is lowered significantly, any boot time after the first start is lowered significantly.\nCons: Stutters on play (depending on audio size), caching on disk can be expensive on storage. (depending on audio size/format)");

            PreloadCache = Config.Bind("Audio", "PreloadCache", false,
                "Preloads cached audio from disk.\nCauses slightly longer boot with memory usage increasing like without cache, but prevents stuttering when a song plays.\nRequires Caching to be enabled.");

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
                    StartCoroutine(ReloadSongs());
            };
        }
    }
}