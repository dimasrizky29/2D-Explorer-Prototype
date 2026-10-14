using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Giliran serang antar enemy: maksimal N pemegang token sekaligus, dan pemegang token
/// dibagi ke slot sudut rata di sekeliling player supaya tidak bertumpuk.
/// Di-register sebagai Singleton di GameLifetimeScope.
/// </summary>
public sealed class AttackCoordinator
{
    private readonly int _maxAttackers;
    private readonly List<EnemyBrain> _engaged = new List<EnemyBrain>();
    private readonly List<EnemyBrain> _holders = new List<EnemyBrain>();
    // Bukan readonly: boss (BypassTurns) bisa membuat pemegang token melebihi batas.
    private float[] _angles;
    private float[] _slots;
    private int[] _order;
    private int _slotFrame = -1;

    public AttackCoordinator(int maxAttackers)
    {
        _maxAttackers = Mathf.Max(1, maxAttackers);
        _angles = new float[_maxAttackers];
        _slots = new float[_maxAttackers];
        _order = new int[_maxAttackers];
    }

    public void Engage(EnemyBrain enemy)
    {
        if (!_engaged.Contains(enemy)) _engaged.Add(enemy);
    }

    public void Disengage(EnemyBrain enemy)
    {
        _engaged.Remove(enemy);
        Release(enemy);
    }

    public bool TryAcquire(EnemyBrain enemy)
    {
        if (_holders.Contains(enemy)) return true;

        if (!enemy.BypassesTurns)
        {
            // Aggressive yang sedang menunggu memesan slot duluan; Normal hanya dapat sisanya.
            int reserved = 0;
            if (!enemy.IsAggressive)
                foreach (var other in _engaged)
                    if (other.IsAggressive && !other.BypassesTurns && other.ReadyToAttack && !_holders.Contains(other)) reserved++;

            if (_holders.Count + reserved >= _maxAttackers) return false;
        }

        _holders.Add(enemy);
        if (_angles.Length < _holders.Count)
        {
            _angles = new float[_holders.Count];
            _slots = new float[_holders.Count];
            _order = new int[_holders.Count];
        }
        _slotFrame = -1;
        return true;
    }

    public void Release(EnemyBrain enemy)
    {
        if (_holders.Remove(enemy)) _slotFrame = -1;
    }

    /// <summary>Arah dari center ke slot milik enemy ini. Bukan pemegang token: arah ke posisinya sekarang.</summary>
    public Vector2 SlotDirection(EnemyBrain enemy, Vector2 center)
    {
        int index = _holders.IndexOf(enemy);
        if (index < 0) return (enemy.Origin - center).normalized;

        if (_slotFrame != Time.frameCount)
        {
            for (int i = 0; i < _holders.Count; i++)
            {
                Vector2 d = _holders[i].Origin - center;
                _angles[i] = Mathf.Atan2(d.y, d.x);
            }
            AssignSlots(_angles, _holders.Count, _slots, _order);
            _slotFrame = Time.frameCount;
        }

        return new Vector2(Mathf.Cos(_slots[index]), Mathf.Sin(_slots[index]));
    }

    /// <summary>
    /// Slot sudut rata (radian) untuk n enemy; slots[k] milik angles[k]. Urutan keliling dipertahankan
    /// supaya jalur tidak saling silang, lalu formasi diputar ke rata-rata posisi sekarang
    /// supaya tiap enemy bergeser sesedikit mungkin.
    /// </summary>
    public static void AssignSlots(float[] angles, int n, float[] slots, int[] order)
    {
        // Insertion sort: n kecil (maks attacker), tanpa alokasi.
        for (int i = 0; i < n; i++)
        {
            int idx = i;
            int j = i - 1;
            while (j >= 0 && angles[order[j]] > angles[idx]) { order[j + 1] = order[j]; j--; }
            order[j + 1] = idx;
        }

        float step = 2f * Mathf.PI / n;
        float sx = 0f, sy = 0f;
        for (int j = 0; j < n; j++)
        {
            float a = angles[order[j]] - j * step;
            sx += Mathf.Cos(a);
            sy += Mathf.Sin(a);
        }

        float offset = Mathf.Atan2(sy, sx);
        for (int j = 0; j < n; j++) slots[order[j]] = offset + j * step;
    }
}
