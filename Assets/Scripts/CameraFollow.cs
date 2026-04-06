using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        Instance = this;
        _cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    public void SetTarget(Transform target)
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Follow = target;
            _cinemachineCamera.LookAt = target;
        }
    }

    public void ClearTarget()
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Follow = null;
            _cinemachineCamera.LookAt = null;
        }
    }
}
