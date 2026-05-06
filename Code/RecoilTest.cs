using Sandbox;
using TFT.VR.Abstractions;

public sealed class RecoilTest : Component
{
	[Property] private GameObject Barrel { get; set; }

	private IVRInputProvider _input;
	float lastTrigger;

	protected override void OnAwake()
	{
		_input = Components.Get<IVRInputProvider>( FindMode.EverythingInSelfAndAncestors );
	}

	protected override void OnUpdate()
	{
		if ( _input is null || !_input.IsAvailable )
			return;

		var trigger = _input.RightHand.Trigger;

		if ( trigger > 0.75f && lastTrigger <= 0.75f )
		{
			GetComponent<Rigidbody>().ApplyImpulseAt( Barrel.WorldPosition, -WorldTransform.Forward * 5000 );
			Gizmo.Draw.Arrow( WorldPosition, WorldPosition + WorldTransform.Forward * 20 );
		}
		lastTrigger = trigger;
	}
}
