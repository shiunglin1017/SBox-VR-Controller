using Sandbox;
using System.Collections.Generic;
using System.Linq;
using TFT.VR.Abstractions;
using TFT.VR.Logic;

namespace TFT.VR.Services;

[Title( "Rig Rebinder (Default)" )]
[Category( "VR/Services" )]
[Icon( "swap_horiz" )]
public sealed class DefaultRigRebinder : Component, IRigRebinder
{
	[Property, Group( "Debug" )] public bool DebugLogs { get; set; }
	[Property] public List<SkeletonMappingProfile> Profiles { get; set; } = new();

	public bool TryRebindRig( SkinnedModelRenderer targetRenderer, string mappingProfileId )
	{
		if ( !targetRenderer.IsValid() || string.IsNullOrWhiteSpace( mappingProfileId ) )
			return false;

		var profile = Profiles?.FirstOrDefault( p => p is not null && p.ProfileId == mappingProfileId );
		if ( profile is null )
			return false;

		if ( DebugLogs )
			Log.Info( $"[RigRebinder] target={targetRenderer.GameObject?.Name} profile={mappingProfileId} mapCount={profile.Entries?.Count ?? 0}" );

		// Mapping profile 與完整骨架重綁流程會在後續階段擴充；此處先提供可注入的安全入口。
		return true;
	}
}
