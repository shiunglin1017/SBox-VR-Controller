using Sandbox;
using TFT.VR.Logic;

namespace TFT.VR.Abstractions;

public interface IThrowVelocityEstimator
{
	void PushSample( Vector3 linear, Vector3 angular, int maxSamples );

	bool TryEstimate(
		int peakNeighborhood,
		GrabWeightProfile weightProfile,
		out Vector3 linear,
		out Vector3 angular );
}
