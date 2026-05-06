# VR Troubleshooting

Quick checklist for common VR issues after the interface-based DI refactor.

## Symptoms and Fixes

### 1. Hands don't move with the controllers (Quest 3 / Index / etc.)

Most likely causes, in order:

1. **`SandboxVRInputProvider` missing on the player root.** Open
   `Assets/prefabs/Player.prefab` and confirm the four service components are
   on the root. If you opened the project in the editor and re-saved the
   prefab without them, re-add via:
   - `+ Component -> VR/Services -> VR Input Provider (Sandbox)`
   - Then `VR Movement Input Source`, `Keyboard Movement Input Source`,
     `Composite Movement Input Source`
   - Wire `Anchor` and `ManagedTrackers` on the provider, and
     `VRSource` / `KbmSource` on the composite source.

2. **`SandboxVRHandTracker` missing on `HandLRef` / `HandRRef`.** `Side` must
   be set to `Left` or `Right`, and `Reference` should point to the same
   GameObject the `VRTrackedObject` is on (defaults to self).

3. **`VRTrackedObject` got disabled.** The provider disables it for proxy
   players and outside VR. Confirm `Game.IsRunningInVR` is true and
   `Component.IsProxy` is false at runtime by adding a temporary
   `Log.Info(...)` in `SandboxVRInputProvider.ApplyOwnership()`.

### 2. Hand tracks but lags / overshoots

The legacy code used a `PhysicsSpring(150, 5)` `FixedJoint` to pull the hand
toward the controller. The new code snaps directly:

```csharp
// VrhandInteraction.OnPreRender (Searching state)
WorldPosition = _tracker.Pose.Position;
WorldRotation = _tracker.Pose.Rotation;
```

If you still see lag:

- Confirm the rigid body's `MotionEnabled` is `false` (set in `OnStart`).
- Confirm the script runs in `OnPreRender`, not `OnUpdate`. Animation bones
  and tracked transforms are written in pre-render; reading them in update
  yields previous-frame data.
- Check that nothing else is parenting / forcing the hand each frame
  (look for `WorldPosition =` or `SetParent` on `HandL` / `HandR`).

### 3. NullReferenceException at startup mentioning `Input.VR`

Should no longer happen - every consumer goes through `IVRInputProvider` and
falls back to `NullController` when VR is unavailable. If it does, search for
any leftover `Input.VR.X` access:

```powershell
rg -n "Input\.VR\." Code/
```

Add the standard guard:

```csharp
if ( _input is null || !_input.IsAvailable ) return;
```

### 4. Proxy player twitches when local user moves their hands

Make sure `SandboxVRInputProvider.ApplyOwnership()` ran:

- It runs in `OnAwake` and `OnEnabled`.
- It disables the provider's `Anchor` plus every `VRTrackedObject` in
  `ManagedTrackers`.

If a proxy still moves with you:

- Verify `IsProxy` is true on that player at runtime.
- Verify `ManagedTrackers` lists exactly the head + two hand `VRTrackedObject`
  components on the prefab. Missing entries leave their tracker enabled.

### 5. Keyboard mode broken after the refactor

`PlayerWalkControllerSimple` no longer reads `Input.AnalogMove` directly; it
reads `IMovementInputSource.WishMove`. If you removed the
`KeyboardMovementInputSource` or `CompositeMovementInputSource` from the
prefab the keyboard fallback won't resolve.

Resolution sequence at runtime:

1. `PlayerWalkControllerSimple.OnAwake` calls
   `Components.Get<IMovementInputSource>( FindMode.EverythingInSelfAndAncestors )`.
2. With the prefab as shipped, that returns `CompositeMovementInputSource`.
3. The composite picks `VRMovementInputSource` if `Game.IsRunningInVR`,
   otherwise `KeyboardMovementInputSource`.

## Diagnostic Snippets

### Print active provider state

Add this temporarily anywhere you have a reference to the provider:

```csharp
Log.Info( $"VR available={_input?.IsAvailable}, " +
          $"L tracked={_input?.LeftHand.IsTracked}, " +
          $"R tracked={_input?.RightHand.IsTracked}" );
```

### Check what `Components.Get` finds

```csharp
foreach ( var c in GameObject.Components.GetAll<Component>( FindMode.EverythingInSelfAndAncestors ) )
    Log.Info( $"{c.GameObject.Name}.{c.GetType().Name} enabled={c.Enabled}" );
```

## Build commands

The project is a standard s&box / Sandbox project. Open
`sboxvrcontroller.sbproj` in the s&box launcher and use the editor's
Compile / Play / Mount workflow - there is no separate command-line build for
this template. Validate the DI prefab JSON after manual edits with:

```powershell
node -e "JSON.parse(require('fs').readFileSync('Assets/prefabs/Player.prefab','utf8')); console.log('OK')"
```
