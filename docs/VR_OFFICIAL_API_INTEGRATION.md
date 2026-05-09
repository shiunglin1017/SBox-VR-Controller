# VR Official API Integration

This doc describes the **2026-05-08** integration that puts as much of the
runtime through `Sandbox.VR.*` / `Sandbox.FixedJoint` as possible. It maps to
the four scoped sections from the plan:

- **A.** Input abstraction extension (controller inputs, haptics)
- **C.** Skeletal hand tracking (full OpenXR hand joints)
- **D.** Official controller / hand model toggle
- **G.** ModelDoc attachment holstering + physical "heavy hand" sway

Everything goes through the existing DI layer
([`docs/VR_DI_ARCHITECTURE.md`](VR_DI_ARCHITECTURE.md)). No consumer reads
`Input.VR` directly.

---

## 中文速查（zh-TW）

> 對應 `2026-05-08` 變更，原則：能用 `Sandbox.VR.*` 與 `Sandbox.FixedJoint`
> 就用，自寫元件只做「薄路由 + 編輯器欄位」。

| 區塊 | 你會看到的新東西 | 關鍵檔案 |
| --- | --- | --- |
| **A 輸入** | `IControllerInput` 多了 `AimPose` / `IsHandTracking` / `*Delta` / `*Active` / `GetFingerSplay` / `GetFingerValue` / 新版 haptic；`VRFingerKind` enum | [`IControllerInput.cs`](../Code/Player/Abstractions/IControllerInput.cs) [`VRControllerAdapter.cs`](../Code/Player/Services/VRControllerAdapter.cs) |
| **A 邊緣偵測** | 武器全面改 `(Value - Delta)` rising-edge / `WasPressed`，順便加震動 | [`PistolTrigger.cs`](../Code/Weapons/PistolTrigger.cs) [`PistolSlide.cs`](../Code/Weapons/PistolSlide.cs) [`MagazineLoader.cs`](../Code/Weapons/MagazineLoader.cs) [`RecoilTest.cs`](../Code/RecoilTest.cs) |
| **A AimPose 抓取射線** | `Search()` 從 `AimPose.Position` 沿 `AimPose.Forward` 找物品 | [`VrhandInteraction.cs`](../Code/Player/VrhandInteraction.cs) |
| **C 骨骼手追** | `IHandSkeletonProvider` + `SandboxVRHandSkeletonProvider` 一次性快取 `GetJoints` 的兩種 motion range；`VRAnimationHelper.VRHand` 加 `UseSkeletalJoints` + `JointBones` | [`IHandSkeletonProvider.cs`](../Code/Player/Abstractions/IHandSkeletonProvider.cs) [`SandboxVRHandSkeletonProvider.cs`](../Code/Player/Services/SandboxVRHandSkeletonProvider.cs) [`VRAnimationHelper.cs`](../Code/Player/VRAnimationHelper.cs) |
| **D 官方手 / 控制器模型** | `OfficialHandToggle` 依 `IsHolding` + `IsHandTracking` 切官方 `Sandbox.VR.VRHand` / Citizen 手 | [`OfficialHandToggle.cs`](../Code/Player/Services/OfficialHandToggle.cs) |
| **G 掛載 + 物理甩動** | `VRHolsterSlot` 用 `GetAttachmentObject` + `Sandbox.FixedJoint`，支援「鎖死 / 彈簧甩動」兩模式；`VrhandInteraction` 加 holster 互動 + `UsePhysicalHand` 物理手開關 | [`VRHolsterSlot.cs`](../Code/Player/Services/VRHolsterSlot.cs) [`VrhandInteraction.cs`](../Code/Player/VrhandInteraction.cs) |
| **測試** | 26 通過 / 0 失敗（既有 11 + 新增 15） | [`NullControllerTests.cs`](../Code/unittest/VRLogic/NullControllerTests.cs) [`VRFingerKindTests.cs`](../Code/unittest/VRLogic/VRFingerKindTests.cs) |

### 編輯器需手動接線（只能在 s&box 編輯器裡完成，不能手寫 prefab GUID）

1. `Assets/prefabs/Player.prefab`
   - `HandLRef` / `HandRRef` 各新增子物件 `OfficialHand`：上面掛
     `SkinnedModelRenderer` + `Sandbox.VR.VRHand`，預設 `Enabled = false`。
   - 同層加 `SandboxVRHandSkeletonProvider`（`Side` = 對應的左/右）。
   - 同層加 `OfficialHandToggle`，`Hand` 指 `VrhandInteraction`、
     `OfficialHand` 指上一步那個子物件、`CitizenHand` 指既有的 Citizen 手。
