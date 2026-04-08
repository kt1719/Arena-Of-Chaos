using UnityEngine;

public class SlimeVisual : MonoBehaviour
{
    [SerializeField] private EnemyPathfinding enemyPathfinding;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField] private float hitFlashDuration = 0.1f;

    private Material _originalMaterial;

    private void Awake()
    {
        _originalMaterial = spriteRenderer.sharedMaterial;
    }

    private void Update()
    {
        if (enemyPathfinding.FacingLeft)
        {
            spriteRenderer.flipX = true;
        } else
        {
            spriteRenderer.flipX = false;
        }
    }

    public void TriggerHitFlash()
    {
        CancelInvoke();
        spriteRenderer.material = _hitFlashMaterial;
        Invoke(nameof(ResetMaterial), hitFlashDuration);
    }

    private void ResetMaterial()
    {
        spriteRenderer.material = _originalMaterial;
    }
}
