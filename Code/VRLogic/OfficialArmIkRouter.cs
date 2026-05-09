using Sandbox;

namespace TFT.VR.Logic;

/// <summary>
/// Minimal abstraction over <see cref="SkinnedModelRenderer.SetIk(string, Transform)"/>
/// and <see cref="SkinnedModelRenderer.ClearIk(string)"/> so arm-IK routing
/// logic stays unit-testable without instantiating an engine renderer. The
/// production adapter lives next to <c>VRAnimationHelper</c>; tests inject
/// a recording fake.
/// </summary>
public interface IIkParameterSink
{
	void SetIk( string name, Transform pose );
	void ClearIk( string name );
}

/// <summary>
/// Pure routing logic that decides when to push hand poses to the model's
/// animgraph IK rules vs. when to clear them. Mirrors how feet are wired in
/// <c>VRAnimationHelper.OnUpdate</c> for <c>foot_left</c> / <c>foot_right</c>,
/// but for hands. Keeping this purely static makes it trivial to test and
/// reuse from anywhere that already speaks the official IK protocol.
/// </summary>
public static class OfficialArmIkRouter
{
	public const string LeftHandKey = "hand_left";
	public const string RightHandKey = "hand_right";

	/// <summary>
	/// Pushes the active hand poses into the sink, or clears them if the
	/// hand is currently not driveable. A null <paramref name="sink"/> is
	/// silently ignored to make it safe to call before component bindings
	/// are resolved.
	/// </summary>
	public static void Apply(
		IIkParameterSink sink,
		bool leftActive, Transform leftPose,
		bool rightActive, Transform rightPose )
	{
		if ( sink is null )
			return;

		if ( leftActive ) sink.SetIk( LeftHandKey, leftPose );
		else sink.ClearIk( LeftHandKey );

		if ( rightActive ) sink.SetIk( RightHandKey, rightPose );
		else sink.ClearIk( RightHandKey );
	}
}
