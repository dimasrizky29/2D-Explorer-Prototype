using System;

/// <summary>
/// Berapa hit player yang membatalkan serangan enemy sebelum satu hit dibiarkan lolos
/// (enemy memukul balik), supaya spam attack tidak membuat enemy lumpuh selamanya.
/// Jumlahnya diacak ulang setelah tiap hit yang lolos.
/// </summary>
public sealed class StaggerCounter
{
    private readonly int _min;
    private readonly int _max;
    private readonly Func<int, int, int> _roll;
    private int _remaining;

    /// <param name="roll">Mengembalikan bilangan bulat acak di [min, max] inklusif.</param>
    public StaggerCounter(int min, int max, Func<int, int, int> roll)
    {
        _min = min;
        _max = max;
        _roll = roll;
        _remaining = _roll(_min, _max);
    }

    /// <summary>True = hit ini membatalkan serangan; false = serangan lolos dan hitungan diacak ulang.</summary>
    public bool ShouldCancel()
    {
        if (_remaining > 0)
        {
            _remaining--;
            return true;
        }

        _remaining = _roll(_min, _max);
        return false;
    }
}
