namespace TFT.VR.Logic;

public enum GrabWeightClass
{
	Light,
	Medium,
	Heavy
}

public struct GrabWeightProfile
{
	public GrabWeightClass WeightClass;
	public float FollowPositionLerp;
	public float FollowRotationLerp;
	public float MaxDegreesPerSecond;
	public float ReleaseLinearClamp;
	public float ReleaseAngularClamp;

	public static GrabWeightProfile Light => new()
	{
		WeightClass = GrabWeightClass.Light,
		FollowPositionLerp = 20f,
		FollowRotationLerp = 22f,
		MaxDegreesPerSecond = 1080f,
		ReleaseLinearClamp = 8.5f,
		ReleaseAngularClamp = 30f
	};

	public static GrabWeightProfile Medium => new()
	{
		WeightClass = GrabWeightClass.Medium,
		FollowPositionLerp = 14f,
		FollowRotationLerp = 16f,
		MaxDegreesPerSecond = 720f,
		ReleaseLinearClamp = 6f,
		ReleaseAngularClamp = 22f
	};

	public static GrabWeightProfile Heavy => new()
	{
		WeightClass = GrabWeightClass.Heavy,
		FollowPositionLerp = 9f,
		FollowRotationLerp = 10f,
		MaxDegreesPerSecond = 420f,
		ReleaseLinearClamp = 4f,
		ReleaseAngularClamp = 14f
	};
}
