using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Hancurkan GameObject setelah jeda singkat. Pasang di prefab enemy.
/// </summary>
public sealed class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private float destroyDelay = 0.5f;

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
        DespawnAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid DespawnAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(destroyDelay), cancellationToken: ct);
        }
        catch (OperationCanceledException) { }

        if (this != null) Destroy(gameObject);
    }
}
