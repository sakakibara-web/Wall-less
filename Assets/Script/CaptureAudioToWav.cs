using UnityEngine;
using System;
using System.IO;

[RequireComponent(typeof(AudioListener))]
public class CaptureAudioToWav : MonoBehaviour
{
    // `AudioRecorder`と同様にシングルトンとしてアクセス可能
    public static CaptureAudioToWav Instance { get; private set; }

    // `AudioRecorder`と同様に録音状態を外部から確認可能
    public bool IsRecording { get; private set; }

    // `AudioRecorder`と同様に録音完了イベントを追加
    public event Action<string> OnRecordingFinished;

    [SerializeField] private bool _muteAudio = false;

    private string _filePath;
    private float[] _recordingBuffer;
    private int _bufferIndex;

    // オーディオ設定をプロパティで取得
    private int SampleRate => AudioSettings.outputSampleRate;
    private int ChannelCount => GetUnityAudioChannelCount();

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

    /// <summary>
    /// 録音を開始します。（AudioRecorderのメソッドシグネチャに合わせる）
    /// </summary>
    /// <param name="path">保存するWAVファイルのパス</param>
    public void StartRecording(string path)
    {
        if (IsRecording)
        {
            Debug.LogWarning("CaptureAudioToWav: Already recording. Please stop the current recording first.");
            return;
        }

        // AudioRecorderと同じ270秒の最大録音時間を設定
        const int maxRecordingSeconds = 270;

        IsRecording = true;
        _filePath = path;

        // 最大録音時間に基づきバッファサイズを確保
        int bufferSize = SampleRate * ChannelCount * maxRecordingSeconds;
        _recordingBuffer = new float[bufferSize];
        _bufferIndex = 0;

        Debug.Log($"CaptureAudioToWav: Recording started. Max length: {maxRecordingSeconds}s");
    }

    /// <summary>
    /// 録音を終了し、WAVファイルを保存します。（AudioRecorderのメソッドシグネチャに合わせる）
    /// </summary>
    public void StopRecording()
    {
        if (!IsRecording)
        {
            Debug.LogWarning("CaptureAudioToWav: Not recording. Cannot stop.");
            return;
        }

        IsRecording = false;

        // 実際に録音されたデータのみを抽出
        float[] recordedData = new float[_bufferIndex];
        Array.Copy(_recordingBuffer, recordedData, _bufferIndex);

        // WAVファイルとして保存
        if (SaveWav(recordedData, ChannelCount, SampleRate))
        {
            Debug.Log($"CaptureAudioToWav: Recording finished. File saved to: {_filePath}");
            // AudioRecorderと同様に、録音完了イベントを呼び出す
            OnRecordingFinished?.Invoke(_filePath);
        }
        else
        {
            Debug.LogError("CaptureAudioToWav: Failed to save WAV file.");
        }

        // バッファを解放
        _recordingBuffer = null;
    }

    // AudioListenerがオーディオデータを処理するたびに呼び出される
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // 録音中であるか、かつ_recordingBufferが初期化されているかを確認
        // このチェックがNullReferenceExceptionを防止する最も重要な部分です。
        if (!IsRecording || _recordingBuffer == null)
        {
            return;
        }

        // ここから元のロジック
        // バッファの容量を超えないか確認
        if (_bufferIndex + data.Length <= _recordingBuffer.Length)
        {
            // データをバッファにコピー
            Array.Copy(data, 0, _recordingBuffer, _bufferIndex, data.Length);
            _bufferIndex += data.Length;
        }
        else
        {
            // バッファがいっぱいになった場合
            Debug.LogWarning("CaptureAudioToWav: Recording buffer is full. Stopping automatically.");
            StopRecording();
        }

        // `_muteAudio`が有効な場合、元のオーディオを消音
        if (_muteAudio)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0f;
            }
        }
    }

    // --- 以下はWAVファイル保存のためのヘルパーメソッド ---
    // (変更なし)

    private bool SaveWav(float[] data, int channels, int frequency)
    {
        if (data == null || data.Length == 0)
        {
            Debug.LogError("Audio data is empty. Skipping file save.");
            return false;
        }

        try
        {
            using (FileStream fileStream = new FileStream(_filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                WriteWavHeader(writer, channels, frequency, data.Length);
                WriteWavData(writer, data);
            }
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
        writer.Write(0); // ファイルサイズ
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // フォーマットチャンクサイズ
        writer.Write((ushort)1); // PCMフォーマット
        writer.Write((ushort)channels);
        writer.Write(frequency);
        writer.Write(frequency * channels * 2); // ByteRate
        writer.Write((ushort)(channels * 2)); // BlockAlign
        writer.Write((ushort)16); // BitsPerSample
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize * 2); // データサイズ
    }

    private void WriteWavData(BinaryWriter writer, float[] data)
    {
        foreach (float sample in data)
        {
            short intSample = (short)(sample * short.MaxValue);
            writer.Write(intSample);
        }

        // ファイルサイズを更新
        writer.Seek(4, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 8));
        // データチャンクサイズを更新
        writer.Seek(40, SeekOrigin.Begin);
        writer.Write((int)(writer.BaseStream.Length - 44));
    }

    // UnityのAudioSettingsからチャンネル数を取得するヘルパーメソッド
    private int GetUnityAudioChannelCount()
    {
        // UnityのAudioSettings.speakerModeからチャンネル数を取得
        switch (AudioSettings.speakerMode)
        {
            case AudioSpeakerMode.Mono:
                return 1;
            case AudioSpeakerMode.Stereo:
                return 2;
            case AudioSpeakerMode.Quad:
                return 4;
            case AudioSpeakerMode.Surround:
                return 5;
            case AudioSpeakerMode.Prologic:
                return 6;
            // case AudioSpeakerMode.Surround7Point1:
            //     return 8;
            default:
                return 2;
        }
    }
}