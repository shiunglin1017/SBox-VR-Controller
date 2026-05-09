using Sandbox;
using Sandbox.Citizen;
using Sandbox.Physics;
using System;
using System.Linq;
using System.Collections.Generic;
using TFT.VR.Abstractions;
using TFT.VR.Logic;
using TFT.VR.Services;

public sealed class VrhandInteraction : Component
{
	[Property, Group( "Debug" )] public bool DebugLogs { get; set; }
	[Property] private HandEnum Hand { get; set; }
	public enum HandEnum
	{
		Left,
		Right
	}

	[Property] private VRAnimationHelper VRAnimationHelper { get; set; }
	[Property] public GameObject Reference { get; set; }
	[Property] public GameObject IKTarget { get; set; }
	[Property] public GameObject UpRef { get; set; }
	[Property] private HandState CurrentHandState { get; set; }
	[Property] private float SearchRadius { get; set; } = 5f;
	[Property] private float SearchDistance { get; set; } = 200f;
	[Property] private float StrengthModifier { get; set; } = 1f;

	public enum HandState
	{
		None,
		Searching,
		Holding
	}

	private IVRInputProvider _input;
	private IHandTracker _tracker;
	private IGrabPoseResolver _grabPoseResolver;
	private IHandPoseStabilizer _handPoseStabilizer;
	private IThrowVelocityEstimator _throwVelocityEstimator;
	private IWeightProfileProvider _weightProfileProvider;

	public HandSide HandAsSide => Hand == HandEnum.Left ? HandSide.Left : HandSide.Right;

	/// <summary>
	/// Live <see cref="IControllerInput"/> for whichever physical controller
	/// drives this hand. Public so that anything held by this hand (weapons,
	/// magazines, ...) can poll button state without going back to
	/// <c>Input.VR</c> directly.
	/// </summary>
	public IControllerInput Controller => _input?.GetHand( HandAsSide );

	private VRAnimationHelper.VRHand AnimatedHand =>
		Hand.Equals( HandEnum.Left ) ? VRAnimationHelper.LeftHand : VRAnimationHelper.RightHand;

	private Rigidbody Body { get; set; }
	public Rigidbody JointPoint { get; set; }
	private Sandbox.Physics.FixedJoint ItemJoint { get; set; }
	private TimeUntil _nextDebugLog;
	private GrabWeightProfile _currentWeightProfile = GrabWeightProfile.Medium;
	private Vector3 _releaseLinearVelocity;
	private Vector3 _releaseAngularVelocity;
	private Vector3 _lastReferencePos;

	[Property, Group( "Grab Rules" )] private float GripPressThreshold { get; set; } = 0.5f;
	[Property, Group( "Grab Rules" )] private float GripReleaseThreshold { get; set; } = 0.2f;
	[Property, Group( "Throw" )] private bool UseThrowSignalEstimator { get; set; } = true;
	[Property, Group( "Throw" )] private int ThrowSignalSampleCount { get; set; } = 12;
	[Property, Group( "Throw" )] private int ThrowPeakNeighborhood { get; set; } = 2;

	/// <summary>
	/// When true the hand body becomes dynamic and is held against the
	/// tracker by an official <c>Sandbox.FixedJoint</c> with weight-class
	/// driven frequencies. Heavy items will visibly drag the hand back; the
	/// hand recovers as soon as the item is released. Off by default to
	/// preserve the existing kinematic snap behaviour.
	/// </summary>
	[Property, Group( "Physical Hand" )] public bool UsePhysicalHand { get; set; } = false;
	[Property, Group( "Physical Hand" )] public float PhysicalHandLightFreq { get; set; } = 25f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandLightAngFreq { get; set; } = 22f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandMediumFreq { get; set; } = 14f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandMediumAngFreq { get; set; } = 12f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandHeavyFreq { get; set; } = 8f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandHeavyAngFreq { get; set; } = 6f;
	[Property, Group( "Physical Hand" )] public float PhysicalHandDamping { get; set; } = 0.7f;

	private Sandbox.FixedJoint _handAnchorJoint;
	private GrabWeightClass _appliedPhysicalHandClass = (GrabWeightClass)(-1);

