using System;

namespace TFT.VR.Logic;

public static class RotationClampRules
{
	public static float ClampInterpolationBySpeed( float angleDeg, float baseT, float maxDegreesPerSecond, float deltaTime )
	{
		if ( angleDeg <= 0.001f || deltaTime <= 0f || maxDegreesPerSecond <= 0f )
			return MathX.Clamp( baseT, 0f, 1f );

		var allowedAngle = maxDegreesPerSecond * deltaTime;
		var cap = MathX.Clamp( allowedAngle / angleDeg, 0f, 1f );
		return MathX.Clamp( Math.Min( baseT, cap ), 0f, 1f );
	}
}
