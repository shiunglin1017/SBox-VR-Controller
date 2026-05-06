using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// No-op <see cref="IControllerInput"/> used whenever VR isn't available
/// (editor without HMD, network proxy, mid-frame between mode switches).
/// Returning this instead of <c>null</c> lets every consumer keep its
/// straight-line path without per-call null checks.
/// </summary>
internal sealed class NullController : IControllerInput
{
	public static readonly NullController Left  = new( HandSide.Left );
	public static readonly NullController Right = new( HandSide.Right );

	private NullController( HandSide side ) { Side = side; }

	public HandSide Side { get; }
	public bool IsTracked => false;

	public Vector2 Joystick => Vector2.Zero;
	public bool JoystickActive => false;
	public bool JoystickPress => false;
	public bool JoystickPressed => false;

	public bool ButtonA => false;
	public bool ButtonAPressed => false;
	public bool ButtonB => false;
	public bool ButtonBPressed => false;

	public float Trigger => 0f;
	public float Grip => 0f;

	public float GetFingerCurl( int finger ) => 0f;

	public void TriggerHaptic( float duration, float frequency, float amplitude ) { }
}
