using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int maxAttackers = 3;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ActionGate>(Lifetime.Singleton);
        builder.RegisterInstance(new AttackCoordinator(maxAttackers));
        builder.RegisterComponentInHierarchy<PlayerFacing>();
        builder.RegisterComponentInHierarchy<PlayerController>();
        builder.RegisterComponentInHierarchy<PlayerAnimator>();
        builder.RegisterComponentInHierarchy<PlayerAttack>();
        builder.RegisterComponentInHierarchy<PlayerDash>();

        builder.RegisterBuildCallback(resolver =>
        {
            // Enemy yang sudah ada di scene: bisa lebih dari satu, jadi di-inject satu per satu
            // (RegisterComponentInHierarchy hanya mendukung satu instance).
            foreach (var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                resolver.Inject(enemy);

            if (enemyPrefab != null) resolver.Instantiate(enemyPrefab);
        });
    }
}