	Vector3 targetPos;
	Rotation targetRot;

	protected override void OnAwake()
	{
		_input = Components.Get<IVRInputProvider>( FindMode.EverythingInSelfAndAncestors );
		ResolveTracker();
		_grabPoseResolver = Components.Get<IGrabPoseResolver>( FindMode.EverythingInSelfAndAncestors );
		_handPoseStabilizer = Components.Get<IHandPoseStabilizer>( FindMode.EverythingInSelfAndAncestors );
		_throwVelocityEstimator = Components.Get<IThrowVelocityEstimator>( FindMode.EverythingInSelfAndAncestors );
		_weightProfileProvider = Components.Get<IWeightProfileProvider>( FindMode.EverythingInSelfAndAncestors );
	}

	protected override void OnStart()
	{
		Body = GetComponent<Rigidbody>();

		// JointPoint stays kinematic and acts as the world anchor for ItemJoint
		// while the player is holding something. We no longer wire it to Body
		// through a spring FixedJoint - that physics chain is what made the
		// hand lag behind the controller pose.
		JointPoint = new GameObject().AddComponent<Rigidbody>();
		JointPoint.GameObject.SetParent( GameObject.Parent );
		JointPoint.MotionEnabled = false;

		if ( Body.IsValid() )
		{
			if ( UsePhysicalHand )
			{
				// Dynamic hand: an official Sandbox.FixedJoint pulls us
				// toward the tracker pose with weight-tuned frequencies,
				// instead of snapping every frame. Heavy items can drag the
				// hand back via ItemJoint reaction forces (see Grab()).
				Body.MotionEnabled = true;
				CreatePhysicalHandAnchor();
			}
			else
			{
				// Hand body is purely kinematic; OnPreRender drives
				// WorldPosition / WorldRotation directly.
				Body.MotionEnabled = false;
			}
		}

		CurrentHandState = HandState.Searching;

		targetPos = IKTarget.LocalPosition;
		targetRot = IKTarget.LocalRotation;
		_lastReferencePos = Reference.IsValid() ? Reference.WorldPosition : WorldPosition;
	}

	private void CreatePhysicalHandAnchor()
	{
		if ( !UsePhysicalHand || !Reference.IsValid() || _handAnchorJoint.IsValid() )
			return;

		_handAnchorJoint = Components.Create<Sandbox.FixedJoint>();
		_handAnchorJoint.Body = GameObject;
		_handAnchorJoint.AnchorBody = Reference;
		_handAnchorJoint.EnableCollision = false;

		ApplyPhysicalHandFrequencies( _currentWeightProfile.WeightClass );
	}

	private void ApplyPhysicalHandFrequencies( GrabWeightClass weightClass )
	{
		if ( !_handAnchorJoint.IsValid() )
			return;
		if ( _appliedPhysicalHandClass == weightClass )
			return;

		float linear, angular;
		switch ( weightClass )
		{
			case GrabWeightClass.Light:
				linear = PhysicalHandLightFreq; angular = PhysicalHandLightAngFreq; break;
			case GrabWeightClass.Heavy:
				linear = PhysicalHandHeavyFreq; angular = PhysicalHandHeavyAngFreq; break;
			default:
				linear = PhysicalHandMediumFreq; angular = PhysicalHandMediumAngFreq; break;
		}

		_handAnchorJoint.LinearFrequency = linear;
		_handAnchorJoint.LinearDamping = PhysicalHandDamping;
		_handAnchorJoint.AngularFrequency = angular;
		_handAnchorJoint.AngularDamping = PhysicalHandDamping;
		_appliedPhysicalHandClass = weightClass;
	}

	HandState previousHandState;

