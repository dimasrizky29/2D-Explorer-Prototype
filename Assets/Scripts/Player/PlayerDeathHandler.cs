using UnityEngine;

/// <summary>
/// Matikan input + collider saat mati. Pasang di prefab player.
/// </summary>
public sealed class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private PlayerController controller;
    [SerializeField] private Collider2D[] colliders;

    private void OnEnable()
    {
        if (health == null) return;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        if (controller != null) controller.enabled = false;

        if (colliders == null) return;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }
    }
}
