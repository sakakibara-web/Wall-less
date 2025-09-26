using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerNameDisplay : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshPro textMeshPro;
    private PhotonView photonView;

    void Start()
    {
        // 親のPhotonViewを見つける
        photonView = GetComponentInParent<PhotonView>();

        // PhotonViewが正常に見つかった場合のみ処理を続行
        if (photonView != null && photonView.Owner != null)
        {
            // PCAvatarの場合は名前を非表示にする
            if (photonView.Owner.IsMasterClient)
            {
                if (textMeshPro != null)
                {
                    textMeshPro.text = "";
                }
            }
            else
            {
                UpdateNameDisplay(photonView.Owner.NickName);
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // photonViewがnullでないか確認
        if (photonView != null && targetPlayer == photonView.Owner && changedProps.ContainsKey("NickName"))
        {
            if (textMeshPro != null && !targetPlayer.IsMasterClient)
            {
                UpdateNameDisplay((string)changedProps["NickName"]);
            }
        }
    }

    private void UpdateNameDisplay(string newName)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = newName;
        }
    }
}