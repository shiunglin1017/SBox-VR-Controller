using Sandbox;
using Sandbox.Citizen;
using TFT.VR.Abstractions;

namespace XMovement;

public partial class PlayerWalkControllerSimple : Component
{
	[RequireComponent] public PlayerMovement Controller { get; set; }
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public GameObject Head { get; set; }

	/// <summary>
	/// How quickly does the player move by default?
	/// </summary>
	[Property, Group( "Config" )] public float RunSpeed { get; set; } = 320.0f;
	[Property, Group( "Config" )] public float WalkSpeed { get; set; } = 120.0f;
	[Property, Group( "Config" )] public float CrouchSpeed { get; set; } = 80.0f;

	/// <summary>
	/// How powerful is the player's jump?
	/// </summary>
	[Property, Group( "Config" )] public float JumpPower { get; set; } = 268.3281572999747f;

	[Sync] public bool IsCrouching { get; set; }

	[Sync] public bool IsSlowWalking { get; set; }

	[Sync] public bool IsNoclipping { get; set; }

	[Sync] public Angles LocalEyeAngles { get; set; }
	public Angles EyeAngles
	{
		get
		{
			return LocalEyeAngles + GameObject.LocalRotation.Angles();
		}
		set
		{
			LocalEyeAngles = value - GameObject.LocalRotation.Angles();
		}
	}

	/// <summary>
	/// Do we want to jump next movement update?
	/// </summary>
	public bool WantsJump { get; set; }

	private IMovementInputSource _move;

	protected override void OnAwake()
	{
		_move = Components.Get<IMovementInputSource>( FindMode.EverythingInSelfAndAncestors );
	}

	protected override void OnUpdate()
	{


		base.OnUpdate();
		Controller.CenterOffset = Head.LocalPosition.WithZ( 0 );
		Controller.Height = Head.LocalPosition.z;
		if ( !IsProxy )
		{
			BuildFrameInput();
			if ( Controller.MovementFrequency == PlayerMovement.MovementFrequencyMode.PerUpdate ) DoMovement();
		}
	}

	protected override void OnFixedUpdate()
	{


		if ( !IsProxy )
		{
			if ( Controller.MovementFrequency == PlayerMovement.MovementFrequencyMode.PerFixedUpdate ) DoMovement();
		}
	}

	public void DoMovement()
	{
		Controller.PrepareMovement();

		BuildWishVelocity();
		BuildInput();

		if ( Controller.IsOnGround && WantsJump ) Jump();

		Controller.Move();

		ModelRenderer.Set( "b_grounded", Controller.IsOnGround );

		ResetFrameInput();
	}

	public void Jump()
	{
		Controller.LaunchUpwards( JumpPower );
		BroadcastPlayerJumped();
	}

	/// <summary>
	/// A network message that lets other users that we've triggered a jump.
	/// </summary>
	[Rpc.Broadcast]
	public void BroadcastPlayerJumped()
	{
		ModelRenderer.Set( "b_jump", true );
	}

	private void BuildFrameInput()
	{
		if ( _move is null )
			return;

		if ( _move.WantsJump )
			WantsJump = true;
	}

	private void ResetFrameInput()
	{
		WantsJump = false;
	}

	private void BuildInput()
	{
		if ( _move is null )
			return;

		IsSlowWalking = _move.WantsSlowWalk;
		IsCrouching = _move.WantsCrouch || !CanUncrouch();
	}

	protected float GetWishSpeed()
	{
		if ( IsCrouching ) return CrouchSpeed;
		if ( IsSlowWalking ) return WalkSpeed;
		return RunSpeed;
	}

	public void BuildWishVelocity()
	{
		var rot = Head.WorldRotation.Angles().WithPitch( 0f ).ToRotation();

		var input = _move?.WishMove ?? Vector3.Zero;

		var wishDirection = input.Normal * rot;
		wishDirection = wishDirection.WithZ( 0 );

		Controller.WishVelocity = wishDirection * GetWishSpeed();
	}

	private bool CanUncrouch()
	{
		var b = Controller.Height;
		if ( !IsCrouching ) return true;
		Controller.Height = 72;
		var tr = Controller.TraceDirection( Vector3.Zero );
		Controller.Height = b;
		return !tr.Hit;
	}

	private float _smoothEyeHeight;
	float LastSmoothEyeHeight = 0;

	protected float GetEyeHeightOffset()
	{
		if ( IsCrouching ) return -36f;
		return 0f;
	}

}