2. Citizen body 的 `SkinnedModelRenderer` 把 `CreateAttachments = true` 打開。
3. 在 body 下放數個 `VRHolsterSlot` 子物件，填好 `SourceRenderer` /
   `AttachmentName`（例 `back_rifle`、`hip_holster_l`）/ `AcceptItemTag` /
   `UseSpringPhysics` 等欄位。
4. （可選）在 `VrhandInteraction` 上把 `UsePhysicalHand` 勾起來啟用「重物拖手」。

> 編輯器接線 SOP 詳見下方英文段落 **D**、**G2**、**G4**。

---

## Quick reference - new public surface

### Abstractions

```csharp
// Code/Player/Abstractions/IControllerInput.cs

bool IsHandTracking { get; }              // VRController.IsHandTracked
Transform GripPose  { get; }              // VRController.Transform
Transform AimPose   { get; }              // VRController.AimTransform

float   TriggerDelta { get; }             // Trigger.Delta
float   GripDelta    { get; }             // Grip.Delta
Vector2 JoystickDelta { get; }            // Joystick.Delta

bool TriggerActive    { get; }            // Trigger.Active
bool GripActive       { get; }            // Grip.Active
bool ButtonAActive    { get; }            // ButtonA.Active
bool ButtonBActive    { get; }            // ButtonB.Active

float GetFingerSplay( int finger );
float GetFingerValue( VRFingerKind kind );

void TriggerHaptic( HapticEffect effect,
    float lengthScale = 1f, float frequencyScale = 1f, float amplitudeScale = 1f );
void StopAllHaptics();
```

```csharp
// Code/Player/Abstractions/VRFingerKind  (mirrors Sandbox.VR.FingerValue)
ThumbCurl=0, IndexCurl=1, MiddleCurl=2, RingCurl=3, PinkyCurl=4,
ThumbIndexSplay=10, IndexMiddleSplay=11, MiddleRingSplay=12, RingPinkySplay=13
```

```csharp
// Code/Player/Abstractions/IHandSkeletonProvider.cs

HandSide Side { get; }
bool HasSkeleton { get; }
IReadOnlyList<Sandbox.VR.VRHandJointData> Joints { get; }      // MotionRange.Controller
IReadOnlyList<Sandbox.VR.VRHandJointData> RawHandJoints { get; }// MotionRange.Hand
```

### Services / components (all under `TFT.VR.Services`)

| Component | Purpose |
| --- | --- |
| `SandboxVRHandSkeletonProvider` | One per hand. Caches `VRController.GetJoints` for both motion ranges. |
| `OfficialHandToggle` | Routes visibility between Citizen hand and official `Sandbox.VR.VRHand` / `VRModelRenderer` child. |
| `VRHolsterSlot` | Thin wrapper over `ModelRenderer.GetAttachmentObject` + `Sandbox.FixedJoint`. |

### `VrhandInteraction` additions

| Member | Description |
| --- | --- |
| `bool IsHolding`, `HandState State` | Public read-only state mirrors so toggles / UI can react without touching internals. |
| `bool UsePhysicalHand` (Property) | Switches the hand body from kinematic-snap to dynamic-spring (FixedJoint to tracker). Heavy items can drag the hand. |
| `Search()` ray | Now uses `controller.AimPose.Position/Forward` (with hand-root forward as fallback). |
| `Searching()` holster pickup | If grip is pressed inside a non-empty `VRHolsterSlot`'s `ProximityRadius`, calls `slot.TryUnholster` and routes through `Grab(...)`. |
| `Holding()` holster put-back | If grip is released inside an empty accepting slot, runs `Drop()` then `slot.TryHolster`. |

---

## A. Input abstraction extension

### Why

`VRControllerAdapter` previously flattened only `Joystick.Value`,
`Trigger`, `Grip`, `ButtonA`, `ButtonB`, `GetFingerCurl`, and an
`[Obsolete]` haptic call into `IControllerInput`. Every consumer that
needed edge detection (e.g. `PistolTrigger.lastPullBack`) had to track
the previous frame manually.

### What changed

- `VRControllerAdapter` now forwards `AnalogInput.Delta` / `Active`,
  `DigitalInput.WasPressed` / `Active`, `AimTransform`, `IsHandTracked`,
  `GetFingerSplay` / `GetFingerValue`, and uses
  `TriggerHaptics( HapticEffect, ... )`.
- `NullController` was made public and given matching no-op defaults so
  `dotnet test` can pin them.
- Consumers were upgraded to `Delta` / `WasPressed`:
  - `Code/Weapons/PistolTrigger.cs` -- rising edge via
    `(Trigger - TriggerDelta) < 0.9`, fires `HapticEffect.HardImpact`.
  - `Code/Weapons/PistolSlide.cs` -- `JoystickPressed`, chamber haptic.
  - `Code/Weapons/MagazineLoader.cs` -- `ButtonBPressed`, insert / drop
    haptic.
  - `Code/RecoilTest.cs` -- `TriggerDelta`, recoil haptic.
