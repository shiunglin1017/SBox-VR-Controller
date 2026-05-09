using Sandbox;
using TFT.VR.Abstractions;

public sealed class MagazineLoader : Component, Component.ITriggerListener
{
	[Property] public GrabPoint GrabPoint { get; set; }
	[Property] public Magazine Magazine { get; set; }
	[Property] public GameObject MagParent { get; set; }
	[Property] public GameObject MagDrop { get; set; }
	[Property] public List<string> AcceptedMags { get; set; }
	[Property] public bool PickupMag { get; set; }
	[Property] public float MagTime { get; set; } = 0.1f;
	[Property, Group( "Haptics" )] public bool UseInsertHaptics { get; set; } = true;

	public void OnTriggerEnter( Collider other )
	{

		if ( Magazine.IsValid() )
			return;

		var item = other.GetComponent<Item>();

		if ( !item.IsValid() )
			return;

		if ( !item.Held() )
			return;

		if ( !AcceptedMags.Contains( item.Name ) )
			return;

		var magazine = other.GetComponent<Magazine>();

		if ( !magazine.IsValid() )
			return;

		// Pulse the hand that was holding the magazine before we drop it.
		if ( UseInsertHaptics )
			PulseHapticForGrabPoints( item.GrabPoints, HapticEffect.SoftImpact );

		foreach ( GrabPoint grabPoint in item.GrabPoints )
		{
			grabPoint.GrabbedHand.Drop();
		}

		Magazine = magazine;

		item.Body.MotionEnabled = false;

		other.GameObject.SetParent( MagParent );

		other.LocalPosition = MagDrop.LocalPosition;

		other.LocalRotation = Rotation.Identity;

		if ( !PickupMag )
			other.Tags.Add( "uninteractable" );

		// Click for the gun-holding hand once the mag fully seats.
		if ( UseInsertHaptics && GrabPoint.IsValid() && GrabPoint.Held )
		{
			var holdingController = GrabPoint.GrabbedHand?.Controller;
			if ( holdingController is not null && holdingController.IsTracked )
				holdingController.TriggerHaptic( HapticEffect.SoftImpact, 0.6f, 1f, 0.6f );
		}

		SlideT = 0;

	}

	bool Dropping;
	RealTimeSince SlideT;
	RealTimeSince SleepClock;

	protected override void OnFixedUpdate()
	{
		if ( GrabPoint.Held )
			SleepClock = 0;

		if ( SleepClock > 1 )
			return;

		if ( !Magazine.IsValid() )
			return;

		if ( Dropping )
		{

			Magazine.LocalPosition = Vector3.Lerp( MagParent.LocalPosition, MagDrop.LocalPosition, SlideT / MagTime );

			if ( SlideT >= MagTime )
			{
				Magazine.Item.Body.MotionEnabled = true;
				Magazine.GameObject.SetParent( null );
				if ( !PickupMag )
					Magazine.GameObject.Tags.Remove( "uninteractable" );
				Magazine = null;
				Dropping = false;
			}
			return;
		}

		if ( Magazine.GameObject.Parent != MagParent )
		{
			Magazine = null;
			return;
		}

		float lerp = MathX.Clamp( SlideT / MagTime, 0, 1 );

		Magazine.LocalPosition = Vector3.Lerp( MagDrop.LocalPosition, Vector3.Zero, lerp );

		var controller = GrabPoint.GrabbedHand?.Controller;
		// Rising-edge via DigitalInput.WasPressed: avoid re-triggering Dropping
		// every frame the user holds B.
		if ( controller is not null && controller.IsTracked && controller.ButtonBPressed )
		{
			Dropping = true;
			SlideT = 0;

			if ( UseInsertHaptics )
				controller.TriggerHaptic( HapticEffect.SoftImpact, 0.5f, 1f, 0.5f );
		}
	}

	private static void PulseHapticForGrabPoints( System.Collections.Generic.IList<GrabPoint> points, HapticEffect effect )
	{
		if ( points is null )
			return;

		for ( int i = 0; i < points.Count; i++ )
		{
			var ctrl = points[i]?.GrabbedHand?.Controller;
			if ( ctrl is not null && ctrl.IsTracked )
				ctrl.TriggerHaptic( effect, 0.6f, 1f, 0.7f );
		}
	}
}
