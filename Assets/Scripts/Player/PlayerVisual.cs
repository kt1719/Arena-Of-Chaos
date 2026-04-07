using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField] private float hitFlashDuration = 0.1f;
    
    private Renderer[] _renderers;
    private Material[] _originalMaterials;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _originalMaterials = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMaterials[i] = _renderers[i].material;
        }
    }

    public void TriggerHitFlash()
    {
        CancelInvoke();
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].material = _hitFlashMaterial;
        }
        Invoke(nameof(ResetMaterials), hitFlashDuration);
    }

    private void ResetMaterials()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].material = _originalMaterials[i];
        }
    }
}