	protected override void OnPreRender()
	{
		if ( IsProxy )
		{
			LogDebugOnce( "skip: is proxy" );
			return;
		}
		if ( _input is null || !_input.IsAvailable )
		{
			LogDebugOnce( $"skip: provider unavailable (_input null={_input is null})" );
			return;
		}

		var ctrl = Controller;
		if ( ctrl is not null && ctrl.IsTracked && Reference.IsValid() )
		{
			_releaseLinearVelocity = (Reference.WorldPosition - _lastReferencePos) / Math.Max( Time.Delta, 0.0001f );
			_releaseAngularVelocity = Vector3.Zero;
			_lastReferencePos = Reference.WorldPosition;
			_throwVelocityEstimator?.PushSample( _releaseLinearVelocity, _releaseAngularVelocity, Math.Max( 1, ThrowSignalSampleCount ) );
		}

		PositionJointPoint();

		AnimatedHand.NoControl = !CurrentHandState.Equals( HandState.Searching );

		if ( !UsePhysicalHand && CurrentHandState == HandState.Searching && _tracker is { IsTracked: true } )
		{
			// Kinematic mode: snap directly to the controller pose so the
			// hand keeps up with the headset frame-for-frame.
			var pose = _tracker.Pose;
			WorldPosition = pose.Position;
			WorldRotation = pose.Rotation;
			LogDebugOnce( $"tracking ok: state={CurrentHandState} pose={pose.Position}" );
		}
		else if ( UsePhysicalHand )
		{
			// Dynamic mode: the FixedJoint pulls us toward the tracker.
			// Nothing to do here other than ensure the joint is configured;
			// physics handles the lag / sway for free.
			LogDebugOnce( $"physical hand active: weight={_currentWeightProfile.WeightClass}" );
		}
		else
		{
			LogDebugOnce( $"no tracking snap: state={CurrentHandState} trackerNull={_tracker is null} trackerTracked={_tracker?.IsTracked}" );
		}

		switch ( CurrentHandState )
		{
			case HandState.Searching:
				Searching();
				break;

			case HandState.Holding:
				Holding();
				UpdateItemJoint();
				break;
		}

		previousHandState = CurrentHandState;
	}

	void UpdateItemJoint()
	{
		if ( !ItemJoint.IsValid() || !HeldPoint.IsValid() )
			return;

		ItemJoint.Point1 = new PhysicsPoint(
			ItemJoint.Point1.Body,
			HeldPoint.Body.WorldTransform.PointToLocal( HeldPoint.VisualPoint ),
			HeldPoint.Body.WorldTransform.RotationToLocal( HeldPoint.WorldRotation ) );

		if ( HeldPoint.RifleHold && HeldPoint.SecondaryPoint.Held )
		{
			var localForward = UpRef.WorldTransform.Forward;
			var targetForward = HeldPoint.SecondaryPoint.GrabbedHand.UpRef.WorldPosition - UpRef.WorldPosition;

			ItemJoint.Point2 = new PhysicsPoint(
				ItemJoint.Point2.Body,
				WorldTransform.PointToLocal( HeldPoint.VisualPoint ),
				WorldTransform.RotationToLocal( Rotation.FromToRotation( localForward, targetForward ) ) );
		}
		else
		{
			ItemJoint.Point2 = new PhysicsPoint(
				ItemJoint.Point2.Body,
				WorldTransform.PointToLocal( HeldPoint.VisualPoint ) );
		}

		var springScale = MathX.Clamp( _currentWeightProfile.FollowPositionLerp / 14f, 0.5f, 2f );
		ItemJoint.SpringLinear = new PhysicsSpring( 100 * HeldPoint.StrengthMult * springScale, 5 );
		ItemJoint.SpringAngular = new PhysicsSpring( 100 * HeldPoint.StrengthMult * springScale, 5 );
	}

	RealTimeSince SearchDelay;
	void Searching()
	{
		var ctrl = Controller;
		if ( ctrl is null )
			return;

		if ( previousHandState != HandState.Searching )
		{
			Tags.Remove( "activehand" );
			SearchDelay = 0f;
		}

		if ( SearchDelay < 0.5f )
			return;

		dropped = false;

		// Holster pickup pass: if the hand is hovering a non-empty slot and
		// the player presses grip, pull the item off the slot and grab it
		// using the existing grab path (which reuses ItemJoint, weight
		// profiles etc). We do this before the normal search so the player
		// always wins the race against scenery picks at the same range.
		if ( TryUnholsterFromNearbySlot( ctrl ) )
			return;

		List<GrabPoint> GrabbablePoints = new List<GrabPoint>();
		List<Interactable> InteractablePoints = new List<Interactable>();

		Search( ref GrabbablePoints, ref InteractablePoints );

		var closestDistance = 10000f;
		var closestPoint = GrabbablePoints.Count > 0 ? GrabbablePoints[0] : null;

		IKTarget.LocalPosition = targetPos;
		IKTarget.LocalRotation = targetRot;

		foreach ( var gPoint in GrabbablePoints )
		{
			var distance = Vector3.DistanceBetween( WorldPosition, gPoint.VisualPoint );

			if ( distance > closestDistance )
				continue;

			closestDistance = distance;

			closestPoint = gPoint;
		}

		if ( closestPoint.IsValid() )
			GrabPointSelection( closestPoint, ctrl );
	}

