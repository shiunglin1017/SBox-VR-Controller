using Sandbox;
using TFT.VR.Logic;

namespace TFT.VR.Abstractions;

public interface IHandPoseStabilizer
{
	Transform Stabilize( Transform current, Transform target, GrabWeightProfile profile, float deltaTime );
}
