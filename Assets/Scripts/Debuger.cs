using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Debuger 
{
	public static List<string> myLogs = new List<string> ();
	static public void Log(object message)
	{
		Debug.Log (message);
		myLogs.Add (message.ToString());
	}
}

