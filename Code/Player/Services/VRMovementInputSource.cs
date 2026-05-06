using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Translates VR controller input into the abstract <see cref="IMovementInputSource"/>:
/// left stick drives <see cref="WishMove"/>, right A jumps, right B crouches,
/// pressing the left stick toggles between walk and run.
///
/// <para>
/// Falls back to zero / false whenever the provider isn't available, so it can
/// be safely composed with the keyboard source via <see cref="CompositeMovementInputSource"/>.
/// </para>
/// </summary>
[Title( "VR Movement Input Source" )]
[Category( "VR/Services" )]
[Icon( "videogame_asset" )]
public sealed class VRMovementInputSource : Component, IMovementInputSource
{
	[Property] public SandboxVRInputProvider Provider { get; set; }

	private IVRInputProvider Resolved =>
		Provider ?? Components.Get<IVRInputProvider>( FindMode.EverythingInSelfAndAncestors );

	public Vector3 WishMove
	{
		get
		{
			var p = Resolved;
			if ( p is null || !p.IsAvailable )
				return Vector3.Zero;

			var stick = p.LeftHand.Joystick;
			return new Vector3( stick.y, -stick.x, 0 );
		}
	}

	public bool WantsJump
	{
		get
		{
			var p = Resolved;
			return p is { IsAvailable: true } && p.RightHand.ButtonAPressed;
		}
	}

	public bool WantsCrouch
	{
		get
		{
			var p = Resolved;
			return p is { IsAvailable: true } && p.RightHand.ButtonB;
		}
	}

	public bool WantsSlowWalk
	{
		get
		{
			var p = Resolved;
			return p is { IsAvailable: true } && !p.LeftHand.JoystickPress;
		}
	}
}
