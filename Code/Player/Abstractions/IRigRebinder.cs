using Sandbox;

namespace TFT.VR.Abstractions;

public interface IRigRebinder
{
	bool TryRebindRig( SkinnedModelRenderer targetRenderer, string mappingProfileId );
}
