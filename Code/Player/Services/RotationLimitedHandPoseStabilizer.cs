using Sandbox;
using TFT.VR.Abstractions;
using TFT.VR.Logic;

namespace TFT.VR.Services;

[Title( "Hand Pose Stabilizer (Rotation Limited)" )]
[Category( "VR/Services" )]
[Icon( "3d_rotation" )]
public sealed class RotationLimitedHandPoseStabilizer : Component, IHandPoseStabilizer
{
	public Transform Stabilize( Transform current, Transform target, GrabWeightProfile profile, float deltaTime )
	{
		var posT = MathX.Clamp( deltaTime * profile.FollowPositionLerp, 0f, 1f );
		var rotBaseT = MathX.Clamp( deltaTime * profile.FollowRotationLerp, 0f, 1f );

		var currentRot = current.Rotation;
		var targetRot = target.Rotation;
		var angle = currentRot.Distance( targetRot );
		var rotT = RotationClampRules.ClampInterpolationBySpeed( angle, rotBaseT, profile.MaxDegreesPerSecond, deltaTime );

		return new Transform(
			Vector3.Lerp( current.Position, target.Position, posT ),
			Rotation.Slerp( currentRot, targetRot, rotT ) );
	}
}
