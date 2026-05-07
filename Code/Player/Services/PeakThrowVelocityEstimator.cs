using Sandbox;
using TFT.VR.Abstractions;
using TFT.VR.Logic;

namespace TFT.VR.Services;

[Title( "Throw Velocity Estimator" )]
[Category( "VR/Services" )]
[Icon( "sports_baseball" )]
public sealed class PeakThrowVelocityEstimator : Component, IThrowVelocityEstimator
{
	private readonly ThrowSignalBuffer _buffer = new();

	public void PushSample( Vector3 linear, Vector3 angular, int maxSamples )
	{
		_buffer.Push( linear, angular, maxSamples );
	}

	public bool TryEstimate(
		int peakNeighborhood,
		GrabWeightProfile weightProfile,
		out Vector3 linear,
		out Vector3 angular )
	{
		return ThrowEstimator.TryEstimatePeakNeighborhoodAverage(
			_buffer,
			peakNeighborhood,
			weightProfile.ReleaseLinearClamp,
			weightProfile.ReleaseAngularClamp,
			out linear,
			out angular );
	}
}
