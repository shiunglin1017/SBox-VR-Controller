using System.Collections.Generic;
using Sandbox;
using Sandbox.VR;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Implements <see cref="IHandSkeletonProvider"/> by polling
/// <c>Sandbox.VR.VRController.GetJoints</c> once per <c>OnUpdate</c> and
/// stashing the result in a re-used <see cref="List{T}"/>. Without this cache,
/// <c>VRAnimationHelper.AnimateFingers</c> would have to call <c>GetJoints</c>
/// every iteration; SteamVR returns a fresh allocation each call so we'd be
/// hammering the GC.
/// </summary>
[Title( "VR Hand Skeleton Provider" )]
[Category( "VR/Services" )]
[Icon( "front_hand" )]
public sealed class SandboxVRHandSkeletonProvider : Component, IHandSkeletonProvider
{
	[Property] public HandSide Side { get; set; }

	private readonly List<VRHandJointData> _controllerJoints = new( 32 );
	private readonly List<VRHandJointData> _handJoints = new( 32 );

	public bool HasSkeleton { get; private set; }
	public IReadOnlyList<VRHandJointData> Joints => _controllerJoints;
	public IReadOnlyList<VRHandJointData> RawHandJoints => _handJoints;

	private VRController Controller =>
		Side == HandSide.Left ? Input.VR.LeftHand : Input.VR.RightHand;

	protected override void OnUpdate()
	{
		_controllerJoints.Clear();
		_handJoints.Clear();
		HasSkeleton = false;

		if ( Input.VR is null )
			return;

		var controllerData = Controller.GetJoints( MotionRange.Controller );
		if ( controllerData is { Length: > 0 } )
		{
			_controllerJoints.AddRange( controllerData );
			HasSkeleton = true;
		}

		var handData = Controller.GetJoints( MotionRange.Hand );
		if ( handData is { Length: > 0 } )
		{
			_handJoints.AddRange( handData );
			HasSkeleton = true;
		}
	}
}
