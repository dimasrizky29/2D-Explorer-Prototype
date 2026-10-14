using TMPro;
using UnityEngine;

/// <summary>
/// Angka damage melayang di atas entity saat kena hit.
/// ponytail: satu label dipakai ulang, bukan di-pool per hit. Ganti ke beberapa label
/// kalau ada serangan multi-hit yang tumpang tindih.
/// </summary>
public sealed class DamageNumber : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float rise = 1.2f;
    [SerializeField] private float fontSize = 5f;
    [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f);

    private TextMeshPro _label;
    private Vector3 _start;
    private float _age = -1f;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();

        // Tanpa parent: label tidak ikut scale/rotasi entity dan tidak mati saat entity hancur.
        _label = new GameObject("DamageNumber").AddComponent<TextMeshPro>();
        _label.fontSize = fontSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = color;
        _label.gameObject.SetActive(false);
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
        if (_label != null) Destroy(_label.gameObject);
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (_label == null) return;

        _start = transform.position + offset;
        _label.text = info.Amount.ToString();
        _label.transform.position = _start;
        _label.gameObject.SetActive(true);
        _age = 0f;
    }

    private void Update()
    {
        if (_age < 0f) return;

        _age += Time.deltaTime;
        float k = _age / lifetime;
        if (k >= 1f)
        {
            _age = -1f;
            _label.gameObject.SetActive(false);
            return;
        }

        _label.transform.position = _start + Vector3.up * (rise * k);

        Color c = color;
        c.a = 1f - k;
        _label.color = c;
    }
}
