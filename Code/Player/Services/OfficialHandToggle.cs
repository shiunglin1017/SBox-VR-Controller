using Sandbox;
using TFT.VR.Abstractions;

namespace TFT.VR.Services;

/// <summary>
/// Drives the visibility of an "official hand / controller model" GameObject
/// (a <c>Sandbox.VR.VRHand</c> or <c>Sandbox.VR.VRModelRenderer</c> child)
/// based on what the player is currently doing:
/// <list type="bullet">
///   <item><description><b>Searching + hand-tracked</b>: show the official VR hand (skeletal pose).</description></item>
///   <item><description><b>Searching + controller</b>: show the official hand / controller model.</description></item>
///   <item><description><b>Holding</b>: hide it; the Citizen hand grips the item.</description></item>
/// </list>
///
/// <para>
/// The component is a thin router: it doesn't know how the official hand is
/// realised (skeletal hand vs full controller model) - it just toggles the
/// referenced GameObject's <c>Enabled</c> flag.
/// </para>
/// </summary>
[Title( "Official Hand Toggle" )]
[Category( "VR/Services" )]
[Icon( "front_hand" )]
public sealed class OfficialHandToggle : Component
{
	[Property] public VrhandInteraction Hand { get; set; }
	[Property] public GameObject OfficialHand { get; set; }
	[Property] public GameObject CitizenHand { get; set; }

	/// <summary>
	/// When true, the official hand is shown only while real hand-tracking is
	/// active (no controller in user's hand). When false, the official hand
	/// is also shown while holding a controller (handy for fancy VRModelRenderer
	/// setups). Defaults to false to match the plan's preferred D1 path.
	/// </summary>
	[Property] public bool RequireHandTracking { get; set; } = false;

	[Property, Group( "Debug" )] public bool DebugLogs { get; set; }

	private TimeUntil _nextDebugLog;

	protected override void OnUpdate()
	{
		if ( !Hand.IsValid() )
			return;

		var ctrl = Hand.Controller;

		// "Holding" always hides the official hand; the held item plus the
		// Citizen mesh are doing the visual work.
		var showOfficial = !Hand.IsHolding;

		if ( showOfficial && RequireHandTracking )
			showOfficial = ctrl is not null && ctrl.IsHandTracking;

		if ( OfficialHand.IsValid() && OfficialHand.Active != showOfficial )
		{
			OfficialHand.Enabled = showOfficial;
			LogDebugOnce( $"OfficialHand -> {(showOfficial ? "on" : "off")}" );
		}

		if ( CitizenHand.IsValid() && CitizenHand.Active == showOfficial )
		{
			CitizenHand.Enabled = !showOfficial;
			LogDebugOnce( $"CitizenHand -> {(!showOfficial ? "on" : "off")}" );
		}
	}

	private void LogDebugOnce( string message )
	{
		if ( !DebugLogs || _nextDebugLog > 0f )
			return;

		Log.Info( $"[OfficialHandToggle] {GameObject?.Name} {message}" );
		_nextDebugLog = 0.5f;
	}
}