	void GrabPointSelection( GrabPoint closestPoint, IControllerInput ctrl )
	{
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.SolidSphere( closestPoint.VisualPoint, 0.5f );

		if ( GrabInteractionRules.ShouldStartGrab( ctrl.Grip, GripPressThreshold, HeldPoint.IsValid(), closestPoint.IsValid() ) )
			Grab( closestPoint );
	}


	[Property] GrabPoint HeldPoint { get; set; }

	/// <summary>True when this hand currently holds a grab-point.</summary>
	public bool IsHolding => HeldPoint.IsValid();

	/// <summary>The current high-level state machine state.</summary>
	public HandState State => CurrentHandState;
	void Holding()
	{
		var ctrl = Controller;
		if ( ctrl is null )
			return;

		if ( dropped )
			return;

		if ( GrabInteractionRules.ShouldReleaseGrab( ctrl.Grip, GripReleaseThreshold, HeldPoint.IsValid() ) ||
			(!HeldPoint.Main && !HeldPoint.MainPoint.Held) )
		{
			// If we're letting go right next to an empty accepting slot,
			// holster the held item instead of throwing it away. The slot
			// itself decides rigid vs spring physics. If no slot is in
			// range we fall through to the regular drop / throw path.
			if ( !TryHolsterIntoNearbySlot() )
				Drop();
			return;
		}

		if ( HeldPoint.IsValid() )
		{
			_currentWeightProfile = _weightProfileProvider?.ResolveProfile( HeldPoint, HeldPoint.RifleHold && HeldPoint.SecondaryPoint?.Held == true )
				?? _currentWeightProfile;

			ApplyPhysicalHandFrequencies( _currentWeightProfile.WeightClass );

			var HeldPointSkeleton = Hand.Equals( HandEnum.Left ) ? HeldPoint.LeftHand : HeldPoint.RightHand;
			if ( HeldPointSkeleton.IsValid() )
			{
				AnimatedHand.Root.WorldPosition = HeldPointSkeleton.WorldPosition;
				AnimatedHand.Root.WorldRotation = HeldPointSkeleton.WorldRotation;
				CopyTransformRecursive( HeldPointSkeleton, AnimatedHand.Root, Vector3.One, new Angles( 1, 1, 1 ) );
			}

			var targetPose = HeldPoint.WorldTransform;
			if ( _grabPoseResolver is not null && _grabPoseResolver.TryResolvePose( HandAsSide, HeldPoint, out var resolvedPose ) )
				targetPose = resolvedPose;

			if ( _handPoseStabilizer is not null )
				targetPose = _handPoseStabilizer.Stabilize( new Transform( WorldPosition, WorldRotation ), targetPose, _currentWeightProfile, Time.Delta );

			IKTarget.WorldPosition = targetPose.Position;
			IKTarget.WorldRotation = targetPose.Rotation;
			WorldPosition = targetPose.Position;
			WorldRotation = targetPose.Rotation;
		}
	}

