using UnityEngine;
using UnityEngine.UI;

public sealed class HealthBarView : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Image fill;

    private void OnEnable()
    {
        if (health == null) return;
        health.Changed += HandleChanged;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Changed -= HandleChanged;
    }

    private void HandleChanged(int current, int max)
    {
        if (fill == null || max <= 0) return;
        fill.fillAmount = (float)current / max;
    }
}
