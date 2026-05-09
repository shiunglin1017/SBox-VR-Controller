using Sandbox;

namespace TFT.VR.Abstractions;

/// <summary>
/// Per-frame snapshot of a single VR controller. Implementations are expected to
/// refresh their state once per frame (in <c>OnUpdate</c> or <c>OnPreRender</c>),
/// so consumers can read the same value many times without re-entering the
/// underlying <c>Input.VR</c> graph.
///
/// <para>
/// When VR isn't running or the player is a network proxy, the provider should
/// return a no-op implementation (<c>NullController</c>) instead of <c>null</c>.
/// </para>
/// </summary>
public interface IControllerInput
{
	HandSide Side { get; }
	bool IsTracked { get; }

	/// <summary>
	/// True when the controller is currently being represented by full hand
	/// tracking (Quest hand-tracking, Index skeletal). Forwarded from
	/// <c>Sandbox.VR.VRController.IsHandTracked</c>.
	/// </summary>
	bool IsHandTracking { get; }

	/// <summary>
	/// Grip pose in world space (palm-centered). Mirrors
	/// <c>Sandbox.VR.VRController.Transform</c>.
	/// </summary>
	Transform GripPose { get; }

	/// <summary>
	/// Aim pose in world space (forward-pointing). Mirrors
	/// <c>Sandbox.VR.VRController.AimTransform</c>. Use this for ray casts /
	/// pointing / weapon iron sights instead of <see cref="GripPose"/>.
	/// </summary>
	Transform AimPose { get; }

	Vector2 Joystick { get; }
	Vector2 JoystickDelta { get; }
	bool JoystickActive { get; }
	bool JoystickPress { get; }
	bool JoystickPressed { get; }

	bool ButtonA { get; }
	bool ButtonAPressed { get; }
	bool ButtonAActive { get; }
	bool ButtonB { get; }
	bool ButtonBPressed { get; }
	bool ButtonBActive { get; }

	float Trigger { get; }
	float TriggerDelta { get; }
	bool TriggerActive { get; }

	float Grip { get; }
	float GripDelta { get; }
	bool GripActive { get; }

	float GetFingerCurl( int finger );

	/// <summary>
	/// Sideways spread of a finger (0..1). Mirrors
	/// <c>Sandbox.VR.VRController.GetFingerSplay(int)</c>.
	/// </summary>
	float GetFingerSplay( int finger );

	/// <summary>
	/// Read a specific finger curl/splay channel by name. Mirrors
	/// <c>Sandbox.VR.VRController.GetFingerValue(Sandbox.VR.FingerValue)</c>;
	/// the abstraction uses its own <see cref="VRFingerKind"/> enum so the
	/// abstractions layer doesn't take a direct dependency on
	/// <c>Sandbox.VR</c>.
	/// </summary>
	float GetFingerValue( VRFingerKind kind );

	/// <summary>
	/// Compatibility path for the legacy three-arg haptic call. Implementations
	/// must forward to the official non-obsolete API.
	/// </summary>
	void TriggerHaptic( float duration, float frequency, float amplitude );

	/// <summary>
	/// Pattern-based haptic vibration. Mirrors
	/// <c>Sandbox.VR.VRController.TriggerHaptics(HapticEffect, ...)</c>.
	/// </summary>
	void TriggerHaptic( HapticEffect effect, float lengthScale = 1f, float frequencyScale = 1f, float amplitudeScale = 1f );

	/// <summary>
	/// Stops every active haptic / vibration on this controller. Mirrors
	/// <c>Sandbox.VR.VRController.StopAllHaptics()</c>.
	/// </summary>
	void StopAllHaptics();
}

/// <summary>
/// Mirrors <c>Sandbox.VR.FingerValue</c> with identical ordinal values so the
/// adapter can cast directly. Keeping the enum here means consumers don't need
/// to <c>using Sandbox.VR;</c> just to ask for a finger value.
/// <para>
/// Note: the splay values intentionally start at 10, matching the runtime
/// gap between the curl group (0..4) and the splay group (10..13).
/// </para>
/// </summary>
public enum VRFingerKind
{
	ThumbCurl = 0,
	IndexCurl = 1,
	MiddleCurl = 2,
	RingCurl = 3,
	PinkyCurl = 4,
	ThumbIndexSplay = 10,
	IndexMiddleSplay = 11,
	MiddleRingSplay = 12,
	RingPinkySplay = 13,
}
