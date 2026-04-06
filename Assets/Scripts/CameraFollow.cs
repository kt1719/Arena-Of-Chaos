using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    private Transform _target;

    private void Awake()
    {
        Instance = this;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void ClearTarget()
    {
        _target = null;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 pos = transform.position;
        pos.x = _target.position.x;
        pos.y = _target.position.y;
        transform.position = pos;
    }
}
