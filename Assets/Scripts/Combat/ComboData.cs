using UnityEngine;

/// <summary>
/// Urutan serangan combo player. Langkah terakhir = finisher. Animasi dan feel ada di tiap AttackData,
/// jadi animasi baru cukup dipasang di sini tanpa ubah kode.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Combo Data", fileName = "ComboData")]
public sealed class ComboData : ScriptableObject
{
    [SerializeField] private AttackData[] steps;
    [SerializeField, Tooltip("Detik dari momen hit sampai tombol berikutnya masih melanjutkan combo. Harus lebih besar dari Recovery - Windup.")]
    private float comboWindow = 0.6f;
    [SerializeField, Tooltip("Tombol yang ditekan sebelum serangan selesai disimpan selama ini.")]
    private float inputBuffer = 0.2f;

    public AttackData[] Steps => steps;
    public int Length => steps != null ? steps.Length : 0;
    public float ComboWindow => comboWindow;
    public float InputBuffer => inputBuffer;
}
