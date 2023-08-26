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
    [BepInPlugin("kade.bombrushradio", "Bomb Rush Radio!", "1.3.0.0")]
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

        public void SanitizeSongs()
        {
            if (Core.Instance == null)
                return;
            if (Core.Instance.audioManager == null)
                return;
            if (Core.Instance.audioManager.musicPlayer != null)
            {
                List<MusicTrack> toRemove = new List<MusicTrack>();
                foreach (MusicTrack tr in audios)
                {
                    if (loaded.FirstOrDefault(l => l == tr.Artist + "-" + tr.Title) == null)
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
                    audios.Remove(tr);
                    tr.AudioClip.UnloadAudioData();
                }
                
                mInstance.musicTrackQueue.currentMusicTracks.Sort((m,m2) => String.Compare(m.Title, m2.Title, StringComparison.Ordinal));
            }
        }
        public IEnumerator LoadAudioFile(string filePath, AudioType type)
        {
            string cleanN = filePath;
            string clean = filePath.Split('\\').Last();
            
            string extension = clean.Split('.').Last().ToLower();
            string[] spl = clean.Substring(0,clean.Length - extension.Length - 1).Split('-');
            string songName = spl[1];
            string songArtist = spl[0];

            string fullClean = cleanN.Replace("\\", "/");

            string directory = fullClean.Substring(0, fullClean.LastIndexOf("/", StringComparison.Ordinal));
                    
            string cacheFile = directory + "/" + songArtist + "-" +
                               songName + ".cache";
            string tagFile = directory + "/" + songArtist + "-" +
                             songName + ".tag";
            filePaths.Add(songArtist + "-" + songName, cacheFile + "," + tagFile);
            if (CacheAudios.Value)
            {
                if (File.Exists(cacheFile)) // cache
                {
                    MusicTrack t = ScriptableObject.CreateInstance<MusicTrack>();
                    t.AudioClip = null;
                    t.Artist = songArtist;
                    t.Title = songName;
                    t.isRepeatable = false;
                    audios.Add(t);
                    done++;
                    loaded.Add(songArtist + "-" + songName);
                    Logger.LogInfo("[BRR] Cache found for " + songArtist + " - " + songName);
                    yield break;
                }
            }

            // Escape special characters so we don't get an HTML error when we send the request
            filePath = UnityWebRequest.EscapeURL(filePath);
 
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///"+filePath, type))
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
                    t.Artist = songArtist;
                    t.Title = songName;
                    t.isRepeatable = false;
                    AudioClip
                        myClip = DownloadHandlerAudioClip
                            .GetContent(www); // this has preloadAudioData on it, which is bad.
                    myClip.name = clean;
                    if (CacheAudios.Value)
                    {
                        int lengthInSamples = myClip.samples * myClip.channels;
                        float[] samples = new float[lengthInSamples];
                        myClip.GetData(samples, 0);

                        File.WriteAllBytes(cacheFile, Helpers.ConvertFloatToByte(samples));
                        File.WriteAllText(tagFile,
                            lengthInSamples + "," + myClip.channels + "," + myClip.frequency);
                        myClip.UnloadAudioData();
                        Logger.LogInfo("[BRR] Cached " + t.Artist + " - " + t.name);
                    }
                    else
                    {
                        t.AudioClip = myClip;
                    }                    
                    audios.Add(t);


                    Logger.LogInfo("[BRR] Loaded " + songName + " by " + songArtist + " (" + done + "/" + shouldBeDone + ")");
                    loaded.Add(songArtist + "-" + songName);
                }
            }
        }
        public IEnumerator LoadFile(string f)
        {
            string clean = f.Split('\\').Last();
            string extension = f.Split('.').Last().ToLower();
            if (!clean.Contains("-"))
                Logger.LogError("[BRR] " + clean + " doesn't contain a '-' and will not be loaded!");
            string[] spl = clean.Substring(0,clean.Length - extension.Length - 1).Split('-');
            string songName = spl[1];
            string songArtist = spl[0];
            if (audios.Find(m => m.Artist == songArtist && songName == m.Title) && extension != "cache" && extension != "tag")
            {
                loaded.Add(songArtist + "-" + songName);
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
            string p = path.Length == 0 ? Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs" : path;
            
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
        
        public IEnumerator  ReloadSongs()
        {
            loaded.Clear();
            loading = true;
            if (audios.Count > 0)
            {
                if (Core.Instance.audioManager.musicPlayer.IsPlaying && mInstance != null)
                {
                    Core.Instance.audioManager.musicPlayer.ForcePaused();
                }
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
            // setup mod dirs
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods");
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods/BombRushRadio"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods/BombRushRadio");
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs");
            
            // bind to config

            CacheAudios = Config.Bind("Audio", "Caching", false,
                "Caches audios to disc (Pros: Memory is lowered significantly, Any startup load time after the first start is lowered significantly, Cons: Stutters on play (depending on audio size), Caching on disc can be expensive on storage (depending on audio size/format))");
            
            // load em
            
            StartCoroutine(ReloadSongs());

            var harmony = new Harmony("kade.bombrushradio");
            harmony.PatchAll();
            Logger.LogInfo("[BRR] Patched...");


            bool created = false;
            
            Core.OnUpdate += () =>
            {
                if (Input.GetKeyDown(KeyCode.F1) && !inMainMenu) // reload songs
                {
                    if (!CacheAudios.Value)
                        StartCoroutine(ReloadSongs());
                    else
                    {
                        Logger.LogWarning("[BRR] Reloading cached audios is not supported atm (im lazy)");
                    }
                }
            };
        }
    }
}