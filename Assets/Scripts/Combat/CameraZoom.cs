using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Zoom kamera singkat saat hit, sesuai <see cref="HitFeel.ZoomAmount"/>. Pasang di GameObject Camera,
/// setelah PixelPerfectCamera (urutan komponen menentukan urutan callback render).
/// PixelPerfectCamera menulis orthographicSize tiap frame persis sebelum render, dan ukurannya hanya
/// bergeser lewat lompatan zoom bilangan bulat, jadi mengubah resolusi referensi atau PPU tidak terlihat
/// (atau melompat 4% per PPU). Karena itu faktor zoom diterapkan sesudahnya, di callback render yang sama.
/// </summary>
[DefaultExecutionOrder(100)]   // OnEnable setelah PixelPerfectCamera, supaya callback render kita jalan sesudahnya
public sealed class CameraZoom : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private PixelPerfectCamera pixelPerfect;

    private float _baseSize;
    private float _factor = 1f;
    private float _amount;
    private float _duration = 1f;
    private float _startTime;
    private bool _active;

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (pixelPerfect == null) pixelPerfect = GetComponent<PixelPerfectCamera>();

        _baseSize = cam.orthographicSize;
    }

    private void OnEnable()
    {
        HitSystem.AnyHit += HandleHit;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        HitSystem.AnyHit -= HandleHit;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        if (!_active) return;

        _factor = 1f;
        _active = false;
        if (pixelPerfect == null) cam.orthographicSize = _baseSize;
    }

    private void HandleHit(DamageInfo info, Vector2 direction)
    {
        HitFeel feel = info.Feel ?? HitFeel.Default;
        if (feel.ZoomAmount == 0f) return;

        // Zoom yang sedang berjalan dan masih lebih kuat tidak ditimpa hit yang lebih lemah.
        if (_active && Mathf.Abs(feel.ZoomAmount) < Mathf.Abs(Current())) return;

        _amount = feel.ZoomAmount;
        _duration = Mathf.Max(0.01f, feel.ZoomDuration);
        _startTime = Time.unscaledTime;
        _active = true;
    }

    private float Progress() => (Time.unscaledTime - _startTime) / _duration;

    private float Current() => ZoomCurve.Evaluate(_amount, Progress());

    // LateUpdate + unscaledTime: zoom tetap jalan saat hit stop dan slow motion.
    private void LateUpdate()
    {
        if (!_active) return;

        if (Progress() >= 1f)
        {
            _factor = 1f;
            _active = false;
        }
        else
        {
            _factor = Mathf.Max(0.1f, 1f + Current());
        }

        if (pixelPerfect == null) cam.orthographicSize = _baseSize / _factor;
    }

    // Jalan setelah PixelPerfectCamera menulis orthographicSize dasar frame ini (tidak menumpuk antar frame).
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (pixelPerfect == null || camera != cam || _factor == 1f) return;
        cam.orthographicSize /= _factor;
    }
}
