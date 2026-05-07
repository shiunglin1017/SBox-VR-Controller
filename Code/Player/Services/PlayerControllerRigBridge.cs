using Sandbox;
using Sandbox.Citizen;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

[Title( "PlayerController Rig Bridge" )]
[Category( "VR/Services" )]
[Icon( "link" )]
public sealed class PlayerControllerRigBridge : Component, PlayerController.IEvents
{
	[Property] public VRAnimationHelper VRAnimationHelper { get; set; }
	[Property] public string MappingProfileId { get; set; } = "default";
	[Property] public bool RebindOnLand { get; set; } = false;

	protected override void OnStart()
	{
		VRAnimationHelper ??= Components.Get<VRAnimationHelper>( FindMode.EverythingInSelfAndDescendants );
	}

	void PlayerController.IEvents.OnLanded( float distance, Vector3 impactVelocity )
	{
		if ( !RebindOnLand || !VRAnimationHelper.IsValid() )
			return;

		VRAnimationHelper.RebindRig( MappingProfileId );
	}
}
