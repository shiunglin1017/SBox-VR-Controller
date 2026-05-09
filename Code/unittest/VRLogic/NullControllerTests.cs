using Sandbox;
using TFT.VR.Abstractions;
using TFT.VR.Services;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

/// <summary>
/// Verifies that the no-op controller surfaces every <see cref="IControllerInput"/>
/// member as a safe default. Consumers rely on this so they don't have to
/// null-check after pulling input from a provider that's currently inactive.
/// </summary>
public class NullControllerTests
{
	[Fact]
	public void Side_MatchesConstructor()
	{
		Assert.Equal( HandSide.Left, NullController.Left.Side );
		Assert.Equal( HandSide.Right, NullController.Right.Side );
	}

	[Fact]
	public void TrackingFlags_AreFalse()
	{
		Assert.False( NullController.Left.IsTracked );
		Assert.False( NullController.Left.IsHandTracking );
	}

	[Fact]
	public void AnalogValues_AreZero()
	{
		var c = NullController.Left;
		Assert.Equal( 0f, c.Trigger );
		Assert.Equal( 0f, c.TriggerDelta );
		Assert.Equal( 0f, c.Grip );
		Assert.Equal( 0f, c.GripDelta );
		Assert.Equal( Vector2.Zero, c.Joystick );
		Assert.Equal( Vector2.Zero, c.JoystickDelta );
	}

	[Fact]
	public void DigitalFlags_AreFalse()
	{
		var c = NullController.Right;
		Assert.False( c.TriggerActive );
		Assert.False( c.GripActive );
		Assert.False( c.JoystickActive );
		Assert.False( c.JoystickPress );
		Assert.False( c.JoystickPressed );
		Assert.False( c.ButtonA );
		Assert.False( c.ButtonAActive );
		Assert.False( c.ButtonAPressed );
		Assert.False( c.ButtonB );
		Assert.False( c.ButtonBActive );
		Assert.False( c.ButtonBPressed );
	}

	[Fact]
	public void FingerReads_ReturnZero()
	{
		var c = NullController.Left;
		for ( int i = 0; i < 5; i++ )
		{
			Assert.Equal( 0f, c.GetFingerCurl( i ) );
			Assert.Equal( 0f, c.GetFingerSplay( i ) );
		}
		Assert.Equal( 0f, c.GetFingerValue( VRFingerKind.ThumbCurl ) );
		Assert.Equal( 0f, c.GetFingerValue( VRFingerKind.RingPinkySplay ) );
	}

	[Fact]
	public void Haptics_AreNoOp()
	{
		var c = NullController.Right;
		var ex = Record.Exception( () =>
		{
			c.TriggerHaptic( 0.1f, 100, 0.5f );
			c.TriggerHaptic( HapticEffect.HardImpact );
			c.TriggerHaptic( HapticEffect.SoftImpact, 1, 1, 1 );
			c.StopAllHaptics();
		} );
		Assert.Null( ex );
	}
}
