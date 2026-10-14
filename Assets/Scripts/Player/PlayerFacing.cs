using UnityEngine;

public sealed class PlayerFacing : MonoBehaviour
{
    [SerializeField] private Vector2 initialDirection = Vector2.down;

    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        Direction = initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : Vector2.down;
    }

    public void Set(Vector2 dir, float deadzone = 0.05f)
    {
        if (dir.sqrMagnitude < deadzone * deadzone) return;
        Direction = dir.normalized;
    }
}
