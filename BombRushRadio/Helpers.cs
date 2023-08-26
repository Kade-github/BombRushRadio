using System;
using System.IO;
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
        
        AudioClip a = AudioClip.Create("cachedAudioAsset", samples.Length, int.Parse(c[1]), int.Parse(c[2]), false);
        a.SetData(samples, 0);
        return a;
    }
}