# VR Migration Validation

## Automated Checks

- Run unit tests:
  - `dotnet test "Code/unittest/tftvrfullbody.unittest.csproj"`

## Manual Validation

### Grab Resolver

- Verify attachment-first:
  - Add `weapon_hold` attachment on weapon model and grab it.
  - Hand should align attachment without jitter.
- Verify fallback:
  - Remove/rename attachment and keep `GrabPoint`.
  - Hand should still grab via `GrabPoint`.

### Throw / Stabilizer

- Fast swing and release:
  - Release velocity should be clamped and stable.
- Wrist spin stress:
  - Rotation should not overshoot when rapidly twisting controller.

### Weight Profiles

- Compare `vr_weight_light` vs `vr_weight_heavy` tags:
  - Heavy should follow slower and release with lower max speed.

### Rig Rebind

- Trigger `VRAnimationHelper.RebindRig("default")` at runtime:
  - Hand IK targets should blend back without large pop.