	public void Grab( GrabPoint point )
	{
		Tags.Add( "activehand" );
		CurrentHandState = HandState.Holding;
		HeldPoint = point;
		HeldPoint.Held = true;
		HeldPoint.Hand = Hand;
		HeldPoint.GrabbedHand = this;

		if ( !HeldPoint.Main )
			return;

		_currentWeightProfile = _weightProfileProvider?.ResolveProfile( HeldPoint, HeldPoint.RifleHold && HeldPoint.SecondaryPoint?.Held == true )
			?? GrabWeightProfile.Medium;

		HeldPoint.Body.GameObject.SetParent( GameObject.Parent );

		var p1 = new PhysicsPoint( point.Body.PhysicsBody, point.Body.WorldTransform.PointToLocal( point.WorldPosition ), point.Body.WorldTransform.RotationToLocal( point.WorldRotation ) );

		// In physical-hand mode anchor the spring directly to the dynamic
		// hand body so heavy items can pull the hand back; otherwise stick
		// with the kinematic JointPoint which is glued to the tracker.
		var anchorBody = (UsePhysicalHand && Body.IsValid()) ? Body.PhysicsBody : JointPoint.PhysicsBody;
		var p2 = new PhysicsPoint( anchorBody, Vector3.Zero );

		ItemJoint = PhysicsJoint.CreateFixed( p1, p2 );
		var springScale = MathX.Clamp( _currentWeightProfile.FollowPositionLerp / 14f, 0.5f, 2f );
		ItemJoint.SpringLinear = new PhysicsSpring( 100 * HeldPoint.StrengthMult * springScale, 5 );
		ItemJoint.SpringAngular = new PhysicsSpring( 100 * HeldPoint.StrengthMult * springScale, 5 );
	}

	bool dropped;
	public void Drop()
	{
		if ( HeldPoint.IsValid() && HeldPoint.Main && HeldPoint.Body.IsValid() && UseThrowSignalEstimator && _throwVelocityEstimator is not null )
		{
			if ( _throwVelocityEstimator.TryEstimate( Math.Max( 0, ThrowPeakNeighborhood ), _currentWeightProfile, out var estLinear, out var estAngular ) )
			{
				HeldPoint.Body.Velocity = estLinear;
				HeldPoint.Body.AngularVelocity = estAngular;
			}
		}

		CurrentHandState = HandState.Searching;
		HeldPoint.Held = false;
		HeldPoint.GrabbedHand = null;

		if ( HeldPoint.Main && HeldPoint.DoUnparent() )
			HeldPoint?.Body.GameObject.SetParent( null );

		HeldPoint = null;
		ItemJoint?.Remove();
		ItemJoint = null;

		// Free hand -> stiffest spring so the hand snaps back to tracker.
		_currentWeightProfile = GrabWeightProfile.Light;
		ApplyPhysicalHandFrequencies( GrabWeightClass.Light );

		dropped = true;
	}

	void Search( ref List<GrabPoint> GrabbablePoints, ref List<Interactable> InteractablePoints )
	{
		for ( int i = 0; i < 2; i++ )
		{
			Vector3 searchPos = WorldPosition;
			if ( i > 0 )
			{
				// SteamVR's "aim pose" represents where the controller is
				// pointed (vs the grip pose, which is where it's held). Use it
				// for the search ray so picking targets feels natural - the
				// hand-root forward is only a fallback when AimPose isn't
				// available (e.g. hand-tracking degraded modes).
				var ctrl = Controller;
				Vector3 rayOrigin;
				Vector3 rayForward;
				if ( ctrl is not null && ctrl.IsTracked )
				{
					var aim = ctrl.AimPose;
					rayOrigin = aim.Position;
					rayForward = aim.Forward;
				}
				else
				{
					rayOrigin = AnimatedHand.Root.WorldPosition;
					rayForward = AnimatedHand.Root.WorldTransform.Forward;
				}

				var ray = Scene.Trace.Ray( rayOrigin, rayOrigin + rayForward * SearchDistance ).Radius( SearchRadius ).WithoutTags( "uninteractable" ).Run();
				if ( ray.Hit ) searchPos = ray.HitPosition;
			}
			IEnumerable<GameObject> gameObjects = Scene.FindInPhysics( new Sphere( searchPos, SearchRadius ) );
			GrabbablePoints = new List<GrabPoint>();
			InteractablePoints = new List<Interactable>();

			foreach ( GameObject g in gameObjects )
			{
				if ( g.Tags.Contains( "uninteractable" ) ) continue;

				if ( i > 0 && g.Tags.Contains( "closepickup" ) ) continue;

				if ( g.Tags.Contains( "interactable" ) )
				{
					var interactablePoint = g.GetComponent<Interactable>();
					if ( !interactablePoint.IsValid )
						continue;
					i = 10;
					InteractablePoints.Add( interactablePoint );
				}
				if ( g.Tags.Contains( "grabpoint" ) )
				{

					var grabPoint = g.GetComponent<GrabPoint>();
					if ( !grabPoint.IsValid() )
						continue;

					if ( grabPoint.Held )
						continue;

					if ( !grabPoint.Main && !grabPoint.MainPoint.Held )
						continue;

					i = 10;
					GrabbablePoints.Add( grabPoint ); ;
				}
			}
		}
	}

