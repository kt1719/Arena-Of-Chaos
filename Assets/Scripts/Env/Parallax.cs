using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private float parallaxOffset = -0.5f;

    private Camera mainCamera;
    private Vector2 startPos;
    private Vector2 travel => (Vector2)mainCamera.transform.position - startPos;

    private void Awake()
    {
        mainCamera = Camera.main;

        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        transform.position = startPos + travel * parallaxOffset;
    }
}
