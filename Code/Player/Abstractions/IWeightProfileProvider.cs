using Sandbox;
using TFT.VR.Logic;

namespace TFT.VR.Abstractions;

public interface IWeightProfileProvider
{
	GrabWeightProfile ResolveProfile( GrabPoint heldPoint, bool twoHandedHolding );
}
