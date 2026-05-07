using System;

namespace TFT.VR.Logic;

public static class ThrowEstimator
{
	public static bool TryEstimatePeakNeighborhoodAverage(
		ThrowSignalBuffer buffer,
		int neighborhood,
		float maxLinearSpeed,
		float maxAngularSpeed,
		out Vector3 linear,
		out Vector3 angular )
	{
		linear = Vector3.Zero;
		angular = Vector3.Zero;

		if ( buffer is null || buffer.Count == 0 )
			return false;

		var samples = buffer.Samples;
		var peakIdx = 0;
		var peakSpeed = float.MinValue;
		for ( var i = 0; i < samples.Count; i++ )
		{
			if ( samples[i].Speed <= peakSpeed )
				continue;

			peakSpeed = samples[i].Speed;
			peakIdx = i;
		}

		var n = Math.Max( 0, neighborhood );
		var start = Math.Max( 0, peakIdx - n );
		var end = Math.Min( samples.Count - 1, peakIdx + n );

		var count = 0;
		for ( var i = start; i <= end; i++ )
		{
			linear += samples[i].Linear;
			angular += samples[i].Angular;
			count++;
		}

		if ( count <= 0 )
			return false;

		linear /= count;
		angular /= count;

		ClampMagnitude( ref linear, maxLinearSpeed );
		ClampMagnitude( ref angular, maxAngularSpeed );
		return true;
	}

	public static void ClampMagnitude( ref Vector3 vector, float maxMagnitude )
	{
		if ( maxMagnitude <= 0f )
		{
			vector = Vector3.Zero;
			return;
		}

		var len = vector.Length;
		if ( len <= maxMagnitude || len <= 0.0001f )
			return;

		vector *= maxMagnitude / len;
	}
}
