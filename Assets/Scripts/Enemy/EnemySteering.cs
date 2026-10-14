using UnityEngine;

/// <summary>
/// Arah gerak enemy: menyusuri props (cast bentuk collider kaki), keluar dari macet,
/// dan menjauh dari enemy lain. Props dibaca saat runtime, jadi props yang muncul/hilang
/// belakangan langsung terhitung tanpa bake navmesh.
/// </summary>
public sealed class EnemySteering : MonoBehaviour
{
    [SerializeField] private Collider2D feet;               // collider kaki (child, layer WalkEnemy)
    [SerializeField] private LayerMask obstacleMask;        // Props
    [SerializeField] private LayerMask enemyMask;           // badan enemy lain
    [SerializeField] private float lookAhead = 0.6f;
    [SerializeField] private float separationRadius = 1f;
    [SerializeField] private float separationWeight = 1.2f;
    [SerializeField] private float stuckTime = 0.5f;

    private const float SeparationInterval = 0.1f;

    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[4];
    private readonly Collider2D[] _neighbors = new Collider2D[8];
    private ContactFilter2D _obstacleFilter;
    private ContactFilter2D _enemyFilter;
    private Rigidbody2D _body;

    private Vector2 _separation;
    private float _nextSeparationTime;
    private float _slideSign = 1f;
    private float _holdSideUntil;
    private Vector2 _stuckCheckPos;
    private float _stuckCheckTime;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _obstacleFilter = new ContactFilter2D { useLayerMask = true, layerMask = obstacleMask, useTriggers = false };
        _enemyFilter = new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = false };
    }

    /// <summary>
    /// Ubah arah keinginan (panjang 0..1 = porsi kecepatan) jadi arah aman. Panjang hasil tetap 0..1.
    /// </summary>
    public Vector2 Steer(Vector2 desired)
    {
        if (Time.time >= _nextSeparationTime)
        {
            _separation = ComputeSeparation();
            _nextSeparationTime = Time.time + SeparationInterval;
        }

        Vector2 wish = desired + _separation * separationWeight;
        float magnitude = Mathf.Clamp01(wish.magnitude);
        UpdateStuck(magnitude);
        if (magnitude < 0.01f) return Vector2.zero;

        return AvoidObstacles(wish / wish.magnitude) * magnitude;
    }

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        if (feet == null || !TryCast(dir, lookAhead, out RaycastHit2D hit)) return dir;

        // Geser sejajar permukaan. Pilih sisi yang paling searah tujuan; tabrakan lurus atau baru macet: pakai sisi terakhir.
        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
        float along = Vector2.Dot(tangent, dir);
        if (Time.time >= _holdSideUntil && Mathf.Abs(along) > 0.1f) _slideSign = Mathf.Sign(along);

        Vector2 slide = tangent * _slideSign;
        if (TryCast(slide, lookAhead * 0.5f, out _))   // pojok: sisi ini juga buntu
        {
            _slideSign = -_slideSign;
            slide = -slide;
        }
        return slide;
    }

    private bool TryCast(Vector2 dir, float distance, out RaycastHit2D closest)
    {
        int count = feet.Cast(dir, _obstacleFilter, _castHits, distance);
        closest = default;
        if (count == 0) return false;

        closest = _castHits[0];
        for (int i = 1; i < count; i++)
            if (_castHits[i].distance < closest.distance) closest = _castHits[i];
        return true;
    }

    private Vector2 ComputeSeparation()
    {
        Vector2 self = _body.position;
        int count = Physics2D.OverlapCircle(self, separationRadius, _enemyFilter, _neighbors);

        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D other = _neighbors[i].attachedRigidbody;
            if (other == null || other == _body) continue;

            Vector2 away = self - other.position;
            float dist = away.magnitude;
            if (dist < 0.0001f) away = Random.insideUnitCircle.normalized;   // persis bertumpuk
            else away /= dist;

            push += away * (1f - Mathf.Clamp01(dist / separationRadius));   // makin dekat makin kuat
        }
        return push;
    }

    // Ingin jalan tapi hampir tidak bergeser: balik sisi slide sebentar.
    private void UpdateStuck(float magnitude)
    {
        if (Time.time < _stuckCheckTime) return;

        Vector2 pos = _body.position;
        if (magnitude > 0.3f && (pos - _stuckCheckPos).sqrMagnitude < 0.01f)
        {
            _slideSign = -_slideSign;
            _holdSideUntil = Time.time + 0.6f;
        }
        _stuckCheckPos = pos;
        _stuckCheckTime = Time.time + stuckTime;
    }
}
