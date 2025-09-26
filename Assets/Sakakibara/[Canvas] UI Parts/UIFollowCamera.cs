using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform cameraTransform; // VRカメラ（HMD）のTransform
    public float uiDistance = 0.5f; // UIの表示距離

    void Start()
    {
        if (cameraTransform == null)
        {
            // 自動でメインカメラを探す
            cameraTransform = Camera.main?.transform;
        }
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            // UIの位置をカメラの前方に固定
            transform.position = cameraTransform.position + cameraTransform.forward * uiDistance;
            // カメラの方向を向くように回転
            transform.LookAt(cameraTransform);
            transform.Rotate(0, 180, 0);
        }
    }
}