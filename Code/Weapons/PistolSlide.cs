using Sandbox;
using TFT.VR.Abstractions;

public sealed class PistolSlide : Component
{
	[Property] private GrabPoint GrabPoint { get; set; }
	[Property] private GrabPoint MainGrabPoint { get; set; }
	[Property] private GameObject ReferencePoint { get; set; }
	[Property] private Barrel Barrel { get; set; }
	[Property] private MagazineLoader MagazineLoader { get; set; }
	[Property] private ModelRenderer BulletVisual { get; set; }
	[Property] private float Distance { get; set; }
	[Property] private float BulletPickupPoint { get; set; } = 0.78f;
	[Property, Group( "Haptics" )] public bool UseChamberHaptics { get; set; } = true;

	float _pullBack;
	[Property] public float PullBack
	{
		get
		{
			return _pullBack;
		}
		set
		{
			_pullBack = value;
			SleepClock = 0;
		}
	}
	[Property] private int HoldingBullet { get; set; }
	[Property] private string CasingGroup { get; set; } = "9mm_bullet";

	RealTimeSince SleepClock { get; set; }

	Vector3 origin;

	float minPullBack;
	protected override void OnStart()
	{
		origin = LocalPosition;
	}

	[Property] public float visualPullBack;

	float lastPullBack;
	protected override void OnUpdate()
	{
		if ( GrabPoint.Held || MainGrabPoint.Held )
			SleepClock = 0;

		if ( SleepClock > 1 )
			return;

		BulletVisual.Enabled = Barrel.Contents != -1 || HoldingBullet != -1;

		BulletVisual.SetBodyGroup( CasingGroup, HoldingBullet == -2 ? 1 : 0 );

		visualPullBack = MathX.Clamp( visualPullBack + (PullBack - visualPullBack) * 60 * Time.Delta, 0, 1 );

		LocalPosition = origin - ReferencePoint.LocalTransform.Forward * Distance * visualPullBack;

		if ( PullBack > 0 && lastPullBack == 0 )
			StartPullBack();
		if ( PullBack == 1 && lastPullBack < 1 )
			PulledBack();
		if ( PullBack < BulletPickupPoint && lastPullBack >= BulletPickupPoint )
			ExitPullBack();
		if ( PullBack == 0 && lastPullBack > 0 )
			Load();

		lastPullBack = PullBack;

		if ( PullBack == 1 && MainGrabPoint.Held )
		{
			var controller = MainGrabPoint.GrabbedHand?.Controller;
			// Rising-edge via DigitalInput.WasPressed: only release the
			// "stuck-back" state once per click, not every frame.
			if ( controller is not null && controller.IsTracked && controller.JoystickPressed )
			{
				minPullBack = 0;
			}
		}

		if ( !GrabPoint.Held )
			PullBack = minPullBack;
		else
		{
			PullBack = MathX.Clamp( Vector3.Dot( -ReferencePoint.WorldTransform.Forward * Distance, GrabPoint.GrabbedHand.Reference.WorldPosition - ReferencePoint.WorldPosition ), 0, 1 );
			if ( PullBack < 1 )
				minPullBack = 0;
		}
	}

	void StartPullBack()
	{
		HoldingBullet = Barrel.Contents;

		Barrel.Contents = -1;
	}


	void PulledBack()
	{
		if ( HoldingBullet == -2 )
		{
			//eject casing
			HoldingBullet = -1;
		}
		if ( HoldingBullet != -1 )
		{
			//eject bullet
			HoldingBullet = -1;
		}

		if ( !MagazineLoader.Magazine.IsValid() || MagazineLoader.Magazine.Contents.Count <= 0 )
			minPullBack = 1;

		PulseChamberHaptic();
	}

	void ExitPullBack()
	{

		if ( !MagazineLoader.Magazine.IsValid() || MagazineLoader.Magazine.Contents.Count <= 0 )
			return;

		HoldingBullet = MagazineLoader.Magazine.Contents[0];

		MagazineLoader.Magazine.Contents.RemoveAt( 0 );

		MagazineLoader.Magazine.UpdateVisuals();
	}

	void Load()
	{
		if ( HoldingBullet == -1 )
			return;

		Barrel.Contents = HoldingBullet;
		HoldingBullet = -1;

		PulseChamberHaptic();
	}

	void PulseChamberHaptic()
	{
		if ( !UseChamberHaptics )
			return;

		// Buzz both the slide hand and the main grip hand if both are held.
		var slideController = GrabPoint?.GrabbedHand?.Controller;
		var mainController = MainGrabPoint?.GrabbedHand?.Controller;

		if ( slideController is not null && slideController.IsTracked )
			slideController.TriggerHaptic( HapticEffect.HardImpact, 0.6f, 1f, 0.8f );

		if ( mainController is not null && mainController.IsTracked && mainController != slideController )
			mainController.TriggerHaptic( HapticEffect.SoftImpact, 0.6f, 1f, 0.6f );
	}
}
