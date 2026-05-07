using System.Collections.Generic;
using System;

namespace TFT.VR.Logic;

public struct ThrowSignalSample
{
	public Vector3 Linear;
	public Vector3 Angular;
	public float Speed;
}

public sealed class ThrowSignalBuffer
{
	private readonly List<ThrowSignalSample> _samples = new();

	public int Count => _samples.Count;
	public IReadOnlyList<ThrowSignalSample> Samples => _samples;

	public void Push( Vector3 linear, Vector3 angular, int maxSamples )
	{
		var sample = new ThrowSignalSample
		{
			Linear = linear,
			Angular = angular,
			Speed = linear.Length
		};

		_samples.Add( sample );
		while ( _samples.Count > Math.Max( 1, maxSamples ) )
			_samples.RemoveAt( 0 );
	}
}
