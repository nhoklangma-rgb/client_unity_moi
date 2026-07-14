using System;
using UnityEngine;

public class Out
{
	public static void printLine(string text)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.Log("aaa: " + text);
#endif
	}

	public static void printError(Exception e)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogError(e);
#endif
	}
}
