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
    [BepInPlugin("kade.bombrushradio", "Bomb Rush Radio!", "1.1.1.0")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    public class BombRushRadio : BaseUnityPlugin
    {
        public static MusicPlayer mInstance;
        public static BombRushRadio Instance = null;
        public static List<MusicTrack> audios = new();
        public int shouldBeDone = 0;
        public int done = 0;

        public static bool inMainMenu = false;
        public static bool loading = false;
        public static bool addOnComplete = false;
        public IEnumerator LoadAudioFile(string filePath, AudioType type)
        {
            string clean = filePath.Split('\\').Last();
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
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    myClip.name = clean;
                    string[] spl = clean.Substring(0,clean.Length - 4).Split('-');
                    string songName = spl[1];
                    string songArtist = spl[0];
                    done++;
      
                    MusicTrack t = ScriptableObject.CreateInstance<MusicTrack>();
                    t.AudioClip = myClip;
                    t.Artist = songArtist;
                    t.Title = songName;
                    t.isRepeatable = false;
                    audios.Add(t);
                    
                    Logger.LogInfo("[BRR] Loaded " + songName + " by " + songArtist + " (" + done + "/" + shouldBeDone + ")");
                    if (done == shouldBeDone)
                    {
                        Logger.LogInfo("[BRR] Bomb Rush Radio has been loaded!");
                        if (addOnComplete)
                        {
                            if (Core.Instance.audioManager.musicPlayer != null)
                            {
                                foreach(MusicTrack tr in audios)
                                    Core.Instance.audioManager.musicPlayer.AddMusicTrack(tr);
                            }

                            addOnComplete = false;
                        }
                        loading = false;
                    }
                }
            }
        }
        
        public MusicBundle bundle;
        internal static ConfigEntry<KeyCode> reloadKey;

        public IEnumerator LoadFile(string f)
        {
            AudioType type = AudioType.UNKNOWN;
            switch (f.Split('.').Last().ToLower())
            {
                case "mp3":
                    type = AudioType.MPEG;
                    break;
                case "wav":
                    type = AudioType.WAV;
                    break;
                case "ogg":
                    type = AudioType.OGGVORBIS;
                    break;
                default:
                    yield return null;
                    break;
            }
            shouldBeDone++;
            StartCoroutine(LoadAudioFile(f, type));
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
        
        public void ReloadSongs()
        {
            loading = true;
            if (audios.Count > 0)
            {
                if (Core.Instance.audioManager.musicPlayer.IsPlaying && mInstance != null)
                {
                    Core.Instance.audioManager.musicPlayer.ForcePaused();
                    foreach (MusicTrack tra in mInstance.musicTrackQueue.currentMusicTracks)
                    {
                        if (audios.Find(m => tra.Artist == m.Artist && m.Title == tra.Title))
                            mInstance.musicTrackQueue.currentMusicTracks.Remove(tra);
                    }
                    addOnComplete = true;
                }

                foreach (MusicTrack t in audios)
                {
                    t.AudioClip.UnloadAudioData();
                }
                audios.Clear();
                
            }
            
            Logger.LogInfo("[BRR] Loading songs...");
            shouldBeDone = 0;
            done = 0;

            StartCoroutine(SearchDirectories());
        }
        

        private void Awake()
        {
            Instance = this;
            // setup mod dirs
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods");
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods/BombRushRadio"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods/BombRushRadio");
            if (!Directory.Exists(Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs");
            
            // load em
            
            ReloadSongs();

            var harmony = new Harmony("kade.bombrushradio");
            harmony.PatchAll();
            Logger.LogInfo("[BRR] Patched...");

            Core.OnUpdate += () =>
            {
                if (Input.GetKeyDown(KeyCode.F1) && !inMainMenu && !loading) // reload songs
                    ReloadSongs();
            };
        }
    }
}