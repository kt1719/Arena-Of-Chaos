using UnityEngine;

public class SlimeVisual : MonoBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private EnemyPathfinding _pathfinding;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField] private float _hitFlashDuration = 0.1f;

    // ===== Private Variables =====
    private Material _originalMaterial;

    // ===== Lifecycle =====

    private void Awake() {
        _originalMaterial = _spriteRenderer.sharedMaterial;
    }

    private void Update() {
        _spriteRenderer.flipX = _pathfinding.FacingLeft;
    }

    // ===== Public API =====

    public void TriggerHitFlash() {
        CancelInvoke();
        _spriteRenderer.material = _hitFlashMaterial;
        Invoke(nameof(ResetMaterial), _hitFlashDuration);
    }

    // ===== Helpers =====

    private void ResetMaterial() {
        _spriteRenderer.material = _originalMaterial;
    }
}
