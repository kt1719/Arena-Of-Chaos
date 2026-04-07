using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField] private float hitFlashDuration = 0.1f;
    
    private Material _originalMaterial;

    private void Awake()
    {
        _originalMaterial = GetComponent<Renderer>().material;
    }

    public void TriggerHitFlash()
    {
        GetComponent<Renderer>().material = _hitFlashMaterial;
        Invoke(nameof(ResetMaterial), hitFlashDuration);
    }

    private void ResetMaterial()
    {
        GetComponent<Renderer>().material = _originalMaterial;
    }
}
