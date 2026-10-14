using UnityEngine;

public static class AnimatorParams
{
    public static readonly int MoveX = Animator.StringToHash("MoveX");
    public static readonly int MoveY = Animator.StringToHash("MoveY");
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
    public static readonly int Dash = Animator.StringToHash("Dash");
    public static readonly int Hurt = Animator.StringToHash("Hurt");
    public static readonly int IsDead = Animator.StringToHash("IsDead");
}
