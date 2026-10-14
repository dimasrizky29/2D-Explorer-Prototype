using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Reaksi saat entity kena hit: flash sprite + hit stop (freeze global singkat).
/// Memancarkan <see cref="AnyHit"/> supaya CameraShake bisa bergetar untuk hit siapa pun.
/// </summary>
public sealed class HitSystem : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float duration = 0.1f;

    /// <summary>Dipancarkan untuk setiap hit di entity manapun yang punya HitSystem. Vector2 = arah pukulan (dari sumber ke entity, dinormalkan).</summary>
    public static event Action<DamageInfo, Vector2> AnyHit;

    private Color _baseColor;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _baseColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.Damaged += HandleDamaged;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Damaged -= HandleDamaged;
        health.Died -= HandleDied;
    }

    private void HandleDamaged(DamageInfo info)
    {
        FlashAsync(destroyCancellationToken).Forget();
        HitFeel feel = info.Feel ?? HitFeel.Default;
        TimeControl.Request(0f, feel.HitStop);   // hit beruntun memperpanjang, tidak menumpuk
        // Slow motion mulai setelah hit stop selesai, jadi durasinya ditambah hit stop.
        TimeControl.Request(feel.SlowScale, feel.HitStop + feel.SlowDuration);
        AnyHit?.Invoke(info, HitDirection(info));
    }

    // Dari sumber damage ke pusat sprite (bukan pivot kaki), supaya pukulan lurus tetap terbaca horizontal.
    private Vector2 HitDirection(DamageInfo info)
    {
        Vector2 center = spriteRenderer != null ? (Vector2)spriteRenderer.bounds.center : (Vector2)transform.position;
        Vector2 dir = center - info.Source;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    private void HandleDied()
    {
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
    }

    private async UniTaskVoid FlashAsync(CancellationToken ct)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = flashColor;
        try
        {
            // ignoreTimeScale: flash tetap tampil selama hit stop.
            await UniTask.Delay(TimeSpan.FromSeconds(duration), ignoreTimeScale: true, cancellationToken: ct);
        }
        catch (OperationCanceledException) { }

        if (this != null && spriteRenderer != null) spriteRenderer.color = _baseColor;
    }
}
