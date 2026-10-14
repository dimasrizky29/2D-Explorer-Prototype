using UnityEngine;

/// <summary>
/// Bentuk satu denyut zoom: naik cepat ke puncak, lalu kembali halus ke nol.
/// </summary>
public static class ZoomCurve
{
    private const float PeakAt = 0.15f;   // porsi durasi untuk naik ke puncak

    /// <param name="amount">Pecahan zoom di puncak (negatif = zoom out).</param>
    /// <param name="t">Progres 0..1 dari durasi zoom.</param>
    public static float Evaluate(float amount, float t)
    {
        if (t <= 0f || t >= 1f) return 0f;
        if (t < PeakAt) return amount * (t / PeakAt);

        float u = (t - PeakAt) / (1f - PeakAt);
        return amount * (1f - u * u * (3f - 2f * u));   // smoothstep turun
    }
}
