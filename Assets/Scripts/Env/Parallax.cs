using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private float parallaxOffset = -0.5f;

    private Vector2 startPos;
    private Vector2 travel => (Vector2)GameManager.Instance.CurrentActiveCamera.transform.position - startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        transform.position = startPos + travel * parallaxOffset;
    }
}
