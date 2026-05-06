using Sandbox;

namespace TFT.VR.Abstractions;

/// <summary>
/// Resolves the world-space pose of a VR hand reference object that is being
/// driven by <c>Sandbox.VR.VRTrackedObject</c>. Decouples hand-tracking
/// consumers (IK, grab logic, ...) from the concrete tracking GameObject
/// hierarchy in the prefab.
/// </summary>
public interface IHandTracker
{
	HandSide Side { get; }
	bool IsTracked { get; }
	Transform Pose { get; }
	GameObject ReferenceObject { get; }
}
