using UnityEditor;
using UnityEngine;

/// <summary>Self-check pembagian slot sudut. Jalankan: Tools > Checks > Attack Slots.</summary>
public static class AttackCoordinatorCheck
{
    [MenuItem("Tools/Checks/Attack Slots")]
    public static void Run()
    {
        var slots = new float[3];
        var order = new int[3];

        // Tiga enemy menumpuk di sisi kanan player: slot harus terpisah 120 derajat.
        AttackCoordinator.AssignSlots(new[] { 0.1f, 0f, -0.1f }, 3, slots, order);
        for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 3; j++)
                Debug.Assert(Mathf.Abs(Mathf.DeltaAngle(slots[i] * Mathf.Rad2Deg, slots[j] * Mathf.Rad2Deg)) > 119f,
                    "Slot bertumpuk");

        // Satu enemy: slot = posisinya sendiri, tidak disuruh pindah.
        AttackCoordinator.AssignSlots(new[] { 1.2f }, 1, slots, order);
        Debug.Assert(Mathf.Abs(Mathf.DeltaAngle(slots[0] * Mathf.Rad2Deg, 1.2f * Mathf.Rad2Deg)) < 0.01f,
            "Slot tunggal bergeser");

        // Dua enemy berseberangan: sudah rata, tidak bergeser.
        AttackCoordinator.AssignSlots(new[] { 0f, Mathf.PI }, 2, slots, order);
        Debug.Assert(Mathf.Abs(Mathf.DeltaAngle(slots[0] * Mathf.Rad2Deg, 0f)) < 0.01f, "Slot berseberangan bergeser");

        Debug.Log("[AttackCoordinatorCheck] selesai (assert gagal muncul sebagai error di atas).");
    }
}
