using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Non-VR fallback. Reads the standard Sandbox keyboard / gamepad bindings so
/// the player can still test the project without an HMD attached.
/// </summary>
[Title( "Keyboard Movement Input Source" )]
[Category( "VR/Services" )]
[Icon( "keyboard" )]
public sealed class KeyboardMovementInputSource : Component, IMovementInputSource
{
	public Vector3 WishMove => Input.AnalogMove;
	public bool WantsJump => Input.Pressed( "Jump" );
	public bool WantsCrouch => Input.Down( "Duck" );
	public bool WantsSlowWalk => Input.Down( "Walk" );
}
