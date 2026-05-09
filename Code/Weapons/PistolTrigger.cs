using Sandbox;
using TFT.VR.Abstractions;

public sealed class PistolTrigger : Component
{
	[Property] public GrabPoint GrabPoint { get; set; }
	[Property] public PistolSlide Slide { get; set; }
	[Property] public Barrel Barrel { get; set; }
	[Property] public GameObject LeftIndex { get; set; }
	[Property] public GameObject RightIndex { get; set; }
	[Property] public GameObject OffTrigger { get; set; }
	[Property] public GameObject OnTrigger { get; set; }
	[Property] public GameObject TriggerDown { get; set; }
	[Property] public bool RapidFire { get; set; }
	[Property] public Angles StartRot { get; set; }
	[Property] public Angles DownRot { get; set; }

	[Property, Group( "Haptics" )] public bool UseFireHaptics { get; set; } = true;
	[Property, Group( "Haptics" )] public float FireHapticAmplitude { get; set; } = 1f;
	[Property, Group( "Haptics" )] public float FireHapticLength { get; set; } = 1f;

	const float FireThreshold = 0.9f;

	protected override void OnUpdate()
	{
		if ( !GrabPoint.IsValid() || !GrabPoint.Held )
			return;

		var controller = GrabPoint.GrabbedHand?.Controller;
		if ( controller is null || !controller.IsTracked )
			return;

		if ( controller.GetFingerCurl( 1 ) < 0.1f )
			OffTriggerPose();
		else
			OnTriggerPose( controller.Trigger );

		LocalRotation = Angles.Lerp( StartRot, DownRot, controller.Trigger );

		// Rising-edge detection via official AnalogInput.Delta. The previous
		// frame's value is reconstructed as (Value - Delta), so this is the
		// direct equivalent of the old "lastPullBack < 0.9 && Trigger >= 0.9"
		// check without needing a manually-tracked field.
		var previousTrigger = controller.Trigger - controller.TriggerDelta;
		var crossedFireEdge = controller.Trigger >= FireThreshold && previousTrigger < FireThreshold;

		if ( (crossedFireEdge || (RapidFire && controller.Trigger >= FireThreshold)) && Slide.visualPullBack == 0 )
		{
			Barrel.TryFire();

			if ( UseFireHaptics )
				controller.TriggerHaptic( HapticEffect.HardImpact, FireHapticLength, 1f, FireHapticAmplitude );
		}
	}

	void OffTriggerPose()
	{
		if ( GrabPoint.Hand.Equals( VrhandInteraction.HandEnum.Left ) )
		{
			VrhandInteraction.CopyTransformRecursive( OffTrigger, LeftIndex, new Vector3( 1, 1, -1 ), new Angles( -1, 1, -1 ), 10 * Time.Delta );
		}
		else
		{
			VrhandInteraction.CopyTransformRecursive( OffTrigger, RightIndex, new Vector3( 1, 1, 1 ), new Angles( 1, 1, 1 ), 10 * Time.Delta );
		}
	}

	void OnTriggerPose( float pullBack )
	{
		if ( GrabPoint.Hand.Equals( VrhandInteraction.HandEnum.Left ) )
		{
			VrhandInteraction.CopyTransformRecursiveLerp( OnTrigger, TriggerDown, LeftIndex, new Vector3( 1, 1, -1 ), new Angles( -1, 1, -1 ), pullBack, 10 * Time.Delta );
		}
		else
		{
			VrhandInteraction.CopyTransformRecursiveLerp( OnTrigger, TriggerDown, RightIndex, new Vector3( 1, 1, 1 ), new Angles( 1, 1, 1 ), pullBack, 10 * Time.Delta );
		}
	}
}
