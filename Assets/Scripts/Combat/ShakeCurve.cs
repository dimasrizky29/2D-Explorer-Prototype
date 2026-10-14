using UnityEngine;

/// <summary>
/// Offset kamera saat shake. Bagian terarah menendang searah pukulan lalu memantul bolak-balik
/// (cos), bagian acak adalah getar biasa; keduanya mengecil linear sampai nol.
/// </summary>
public static class ShakeCurve
{
    private const float Cycles = 2f;   // jumlah bolak-balik dalam satu shake

    /// <param name="direction">Arah pukulan, sudah dinormalkan.</param>
    /// <param name="noise">Vektor acak (mis. Random.insideUnitCircle).</param>
    /// <param name="directional">0 = murni acak, 1 = murni searah pukulan.</param>
    /// <param name="t">Progres 0..1 dari durasi shake.</param>
    public static Vector2 Evaluate(Vector2 direction, Vector2 noise, float magnitude, float directional, float t)
    {
        if (t >= 1f) return Vector2.zero;

        float falloff = 1f - Mathf.Max(0f, t);
        float swing = Mathf.Cos(2f * Mathf.PI * Cycles * t);
        Vector2 kick = direction * (swing * directional);
        Vector2 jitter = noise * (1f - directional);
        return (kick + jitter) * (magnitude * falloff);
    }
}
