using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : NetworkBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private GameObject _healthBarRoot;
    [SerializeField] private Slider _fillSlider;

    // ===== Lifecycle =====

    public override void Spawned()
    {
        _stats.OnHealthChanged += HandleHealthChanged;
        UpdateBar(_stats.CurrentHealth, _stats.MaxHealth);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_stats != null)
            _stats.OnHealthChanged -= HandleHealthChanged;
    }

    // ===== Private =====

    private void HandleHealthChanged(float current, float max) => UpdateBar(current, max);

    private void UpdateBar(float current, float max)
    {
        bool damaged = max > 0f && current < max;
        _healthBarRoot.SetActive(damaged);
        _fillSlider.value = max > 0f ? current / max : 0f;
    }
}
