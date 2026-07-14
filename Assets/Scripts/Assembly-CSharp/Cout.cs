using UnityEngine;

public class Cout
{
	public static int count;

	public static void println(string s)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.Log(s);
#endif
	}

	public static void Log(string str)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.Log(str);
#endif
	}

	public static void LogError(string str)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogError(str);
#endif
	}

	public static void LogError2(string str)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogError(str);
#endif
	}

	public static void LogError3(string str)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogError(str);
#endif
	}

	public static void LogWarning(string str)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogWarning(str);
#endif
	}
}
