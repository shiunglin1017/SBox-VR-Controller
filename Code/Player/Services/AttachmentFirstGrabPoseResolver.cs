using Sandbox;
using TFT.VR.Abstractions;
using TFT.VR.Logic;

namespace TFT.VR.Services;

[Title( "Grab Pose Resolver (Attachment First)" )]
[Category( "VR/Services" )]
[Icon( "pan_tool" )]
public sealed class AttachmentFirstGrabPoseResolver : Component, IGrabPoseResolver
{
	[Property] public bool UseAttachmentFirst { get; set; } = true;
	[Property] public string AttachmentName { get; set; } = VrInteractionConstants.DefaultGripAttachmentName;

	public bool TryResolvePose( HandSide handSide, GrabPoint heldPoint, out Transform pose )
	{
		pose = heldPoint.WorldTransform;
		if ( !heldPoint.IsValid() )
			return false;

		if ( UseAttachmentFirst &&
			TryResolveFromAttachment( heldPoint, out pose ) )
			return true;

		pose = heldPoint.WorldTransform;
		return true;
	}

	private bool TryResolveFromAttachment( GrabPoint heldPoint, out Transform pose )
	{
		pose = default;
		if ( string.IsNullOrWhiteSpace( AttachmentName ) )
			return false;

		var root = heldPoint.Body?.GameObject;
		if ( !root.IsValid() )
			root = heldPoint.GameObject.Parent.IsValid() ? heldPoint.GameObject.Parent : heldPoint.GameObject;

		var renderer = root.Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		if ( !renderer.IsValid() )
			return false;

		var tx = renderer.GetAttachment( AttachmentName );
		if ( !tx.HasValue )
			return false;

		pose = new Transform( tx.Value.Position, tx.Value.Rotation );
		return true;
	}
}
