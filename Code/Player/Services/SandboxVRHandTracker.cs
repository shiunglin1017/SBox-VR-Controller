using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Exposes the world transform of a hand reference GameObject (the one
/// driven by <c>Sandbox.VR.VRTrackedObject</c>) through the
/// <see cref="IHandTracker"/> abstraction. Place one on each
/// <c>HandLRef</c> / <c>HandRRef</c>; consumers (notably
/// <c>VrhandInteraction</c>) read pose via the interface in
/// <c>OnPreRender</c>, the same phase the tracker writes into.
/// </summary>
[Title( "VR Hand Tracker (Sandbox)" )]
[Category( "VR/Services" )]
[Icon( "back_hand" )]
public sealed class SandboxVRHandTracker : Component, IHandTracker
{
	[Property] public HandSide Side { get; set; }

	/// <summary>
	/// The GameObject driven by <c>Sandbox.VR.VRTrackedObject</c>. Defaults to
	/// this component's own GameObject when left blank.
	/// </summary>
	[Property] public GameObject Reference { get; set; }

	GameObject IHandTracker.ReferenceObject => ResolvedReference;

	public bool IsTracked
	{
		get
		{
			if ( !Game.IsRunningInVR || Input.VR is null )
				return false;
			return ResolvedReference.IsValid();
		}
	}

	public Transform Pose =>
		ResolvedReference.IsValid() ? ResolvedReference.WorldTransform : WorldTransform;

	private GameObject ResolvedReference =>
		Reference.IsValid() ? Reference : GameObject;

	protected override void OnValidate()
	{
		Reference ??= GameObject;
	}
}
