using Sandbox;
using TFT.VR.Abstractions;

public sealed class RecoilTest : Component
{
	[Property] private GameObject Barrel { get; set; }
	[Property, Group( "Haptics" )] public bool UseFireHaptics { get; set; } = true;

	private IVRInputProvider _input;

	const float FireThreshold = 0.75f;

	protected override void OnAwake()
	{
		_input = Components.Get<IVRInputProvider>( FindMode.EverythingInSelfAndAncestors );
	}

	protected override void OnUpdate()
	{
		if ( _input is null || !_input.IsAvailable )
			return;

		var hand = _input.RightHand;
		var trigger = hand.Trigger;

		// Rising-edge detection via AnalogInput.Delta - no manually-tracked
		// previous-frame field.
		var previous = trigger - hand.TriggerDelta;
		if ( trigger > FireThreshold && previous <= FireThreshold )
		{
			GetComponent<Rigidbody>().ApplyImpulseAt( Barrel.WorldPosition, -WorldTransform.Forward * 5000 );
			Gizmo.Draw.Arrow( WorldPosition, WorldPosition + WorldTransform.Forward * 20 );

			if ( UseFireHaptics )
				hand.TriggerHaptic( HapticEffect.HardImpact );
		}
	}
}
