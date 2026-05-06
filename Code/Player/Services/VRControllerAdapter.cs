using Sandbox;
using Sandbox.VR;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Thin wrapper over <see cref="Sandbox.VR.VRController"/> that exposes the
/// same data through <see cref="IControllerInput"/>. The adapter is stateless;
/// every property access reads <c>Input.VR.LeftHand</c> / <c>RightHand</c>
/// once. This is cheaper than it looks because <c>Input.VR</c> is internally
/// cached by Sandbox, and routing every read through one place lets us
/// short-circuit consistently when the headset is absent.
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

	public Vector2 Joystick => Controller.Joystick.Value;
	public bool JoystickActive => Controller.Joystick.Active;
	public bool JoystickPress => Controller.JoystickPress;
	public bool JoystickPressed => Controller.JoystickPress.WasPressed;

	public bool ButtonA => Controller.ButtonA;
	public bool ButtonAPressed => Controller.ButtonA.WasPressed;
	public bool ButtonB => Controller.ButtonB;
	public bool ButtonBPressed => Controller.ButtonB.WasPressed;

	public float Trigger => Controller.Trigger;
	public float Grip => Controller.Grip;

	public float GetFingerCurl( int finger ) => Controller.GetFingerCurl( finger );

	public void TriggerHaptic( float duration, float frequency, float amplitude ) =>
		Controller.TriggerHapticVibration( duration, frequency, amplitude );
}