- `Code/Player/VrhandInteraction.cs::Search()` now starts the ray at
  `controller.AimPose.Position` and aims down `AimPose.Forward`.

### How to use

```csharp
var ctrl = GrabPoint.GrabbedHand?.Controller;
if ( ctrl is null || !ctrl.IsTracked ) return;

// Pull the trigger past 0.9 this frame? (no extra fields needed)
var prev = ctrl.Trigger - ctrl.TriggerDelta;
if ( ctrl.Trigger >= 0.9f && prev < 0.9f )
{
    Fire();
    ctrl.TriggerHaptic( HapticEffect.HardImpact );
}
```

### Compat / backwards behaviour

The legacy `TriggerHaptic( duration, frequency, amplitude )` signature
stays in place; it forwards to the legacy
`VRController.TriggerHapticVibration` under a `#pragma warning disable
CS0618` so the call site doesn't go red. Prefer the new
`HapticEffect`-based overload for new code.

---

## C. Skeletal hand tracking

### Wiring

1. Add a `SandboxVRHandSkeletonProvider` to each of `HandLRef` and
   `HandRRef` (one per `HandSide`).
2. On the Citizen hand model's nested
   `VRAnimationHelper.VRHand` (Hands/Left and Hands/Right):
   - Set `UseSkeletalJoints = true`.
   - Populate `JointBones` (a
     `Dictionary<Sandbox.VR.VRHandJoint, GameObject>`) with the rig
     bones. Partial bindings are fine -- joints absent from the dictionary
     are left to the `BendFingers()` fallback.

When skeletal data is unavailable (`HasSkeleton == false`) or
`UseSkeletalJoints` is false, `VRAnimationHelper.Fingers()` falls back to
the existing `BendFingers()` lerp on `OpenRotation`/`ClosedRotation`, so
non-VR play and remote proxies still animate cleanly.

### Note on namespaces

`Sandbox.Citizen.VRAnimationHelper` has a nested `VRHand` class that
collides with `Sandbox.VR.VRHand`. To keep both visible we **don't**
`using Sandbox.VR;` in that file -- references use the full names
`Sandbox.VR.VRHandJoint` and `Sandbox.VR.VRHandJointData`.

---

## D. Official controller / hand model

### D1 (recommended) -- official `Sandbox.VR.VRHand`

In `Assets/prefabs/Player.prefab`:

1. Add a child GameObject `OfficialHand` under each of `HandLRef` and
   `HandRRef`.
