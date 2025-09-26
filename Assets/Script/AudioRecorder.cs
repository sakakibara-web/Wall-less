using UnityEngine;
using System.IO;
using System;

public class AudioRecorder : MonoBehaviour
{
    public static AudioRecorder Instance { get; private set; }

    public bool IsRecording { get; private set; }
    private string filePath;
    private AudioClip recordingClip;
    private string microphoneDevice;

    public event Action<string> OnRecordingFinished;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"AudioRecorder: Using microphone device: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("AudioRecorder: No microphone devices found!");
        }
    }

    public void StartRecording(string path)
    {
        if (IsRecording || string.IsNullOrEmpty(microphoneDevice)) return;

        Debug.Log("AudioRecorder: Recording started.");
        IsRecording = true;
        filePath = path;

        // 修正箇所: 録音時間を4分30秒（270秒）に設定しました。
        recordingClip = Microphone.Start(microphoneDevice, false, 270, 44100);
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        Debug.Log("AudioRecorder: Recording stopping.");
        IsRecording = false;

        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);

            float[] data = new float[recordingClip.samples * recordingClip.channels];
            recordingClip.GetData(data, 0);

            if (SaveWav(data, recordingClip.channels, recordingClip.frequency))
            {
                Debug.Log("AudioRecorder: Recording finished. Starting API calls.");
                OnRecordingFinished?.Invoke(filePath);
            }
        }
        else
        {
            Debug.LogError("AudioRecorder: No audio data was recorded.");
        }
    }

    private bool SaveWav(float[] data, int channels, int frequency)
    {
        if (data == null || data.Length == 0)
        {
            Debug.LogError("AudioRecorder: Recorded audio data is empty. Skipping file save.");
            return false;
        }

        try
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    WriteWavHeader(writer, channels, frequency, data.Length);
                    WriteWavData(writer, data);
                }
            }
            Debug.Log($"WAV file saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save WAV file: {e.Message}");
            return false;
        }
    }

    private void WriteWavHeader(BinaryWriter writer, int channels, int frequency, int dataSize)
    {
        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(0);
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(frequency);
        writer.Write(frequency * channels * 2);
        writer.Write((ushort)(channels * 2));
        writer.Write((ushort)16);
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize * 2);
    }

    private void WriteWavData(BinaryWriter writer, float[] data)
    {
        foreach (float sample in data)
        {
            short intSample = (short)(sample * short.MaxValue);
            writer.Write(intSample);
        }

        writer.Seek(4, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 8));
        writer.Seek(40, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 44));
    }
}