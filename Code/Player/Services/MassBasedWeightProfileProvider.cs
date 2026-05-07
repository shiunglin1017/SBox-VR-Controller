using Sandbox;
using TFT.VR.Abstractions;
using TFT.VR.Logic;

namespace TFT.VR.Services;

[Title( "Weight Profile Provider (Mass Based)" )]
[Category( "VR/Services" )]
[Icon( "fitness_center" )]
public sealed class MassBasedWeightProfileProvider : Component, IWeightProfileProvider
{
	[Property] public float LightMaxMass { get; set; } = 1.5f;
	[Property] public float MediumMaxMass { get; set; } = 6.0f;
	[Property] public float TwoHandedMultiplier { get; set; } = 0.75f;

	public GrabWeightProfile ResolveProfile( GrabPoint heldPoint, bool twoHandedHolding )
	{
		var weightClass = ResolveWeightClass( heldPoint );
		var profile = weightClass switch
		{
			GrabWeightClass.Light => GrabWeightProfile.Light,
			GrabWeightClass.Heavy => GrabWeightProfile.Heavy,
			_ => GrabWeightProfile.Medium
		};

		if ( twoHandedHolding )
		{
			var scale = MathX.Clamp( TwoHandedMultiplier, 0.1f, 1f );
			profile.FollowPositionLerp /= scale;
			profile.FollowRotationLerp /= scale;
			profile.MaxDegreesPerSecond /= scale;
		}

		return profile;
	}

	private GrabWeightClass ResolveWeightClass( GrabPoint heldPoint )
	{
		if ( !heldPoint.IsValid() )
			return GrabWeightClass.Medium;

		if ( heldPoint.GameObject.Tags.Contains( "vr_weight_light" ) )
			return GrabWeightClass.Light;
		if ( heldPoint.GameObject.Tags.Contains( "vr_weight_heavy" ) )
			return GrabWeightClass.Heavy;
		if ( heldPoint.GameObject.Tags.Contains( "vr_weight_medium" ) )
			return GrabWeightClass.Medium;

		var mass = heldPoint.Body?.Mass ?? 2f;
		if ( mass <= LightMaxMass )
			return GrabWeightClass.Light;
		if ( mass <= MediumMaxMass )
			return GrabWeightClass.Medium;
		return GrabWeightClass.Heavy;
	}
}
