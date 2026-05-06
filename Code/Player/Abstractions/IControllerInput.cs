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

	Vector2 Joystick { get; }
	bool JoystickActive { get; }
	bool JoystickPress { get; }
	bool JoystickPressed { get; }

	bool ButtonA { get; }
	bool ButtonAPressed { get; }
	bool ButtonB { get; }
	bool ButtonBPressed { get; }

	float Trigger { get; }
	float Grip { get; }

	float GetFingerCurl( int finger );

	void TriggerHaptic( float duration, float frequency, float amplitude );
}