	/// <summary>
	/// Keeps the (kinematic) JointPoint glued to the controller-tracked Reference
	/// so that ItemJoint's anchor updates with the player's hand. Trace nudges
	/// the anchor away from world geometry to stop held items penetrating walls.
	/// </summary>
	void PositionJointPoint()
	{
		if ( !Reference.IsValid() || !JointPoint.IsValid() )
			return;

		JointPoint.WorldRotation = Reference.WorldRotation;

		var direction = Reference.WorldPosition - JointPoint.WorldPosition;
		var trace = Scene.Trace.Ray( JointPoint.WorldPosition, Reference.WorldPosition + direction.Normal * 2 )
			.IgnoreGameObjectHierarchy( GameObject.Parent ).Run();

		JointPoint.WorldPosition = trace.Hit
			? trace.HitPosition - direction.Normal * 2
			: Reference.WorldPosition;
	}

	public static void CopyTransformRecursive( GameObject target, GameObject set, Vector3 posMod, Angles angMod, float lerp = 1 )
	{

		for ( int i = 0; i < target.Children.Count; i++ )
		{
			if ( i >= set.Children.Count )
				continue;
			GameObject targetChild = target.Children[i];
			GameObject setChild = set.Children[i];

			setChild.LocalPosition = Vector3.Lerp( setChild.LocalPosition, targetChild.LocalPosition * posMod, lerp );
			Vector3 modifiedAngles = targetChild.LocalRotation.Angles().AsVector3() * angMod.AsVector3();
			setChild.LocalRotation = Angles.Lerp( setChild.LocalRotation.Angles(), new Angles( modifiedAngles.x, modifiedAngles.y, modifiedAngles.z ), lerp );
			CopyTransformRecursive( targetChild, setChild, posMod, angMod, lerp );
		}
	}

	public static void CopyTransformRecursiveLerp( GameObject targetFrom, GameObject targetTo, GameObject set, Vector3 posMod, Angles angMod, float targetLerp, float lerp = 1 )
	{

		for ( int i = 0; i < targetFrom.Children.Count; i++ )
		{
			if ( i >= set.Children.Count )
				continue;
			GameObject targetFromChild = targetFrom.Children[i];
			GameObject targetToChild = targetTo.Children[i];
			GameObject setChild = set.Children[i];

			setChild.LocalPosition = Vector3.Lerp( setChild.LocalPosition, Vector3.Lerp( targetFromChild.LocalPosition, targetToChild.LocalPosition, targetLerp ) * posMod, lerp );
			Vector3 modifiedAngles = Vector3.Lerp( targetFromChild.LocalRotation.Angles().AsVector3(), targetToChild.LocalRotation.Angles().AsVector3(), targetLerp ) * angMod.AsVector3();
			setChild.LocalRotation = Angles.Lerp( setChild.LocalRotation.Angles(), new Angles( modifiedAngles.x, modifiedAngles.y, modifiedAngles.z ), lerp );
			CopyTransformRecursiveLerp( targetFromChild, targetToChild, setChild, posMod, angMod, lerp );
		}
	}

	private void LogDebugOnce( string message )
	{
		if ( !DebugLogs || _nextDebugLog > 0f )
			return;

		Log.Info( $"[VrhandInteraction:{Hand}] go={GameObject?.Name} {message}" );
		_nextDebugLog = 0.5f;
	}

