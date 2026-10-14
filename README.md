# 2D Explorer Prototype

A top-down 2D pixel-art action-exploration prototype built in Unity. The focus is **combat that feels heavy and alive** (a 5-hit combo, hit stop, directional camera shake, finisher zoom, slow motion) and **enemy AI that attacks in turns**, with a boss that has an ultimate move, all on a clean, testable architecture.
Mobile and PC Platform

> Unity `6000.3.7f1` · URP 2D · UniTask · VContainer · Input System

---

## Key Features

### Player combat
- **5-hit combo.** The fifth hit is the finisher. The combo only advances when an attack connects; a miss or a late button press resets it to the first hit.
- **Input buffer.** An attack press made shortly before the current swing ends is remembered, so the chain does not break over a few milliseconds.
- **Auto-lock targeting.** With the joystick idle, attacks face the nearest enemy. With the joystick held, an attack only turns toward an enemy when the angle difference is small.
- **Dash** shares an action lock with attack, so the two never overlap.

### Game feel ("juice")
- **Hit stop** and **slow motion** through a single shared `timeScale` controller, so several effects can run together without overwriting each other.
- **Directional camera shake.** The camera kicks along the direction of the hit, then rebounds and decays. A small random part keeps the shake lively.
- **Finisher-only camera zoom**, compatible with `PixelPerfectCamera`.
- **Hit sparks** (particles) at the point of impact, with count and size scaled to hit strength.
- **Knockback with its own duration**, including a finishing blow that launches the enemy even as it dies.
- Every value above is **per-attack data** (`HitFeel` inside `AttackData`). Tuning the feel of a single hit requires no code changes.

### Enemies and boss
- **Stagger.** An enemy hit while preparing an attack cancels it. Each enemy has a random counter of 2-3 hits; once it runs out, one attack is allowed through, so a player who mashes the button still gets punished.
- **Turn-based attacking.** `AttackCoordinator` limits how many enemies attack at once and spreads them across evenly spaced angular slots around the player. Enemies without a turn circle on a waiting ring.
- **Custom steering** (no NavMesh): wall sliding through foot-collider casts, plus separation between enemies.
- **Enemy profiles as ScriptableObjects** (`EnemyProfile`). Normal, aggressive, and boss enemies differ by numbers, not by new classes.
- **Boss** with a **Charge** attack (telegraph, direction lock, dash, recover, or stun when it hits a prop) and a **phase 2** below 50% HP. Charge cannot be cancelled, and its hit gets a heavier feel.

### Other
- Interaction system (`InteractionDetector`, `IInteractable`, on-screen prompt).
- Health bars and floating damage numbers.
- On-screen virtual joystick and Attack/Dash buttons, in addition to keyboard and gamepad input.

---

## Strengths

| Area | Details |
|---|---|
| **Data-driven** | Combos, feel, and enemy behavior are assets (`ComboData`, `AttackData`, `HitFeel`, `EnemyProfile`). New animations are plugged into data without code changes. |
| **Testable pure logic** | `ComboTracker`, `StaggerCounter`, `TimeScaleStack`, `ZoomCurve`, and `ShakeCurve` do not depend on MonoBehaviours and are covered by NUnit tests. |
| **One owner of `timeScale`** | `TimeControl` merges hit stop and slow motion through `TimeScaleStack`, and restores `timeScale` to 1 even if an entity is destroyed mid-effect. |
| **Cancellation-safe** | Enemy attacks use a per-attack cancellation token (UniTask) that is disposed exactly once, and an attack cannot be taken back after its hit lands. |
| **Lightweight** | Physics queries use buffers allocated once, with no per-frame allocation on hot paths. UniTask replaces coroutines. |
| **Well structured** | Separate assembly definitions for game, editor, and tests. Dependency injection with VContainer. |

---

## Getting Started

1. Open the project with Unity **6000.3.7f1**.
2. Open the scene `Assets/Scenes/Gameplay.unity`.
3. Press **Play**.

### Controls

| Action | Input |
|---|---|
| Move | `W` `A` `S` `D` / arrow keys / gamepad left stick / on-screen joystick |
| Attack | `Enter` / on-screen **Attack** button |
| Dash | `Left Shift` / on-screen **Dash** button |
| Interact | Interact binding in `Assets/InputSystem_Actions.inputactions` (default: `E`) |

Press attack repeatedly with good timing to chain the combo up to the finisher.

---

## Build and Demo

- **Build:** _coming soon_
- **Demo:** 


https://github.com/user-attachments/assets/cf6d800f-f24d-4099-9361-eb053716a6ba





https://github.com/user-attachments/assets/cd7cc689-1c9b-45d9-8649-92f661f2dfca







---

## Architecture

