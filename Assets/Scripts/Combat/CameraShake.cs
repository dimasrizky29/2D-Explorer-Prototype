using UnityEngine;

/// <summary>
/// Getar kamera saat entity manapun kena hit (player kena musuh, atau musuh kena player).
/// Kekuatan, durasi, dan seberapa searah pukulan diambil dari <see cref="HitFeel"/> hit tersebut.
/// Offset ditulis ke localPosition, jadi aman walau kamera mengikuti pemain sebagai child.
/// </summary>
public sealed class CameraShake : MonoBehaviour
{
    private Vector3 _baseLocalPosition;
    private float _startTime;
    private float _duration;
    private float _magnitude;
    private float _directional;
    private Vector2 _direction;

    private void Awake()
    {
        _baseLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        HitSystem.AnyHit += HandleHit;
    }

    private void OnDisable()
    {
        HitSystem.AnyHit -= HandleHit;
    }

    private float Progress() => _duration > 0f ? (Time.unscaledTime - _startTime) / _duration : 1f;

    private void HandleHit(DamageInfo info, Vector2 direction)
    {
        HitFeel feel = info.Feel ?? HitFeel.Default;

        // Shake yang sedang berjalan dan masih lebih kuat tidak ditimpa hit yang lebih lemah.
        float current = _magnitude * Mathf.Max(0f, 1f - Progress());
        if (feel.ShakeMagnitude < current) return;

        _magnitude = feel.ShakeMagnitude;
        _duration = feel.ShakeDuration;
        _directional = feel.ShakeDirectional;
        _direction = direction;
        _startTime = Time.unscaledTime;
    }

    // LateUpdate: setelah semua pergerakan kamera frame ini.
    // unscaledTime: shake tetap jalan saat hit stop (timeScale 0).
    private void LateUpdate()
    {
        float t = Progress();
        if (t >= 1f)
        {
            if (transform.localPosition != _baseLocalPosition) transform.localPosition = _baseLocalPosition;
            return;
        }

        Vector2 offset = ShakeCurve.Evaluate(_direction, Random.insideUnitCircle, _magnitude, _directional, t);
        transform.localPosition = _baseLocalPosition + (Vector3)offset;
    }
}
