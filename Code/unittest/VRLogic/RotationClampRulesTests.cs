using TFT.VR.Logic;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

public class RotationClampRulesTests
{
	[Theory]
	[InlineData( 180f )]
	[InlineData( 360f )]
	[InlineData( 540f )]
	public void ClampInterpolationBySpeed_GivenMaxDegreesPerSecond_RespectsLimit( float maxDegreesPerSecond )
	{
		var t = RotationClampRules.ClampInterpolationBySpeed(
			angleDeg: 90f,
			baseT: 1f,
			maxDegreesPerSecond: maxDegreesPerSecond,
			deltaTime: 1f / 90f );

		Assert.InRange( t, 0f, 1f );
	}
}
