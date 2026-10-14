using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public sealed class PlayerDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float cooldown = 0.8f;
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerFacing facing;

    private ActionGate _gate;
    private float _readyAt;

    [Inject]
    public void Construct(ActionGate gate)
    {
        _gate = gate;
    }

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (facing == null) facing = GetComponent<PlayerFacing>();
        if (health == null) health = GetComponent<Health>();
    }

    // Dibaca CombatButtons untuk tombol + overlay cooldown.
    public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);
    public float CooldownPercent => cooldown > 0f ? CooldownRemaining / cooldown : 0f;
    public bool CanDash => _gate != null && !_gate.IsBusy && Time.time >= _readyAt;

    public void TryDash()
    {
        if (!CanDash) return;

        RunAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        _gate.IsBusy = true;
        health.Invulnerable = true;                       // i-frame selama roll
        animator.SetTrigger(AnimatorParams.Dash);
        body.linearVelocity = facing.Direction * dashSpeed;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(dashDuration), cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (this != null && body != null) body.linearVelocity = Vector2.zero;
            if (health != null) health.Invulnerable = false;
            if (_gate != null) _gate.IsBusy = false;
            _readyAt = Time.time + cooldown;
        }
    }
}
