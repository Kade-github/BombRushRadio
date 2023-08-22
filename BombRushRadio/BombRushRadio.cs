using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Reptile;
using UnityEngine;
using UnityEngine.Networking;


namespace BombRushRadio
{
    [BepInPlugin("kade.bombrushradio", "Bomb Rush Radio!", "1.0.0.0")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    public class BombRushRadio : BaseUnityPlugin
    {
        public static List<MusicTrack> audios = new List<MusicTrack>();
        public int shouldBeDone = 0;
        public int done = 0;

        public static bool inMainMenu = true;
        public IEnumerator LoadAudioFile(string filePath)
        {
            string clean = filePath.Split('\\').Last();
            // Escape special characters so we don't get an HTML error when we send the request
            filePath = UnityWebRequest.EscapeURL(filePath);
 
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///"+filePath, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Logger.LogError(www.error);
                }
                else
                {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
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
                        Logger.LogInfo("[BRR] Bomb Rush Radio has been loaded!");
                }
            }
        }
        
        public MusicBundle bundle;

        private void ReloadSongs()
        {
            // maybe in the future let the player reload on the fly, its defn possible. im lazy.
            foreach (MusicTrack t in audios)
            {
                t.AudioClip.UnloadAudioData();
            }
            audios.Clear();
            Logger.LogInfo("[BRR] Loading songs...");
            shouldBeDone = 0;
            done = 0;
            foreach(string f in Directory.GetFiles(Application.streamingAssetsPath + "/Mods/BombRushRadio/Songs"))
            {
                if (!f.EndsWith(".mp3"))
                    continue;
                shouldBeDone++;
                StartCoroutine(LoadAudioFile(f));
            }

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
            
            // load em
            
            ReloadSongs();

            var harmony = new Harmony("kade.bombrushradio");
            harmony.PatchAll();
            Logger.LogInfo("[BRR] Patched...");
        }
    }
}