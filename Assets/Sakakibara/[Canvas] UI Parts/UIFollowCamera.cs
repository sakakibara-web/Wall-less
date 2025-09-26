using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    public Transform cameraTransform; // VR�J�����iHMD�j��Transform
    public float uiDistance = 0.5f; // UI�̕\������

    void Start()
    {
        if (cameraTransform == null)
        {
            // �����Ń��C���J������T��
            cameraTransform = Camera.main?.transform;
        }
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            // UI�̈ʒu���J�����̑O���ɌŒ�
            transform.position = cameraTransform.position + cameraTransform.forward * uiDistance;
            // �J�����̕����������悤�ɉ�]
            transform.LookAt(cameraTransform);
            transform.Rotate(0, 180, 0);
        }
    }
}