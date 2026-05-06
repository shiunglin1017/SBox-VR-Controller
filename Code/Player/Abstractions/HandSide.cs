namespace TFT.VR.Abstractions;

/// <summary>
/// Identifies a VR hand. Mirrors <see cref="VrhandInteraction.HandEnum"/> for new code paths
/// while keeping the legacy enum as the editor-bound choice on weapons / grab points.
/// </summary>
public enum HandSide
{
	Left,
	Right
}
