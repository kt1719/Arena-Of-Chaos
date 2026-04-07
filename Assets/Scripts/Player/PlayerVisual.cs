using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private SpriteRenderer _renderer;

    private Material _originalMaterial;

    private void Awake()
    {
        _originalMaterial = _renderer.sharedMaterial;
    }

    public void TriggerHitFlash()
    {
        CancelInvoke();
        _renderer.material = _hitFlashMaterial;
        Invoke(nameof(ResetMaterial), hitFlashDuration);
    }

    private void ResetMaterial()
    {
        _renderer.material = _originalMaterial;
    }
}
