using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Wraps a single ModelDoc <c>attachment</c> as a holster / inventory slot.
/// <para>
/// The slot is intentionally thin: it asks the source <c>ModelRenderer</c>
/// for the <c>GameObject</c> that the engine creates for the named
/// attachment and uses that object as either a parent (rigid mode) or as a
/// joint anchor (spring mode). All physics behaviour comes from the
/// official <see cref="FixedJoint"/> component - no custom follower or
/// per-frame transform copy.
/// </para>
/// </summary>
[Title( "VR Holster Slot" )]
[Category( "VR/Services" )]
[Icon( "inventory_2" )]
public sealed class VRHolsterSlot : Component
{
	[Property] public ModelRenderer SourceRenderer { get; set; }
	[Property] public string AttachmentName { get; set; } = "";

	/// <summary>Optional tag the candidate item must carry. Empty string
	/// means "accept anything".</summary>
	[Property] public string AcceptItemTag { get; set; } = "";

	/// <summary>How close the hand must be (world units) to hover this slot.
	/// Used by <c>VrhandInteraction</c>'s holster routing.</summary>
	[Property] public float ProximityRadius { get; set; } = 8f;

	/// <summary>True = let the item swing on a spring (gravity still applies);
	/// false = snap rigidly to the attachment.</summary>
	[Property, Group( "Physics" )] public bool UseSpringPhysics { get; set; } = true;

	/// <summary>Spring frequency in Hz. Lower = looser swing (more pronounced
	/// gravity sway). Mirrors <see cref="FixedJoint.LinearFrequency"/>.</summary>
	[Property, Group( "Physics" )] public float LinearFrequency { get; set; } = 4f;

	/// <summary>Angular spring frequency in Hz. Lower = lazier rotation
	/// follow.</summary>
	[Property, Group( "Physics" )] public float AngularFrequency { get; set; } = 3f;

	/// <summary>Damping ratio (0=no damping, 1=critical). Used for both
	/// linear and angular axes.</summary>
	[Property, Group( "Physics" )] public float DampingRatio { get; set; } = 0.7f;

	[Property, Group( "Debug" )] public bool DebugLogs { get; set; }

	private GameObject _attachGo;
	private Item _heldItem;
	private FixedJoint _joint;

	public bool ContainsItem => _heldItem.IsValid();
	public Item HeldItem => _heldItem;

	public Vector3 SlotWorldPosition =>
		_attachGo.IsValid() ? _attachGo.WorldPosition : WorldPosition;

	public Rotation SlotWorldRotation =>
		_attachGo.IsValid() ? _attachGo.WorldRotation : WorldRotation;

	protected override void OnStart()
	{
		ResolveAttachment();
	}

	private void ResolveAttachment()
	{
		if ( !SourceRenderer.IsValid() )
		{
			LogWarn( $"SourceRenderer not assigned" );
			Enabled = false;
			return;
		}

		// Engine only creates the per-attachment GameObjects when this is
		// flipped on; we explicitly opt in so the prefab author doesn't have
		// to remember.
		SourceRenderer.CreateAttachments = true;

		_attachGo = SourceRenderer.GetAttachmentObject( AttachmentName );
		if ( !_attachGo.IsValid() )
		{
			LogWarn( $"attachment '{AttachmentName}' not found on {SourceRenderer.GameObject?.Name} - disabling slot" );
			Enabled = false;
		}
	}

	public bool CanAccept( Item item )
	{
		if ( !item.IsValid() || ContainsItem )
			return false;
		if ( string.IsNullOrEmpty( AcceptItemTag ) )
			return true;
		return item.GameObject.Tags.Contains( AcceptItemTag );
	}

	/// <summary>
	/// Place <paramref name="item"/> at this slot. If <see cref="UseSpringPhysics"/>
	/// is true a <see cref="FixedJoint"/> is created so gravity + motion can
	/// pull the item against the spring; otherwise the item is parented and
	/// has its physics frozen.
	/// </summary>
	public bool TryHolster( Item item )
	{
		if ( !CanAccept( item ) )
			return false;
		if ( !_attachGo.IsValid() || !item.Body.IsValid() )
			return false;

		_heldItem = item;

		if ( !UseSpringPhysics )
		{
			// Rigid lock: no joint, just parent the body and freeze it. We
			// keep MotionEnabled = false so collisions still register but
			// the body can't translate / rotate.
			item.Body.GameObject.SetParent( _attachGo );
			item.Body.WorldTransform = _attachGo.WorldTransform;
			item.Body.MotionEnabled = false;
			LogDebug( $"holstered (rigid) {item.Name} on {AttachmentName}" );
			return true;
		}

		// Spring mode: keep the body dynamic (gravity + collision still on)
		// and let an official Sandbox.FixedJoint pull it back to the
		// attachment. Low frequency = pronounced sway under load.
		item.Body.MotionEnabled = true;
		item.Body.WorldTransform = _attachGo.WorldTransform;

		_joint = item.GameObject.Components.Create<FixedJoint>();
		_joint.Body = item.Body.GameObject;
		_joint.AnchorBody = _attachGo;
		_joint.LinearFrequency = LinearFrequency;
		_joint.LinearDamping = DampingRatio;
		_joint.AngularFrequency = AngularFrequency;
		_joint.AngularDamping = DampingRatio;
		_joint.EnableCollision = false;

		LogDebug( $"holstered (spring) {item.Name} on {AttachmentName}" );
		return true;
	}

	/// <summary>
	/// Removes the held item from the slot. Tears down any joint we created
	/// and reparents the body to the scene root so the next grabber can pick
	/// it up cleanly.
	/// </summary>
	public bool TryUnholster( out Item item )
	{
		item = _heldItem;
		if ( !_heldItem.IsValid() )
			return false;

		if ( _joint.IsValid() )
		{
			_joint.Destroy();
			_joint = null;
		}

		if ( _heldItem.Body.IsValid() )
		{
			_heldItem.Body.MotionEnabled = true;
			_heldItem.Body.GameObject.SetParent( null );
		}

		LogDebug( $"unholstered {_heldItem.Name} from {AttachmentName}" );
		_heldItem = null;
		return true;
	}

	private void LogDebug( string message )
	{
		if ( !DebugLogs )
			return;
		Log.Info( $"[VRHolsterSlot:{GameObject?.Name}] {message}" );
	}

	private void LogWarn( string message )
	{
		Log.Warning( $"[VRHolsterSlot:{GameObject?.Name}] {message}" );
	}
}
