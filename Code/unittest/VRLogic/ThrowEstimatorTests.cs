using Sandbox;
using TFT.VR.Logic;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

public class ThrowEstimatorTests
{
	[Fact]
	public void TryEstimatePeakNeighborhoodAverage_SmoothSignal_ReturnsAverageNearPeak()
	{
		var buffer = new ThrowSignalBuffer();
		buffer.Push( new Vector3( 1, 0, 0 ), Vector3.Zero, 16 );
		buffer.Push( new Vector3( 2, 0, 0 ), Vector3.Zero, 16 );
		buffer.Push( new Vector3( 3, 0, 0 ), Vector3.Zero, 16 );

		var ok = ThrowEstimator.TryEstimatePeakNeighborhoodAverage( buffer, 1, 99f, 99f, out var linear, out _ );

		Assert.True( ok );
		Assert.True( linear.x > 1.9f );
	}

	[Fact]
	public void TryEstimatePeakNeighborhoodAverage_SpikeSignal_IsClamped()
	{
		var buffer = new ThrowSignalBuffer();
		buffer.Push( new Vector3( 1, 0, 0 ), Vector3.Zero, 16 );
		buffer.Push( new Vector3( 100, 0, 0 ), Vector3.Zero, 16 );
		buffer.Push( new Vector3( 1, 0, 0 ), Vector3.Zero, 16 );

		var ok = ThrowEstimator.TryEstimatePeakNeighborhoodAverage( buffer, 1, 6f, 22f, out var linear, out _ );

		Assert.True( ok );
		Assert.InRange( linear.Length, 0f, 6.01f );
	}

	[Fact]
	public void TryEstimatePeakNeighborhoodAverage_EmptyBuffer_ReturnsFalse()
	{
		var buffer = new ThrowSignalBuffer();
		var ok = ThrowEstimator.TryEstimatePeakNeighborhoodAverage( buffer, 1, 6f, 22f, out _, out _ );
		Assert.False( ok );
	}
}
