using Sandbox;
using Sandbox.VR;
using System.Collections.Generic;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Single source of truth for VR input on the player. Sits on the player root
/// alongside the existing <c>Sandbox.VR.VRAnchor</c> and is referenced by
/// every VR-aware component via <see cref="IVRInputProvider"/>.
///
/// <para>
/// Also handles the &quot;owner-only&quot; concern for VR: when this player is
/// a network proxy (or the runtime isn't actually in VR), we disable the
/// referenced <see cref="VRAnchor"/> and any tracked objects so that proxy
/// players don't have their networked transforms overwritten by the local
/// user's pose.
/// </para>
/// </summary>
[Title( "VR Input Provider (Sandbox)" )]
[Category( "VR/Services" )]
[Icon( "vrpano" )]
public sealed class SandboxVRInputProvider : Component, IVRInputProvider
{
	[Property, Group( "Debug" )] public bool DebugLogs { get; set; }

	/// <summary>
	/// The <c>Sandbox.VR.VRAnchor</c> on the player. Disabled automatically on
	/// proxies and outside VR runtime.
	/// </summary>
	[Property] public VRAnchor Anchor { get; set; }

	/// <summary>
	/// Every <c>Sandbox.VR.VRTrackedObject</c> on this player (head + both
	/// hand references). Disabled together with <see cref="Anchor"/> for
	/// proxies.
	/// </summary>
	[Property] public List<VRTrackedObject> ManagedTrackers { get; set; } = new();

	private readonly VRControllerAdapter _left  = new( HandSide.Left );
	private readonly VRControllerAdapter _right = new( HandSide.Right );

	public bool IsRunningInVR => Game.IsRunningInVR;
	public bool IsAvailable => !IsProxy && IsRunningInVR && Input.VR != null;

	public IControllerInput LeftHand  => IsAvailable ? (IControllerInput)_left  : NullController.Left;
	public IControllerInput RightHand => IsAvailable ? (IControllerInput)_right : NullController.Right;

	public IControllerInput GetHand( HandSide side ) =>
		side == HandSide.Left ? LeftHand : RightHand;

	protected override void OnAwake()
	{
		ApplyOwnership();
	}

	protected override void OnEnabled()
	{
		ApplyOwnership();
	}

	private void ApplyOwnership()
	{
		var enable = !IsProxy && Game.IsRunningInVR;

		if ( DebugLogs )
		{
			Log.Info( $"[VRProvider] go={GameObject?.Name} isProxy={IsProxy} runningVR={Game.IsRunningInVR} inputVR={(Input.VR != null)} enable={enable}" );
		}

		if ( Anchor.IsValid() )
			Anchor.Enabled = enable;

		if ( ManagedTrackers == null )
			return;

		for ( int i = 0; i < ManagedTrackers.Count; i++ )
		{
			var t = ManagedTrackers[i];
			if ( t.IsValid() )
			{
				t.Enabled = enable;
				if ( DebugLogs )
					Log.Info( $"[VRProvider] tracker[{i}] go={t.GameObject?.Name} enabled={t.Enabled} poseSource={t.PoseSource}" );
			}
		}
	}
}