	private void ResolveTracker()
	{
		// First try local hierarchy (cheap path).
		_tracker = Components.GetAll<IHandTracker>( FindMode.EverythingInSelfAndAncestors )
			.FirstOrDefault( t => t.Side == HandAsSide );

		if ( _tracker is not null )
			return;

		// Fallback: hand refs are siblings under the same player root, so they are
		// outside "self+ancestors". Search scene trackers and pick the one that
		// belongs to the same root GameObject and matching side.
		var myRoot = GetRoot( GameObject );
		var allTrackers = Scene.GetAllComponents<IHandTracker>();
		_tracker = allTrackers.FirstOrDefault( t =>
		{
			if ( t is null || t.Side != HandAsSide )
				return false;

			var refGo = t.ReferenceObject;
			return refGo.IsValid() && GetRoot( refGo ) == myRoot;
		} );

		LogDebugOnce( _tracker is null
			? "resolve tracker failed: no matching tracker found under same root"
			: $"resolve tracker ok: side={_tracker.Side} trackerGo={_tracker.ReferenceObject?.Name}" );
	}

	private static GameObject GetRoot( GameObject go )
	{
		if ( !go.IsValid() )
			return null;

		var current = go;
		while ( current.Parent.IsValid() )
			current = current.Parent;
		return current;
	}

	/// <summary>
	/// Searches every <see cref="VRHolsterSlot"/> in the scene; if the hand
	/// is inside one's <c>ProximityRadius</c>, contains an item, and grip is
	/// past the press threshold, unholsters the item and routes through the
	/// regular <see cref="Grab"/> path. Returns true on a successful pull.
	/// </summary>
	bool TryUnholsterFromNearbySlot( IControllerInput ctrl )
	{
		if ( ctrl is null || !ctrl.IsTracked )
			return false;
		if ( ctrl.Grip <= GripPressThreshold )
			return false;

		foreach ( var slot in Scene.GetAllComponents<VRHolsterSlot>() )
		{
			if ( slot is null || !slot.ContainsItem )
				continue;
			if ( Vector3.DistanceBetween( WorldPosition, slot.SlotWorldPosition ) > slot.ProximityRadius )
				continue;

			if ( !slot.TryUnholster( out var item ) )
				continue;
			if ( !item.IsValid() || item.GrabPoints is null || item.GrabPoints.Count == 0 )
				return false;

			// Pick the first valid main grab-point; fall back to slot[0] if
			// nothing else looks usable (matches existing search semantics).
			GrabPoint target = null;
			for ( int i = 0; i < item.GrabPoints.Count; i++ )
			{
				if ( item.GrabPoints[i].IsValid() && item.GrabPoints[i].Main )
				{
					target = item.GrabPoints[i];
					break;
				}
			}
			target ??= item.GrabPoints[0];
			Grab( target );
			return true;
		}

		return false;
	}

	/// <summary>
	/// Looks for the closest empty <see cref="VRHolsterSlot"/> that accepts
	/// the held item and tries to slot the item into it. Returns true when
	/// a holster occurred (and <see cref="Drop"/> has already been called),
	/// false when no slot was in range so the caller should run the normal
	/// drop / throw path.
	/// </summary>
	bool TryHolsterIntoNearbySlot()
	{
		if ( !HeldPoint.IsValid() )
			return false;

		var item = HeldPoint.GameObject.Components.Get<Item>( FindMode.EverythingInSelfAndAncestors );
		if ( !item.IsValid() )
			return false;

		VRHolsterSlot best = null;
		float bestDistance = float.MaxValue;

		foreach ( var slot in Scene.GetAllComponents<VRHolsterSlot>() )
		{
			if ( slot is null || !slot.CanAccept( item ) )
				continue;

			var d = Vector3.DistanceBetween( WorldPosition, slot.SlotWorldPosition );
			if ( d > slot.ProximityRadius )
				continue;
			if ( d >= bestDistance )
				continue;

			best = slot;
			bestDistance = d;
		}

		if ( best is null )
			return false;

		// Drop drops the joint + clears HeldPoint, but the body itself stays
		// in the scene; we then immediately ask the slot to attach it. This
		// avoids the body falling for one frame between drop and holster.
		Drop();
		return best.TryHolster( item );
	}

}
