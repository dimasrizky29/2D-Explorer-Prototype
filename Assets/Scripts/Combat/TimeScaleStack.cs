using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kumpulan permintaan timeScale sementara (hit stop, slow motion). Yang berlaku = scale terkecil
/// dari entri yang belum habis. Waktu unscaled, diteruskan pemanggil supaya mudah dites.
/// </summary>
public sealed class TimeScaleStack
{
    private struct Entry
    {
        public float Scale;
        public float End;
    }

    private readonly List<Entry> _entries = new List<Entry>();

    public bool HasEntries => _entries.Count > 0;

    /// <summary>Scale sama memperpanjang entri lama (hit beruntun), tidak menumpuk.</summary>
    public void Add(float scale, float endTime)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (!Mathf.Approximately(_entries[i].Scale, scale)) continue;

            Entry existing = _entries[i];
            if (endTime > existing.End) existing.End = endTime;
            _entries[i] = existing;
            return;
        }
        _entries.Add(new Entry { Scale = scale, End = endTime });
    }

    /// <summary>Scale yang berlaku di waktu now; entri yang sudah habis dibuang.</summary>
    public float Evaluate(float now)
    {
        float result = 1f;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (now >= _entries[i].End)
            {
                _entries.RemoveAt(i);
                continue;
            }
            result = Mathf.Min(result, _entries[i].Scale);
        }
        return result;
    }

    public void Clear() => _entries.Clear();
}
