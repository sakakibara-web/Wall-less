using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.IO;
using TMPro;
using PimDeWitte.UnityMainThreadDispatcher;
using UnityEngine.XR;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using System;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public string pcAvatarPrefabName = "PCAvatarPrefab";
    public string simplePlayerPrefabName = "SimplePlayerPrefab";
    public Vector3 pcAvatarRotation = new Vector3(0, -90, 0);

    public static NetworkManager Instance { get; private set; }

    public TextMeshProUGUI summaryTextUI;
    public GameObject loadingPanel;
    public GameObject cameraRigCanvas;
    public GameObject disconnectedPanel;
    public GameObject quitConfirmationPanel;
    public TextMeshProUGUI disconnectedText;
    public TextMeshProUGUI quitConfirmationText;

    public PhotonView photonView;

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

        photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (cameraRigCanvas != null) cameraRigCanvas.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (disconnectedPanel != null) disconnectedPanel.SetActive(false);

        Debug.Log("NetworkManager: Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();

        // OpenAIServiceのイベント購読
        if (OpenAIService.Instance != null)
        {
            OpenAIService.Instance.OnSummarizationResult += OnSummarizationResultHandler;
        }

        StartCoroutine(WaitForAudioRecorderAndSubscribe());
    }

    private IEnumerator WaitForAudioRecorderAndSubscribe()
    {
        while (CaptureAudioToWav.Instance == null)
        {
            yield return null;
        }

        CaptureAudioToWav.Instance.OnRecordingFinished += OnRecordingFinishedHandler;
        Debug.Log("NetworkManager: Successfully subscribed to AudioRecorder's OnRecordingFinished event.");
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            Debug.Log("NetworkManager: X Button Pressed. Quitting App.");
            OnQuitButtonClicked();
        }
    }

    void OnDestroy()
    {
        if (OpenAIService.Instance != null)
        {
            OpenAIService.Instance.OnSummarizationResult -= OnSummarizationResultHandler;
        }

        if (CaptureAudioToWav.Instance != null)
        {
            CaptureAudioToWav.Instance.OnRecordingFinished -= OnRecordingFinishedHandler;
        }
    }

    public bool IsUIActive()
    {
        return loadingPanel.activeSelf || disconnectedPanel.activeSelf || quitConfirmationPanel.activeSelf;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("NetworkManager: Connected to Master Server.");
        if (ConversationManager.Instance != null)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ConversationManager.Instance.UpdateRecordStatusUI(true, "準備完了");
            });
        }

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 3 };
        PhotonNetwork.JoinOrCreateRoom("MeetingRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"NetworkManager: Joined Room. PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LocalPlayer.NickName = "PC";
        }
        else if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LocalPlayer.NickName = "ゲストさん";
        }
        else if (PhotonNetwork.CurrentRoom.PlayerCount == 3)
        {
            PhotonNetwork.LocalPlayer.NickName = "ホスト";
        }

        Debug.Log($"My Final Nickname is: {PhotonNetwork.LocalPlayer.NickName}");
        photonView.RPC("RequestSpawnPointAndAvatar", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"NetworkManager: Player {newPlayer.NickName} entered the room. Current player count: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    [PunRPC]
    private void RequestSpawnPointAndAvatar(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player != null)
            {
                if (player.NickName == "PC")
                {
                    photonView.RPC("SpawnPlayerAt", RpcTarget.All, actorNumber, new Vector3(-0.92f, 3f, -29f), Quaternion.Euler(pcAvatarRotation), "PCAvatarPrefab");
                }
                else
                {
                    Transform spawnPoint = PlayerSpawnManager.Instance.GetSpawnPointForPlayer(player);
                    if (spawnPoint != null)
                    {
                        photonView.RPC("SpawnPlayerAt", RpcTarget.All, actorNumber, spawnPoint.position, spawnPoint.rotation, "SimplePlayerPrefab");
                    }
                }
            }
        }
    }

    [PunRPC]
    private void SpawnPlayerAt(int actorNumber, Vector3 position, Quaternion rotation, string prefabName)
    {
        if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            GameObject spawnedPlayer = PhotonNetwork.Instantiate(prefabName, position, rotation);

            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (cameraRigCanvas != null) cameraRigCanvas.SetActive(true);

            var voiceRecorder = spawnedPlayer.GetComponentInChildren<Photon.Voice.Unity.Recorder>();
            if (voiceRecorder != null)
            {
                voiceRecorder.RecordWhenJoined = true;
                Debug.Log($"NetworkManager: Successfully enabled PhotonVoiceRecorder for {PhotonNetwork.LocalPlayer.NickName}.");
            }

            // ホストのみ録音ボタンを表示
            if (PhotonNetwork.LocalPlayer.NickName == "ホスト")
            {
                if (ConversationManager.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        ConversationManager.Instance.SetRecordingButtonVisibility(true);
                    });
                }
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("Disconnected with reason {0}", cause);
        if (disconnectedPanel != null && disconnectedText != null)
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (cameraRigCanvas != null) cameraRigCanvas.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
            disconnectedPanel.SetActive(true);
            disconnectedText.text = "サーバーとの接続が切れました。理由: " + cause.ToString();
        }
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnQuitButtonClicked()
    {
        if (quitConfirmationPanel != null)
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (cameraRigCanvas != null) cameraRigCanvas.SetActive(false);
            if (disconnectedPanel != null) disconnectedPanel.SetActive(false);
            quitConfirmationPanel.SetActive(true);
            if (quitConfirmationText != null)
            {
                quitConfirmationText.text = "ありがとうございました。\nアプリを終了します。";
            }
        }
        StartCoroutine(QuitAfterDelay(2.0f));
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    [PunRPC]
    public void OnStartRecordingClicked_RPC(PhotonMessageInfo info)
    {
        Debug.Log($"NetworkManager: Received OnStartRecordingClicked_RPC from {info.Sender.NickName}. Starting local recording.");

        if (ConversationManager.Instance != null)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ConversationManager.Instance.UpdateRecordStatusUI(true, "録音中");
            });
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (CaptureAudioToWav.Instance != null && !CaptureAudioToWav.Instance.IsRecording)
            {
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(Application.persistentDataPath, $"CombinedRecording_{timestamp}.wav");
                CaptureAudioToWav.Instance.StartRecording(filePath);
            }
        }
    }

    [PunRPC]
    public void OnStopRecordingClicked_RPC(PhotonMessageInfo info)
    {
        Debug.Log($"NetworkManager: Received OnStopRecordingClicked_RPC from {info.Sender.NickName}. Stopping local recording and starting API process.");

        if (ConversationManager.Instance != null)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ConversationManager.Instance.UpdateRecordStatusUI(true, "処理中");
            });
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (CaptureAudioToWav.Instance != null && CaptureAudioToWav.Instance.IsRecording)
            {
                CaptureAudioToWav.Instance.StopRecording();
            }
        }
    }

    private async void OnRecordingFinishedHandler(string filePath)
    {
        if (PhotonNetwork.IsMasterClient && OpenAIService.Instance != null)
        {
            Debug.Log("NetworkManager (Master): Recording finished. Starting API calls.");
            string transcribedResult = await OpenAIService.Instance.TranscribeAudio(filePath);
            if (!string.IsNullOrEmpty(transcribedResult) && !transcribedResult.Contains("Error:"))
            {
                await OpenAIService.Instance.SummarizeText(transcribedResult);
            }
        }
    }

    private void OnSummarizationResultHandler(string summarizedText)
    {
        Debug.Log($"NetworkManager: OnSummarizationResultHandler is called with text: {summarizedText}");

        try
        {
            // 修正箇所: UIを直接更新する代わりに、RPCを呼び出す
            if (photonView != null)
            {
                photonView.RPC("UpdateSummaryText", RpcTarget.All, summarizedText);
            }

            // 録音ステータスUIの更新は直接行っても問題ありません
            if (ConversationManager.Instance != null)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ConversationManager.Instance.UpdateRecordStatusUI(true, "準備完了");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnSummarizationResultHandler: {ex.Message}");
        }
    }

    [PunRPC]
    public void UpdateSummaryText(string text)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (summaryTextUI != null)
            {
                summaryTextUI.text = $"要点: {text}";
            }
        });
    }

    public void RequestUpdateSummaryText(string text)
    {
        if (photonView != null)
        {
            photonView.RPC("UpdateSummaryText", RpcTarget.All, text);
        }
    }
}