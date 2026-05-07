using TFT.VR.Logic;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

public class GrabInteractionRulesTests
{
	[Fact]
	public void ShouldStartGrab_HasCandidateAndGripAboveThreshold_ReturnsTrue()
	{
		var result = GrabInteractionRules.ShouldStartGrab( 0.7f, 0.5f, false, true );
		Assert.True( result );
	}

	[Fact]
	public void ShouldStartGrab_AlreadyHolding_ReturnsFalse()
	{
		var result = GrabInteractionRules.ShouldStartGrab( 0.9f, 0.5f, true, true );
		Assert.False( result );
	}

	[Fact]
	public void ShouldReleaseGrab_GripBelowThreshold_ReturnsTrue()
	{
		var result = GrabInteractionRules.ShouldReleaseGrab( 0.1f, 0.2f, true );
		Assert.True( result );
	}
}
