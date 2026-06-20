using System;
using System.IO;
using System.Threading;
using UnityEngine;

public class Rms
{
	public static int status;

	public static sbyte[] data;

	public static string filename;

	private static System.Collections.Generic.Dictionary<string, sbyte[]> rmsCache = new System.Collections.Generic.Dictionary<string, sbyte[]>();

	private const int INTERVAL = 5;

	private const int MAXTIME = 500;

	public static void saveRMS(string filename, sbyte[] data)
	{
		__saveRMS("x" + mGraphics.zoomLevel + filename, data);
	}

	public static sbyte[] loadRMS(string filename)
	{
		return __loadRMS("x" + mGraphics.zoomLevel + filename);
	}

	public static string loadRMSString(string fileName)
	{
		sbyte[] array = loadRMS(fileName);
		if (array == null)
		{
			return null;
		}
		DataInputStream dataInputStream = new DataInputStream(array);
		try
		{
			string result = dataInputStream.readUTF();
			dataInputStream.close();
			return result;
		}
		catch (Exception ex)
		{
			Cout.println(ex.StackTrace);
		}
		return null;
	}

	public static byte[] convertSbyteToByte(sbyte[] var)
	{
		byte[] array = new byte[var.Length];
		for (int i = 0; i < var.Length; i++)
		{
			if (var[i] > 0)
			{
				array[i] = (byte)var[i];
			}
			else
			{
				array[i] = (byte)(var[i] + 256);
			}
		}
		return array;
	}

	public static void saveRMSString(string filename, string data)
	{
		DataOutputStream dataOutputStream = new DataOutputStream();
		try
		{
			dataOutputStream.writeUTF(data);
			saveRMS(filename, dataOutputStream.toByteArray());
			dataOutputStream.close();
		}
		catch (Exception ex)
		{
			Cout.println(ex.StackTrace);
		}
	}

	public static void update()
	{
	}

	public static int loadRMSInt(string file)
	{
		sbyte[] array = loadRMS(file);
		if (array != null)
		{
			return array[0];
		}
		return -1;
	}

	public static void saveRMSInt(string file, int x)
	{
		try
		{
			saveRMS(file, new sbyte[1] { (sbyte)x });
		}
		catch (Exception)
		{
		}
	}

	public static string GetiPhoneDocumentsPath()
	{
		return Application.persistentDataPath;
	}

	private static void __saveRMS(string filename, sbyte[] data)
	{
		lock (rmsCache)
		{
			rmsCache[filename] = data;
		}
		string text = GetiPhoneDocumentsPath() + "/" + filename;
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				FileStream fileStream = new FileStream(text, FileMode.Create);
				fileStream.Write(ArrayCast.cast(data), 0, data.Length);
				fileStream.Flush();
				fileStream.Close();
				Main.setBackupIcloud(text);
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.Log(ex.StackTrace);
#endif
			}
		});
	}

	private static sbyte[] __loadRMS(string filename)
	{
		lock (rmsCache)
		{
			if (rmsCache.TryGetValue(filename, out sbyte[] cachedData))
			{
				return cachedData;
			}
		}
		try
		{
			FileStream fileStream = new FileStream(GetiPhoneDocumentsPath() + "/" + filename, FileMode.Open);
			byte[] array = new byte[fileStream.Length];
			fileStream.Read(array, 0, array.Length);
			fileStream.Close();
			sbyte[] casted = ArrayCast.cast(array);
			lock (rmsCache)
			{
				rmsCache[filename] = casted;
			}
			return casted;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static void clearAll()
	{
		Debug.LogWarning("ALL RMS CLEAR");
		lock (rmsCache)
		{
			rmsCache.Clear();
		}
		PlayerPrefs.DeleteAll();
		FileInfo[] files = new DirectoryInfo(GetiPhoneDocumentsPath() + "/").GetFiles();
		for (int i = 0; i < files.Length; i++)
		{
			files[i].Delete();
		}
	}

	public static void DeleteStorage(string path)
	{
		try
		{
			lock (rmsCache)
			{
				rmsCache.Remove(path);
				rmsCache.Remove("x" + mGraphics.zoomLevel + path);
			}
			File.Delete(GetiPhoneDocumentsPath() + "/" + path);
		}
		catch (Exception)
		{
		}
	}

	public static string ByteArrayToString(byte[] ba)
	{
		return BitConverter.ToString(ba).Replace("-", "");
	}

	public static byte[] StringToByteArray(string hex)
	{
		int length = hex.Length;
		byte[] array = new byte[length / 2];
		for (int i = 0; i < length; i += 2)
		{
			array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
		}
		return array;
	}

	public static void deleteRecord(string name)
	{
		try
		{
			lock (rmsCache)
			{
				rmsCache.Remove(name);
				rmsCache.Remove("x" + mGraphics.zoomLevel + name);
			}
			PlayerPrefs.DeleteKey(name);
		}
		catch (Exception ex)
		{
			Cout.println("loi xoa RMS --------------------------" + ex.ToString());
		}
	}

	public static void clearRMS()
	{
		deleteRecord("data");
		deleteRecord("dataVersion");
		deleteRecord("map");
		deleteRecord("mapVersion");
		deleteRecord("skill");
		deleteRecord("killVersion");
		deleteRecord("item");
		deleteRecord("itemVersion");
	}

	public static void saveIP(string strID)
	{
		saveRMSString("NRIPlink", strID);
	}

	public static string loadIP()
	{
		string text = loadRMSString("NRIPlink");
		if (text == null)
		{
			return null;
		}
		return text;
	}

	public static int loadRMSInt2(string file)
	{
		sbyte[] array = loadRMS2(file);
		if (array != null)
		{
			return array[0];
		}
		return -1;
	}

	public static void saveRMSInt2(string file, int x)
	{
		try
		{
			saveRMS2(file, new sbyte[1] { (sbyte)x });
		}
		catch (Exception)
		{
		}
	}

	public static void saveRMS2(string filename, sbyte[] data)
	{
		__saveRMS(filename, data);
	}

	public static sbyte[] loadRMS2(string filename)
	{
		return __loadRMS(filename);
	}
}
