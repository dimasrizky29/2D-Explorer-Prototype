using System;
using UnityEngine;

/// <summary>
/// Data "rasa" satu serangan: hit stop, shake, zoom, slow motion, spark, dan durasi dorongan.
/// Diisi per AttackData, dibawa lewat <see cref="DamageInfo"/> ke semua yang bereaksi pada hit.
/// Nilai bawaan = perilaku hit sebelum ada sistem feel.
/// </summary>
[Serializable]
public sealed class HitFeel
{
    [SerializeField] private float hitStop = 0.1f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 0.12f;
    [SerializeField, Range(0f, 1f), Tooltip("0 = getar acak, 1 = menendang searah pukulan lalu memantul.")] private float shakeDirectional;
    [SerializeField, Tooltip("Pecahan: positif = zoom in (0.1 = 10%), negatif = zoom out.")] private float zoomAmount;
    [SerializeField] private float zoomDuration = 0.3f;
    [SerializeField, Range(0f, 1f), Tooltip("Dimulai setelah hit stop selesai.")] private float slowScale = 1f;
    [SerializeField] private float slowDuration;
    [SerializeField] private int sparkCount = 6;
    [SerializeField] private float sparkSize = 1f;
    [SerializeField, Tooltip("0 = pakai durasi bawaan KnockbackReceiver.")] private float knockbackDuration;

    public float HitStop => hitStop;
    public float ShakeDuration => shakeDuration;
    public float ShakeMagnitude => shakeMagnitude;
    public float ShakeDirectional => shakeDirectional;
    public float ZoomAmount => zoomAmount;
    public float ZoomDuration => zoomDuration;
    public float SlowScale => slowScale;
    public float SlowDuration => slowDuration;
    public int SparkCount => sparkCount;
    public float SparkSize => sparkSize;
    public float KnockbackDuration => knockbackDuration;

    public static readonly HitFeel Default = new HitFeel();
}
