using Sandbox;
using Sandbox.VR;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Thin wrapper over <see cref="Sandbox.VR.VRController"/> that exposes the
/// same data through <see cref="IControllerInput"/>. The adapter forwards every
/// read straight to the official structured input types
/// (<see cref="AnalogInput"/>, <see cref="AnalogInput2D"/>,
/// <see cref="DigitalInput"/>) so consumers get <c>Delta</c> / <c>Active</c> /
/// <c>WasPressed</c> for free without mirroring the state ourselves.
/// </summary>
internal sealed class VRControllerAdapter : IControllerInput
{
	public HandSide Side { get; }

	public VRControllerAdapter( HandSide side )
	{
		Side = side;
	}

	private VRController Controller =>
		Side == HandSide.Left ? Input.VR.LeftHand : Input.VR.RightHand;

	public bool IsTracked => Input.VR != null;
	public bool IsHandTracking => Input.VR != null && Controller.IsHandTracked;

	public Transform GripPose => Input.VR != null ? Controller.Transform : Transform.Zero;
	public Transform AimPose => Input.VR != null ? Controller.AimTransform : Transform.Zero;

	public Vector2 Joystick => Controller.Joystick.Value;
	public Vector2 JoystickDelta => Controller.Joystick.Delta;
	public bool JoystickActive => Controller.Joystick.Active;
	public bool JoystickPress => Controller.JoystickPress.IsPressed;
	public bool JoystickPressed => Controller.JoystickPress.WasPressed;

	public bool ButtonA => Controller.ButtonA.IsPressed;
	public bool ButtonAPressed => Controller.ButtonA.WasPressed;
	public bool ButtonAActive => Controller.ButtonA.Active;
	public bool ButtonB => Controller.ButtonB.IsPressed;
	public bool ButtonBPressed => Controller.ButtonB.WasPressed;
	public bool ButtonBActive => Controller.ButtonB.Active;

	public float Trigger => Controller.Trigger.Value;
	public float TriggerDelta => Controller.Trigger.Delta;
	public bool TriggerActive => Controller.Trigger.Active;

	public float Grip => Controller.Grip.Value;
	public float GripDelta => Controller.Grip.Delta;
	public bool GripActive => Controller.Grip.Active;

	public float GetFingerCurl( int finger ) => Controller.GetFingerCurl( finger );
	public float GetFingerSplay( int finger ) => Controller.GetFingerSplay( finger );

	public float GetFingerValue( VRFingerKind kind ) =>
		Controller.GetFingerValue( (FingerValue) kind );

	public void TriggerHaptic( float duration, float frequency, float amplitude ) =>
#pragma warning disable CS0618 // legacy overload kept for compat path
		Controller.TriggerHapticVibration( duration, frequency, amplitude );
#pragma warning restore CS0618

	public void TriggerHaptic( HapticEffect effect, float lengthScale = 1f, float frequencyScale = 1f, float amplitudeScale = 1f ) =>
		Controller.TriggerHaptics( effect, lengthScale, frequencyScale, amplitudeScale );

	public void StopAllHaptics() => Controller.StopAllHaptics();
}