2. On `OfficialHand` add:
   - `SkinnedModelRenderer` (assign the project's hand model).
   - `Sandbox.VR.VRHand` (`HandSource = LeftHand`/`RightHand`,
     `MotionRange = Controller`).
3. Default `OfficialHand.Enabled = false`.
4. Add an `OfficialHandToggle` component to the same `HandLRef` /
   `HandRRef`:
   - `Hand` -> the `VrhandInteraction` on this hand.
   - `OfficialHand` -> the new child GameObject.
   - `CitizenHand` -> the existing Citizen hand GameObject.
   - Optional: `RequireHandTracking = true` if you only want the
     official hand to appear during true hand-tracking sessions.

### D2 (alternative) -- raw controller model

Replace the `Sandbox.VR.VRHand` component above with
`Sandbox.VR.VRModelRenderer` (`ModelSource = LeftHand`/`RightHand`).
Everything else (the toggle wiring) stays identical.

---

## G. ModelDoc attachment holstering + physical "heavy hand" sway

### G1 -- ModelDoc setup (no code)

In ModelDoc, on the character and any holsterable item, add the
attachment names you want to use:

- Body: `back_rifle`, `hip_holster_l`, `hip_holster_r`, `chest_pistol`,
  `belt_mag_1`, ...
- Weapons: keep the existing `weapon_hold`; add `iron_sights` if you
  want a true aim attachment for `AimPose` alignment.

### G2 -- prefab wiring

On the Citizen body's `SkinnedModelRenderer` set
`CreateAttachments = true` (the `VRHolsterSlot` will also flip this on
`OnStart` for safety, but the prefab default removes one frame of
catch-up).

For each holster you want, add a child GameObject under the body with a
`VRHolsterSlot` component:

```
Player
  └── BodyRoot
       └── BodyMesh (SkinnedModelRenderer, CreateAttachments=true)
            └── BackRifleSlot (VRHolsterSlot)
                  SourceRenderer = BodyMesh
                  AttachmentName = "back_rifle"
                  AcceptItemTag  = "rifle"
                  ProximityRadius = 8
                  UseSpringPhysics = true
                  LinearFrequency = 4
                  AngularFrequency = 3
                  DampingRatio = 0.7
            └── HipPistolSlotL (VRHolsterSlot, AttachmentName = "hip_holster_l", ...)
            └── ...
```

### G3 -- runtime behaviour

| Hand state | Action | Result |
| --- | --- | --- |
| `Searching` near non-empty slot, grip pressed | `Slot.TryUnholster` -> `Grab(item.GrabPoints[firstMain])` | Item snaps to hand pose. Spring joint (if any) is destroyed. |
| `Holding` near empty accepting slot, grip released | `Slot.TryHolster(item)` -> regular `Drop()` already done | Item parents to attachment. `UseSpringPhysics=true` keeps it dynamic with a `Sandbox.FixedJoint` so gravity + run-cycle motion swing it. |
| Anywhere else, grip released while holding | regular `Drop()` (with `IThrowVelocityEstimator` if enabled) | Same as before. |

`VRHolsterSlot.UseSpringPhysics` toggles between two modes:

- **`false` (rigid)**: `item.Body.SetParent(_attachGo)`,
  `MotionEnabled = false`. The item is permanently locked to the
  attachment. Use this for back / chest mounts you don't want to swing.
- **`true` (spring)**: keep the body dynamic. Create a
  `Sandbox.FixedJoint`:
  - `Body = item.Body.GameObject`
  - `AnchorBody = _attachGo`
  - `LinearFrequency = LinearFrequency` (Hz)
  - `AngularFrequency = AngularFrequency` (Hz)
  - `LinearDamping = AngularDamping = DampingRatio`
  - `EnableCollision = false` (item shouldn't collide with the body it's
    attached to)

  Lower frequencies = looser swing. `4 / 3` is a good "rifle on back"
  feel; `8 / 6` is closer to "magazine in pouch".

### G4 -- physical hand mode (`UsePhysicalHand`)

By default the hand body stays kinematic and we snap
`WorldPosition / WorldRotation` to the tracker pose every `OnPreRender`.
That means heavy items can never drag the hand back, because the snap
overwrites any physics impulse.

`VrhandInteraction.UsePhysicalHand = true` switches to the official
spring path:

1. `Body.MotionEnabled = true` (hand body becomes dynamic).
2. We create a `Sandbox.FixedJoint` with
   `Body = HandGameObject` and `AnchorBody = Reference (the tracker)`.
3. The kinematic-snap branch in `OnPreRender` is skipped; the joint
   pulls the hand toward the tracker each physics step instead.
4. While `Holding`, every weight-class change calls
   `ApplyPhysicalHandFrequencies(GrabWeightClass)`, mapping
   `Light` / `Medium` / `Heavy` to the configured Hz pairs:

| Weight class | Linear Hz (default) | Angular Hz (default) |
| --- | --- | --- |
| `Light`  | `25` | `22` |
| `Medium` | `14` | `12` |
| `Heavy`  | `8`  | `6`  |

5. `Grab(...)` anchors the held item's `ItemJoint` directly to the
   dynamic hand body (`Body.PhysicsBody`) instead of the kinematic
   `JointPoint`. This is what couples weapon weight back into the hand:
   the spring force from `ItemJoint` produces an equal-and-opposite pull
   on the hand body, which then lags behind the tracker until the
   per-class spring catches it up.

Risks / caveats:

- A dynamic hand will collide with held items unless you also set
  `EnableCollision = false` on the relevant hand colliders. The
  per-joint flag is already set false on the new `_handAnchorJoint`.
- This is a **toggle** because it changes physics behaviour
  perceptibly. Default is `false` so existing builds match the
  pre-integration kinematic snap exactly.

---

## Verification

### Unit tests

```
dotnet test Code/unittest/tftvrfullbody.unittest.csproj
```

Expected: **passed: 26 / failed: 0**. New files:

- `Code/unittest/VRLogic/NullControllerTests.cs` -- `IsTracked`,
  `IsHandTracking`, all analog/digital defaults, finger reads, both
  haptic overloads + `StopAllHaptics()`.
- `Code/unittest/VRLogic/VRFingerKindTests.cs` -- ordinal mapping for
  every entry of `Sandbox.VR.FingerValue`.

### Manual VR checklist (with HMD)

1. Pick up a weapon by pointing at it -- the search ray should track
   the trigger / muzzle direction (AimPose), not the wrist.
2. Pull the trigger past 0.9 -- hand should buzz with
   `HapticEffect.HardImpact` once per pull (not every frame).
3. Cycle the slide -- both the slide hand and the gun-hand should pop
   on chambering.
4. Move to back attachment range, press grip -- weapon should swing in
   under spring physics; running visibly bounces it.
5. With `UsePhysicalHand = true`, swap to a Heavy item -- the hand
   should noticeably lag the controller; releasing the item recovers
   immediately.
6. Disable VR (no HMD) -- nothing in the pipeline should throw;
   `NullController` covers every read, weapon `OnAwake` short-circuits
   on `IsAvailable == false`.
