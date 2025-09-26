using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json; // <-- この行を追加してください

public class OpenAIService : MonoBehaviour
{
    public static OpenAIService Instance { get; private set; }

    // インスペクタから設定
    public string ApiKey;
    public string WhisperApiUrl = "https://api.openai.com/v1/audio/transcriptions";
    public string GptApiUrl = "https://api.openai.com/v1/chat/completions";

    public delegate void SummarizationResult(string summarizedText);
    public event SummarizationResult OnSummarizationResult;

    private readonly HttpClient httpClient = new HttpClient();

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

    void OnDestroy()
    {
        httpClient.Dispose();
    }

    public async Task<string> TranscribeAudio(string filePath)
    {
        Debug.Log("OpenAIService: Transcribing audio file.");
        string url = WhisperApiUrl;

        using (var formData = new MultipartFormDataContent())
        {
            try
            {
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                formData.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));
                formData.Add(new StringContent("whisper-1"), "model");

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);

                HttpResponseMessage response = await httpClient.PostAsync(url, formData);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("OpenAIService: Audio transcription successful.");
                    var jsonResponse = JsonUtility.FromJson<TranscriptionResponse>(responseBody);
                    return jsonResponse.text;
                }
                else
                {
                    Debug.LogError($"OpenAIService: Transcription API Error: {response.ReasonPhrase}");
                    Debug.LogError($"Response: {responseBody}");
                    return "Error: " + response.ReasonPhrase;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OpenAIService: An unexpected error occurred during transcription. Error: {e.Message}");
                return "Error: " + e.Message;
            }
        }
    }

    public async Task SummarizeText(string textToSummarize)
    {
        Debug.Log("OpenAIService: Summarizing text...");
        string url = GptApiUrl;

        var prompt = $"与えられたテキストから、話の要点と最終的な結論のみを抽出して要約してください。話者ごとの発言は統合し、結論を簡潔にまとめてください。テキスト: {textToSummarize}";

        var requestBody = new GptRequest
        {
            model = "gpt-3.5-turbo",
            messages = new Message[]
            {
                new Message { role = "system", content = "あなたは会議の議事録を要約するアシスタントです。" },
                new Message { role = "user", content = prompt }
            }
        };

        string jsonRequestBody = JsonUtility.ToJson(requestBody);

        using (var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json"))
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
                HttpResponseMessage response = await httpClient.PostAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log("OpenAIService: Summarization successful.");
                    var jsonResponse = JsonUtility.FromJson<GptResponse>(responseBody);
                    string summarizedText = jsonResponse.choices[0].message.content;
                    Debug.Log($"OpenAIService: OnSummarizationResultHandler is called with text: {summarizedText}");
                    OnSummarizationResult?.Invoke(summarizedText);
                }
                else
                {
                    Debug.LogError($"OpenAIService: Summarization API Error: {response.ReasonPhrase}");
                    Debug.LogError($"Response: {responseBody}");
                    OnSummarizationResult?.Invoke($"Error: {response.ReasonPhrase}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OpenAIService: An unexpected error occurred during summarization. Error: {e.Message}");
                OnSummarizationResult?.Invoke($"Error: {e.Message}");
            }
        }
    }
}

[System.Serializable]
public class TranscriptionResponse
{
    public string text;
}

[System.Serializable]
public class GptResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

[System.Serializable]
public class GptRequest
{
    public string model;
    public Message[] messages;
}