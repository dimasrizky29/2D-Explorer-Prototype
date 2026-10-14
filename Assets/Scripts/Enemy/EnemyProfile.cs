using UnityEngine;

/// <summary>
/// Sifat satu jenis enemy. Aggressive vs Normal cukup beda angka di asset ini, bukan beda class.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Enemy Profile", fileName = "EnemyProfile")]
public sealed class EnemyProfile : ScriptableObject
{
    [SerializeField] private bool aggressive;               // didahulukan saat rebutan giliran serang
    [SerializeField] private bool bypassTurns;              // boss: selalu dapat giliran, tetap makan satu slot
    [SerializeField] private AttackData attack;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float loseRadius = 8f;         // lepas target kalau lebih jauh dari ini
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1f;     // jeda setelah attack sebelum minta giliran lagi
    [SerializeField] private float waitRadius = 2.5f;       // jarak dari player saat menunggu giliran
    [SerializeField, Range(0f, 1f)] private float strafe = 0.5f;   // porsi kecepatan saat mengitari; 0 = diam menekan
    [SerializeField] private float idleMinDelay = 0.5f;
    [SerializeField] private float idleMaxDelay = 1.5f;
    [SerializeField] private int staggerMinHits = 2;        // hit player yang membatalkan serangan sebelum satu hit lolos
    [SerializeField] private int staggerMaxHits = 3;
    [SerializeField] private float staggerCooldown = 0.6f;  // jeda minta giliran lagi setelah serangan dibatalkan

    public bool Aggressive => aggressive;
    public bool BypassTurns => bypassTurns;
    public AttackData Attack => attack;
    public float WalkSpeed => walkSpeed;
    public float DetectRadius => detectRadius;
    public float LoseRadius => loseRadius;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float WaitRadius => waitRadius;
    public float Strafe => strafe;
    public float IdleMinDelay => idleMinDelay;
    public float IdleMaxDelay => idleMaxDelay;
    public int StaggerMinHits => staggerMinHits;
    public int StaggerMaxHits => staggerMaxHits;
    public float StaggerCooldown => staggerCooldown;
}
