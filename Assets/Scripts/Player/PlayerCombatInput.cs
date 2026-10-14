using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input keyboard/gamepad untuk combat. UI button pakai CombatButtons,
/// keduanya memanggil TryAttack / TryDash yang sama.
/// </summary>
public sealed class PlayerCombatInput : MonoBehaviour
{
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private PlayerDash dash;

    private void OnEnable()
    {
        Subscribe(attackAction, HandleAttack);
        Subscribe(dashAction, HandleDash);
    }

    private void OnDisable()
    {
        Unsubscribe(attackAction, HandleAttack);
        Unsubscribe(dashAction, HandleDash);
    }

    private static void Subscribe(InputActionReference reference, Action<InputAction.CallbackContext> handler)
    {
        if (reference == null) return;
        reference.action.performed += handler;
        reference.action.Enable();
    }

    // Sengaja tidak Disable(): action itu milik asset, dan mematikannya di sini
    // akan mematikan juga pemakai lain di asset yang sama.
    private static void Unsubscribe(InputActionReference reference, Action<InputAction.CallbackContext> handler)
    {
        if (reference == null) return;
        reference.action.performed -= handler;
    }

    private void HandleAttack(InputAction.CallbackContext ctx)
    {
        if (attack != null) attack.TryAttack();
    }

    private void HandleDash(InputAction.CallbackContext ctx)
    {
        if (dash != null) dash.TryDash();
    }
}
