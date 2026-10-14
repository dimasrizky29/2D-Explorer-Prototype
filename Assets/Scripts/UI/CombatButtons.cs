using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wiring tombol combat + overlay cooldown dash.
/// Attack tanpa cooldown UI: spam sudah diblokir ActionGate di PlayerAttack.
/// </summary>
public sealed class CombatButtons : MonoBehaviour
{
    [SerializeField] private ActionButton attackButton;
    [SerializeField] private ActionButton dashButton;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private PlayerDash dash;
    [SerializeField] private Image dashOverlay;   // Image Type = Filled

    private bool _lastDashReady = true;

    private void OnEnable()
    {
        if (attackButton != null && attack != null) attackButton.OnClicked += attack.TryAttack;
        if (dashButton != null && dash != null) dashButton.OnClicked += dash.TryDash;
    }

    private void OnDisable()
    {
        if (attackButton != null && attack != null) attackButton.OnClicked -= attack.TryAttack;
        if (dashButton != null && dash != null) dashButton.OnClicked -= dash.TryDash;
    }

    private void Update()
    {
        if (dash == null) return;

        // Tulis hanya saat berubah: setter interactable memicu state transition uGUI.
        bool ready = dash.CanDash;
        if (dashButton != null && ready != _lastDashReady)
        {
            dashButton.SetInteractable(ready);
            _lastDashReady = ready;
        }

        if (dashOverlay != null) dashOverlay.fillAmount = dash.CooldownPercent;
    }
}
