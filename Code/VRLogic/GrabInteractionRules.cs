namespace TFT.VR.Logic;

public static class GrabInteractionRules
{
	public static bool ShouldStartGrab( float grip, float threshold, bool alreadyHolding, bool hasCandidate )
	{
		return !alreadyHolding && hasCandidate && grip >= threshold;
	}

	public static bool ShouldReleaseGrab( float grip, float threshold, bool isHolding )
	{
		return isHolding && grip <= threshold;
	}

	public static bool WithinGrabDistanceSquared( float distanceSquared, float maxDistanceSquared )
	{
		return distanceSquared <= maxDistanceSquared;
	}
}