```
Input (PlayerCombatInput / CombatButtons)
        |
        v
PlayerAttack --ComboTracker--> AttackData (+ HitFeel)
        |
        v
MeleeDamageDealer.ApplyHit --> Health.TakeDamage(DamageInfo{Amount, Source, Knockback, Feel})
        |
        +--> HitSystem        flash, TimeControl (hit stop + slow motion), AnyHit event
        |        |
        |        +--> CameraShake   (directional, from HitFeel)
        |        +--> CameraZoom    (only when ZoomAmount != 0)
        +--> KnockbackReceiver
        +--> HitSpark
        +--> EnemyBrain.HandleStagger --> cancel windup (StaggerCounter)
```

- **`Game`**: all runtime code under `Assets/Scripts`.
- **`Game.Editor`**: editor tools and self-checks (`Tools > Checks > Attack Slots`).
- **`Game.Tests.EditMode`**: NUnit tests under `Assets/Tests/EditMode`.
- **Dependency injection**: `GameLifetimeScope` (VContainer) registers `ActionGate` and `AttackCoordinator` and injects `EnemyBrain`. Enemies spawned at runtime must go through `IObjectResolver.Instantiate`.
- **Async**: UniTask with `CancellationToken`. Each enemy attack has its own token, and the `EnemyBrain` loop uses a token that ends when the component is disabled.
- **Data and logic separation**: models (`HealthModel`, `ComboTracker`, `StaggerCounter`) are separate from views and MonoBehaviours.

### Interesting design decisions
- **`PixelPerfectCamera` writes the camera size every frame**, and it only changes through integer zoom steps. So `CameraZoom` applies its zoom factor in the `beginCameraRendering` callback after `PixelPerfectCamera`, with `DefaultExecutionOrder` to guarantee the order.
- **Body and feet colliders are separate.** The body is the hitbox/hurtbox; the feet only collide with the environment. Knockback direction is computed from the body center, not the feet pivot.
- **Hit stop does not eat the combo window.** The combo window and input buffer use scaled time, so while the game is frozen the waiting time freezes too.

---

## Project Structure

```
Assets/
  Scenes/Gameplay.unity       Main scene
  Scripts/
    Combat/                   Hits, combo, feel, camera, time control
    Core/                     DamageInfo, HealthModel, IDamageable
    Enemy/                    EnemyBrain, BossBrain, AttackCoordinator, steering, profiles
    Player/                   Controller, attack, dash, interaction
    Interactable/             Interaction system
    UI/                       Health bars, damage numbers, combat buttons
    DI/                       GameLifetimeScope
    Editor/                   Editor self-checks
  Tests/EditMode/             NUnit tests
  Data/                       AttackData, ComboData, EnemyProfile
  Prefabs/                    Player, Enemy, Boss
  Animation/                  Animator controllers and clips
  ThirdParty/                 Third-party assets
```

---

## Testing

Open `Window > General > Test Runner > EditMode > Run All`. The tests cover `TimeScaleStack`, `ZoomCurve`, `ShakeCurve`, `ComboTracker`, `StaggerCounter`, and `HitFeel`.

---

## Tech Stack

- Unity 6 (`6000.3.7f1`), Universal Render Pipeline 17.3.0 (2D Renderer, Pixel Perfect Camera)
- Input System 1.18.0
- [UniTask](https://github.com/Cysharp/UniTask)
- [VContainer](https://github.com/hadashiA/VContainer) 1.17.0
- Unity UI (uGUI) 2.0.0 with TextMeshPro
- Unity 2D packages: Animation, Aseprite, PSD Importer, Sprite, SpriteShape, Tilemap, Tilemap Extras, 2D Tooling
- Timeline 1.8.10
- Unity Test Framework 1.6.0 (NUnit)

---

## Roadmap

- Dedicated boss health bar.
- Ranged enemy (keeps distance, fires projectiles).
- Chain aggro: nearby enemies join the chase when one enemy aggroes.
- Spawning from prop interactions.
- A `BossProfile` ScriptableObject once there is more than one boss type.
- Dedicated hit spark art to replace the built-in Unity particles.

---

## Credits

**Packages and libraries**
- [UniTask](https://github.com/Cysharp/UniTask) by Cysharp
- [VContainer](https://github.com/hadashiA/VContainer) by hadashiA
- TextMeshPro (part of Unity UI), Input System, Universal Render Pipeline, and the 2D packages by Unity Technologies

**Third-party assets** (`Assets/ThirdParty`)
- **Joystick Pack**: Unity Asset Store.
- **Adventurer 2D Pixel Art**, **Demon Portal**, **Enemy**, and **TopDownFantasy-Forest**: free assets from itch.io.

Licenses and copyright for each asset belong to their original creators. Check the source page before reusing or redistributing them.
