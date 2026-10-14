using UnityEngine;

/// <summary>
/// Semburan spark di badan entity saat kena hit. Jumlah dan ukuran mengikuti <see cref="HitFeel"/>.
/// ParticleSystem dibuat lewat kode (belum ada asset); satu per entity, dipakai ulang tiap hit.
/// </summary>
public sealed class HitSpark : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private float baseSize = 0.12f;
    [SerializeField] private float lifetime = 0.25f;
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 5f);
    [SerializeField] private float spreadAngle = 35f;
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = new Color(1f, 0.85f, 0.2f);

    private ParticleSystem _particles;
    private Material _material;
    private Collider2D _body;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();

        // Collider badan = collider solid pertama di root (trigger interaksi dilewati).
        foreach (var c in GetComponents<Collider2D>())
        {
            if (c.isTrigger) continue;
            _body = c;
            break;
        }

        // Tanpa parent: partikel tidak ikut scale/rotasi entity.
        var go = new GameObject("HitSpark");
        _particles = go.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.useUnscaledTime = true;   // tetap terlihat selama hit stop
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 128;

        var emission = _particles.emission;
        emission.enabled = false;      // dipancarkan manual lewat Emit

        var shape = _particles.shape;
        shape.enabled = false;

        _material = new Material(Shader.Find("Sprites/Default"));
        var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = _material;
        particleRenderer.sortingOrder = 100;
    }

    private void OnEnable()
    {
        if (health != null) health.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.Damaged -= HandleDamaged;
    }

    private void OnDestroy()
    {
        if (_particles != null) Destroy(_particles.gameObject);
        if (_material != null) Destroy(_material);
    }

    private void HandleDamaged(DamageInfo info)
    {
        HitFeel feel = info.Feel ?? HitFeel.Default;
        if (feel.SparkCount <= 0) return;

        Vector2 center = _body != null ? (Vector2)_body.bounds.center : (Vector2)transform.position;
        Vector2 away = center - info.Source;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;

        var emit = new ParticleSystem.EmitParams { position = center };
        for (int i = 0; i < feel.SparkCount; i++)
        {
            float angle = Random.Range(-spreadAngle, spreadAngle);
            emit.velocity = Quaternion.Euler(0f, 0f, angle) * (Vector3)away * Random.Range(speedRange.x, speedRange.y);
            emit.startSize = baseSize * feel.SparkSize;
            emit.startLifetime = lifetime;
            emit.startColor = Color.Lerp(colorA, colorB, Random.value);
            _particles.Emit(emit, 1);
        }
    }
}
