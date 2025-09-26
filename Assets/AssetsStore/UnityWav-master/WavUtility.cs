using UnityEngine;
using System;
using System.IO;

// This class is based on a common WavUtility implementation found in Unity communities/forums.
// It helps to convert an AudioClip into a WAV file.
public static class WavUtility
{
    private const int HEADER_SIZE = 44; // Standard WAV header size

    /// <summary>
    /// Converts an AudioClip to a WAV file and saves it to the specified path.
    /// </summary>
    /// <param name="clip">The AudioClip to convert.</param>
    /// <param name="filepath">The full path where the WAV file will be saved.</param>
    /// <returns>True if the file was successfully saved, false otherwise.</returns>
    public static bool FromAudioClip(AudioClip clip, string filepath)
    {
        try
        {
            using (var fileStream = CreateEmptyWavFile(filepath))
            {
                ConvertAndWrite(fileStream, clip);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"WavUtility: Error saving WAV file '{filepath}': {e.Message}");
            return false;
        }
    }

    private static FileStream CreateEmptyWavFile(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        // Write dummy WAV header for now, will be updated later
        byte emptyByte = new byte();
        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }
        return fileStream;
    }

    private static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert floats to 16-bit integers
        Int16[] intSamples = new Int16[samples.Length];
        byte[] bytes = new byte[samples.Length * 2]; // 2 bytes per 16-bit sample

        int byteIndex = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            // Convert float to 16-bit PCM: range [-1, 1] maps to [-32768, 32767]
            intSamples[i] = (Int16)(samples[i] * Int16.MaxValue);

            // Write bytes in little-endian order (standard for WAV)
            bytes[byteIndex++] = (byte)(intSamples[i] & 0xFF);         // Low byte
            bytes[byteIndex++] = (byte)((intSamples[i] >> 8) & 0xFF);   // High byte
        }

        // Write the actual audio data
        fileStream.Write(bytes, 0, bytes.Length);

        // Now, update the WAV header with correct sizes
        WriteHeader(fileStream, clip, bytes.Length);
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip, int samplesLengthBytes)
    {
        fileStream.Seek(0, SeekOrigin.Begin); // Go back to the beginning of the file to write header

        int totalLength = samplesLengthBytes + HEADER_SIZE - 8; // Total file size - 8 bytes (for chunkID and chunkSize)

        // RIFF chunk
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        fileStream.Write(BitConverter.GetBytes(totalLength), 0, 4);
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);

        // fmt chunk
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        fileStream.Write(BitConverter.GetBytes(16), 0, 4);      // Subchunk1Size (16 for PCM)
        fileStream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // AudioFormat (1 for PCM)
        fileStream.Write(BitConverter.GetBytes((ushort)clip.channels), 0, 2); // NumChannels
        fileStream.Write(BitConverter.GetBytes(clip.frequency), 0, 4); // SampleRate
        fileStream.Write(BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, 4); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        fileStream.Write(BitConverter.GetBytes((ushort)(clip.channels * 2)), 0, 2); // BlockAlign (NumChannels * BitsPerSample/8)
        fileStream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // BitsPerSample (16-bit)

        // data chunk
        fileStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        fileStream.Write(BitConverter.GetBytes(samplesLengthBytes), 0, 4); // DataSize
    }
}