using UnityEngine;
using Photon.Pun;
using System.Collections;

public class LocalCameraControlEnforcer : MonoBehaviourPun
{
    private OVRCameraRig vrCameraRig;
    private Vector3 initialSpawnPosition; // アバターの初期Spawn位置を保持
    private float initialYOffset; // アバターのルートと目の高さの初期オフセット

    void Start()
    {
        vrCameraRig = FindObjectOfType<OVRCameraRig>();

        if (photonView.IsMine)
        {
            initialSpawnPosition = this.transform.position;
            StartCoroutine(InitializeForLocalPlayer());
        }
    }

    private IEnumerator InitializeForLocalPlayer()
    {
        while (vrCameraRig == null || vrCameraRig.centerEyeAnchor == null)
        {
            vrCameraRig = FindObjectOfType<OVRCameraRig>();
            yield return null;
        }

        // 修正箇所: 初期オフセットを計算
        // これにより、地面にめり込むことなく、目の高さにカメラリグを配置
        initialYOffset = vrCameraRig.centerEyeAnchor.position.y - this.transform.position.y;

        // VRカメラリグの位置をアバターの初期Spawn位置に合わせる（水平方向）
        // Y座標は初期オフセットで調整
        vrCameraRig.transform.position = new Vector3(initialSpawnPosition.x, initialSpawnPosition.y + initialYOffset, initialSpawnPosition.z);

        Renderer[] renderers = vrCameraRig.centerEyeAnchor.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (vrCameraRig == null || vrCameraRig.centerEyeAnchor == null) return;

            // アバターの水平方向の動きを強制的に停止
            this.transform.position = new Vector3(initialSpawnPosition.x, this.transform.position.y, initialSpawnPosition.z);

            // VRヘッドセットのY軸の回転のみをアバターに適用
            Quaternion headRotation = vrCameraRig.centerEyeAnchor.rotation;
            float yRotation = headRotation.eulerAngles.y;
            this.transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }
}
