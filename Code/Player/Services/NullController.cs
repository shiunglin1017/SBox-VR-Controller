using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// No-op <see cref="IControllerInput"/> used whenever VR isn't available
/// (editor without HMD, network proxy, mid-frame between mode switches).
/// Returning this instead of <c>null</c> lets every consumer keep its
/// straight-line path without per-call null checks.
/// </summary>
public sealed class NullController : IControllerInput
{
	public static readonly NullController Left  = new( HandSide.Left );
	public static readonly NullController Right = new( HandSide.Right );

	public NullController( HandSide side ) { Side = side; }

	public HandSide Side { get; }
	public bool IsTracked => false;
	public bool IsHandTracking => false;

	public Transform GripPose => Transform.Zero;
	public Transform AimPose => Transform.Zero;

	public Vector2 Joystick => Vector2.Zero;
	public Vector2 JoystickDelta => Vector2.Zero;
	public bool JoystickActive => false;
	public bool JoystickPress => false;
	public bool JoystickPressed => false;

	public bool ButtonA => false;
	public bool ButtonAPressed => false;
	public bool ButtonAActive => false;
	public bool ButtonB => false;
	public bool ButtonBPressed => false;
	public bool ButtonBActive => false;

	public float Trigger => 0f;
	public float TriggerDelta => 0f;
	public bool TriggerActive => false;

	public float Grip => 0f;
	public float GripDelta => 0f;
	public bool GripActive => false;

	public float GetFingerCurl( int finger ) => 0f;
	public float GetFingerSplay( int finger ) => 0f;
	public float GetFingerValue( VRFingerKind kind ) => 0f;

	public void TriggerHaptic( float duration, float frequency, float amplitude ) { }
	public void TriggerHaptic( HapticEffect effect, float lengthScale = 1f, float frequencyScale = 1f, float amplitudeScale = 1f ) { }
	public void StopAllHaptics() { }
}
