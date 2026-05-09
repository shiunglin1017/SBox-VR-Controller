using Sandbox.VR;
using TFT.VR.Abstractions;
using Xunit;

namespace TFT.VR.Tests.VRLogic;

/// <summary>
/// Asserts that the abstraction-side <see cref="VRFingerKind"/> enum casts
/// 1-to-1 to <see cref="Sandbox.VR.FingerValue"/>. The adapter does a
/// straight <c>(FingerValue) kind</c> cast so any drift between the two
/// enums would silently route the wrong finger.
/// </summary>
public class VRFingerKindTests
{
	[Theory]
	[InlineData( VRFingerKind.ThumbCurl,        FingerValue.ThumbCurl )]
	[InlineData( VRFingerKind.IndexCurl,        FingerValue.IndexCurl )]
	[InlineData( VRFingerKind.MiddleCurl,       FingerValue.MiddleCurl )]
	[InlineData( VRFingerKind.RingCurl,         FingerValue.RingCurl )]
	[InlineData( VRFingerKind.PinkyCurl,        FingerValue.PinkyCurl )]
	[InlineData( VRFingerKind.ThumbIndexSplay,  FingerValue.ThumbIndexSplay )]
	[InlineData( VRFingerKind.IndexMiddleSplay, FingerValue.IndexMiddleSplay )]
	[InlineData( VRFingerKind.MiddleRingSplay,  FingerValue.MiddleRingSplay )]
	[InlineData( VRFingerKind.RingPinkySplay,   FingerValue.RingPinkySplay )]
	public void OrdinalsLineUp( VRFingerKind ours, FingerValue theirs )
	{
		Assert.Equal( (int) theirs, (int) ours );
	}
}
