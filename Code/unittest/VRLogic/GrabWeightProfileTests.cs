using TFT.VR.Logic;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

public class GrabWeightProfileTests
{
	[Fact]
	public void LightProfile_HasHigherFollowThanHeavy()
	{
		Assert.True( GrabWeightProfile.Light.FollowPositionLerp > GrabWeightProfile.Heavy.FollowPositionLerp );
		Assert.True( GrabWeightProfile.Light.ReleaseLinearClamp > GrabWeightProfile.Heavy.ReleaseLinearClamp );
	}

	[Fact]
	public void MediumProfile_IsBetweenLightAndHeavy()
	{
		Assert.InRange(
			GrabWeightProfile.Medium.FollowRotationLerp,
			GrabWeightProfile.Heavy.FollowRotationLerp,
			GrabWeightProfile.Light.FollowRotationLerp );
	}
}
