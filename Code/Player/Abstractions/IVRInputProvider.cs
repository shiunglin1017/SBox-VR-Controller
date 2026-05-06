namespace TFT.VR.Abstractions;

/// <summary>
/// Abstracts <c>Sandbox.Input.VR</c> behind an injectable service. Consumers
/// resolve a single instance via <c>Components.Get&lt;IVRInputProvider&gt;</c>
/// in <c>OnAwake</c> / <c>OnStart</c> and never touch <c>Input.VR</c> directly.
///
/// <para>
/// Provider implementations are responsible for proxy / non-VR handling.
/// <see cref="LeftHand"/> and <see cref="RightHand"/> must always return a
/// non-null <see cref="IControllerInput"/>; substitute <c>NullController</c>
/// when <see cref="IsAvailable"/> is false.
/// </para>
/// </summary>
public interface IVRInputProvider
{
	bool IsRunningInVR { get; }
	bool IsAvailable { get; }

	IControllerInput LeftHand { get; }
	IControllerInput RightHand { get; }

	IControllerInput GetHand( HandSide side );
}
