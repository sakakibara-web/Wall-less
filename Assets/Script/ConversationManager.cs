using UnityEngine;
using TMPro;
using System.IO;
using Photon.Pun;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections;

public class ConversationManager : MonoBehaviourPun
{
    public GameObject recordButtonParent;
    public GameObject recordButton;
    public GameObject stopButton;
    public TextMeshProUGUI statusTextUI;
    public static ConversationManager Instance { get; private set; }

    public TextMeshProUGUI summaryTextUI;
    public float scrollSpeed = 0.5f;

    // スクロール検知用のフィードバック機能
    // フィードバックImageと関連変数を削除しました。
    private Coroutine feedbackCoroutine;

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
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.NickName == "ホスト")
        {
            SetRecordingButtonVisibility(true);
        }
        else
        {
            SetRecordingButtonVisibility(false);
        }
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.NickName == "ホスト")
        {
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                OnStartRecordingClicked();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                OnStopRecordingClicked();
            }

            HandleUIScroll();
        }
    }

    public void OnStartRecordingClicked()
    {
        Debug.Log("ConversationManager: UI Start Recording button clicked.");
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("ConversationManager: Master client starting local recording.");
            if (CaptureAudioToWav.Instance != null && !CaptureAudioToWav.Instance.IsRecording)
            {
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(Application.persistentDataPath, $"CombinedRecording_{timestamp}.wav");
                CaptureAudioToWav.Instance.StartRecording(filePath);
            }
        }
        else
        {
            Debug.Log("ConversationManager: Sending RPC to start recording on master client.");
            NetworkManager.Instance.photonView.RPC("OnStartRecordingClicked_RPC", RpcTarget.All);
        }
        UpdateRecordStatusUI(true, "録音中");
    }

    public void OnStopRecordingClicked()
    {
        Debug.Log("ConversationManager: UI Stop Recording button clicked.");
        if (PhotonNetwork.IsMasterClient)
        {
            if (CaptureAudioToWav.Instance != null && CaptureAudioToWav.Instance.IsRecording)
            {
                CaptureAudioToWav.Instance.StopRecording();
            }
        }
        else
        {
            NetworkManager.Instance.photonView.RPC("OnStopRecordingClicked_RPC", RpcTarget.All);
        }
        UpdateRecordStatusUI(true, "処理中");
    }

    public void SetRecordingButtonVisibility(bool isVisible)
    {
        if (recordButtonParent != null)
        {
            recordButtonParent.SetActive(isVisible);
        }
        if (recordButton != null)
        {
            recordButton.SetActive(isVisible);
        }
        if (stopButton != null)
        {
            stopButton.SetActive(isVisible);
        }
    }

    public void UpdateRecordStatusUI(bool isVisible, string statusText)
    {
        if (statusTextUI != null)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                statusTextUI.text = statusText;
                statusTextUI.gameObject.SetActive(isVisible);
            });
        }
    }

    // スクロール機能
    private void HandleUIScroll()
    {
        if (summaryTextUI == null) return;

        float stickY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

        if (Mathf.Abs(stickY) > 0.1f)
        {
            RectTransform rectTransform = summaryTextUI.GetComponent<RectTransform>();
            Vector2 newPosition = rectTransform.anchoredPosition;
            newPosition.y += stickY * scrollSpeed * 100 * Time.deltaTime;

            float textHeight = summaryTextUI.preferredHeight;
            float maskHeight = rectTransform.rect.height;

            if (textHeight > maskHeight)
            {
                float minY = -(textHeight - maskHeight);
                newPosition.y = Mathf.Clamp(newPosition.y, minY, 0);
            }
            else
            {
                newPosition.y = 0;
            }

            rectTransform.anchoredPosition = newPosition;
        }
    }
}