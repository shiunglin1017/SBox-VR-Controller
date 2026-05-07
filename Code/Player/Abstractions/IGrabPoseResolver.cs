using Sandbox;

namespace TFT.VR.Abstractions;

public interface IGrabPoseResolver
{
	bool TryResolvePose( HandSide handSide, GrabPoint heldPoint, out Transform pose );
}
