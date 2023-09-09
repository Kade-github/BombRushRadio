using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BombRushRadio;

public class Helpers
{
    public static byte[] ConvertFloatToByte(float[] f)
    {
        var byteArray = new byte[f.Length * 4];
        Buffer.BlockCopy(f, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    public static float[] ConvertByteToFloat(byte[] b)
    {
        var floatArray = new float[b.Length / 4];
        Buffer.BlockCopy(b, 0, floatArray, 0, b.Length);
        return floatArray;
    }

    public static AudioClip LoadACFromCache(string cacheFile, string tagFile)
    {
        float[] samples = ConvertByteToFloat(File.ReadAllBytes(cacheFile));
        string[] c = File.ReadAllText(tagFile).Split(',');

        AudioClip a = AudioClip.Create("cachedAudioAsset", int.Parse(c[0]), int.Parse(c[1]), int.Parse(c[2]), false);
        a.SetData(samples, 0);
        return a;
    }

    public static string[] GetMetadata(string filePath, bool oldMethod)
    {
        string songName;
        string songArtist = String.Empty;

        if (!oldMethod)
        {
            try
            {
                var tag = TagLib.File.Create(filePath);
                songName = tag.Tag.Title;

                for (int i = 0; i < tag.Tag.Performers.Length; i++)
                {
                    if (i == tag.Tag.Performers.Length - 1)
                        songArtist += tag.Tag.Performers[i];
                    else
                        songArtist += $"{tag.Tag.Performers[i]}, ";
                }
            }
            catch (Exception)
            {
                return GetMetadata(filePath, true);
            }

            if (String.IsNullOrEmpty(songName))
                return GetMetadata(filePath, true);
        }
        else
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (!fileName.Contains("-"))
            {
                songName = fileName;
            }
            else
            {
                songArtist = fileName.Split('-').First();
                songName = fileName.Substring(songArtist.Length + 1);
            }
        }

        if (!String.IsNullOrEmpty(songArtist))
            songArtist = songArtist.Trim();

        return new string[] { songArtist, songName.Trim() };
    }

    public static string FormatMetadata(string[] metadata, string type)
    {
        if (String.IsNullOrEmpty(metadata[0]))
            return metadata[1];

        return type switch
        {
            "by" => $"{metadata[1]} by {metadata[0]}",
            _ => $"{metadata[0]} - {metadata[1]}",
        };
    }
}