using System;
using UnityEngine;

public sealed class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 10;

    private HealthModel _model;

    public event Action<int, int> Changed;      // current, max
    public event Action<DamageInfo> Damaged;
    public event Action Died;

    public bool IsAlive => _model.IsAlive;
    public bool Invulnerable { set => _model.Invulnerable = value; }

    private void Awake()
    {
        _model = new HealthModel(maxHealth);
    }

    // Start, bukan Awake: semua subscriber OnEnable sudah terpasang.
    private void Start()
    {
        Changed?.Invoke(_model.Current, _model.Max);
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (!_model.Apply(info)) return;

        Damaged?.Invoke(info);
        Changed?.Invoke(_model.Current, _model.Max);
        if (!_model.IsAlive) Died?.Invoke();
    }
}
