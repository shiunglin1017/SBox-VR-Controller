using Sandbox;

namespace TFT.VR.Abstractions;

/// <summary>
/// Abstracts movement input so that <c>PlayerWalkControllerSimple</c> can be
/// driven by either VR controllers or keyboard / mouse without branching
/// internally. <see cref="WishMove"/> is a normalized analog vector in the
/// player's local frame (X = forward, Y = right by Source-2 conventions).
/// </summary>
public interface IMovementInputSource
{
	Vector3 WishMove { get; }
	bool WantsJump { get; }
	bool WantsCrouch { get; }
	bool WantsSlowWalk { get; }
}
