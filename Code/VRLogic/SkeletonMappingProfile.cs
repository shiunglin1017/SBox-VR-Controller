using Sandbox;
using System.Collections.Generic;

namespace TFT.VR.Logic;

[GameResource( "Skeleton Mapping Profile", "skmap", "Cross-skeleton bone mapping profile." )]
public sealed class SkeletonMappingProfile : GameResource
{
	public string ProfileId { get; set; } = "default";
	public List<BoneMapEntry> Entries { get; set; } = new();

	public sealed class BoneMapEntry
	{
		public string SourceBone { get; set; }
		public string TargetBone { get; set; }
	}
}
