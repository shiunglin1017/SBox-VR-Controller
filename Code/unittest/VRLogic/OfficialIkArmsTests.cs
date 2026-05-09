using System.Collections.Generic;
using Sandbox;
using TFT.VR.Logic;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

/// <summary>
/// Verifies the routing between live VR hand poses and the model's animgraph
/// IK rules (<c>ik.hand_left</c> / <c>ik.hand_right</c>) goes through the
/// expected SetIk / ClearIk calls. We test the pure logic via
/// <see cref="OfficialArmIkRouter"/> because <see cref="SkinnedModelRenderer"/>
/// can't be instantiated outside the engine.
/// </summary>
public class OfficialIkArmsTests
{
	private sealed class RecordingSink : IIkParameterSink
	{
		public List<(string Name, Transform Pose)> Sets { get; } = new();
		public List<string> Clears { get; } = new();

		public void SetIk( string name, Transform pose ) => Sets.Add( (name, pose) );
		public void ClearIk( string name ) => Clears.Add( name );
	}

	[Fact]
	public void Apply_BothActive_PushesBothHands()
	{
		var sink = new RecordingSink();
		var leftPose = new Transform( new Vector3( 1, 2, 3 ) );
		var rightPose = new Transform( new Vector3( 4, 5, 6 ) );

		OfficialArmIkRouter.Apply( sink,
			leftActive: true, leftPose,
			rightActive: true, rightPose );

		Assert.Equal( 2, sink.Sets.Count );
		Assert.Empty( sink.Clears );
		Assert.Equal( OfficialArmIkRouter.LeftHandKey, sink.Sets[0].Name );
		Assert.Equal( leftPose.Position, sink.Sets[0].Pose.Position );
		Assert.Equal( OfficialArmIkRouter.RightHandKey, sink.Sets[1].Name );
		Assert.Equal( rightPose.Position, sink.Sets[1].Pose.Position );
	}

	[Fact]
	public void Apply_LeftInactive_ClearsLeftKeepsRight()
	{
		var sink = new RecordingSink();
		var rightPose = new Transform( new Vector3( 7, 8, 9 ) );

		OfficialArmIkRouter.Apply( sink,
			leftActive: false, default,
			rightActive: true, rightPose );

		Assert.Single( sink.Clears, OfficialArmIkRouter.LeftHandKey );
		Assert.Single( sink.Sets );
		Assert.Equal( OfficialArmIkRouter.RightHandKey, sink.Sets[0].Name );
		Assert.Equal( rightPose.Position, sink.Sets[0].Pose.Position );
	}

	[Fact]
	public void Apply_BothInactive_ClearsBothHands()
	{
		var sink = new RecordingSink();

		OfficialArmIkRouter.Apply( sink,
			leftActive: false, default,
			rightActive: false, default );

		Assert.Empty( sink.Sets );
		Assert.Equal( 2, sink.Clears.Count );
		Assert.Contains( OfficialArmIkRouter.LeftHandKey, sink.Clears );
		Assert.Contains( OfficialArmIkRouter.RightHandKey, sink.Clears );
	}

	[Fact]
	public void Apply_NullSink_DoesNotThrow()
	{
		var ex = Record.Exception( () =>
			OfficialArmIkRouter.Apply( null,
				leftActive: true, default,
				rightActive: true, default ) );

		Assert.Null( ex );
	}

	[Fact]
	public void Keys_MatchAnimgraphConvention()
	{
		Assert.Equal( "hand_left", OfficialArmIkRouter.LeftHandKey );
		Assert.Equal( "hand_right", OfficialArmIkRouter.RightHandKey );
	}
}
