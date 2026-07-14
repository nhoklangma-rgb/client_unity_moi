using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Session_ME : ISession
{
	public class Sender
	{
		// ConcurrentQueue is thread-safe and O(1) for enqueue/dequeue (was List with O(n) RemoveAt(0))
		public ConcurrentQueue<Message> sendingMessage;

		// Event-based signaling: sender thread sleeps until a message is enqueued
		private ManualResetEventSlim _sendEvent = new ManualResetEventSlim(false);

		public Sender()
		{
			sendingMessage = new ConcurrentQueue<Message>();
		}

		public void AddMessage(Message message)
		{
			sendingMessage.Enqueue(message);
			_sendEvent.Set();
		}

		public void run()
		{
			while (connected)
			{
				try
				{
					// Wait efficiently until a message is enqueued (or timeout to re-check connected flag)
					_sendEvent.Wait(50);
					_sendEvent.Reset();

					if (getKeyComplete)
					{
						Message msg;
						while (sendingMessage.TryDequeue(out msg))
						{
							doSendMessage(msg);
						}
					}
				}
				catch (Exception ex)
				{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
					mSystem.outz("error send message! e: " + ex);
#endif
				}
			}
		}
	}

	private class MessageCollector
	{
		public void run()
		{
			try
			{
				while (connected)
				{
					Message message = readMessage();
					if (message == null)
					{
						break;
					}
					try
					{
						if (message.command == -27)
						{
							getKey(message);
						}
						else
						{
							onRecieveMsg(message);
						}
					}
					catch (Exception e)
					{
						Out.printError(e);
					}
				}
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.Log("error read message!  e: " + ex.Message.ToString());
#endif
			}
			if (!connected)
			{
				return;
			}
			if (messageHandler != null)
			{
				if (currentTimeMillis() - timeConnected > 500)
				{
					isDisconnected = true;
				}
				else
				{
					isConnectionFail = true;
				}
			}
			if (sc != null)
			{
				cleanNetwork();
			}
		}

		private void getKey(Message message)
		{
			try
			{
				sbyte b = message.reader().readSByte();
				key = new sbyte[b];
				for (int i = 0; i < b; i++)
				{
					key[i] = message.reader().readSByte();
				}
				for (int j = 0; j < key.Length - 1; j++)
				{
					key[j + 1] ^= key[j];
				}
				getKeyComplete = true;
			}
			catch (Exception)
			{
			}
		}

		private Message readMessage()
		{
			try
			{
				sbyte b = dis.ReadSByte();
				lastActiveTime = currentTimeMillis();
				if (getKeyComplete)
				{
					b = readKey(b);
				}
				int num;
				if (getKeyComplete)
				{
					if (b == -39 || b == -101 || b == -93 || b == 76)
					{
						sbyte b2 = dis.ReadSByte();
						sbyte b3 = dis.ReadSByte();
						sbyte b4 = dis.ReadSByte();
						sbyte b5 = dis.ReadSByte();
						num = ((readKey(b2) & 0xFF) << 24) | ((readKey(b3) & 0xFF) << 16) | ((readKey(b4) & 0xFF) << 8) | (readKey(b5) & 0xFF);
					}
					else
					{
						sbyte b6 = dis.ReadSByte();
						sbyte b7 = dis.ReadSByte();
						num = ((readKey(b6) & 0xFF) << 8) | (readKey(b7) & 0xFF);
					}
				}
				else if (b == -39)
				{
					num = dis.ReadInt32();
				}
				else
				{
					sbyte num2 = dis.ReadSByte();
					sbyte b8 = dis.ReadSByte();
					num = (num2 & 0xFF00) | (b8 & 0xFF);
				}
				sbyte[] array = new sbyte[num];
				Buffer.BlockCopy(dis.ReadBytes(num), 0, array, 0, num);
				recvByteCount += 5 + num;
				if (getKeyComplete)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = readKey(array[i]);
					}
				}
				return new Message(b, array);
			}
			catch (Exception ex)
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				Debug.Log(ex.StackTrace.ToString());
#endif
			}
			return null;
		}
	}

	protected static Session_ME instance = new Session_ME();

	private static NetworkStream dataStream;

	private static BinaryReader dis;

	private static BinaryWriter dos;

	public static IMessageHandler messageHandler;

	private static TcpClient sc;

	public static volatile bool connected;

	public static bool connecting;

	public static bool isStart;

	private static Sender sender = new Sender();

	public static Thread initThread;

	public static Thread collectorThread;

	public static Thread sendThread;

	public static int sendByteCount;

	public static int recvByteCount;

	private static bool getKeyComplete;

	public static sbyte[] key = null;

	private static sbyte curR;

	private static sbyte curW;

	private static int timeConnected;

	public static string strRecvByteCount = "";

	public static bool isCancel;

	public static bool isConnectOK;
	public static bool isConnectionFail;
	public static bool isDisconnected;

	private string host;

	private int port;

	public static long timeStart = 0L;

	public static volatile int lastActiveTime = 0;

	private static string test = "";

	public static ConcurrentQueue<Message> recieveMsg = new ConcurrentQueue<Message>();

	public Session_ME()
	{
		mSystem.outz("init Session_ME");
	}

	public void clearSendingMessage()
	{
		Message dummy;
		while (sender.sendingMessage.TryDequeue(out dummy)) { }
	}

	public static Session_ME gI()
	{
		if (instance == null)
		{
			instance = new Session_ME();
		}
		return instance;
	}

	public bool isConnected()
	{
		return connected;
	}

	public void setHandler(IMessageHandler msgHandler)
	{
		messageHandler = msgHandler;
	}

	public void connect(string host, int port)
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		mSystem.outz("connect ... " + connected + "  ::  " + connecting + " host = " + host + " port = " + port);
#endif
		if (!connected && !connecting)
		{
			this.host = host;
			this.port = port;
			getKeyComplete = false;
			sc = null;
			isCancel = false;
			isConnectOK = false;
			isConnectionFail = false;
			isDisconnected = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			mSystem.outz("connecting...!  host: " + host + " port: " + port);
#endif
			initThread = new Thread(NetworkInit);
			initThread.Start();
		}
	}

	private void NetworkInit()
	{
		try
		{
			isCancel = false;
			connecting = true;
			Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
			doConnect(host, port);
			isConnectOK = true;
		}
		catch (Exception)
		{
			connecting = false;
			connected = false;
			if (messageHandler != null)
			{
				isConnectionFail = true;
			}
		}
	}

	public void doConnect(string host, int port)
	{
		isStart = true;
		timeStart = GameCanvas.getTime();
		sc = new TcpClient();
		sc.Connect(host, port);
		connected = true;
		dataStream = sc.GetStream();
		isStart = false;
		dis = new BinaryReader(dataStream, new UTF8Encoding());
		dos = new BinaryWriter(dataStream, new UTF8Encoding());
		new Thread(sender.run).Start();
		collectorThread = new Thread(new MessageCollector().run);
		collectorThread.Start();
		timeConnected = currentTimeMillis();
		connecting = false;
		doSendMessage(new Message((sbyte)(-27)));
	}

	public void sendMessage(Message message)
	{
		sender.AddMessage(message);
	}

	public void sendKeepAlive()
	{
		try
		{
			sendMessage(new Message((sbyte)(-120)));
		}
		catch (Exception)
		{
		}
	}

	private static void doSendMessage(Message m)
	{
		lastActiveTime = currentTimeMillis();
		sbyte[] data = m.getData();
		try
		{
			if (getKeyComplete)
			{
				sbyte value = writeKey(m.command);
				dos.Write(value);
			}
			else
			{
				dos.Write(m.command);
			}
			if (data != null)
			{
				int num = data.Length;
				if (getKeyComplete)
				{
					int num2 = writeKey((sbyte)(num >> 8));
					dos.Write((sbyte)num2);
					int num3 = writeKey((sbyte)(num & 0xFF));
					dos.Write((sbyte)num3);
				}
				else
				{
					dos.Write((ushort)num);
				}
				if (getKeyComplete)
				{
					for (int i = 0; i < data.Length; i++)
					{
						sbyte value2 = writeKey(data[i]);
						dos.Write(value2);
					}
				}
				sendByteCount += 5 + data.Length;
			}
			else
			{
				if (getKeyComplete)
				{
					int num4 = writeKey((sbyte)0);
					dos.Write((sbyte)num4);
					int num5 = writeKey((sbyte)0);
					dos.Write((sbyte)num5);
				}
				else
				{
					dos.Write((ushort)0);
				}
				sendByteCount += 5;
			}
			dos.Flush();
		}
		catch (Exception ex)
		{
			mSystem.outz("ERROR SEND MSG  e: " + ex.StackTrace + "   , command : " + m.command);
		}
	}

	public static sbyte readKey(sbyte b)
	{
		sbyte result = (sbyte)((key[curR++] & 0xFF) ^ (b & 0xFF));
		if (curR >= key.Length)
		{
			curR %= (sbyte)key.Length;
		}
		return result;
	}

	public static sbyte writeKey(sbyte b)
	{
		sbyte result = (sbyte)((key[curW++] & 0xFF) ^ (b & 0xFF));
		if (curW >= key.Length)
		{
			curW %= (sbyte)key.Length;
		}
		return result;
	}

	public static void onRecieveMsg(Message msg)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			IMessageHandler handler = messageHandler;
			if (handler != null)
			{
				handler.onMessage(msg);
			}
		}
		else
		{
			recieveMsg.Enqueue(msg);
		}
	}

	public static void update()
	{
		IMessageHandler handler = messageHandler;
		if (handler == null) return;
		
		if (isConnectOK)
		{
			isConnectOK = false;
			handler.onConnectOK();
		}
		if (isConnectionFail)
		{
			isConnectionFail = false;
			handler.onConnectionFail();
		}
		if (isDisconnected)
		{
			isDisconnected = false;
			handler.onDisconnected();
		}

		if (connected && currentTimeMillis() - lastActiveTime > 30000)
		{
			lastActiveTime = currentTimeMillis();
			gI().sendKeepAlive();
		}

		long timeBudget = mSystem.currentTimeMillis() + 8;
		int processCount = 0;
		Message message;
		while (recieveMsg.TryDequeue(out message))
		{
			if (message == null) break;
			try
			{
				handler.onMessage(message);
			}
			catch (Exception)
			{
			}
			processCount++;
			if (processCount >= 10 && mSystem.currentTimeMillis() >= timeBudget) break;
		}
	}

	public void close()
	{
		Message dummy;
		while (recieveMsg.TryDequeue(out dummy)) { }
		cleanNetwork();
		isStart = false;
	}

	private static void cleanNetwork()
	{
		key = null;
		curR = 0;
		curW = 0;
		try
		{
			connected = false;
			connecting = false;
			if (sc != null)
			{
				try { sc.Close(); } catch (Exception) { }
				sc = null;
			}
			if (dataStream != null)
			{
				try { dataStream.Close(); } catch (Exception) { }
				dataStream = null;
			}
			if (dos != null)
			{
				try { dos.Close(); } catch (Exception) { }
				dos = null;
			}
			if (dis != null)
			{
				try { dis.Close(); } catch (Exception) { }
				dis = null;
			}
			sendThread = null;
			collectorThread = null;
			mSystem.gcc(); // Trigger GC on disconnect to free all unused network-related graphics & memory
		}
		catch (Exception)
		{
		}
	}

	public static int currentTimeMillis()
	{
		return Environment.TickCount;
	}

	public static byte convertSbyteToByte(sbyte var)
	{
		if (var > 0)
		{
			return (byte)var;
		}
		return (byte)(var + 256);
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
}
