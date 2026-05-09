using System.Collections.Generic;
using Sandbox.VR;

namespace TFT.VR.Abstractions;

/// <summary>
/// Per-frame snapshot of full skeletal hand data from the VR runtime.
/// <para>
/// We expose <see cref="VRHandJointData"/> (a value type) directly rather than
/// wrapping it: there's exactly one consumer
/// (<c>VRAnimationHelper</c>) and another layer of indirection adds no value.
/// </para>
/// <para>
/// Implementations are expected to cache the lists during their own
/// <c>OnUpdate</c>; consumers can then iterate <see cref="Joints"/> /
/// <see cref="RawHandJoints"/> as many times as they want without re-paying
/// the call into <c>VRController.GetJoints</c>.
/// </para>
/// </summary>
public interface IHandSkeletonProvider
{
	HandSide Side { get; }

	/// <summary>True when the runtime is producing usable joint data this
	/// frame (controller capable + tracked, or hand-tracking active).</summary>
	bool HasSkeleton { get; }

	/// <summary>Joints clamped to the controller's physical motion range
	/// (good for physics-driven hand poses on top of a controller).</summary>
	IReadOnlyList<VRHandJointData> Joints { get; }

	/// <summary>Joints from the unrestricted hand-tracking motion range
	/// (good for hand-tracking-only modes, may pass through the controller
	/// model).</summary>
	IReadOnlyList<VRHandJointData> RawHandJoints { get; }
}
