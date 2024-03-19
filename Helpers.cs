using System;
using System.IO;
using System.Linq;

namespace BombRushRadio;

public class Helpers
{
    public static string[] GetMetadata(string filePath, bool oldMethod)
    {
        string songName;
        string songArtist = string.Empty;

        if (!oldMethod)
        {
            try
            {
                var tag = TagLib.File.Create(filePath);
                songName = tag.Tag.Title;

                for (int i = 0; i < tag.Tag.Performers.Length; i++)
                {
                    if (i == tag.Tag.Performers.Length - 1)
                    {
                        songArtist += tag.Tag.Performers[i];
                    }
                    else
                    {
                        songArtist += $"{tag.Tag.Performers[i]}, ";
                    }
                }
            }
            catch (Exception)
            {
                return GetMetadata(filePath, true);
            }

            if (string.IsNullOrEmpty(songName))
            {
                return GetMetadata(filePath, true);
            }
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

        if (!string.IsNullOrEmpty(songArtist))
        {
            songArtist = songArtist.Trim();
        }

        return new[] { songArtist, songName.Trim() };
    }

    public static string FormatMetadata(string[] metadata, string type)
    {
        if (string.IsNullOrEmpty(metadata[0]))
        {
            return metadata[1];
        }

        return type switch
        {
            "by" => $"{metadata[1]} by {metadata[0]}",
            _ => $"{metadata[0]} - {metadata[1]}",
        };
    }
}