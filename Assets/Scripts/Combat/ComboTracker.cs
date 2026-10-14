using System;

/// <summary>
/// Posisi dalam rantai combo. Maju hanya saat serangan mengenai; telat, meleset, atau
/// serangan yang tidak pernah melapor (dibatalkan) = rantai kembali ke langkah pertama.
/// Waktu diteruskan pemanggil supaya mudah dites.
/// </summary>
public sealed class ComboTracker
{
    private readonly int _length;
    private int _next;
    private int _playing;
    private float _deadline;

    public ComboTracker(int length)
    {
        _length = Math.Max(1, length);
    }

    /// <summary>Mulai serangan baru; mengembalikan indeks langkah yang dimainkan.</summary>
    public int Begin(float now)
    {
        int step = now < _deadline ? _next : 0;
        _playing = step;
        _next = 0;   // tanpa OnHit, serangan ini dianggap tidak melanjutkan rantai
        return step;
    }

    /// <summary>Serangan yang sedang berjalan mengenai target; langkah berikutnya terbuka selama window detik.</summary>
    public void OnHit(float now, float window)
    {
        _next = _playing + 1 >= _length ? 0 : _playing + 1;
        _deadline = now + window;
    }

    public void OnMiss()
    {
        _next = 0;
    }

    public bool IsFinisher(int step) => step == _length - 1;
}
