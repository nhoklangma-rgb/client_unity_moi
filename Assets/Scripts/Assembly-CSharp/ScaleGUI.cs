using System.Collections.Generic;
using UnityEngine;

public class ScaleGUI
{
	public static bool scaleScreen;

	public static float WIDTH;

	public static float HEIGHT;

	public static float OFFSET_X;

	public static float OFFSET_Y;

	private static List<Matrix4x4> stack = new List<Matrix4x4>();

	public static void initScaleGUI()
	{
		Cout.println("Init Scale GUI: Screen.w=" + Screen.width + " Screen.h=" + Screen.height);
		OFFSET_X = 0f;
		OFFSET_Y = 0f;
		WIDTH = Screen.width;
		HEIGHT = Screen.height;
#if UNITY_IOS
		Rect safeArea = Screen.safeArea;
		if (safeArea.width > 0f && safeArea.height > 0f)
		{
			OFFSET_X = safeArea.x;
			OFFSET_Y = Screen.height - safeArea.yMax;
		}
#endif
		scaleScreen = false;
		_ = Screen.width;
		_ = 1200;
	}

	public static void BeginGUI()
	{
		if (scaleScreen)
		{
			stack.Add(GUI.matrix);
			Matrix4x4 matrix4x = default(Matrix4x4);
			float num = Screen.width;
			float num2 = Screen.height;
			float num3 = num / num2;
			float num4 = 1f;
			Vector3 zero = Vector3.zero;
			num4 = ((!(num3 < WIDTH / HEIGHT)) ? ((float)Screen.height / HEIGHT) : ((float)Screen.width / WIDTH));
			matrix4x.SetTRS(zero, Quaternion.identity, Vector3.one * num4);
			GUI.matrix *= matrix4x;
		}
	}

	public static void EndGUI()
	{
		if (scaleScreen)
		{
			GUI.matrix = stack[stack.Count - 1];
			stack.RemoveAt(stack.Count - 1);
		}
	}

	public static float scaleX(float x)
	{
		if (!scaleScreen)
		{
			return x;
		}
		x = x * WIDTH / (float)Screen.width;
		return x;
	}

	public static float scaleY(float y)
	{
		if (!scaleScreen)
		{
			return y;
		}
		y = y * HEIGHT / (float)Screen.height;
		return y;
	}
	public static int toGameX(float screenX)
	{
		float x = screenX;
		if (x < 0f)
		{
			x = 0f;
		}
		if (x > WIDTH)
		{
			x = WIDTH;
		}
		return (int)(x / (float)mGraphics.zoomLevel);
	}

	public static int toGameY(float screenYFromBottom)
	{
		float y = Screen.height - screenYFromBottom;
		if (y < 0f)
		{
			y = 0f;
		}
		if (y > HEIGHT)
		{
			y = HEIGHT;
		}
		return (int)(y / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard;
	}
}
