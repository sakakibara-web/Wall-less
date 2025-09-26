using UnityEngine;
using Photon.Pun;

public class TestPunObservable : MonoBehaviourPunCallbacks, IPunObservable
{
    // このスクリプトは、VRIKの計算結果を同期するテスト用です。
    // VRIKコンポーネントがアタッチされているオブジェクトにこのスクリプトをアタッチしてください。

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // stream.IsWriting は、データを送信する側のクライアントで true になります。
        if (stream.IsWriting)
        {
            // ログにこのメッセージが表示されれば、PhotonViewがこのメソッドを正常に呼び出していることになります。
            Debug.Log("Serialize is called!");

            // VRIKが計算したボーンの最終的な位置と回転を送信します。
            // transform はこのスクリプトがアタッチされているオブジェクト（ボーン）のものです。
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else // stream.IsReading は、データを受信する側のクライアントで true になります。
        {
            // リモートプレイヤー側ではデータを受信します。
            // 今回のテストではログの出力のみ行います。
            // 実際の適用はPhotonTransformView Classicに任せるため、ここでは行いません。
            Debug.Log("Receive is called!");
            Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
            Quaternion receivedRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}