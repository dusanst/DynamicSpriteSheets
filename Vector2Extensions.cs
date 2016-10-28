using UnityEngine;

public static class Vector2Extensions
{
	/// <summary>
	/// Scales vector's each coordinate with corresponding coordinate in scale vector.
	/// If some scale's coordinate is zero than it doesn't have effect on that coordinate.
	/// </summary>
	public static void InverseScale(this Vector2 vector, Vector2 scale)
	{
		if (scale.x != 0f)
		{
			vector.x /= scale.x;
		}
		if (scale.y != 0f)
		{
			vector.y /= scale.y;
		}
	}
}
