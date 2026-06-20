using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using UnityEngine;

public class Main : MonoBehaviour
{
	public static Main main;

	public static mGraphics g;

	public static GameMidlet midlet;

	public static string res = "res";

	public static string mainThreadName;

	public static bool started = false;

	public static bool isIpod;

	public static bool isIphone4;

	public static bool isWindowsPhone;

	public static bool isIPhone;

	public static bool IphoneVersionApp;

	public static string IMEI;

	public static int versionIp = 0;

	public static int numberQuit = 1;

	public static int typeClient = 4;

	public const sbyte PC_VERSION = 4;

	public const sbyte IP_APPSTORE = 5;

	public const sbyte WINDOWSPHONE = 6;

	public const sbyte IP_JB = 3;

	private Queue<IEnumerator> jobs = new Queue<IEnumerator>();

	private int updateCount;

	private int paintCount;

	private int count;

	private bool isRun;

	public static int waitTick;

	public static int f;

	private int valueKey;

	public static bool isResume;

	public static bool isMiniApp = true;

	public static bool isQuitApp;

	private Vector2 lastMousePos;

	public static int a = 1;

	public static bool isCompactDevice = true;

	public static float gameSpeed = 1.0f;

	private static GUIStyle cachedCenteredStyle;
	private static string cachedSpeedLabel;
	private static float cachedSpeedValue = -1f;
	private static bool sizeChangeInitialized = false;

#if UNITY_IOS
	private static AudioSource bgAudioSource;
	private static GameObject bgAudioObject;
#endif

	private void Start()
	{
		Debug.unityLogger.logEnabled = false; // Tắt toàn bộ debug log để giảm giật lag
		if (!started)
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
			if (Thread.CurrentThread.Name != "Main")
			{
				Thread.CurrentThread.Name = "Main";
			}
			mainThreadName = Thread.CurrentThread.Name;
			
			if (Application.platform == RuntimePlatform.Android)
			{
				GameMidlet.isPC = false;
				GameMidlet.DEVICE = GameMidlet.ANDROID;
			}
			else if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				GameMidlet.isPC = false;
				GameMidlet.DEVICE = GameMidlet.IOS;
			}
			else
			{
				GameMidlet.isPC = true;
				GameMidlet.DEVICE = GameMidlet.PC;
			}

			GameCanvas.isTouch = !GameMidlet.isPC;
			started = true;
			GameCanvas.readGraphicsPC();
			if (GameMidlet.isPC)
			{
				if (GameCanvas.lv == 0)
				{
					Screen.SetResolution(600, 355, fullscreen: false);
				}
				else
				{
					Screen.SetResolution(1024, 550, fullscreen: false);
				}
			}
		}
	}

	private void SetInit()
	{
		base.enabled = true;
	}

	private void OnHideUnity(bool isGameShown)
	{
		if (!isGameShown)
		{
			Time.timeScale = 0f;
		}
		else
		{
			Time.timeScale = gameSpeed;
		}
	}

	private void OnGUI()
	{
		if (count >= 10)
		{
			if (GameMidlet.gameCanvas == null)
			{
				return;
			}
			checkKeyInput();
			if (Event.current.type.Equals(EventType.Repaint))
			{
				try
				{
					GameMidlet.gameCanvas.paint(g);
					paintCount++;
				}
				catch (System.Exception)
				{
				}
				finally
				{
					g.reset();
					if (GameMidlet.gameCanvas != null && GameMidlet.gameCanvas.g != null)
					{
						GameMidlet.gameCanvas.g.reset();
					}
				}
			}

			float baseScale = 1f;
			float btnW = 35f * baseScale;
			float btnH = 30f * baseScale;
			float lblW = 100f * baseScale;
			float fontSize = 14f * baseScale;
			float yPos = 5f * baseScale;
			float startX = Screen.width / 2f - (btnW + lblW + btnW) / 2f;
#if UNITY_IOS
			Rect safeArea = Screen.safeArea;
			startX = safeArea.x + safeArea.width / 2f - (btnW + lblW + btnW) / 2f;
			float topOffset = Screen.height - safeArea.yMax;
			yPos = topOffset + 15f;
#endif

			Texture2D bgTex = Texture2D.whiteTexture;
			float bgX = startX - 10f;
			float bgY = yPos - 5f;
			float bgW = btnW + lblW + btnW + 20f;
			float bgH = btnH + 10f;
			Color prevColor = GUI.color;
			GUI.color = new Color(0f, 0f, 0f, 0.45f);
			GUI.DrawTexture(new Rect(bgX, bgY, bgW, bgH), bgTex);
			GUI.color = prevColor;

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = (int)fontSize;
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.fontStyle = FontStyle.Bold;

			GUI.color = Color.white;
			if (GUI.Button(new Rect(startX, yPos, btnW, btnH), "-", buttonStyle))
			{
				gameSpeed = Mathf.Max(0.1f, gameSpeed - 0.1f);
				Time.timeScale = gameSpeed;
				cachedSpeedValue = -1f;
			}

			if (cachedCenteredStyle == null)
			{
				cachedCenteredStyle = new GUIStyle(GUI.skin.label);
				cachedCenteredStyle.alignment = TextAnchor.MiddleCenter;
				cachedCenteredStyle.fontStyle = FontStyle.Bold;
			}
			cachedCenteredStyle.fontSize = (int)fontSize;
			cachedCenteredStyle.normal.textColor = Color.white;
			if (cachedSpeedValue != gameSpeed)
			{
				cachedSpeedValue = gameSpeed;
				string platformStr = "PC";
				if (Application.platform == RuntimePlatform.Android)
				{
					platformStr = "APK";
				}
				else if (Application.platform == RuntimePlatform.IPhonePlayer)
				{
					platformStr = "iOS";
				}
				cachedSpeedLabel = "Speed: " + System.Math.Round(gameSpeed, 1) + "x (" + platformStr + ")";
			}
			GUI.Label(new Rect(startX + btnW, yPos, lblW, btnH), cachedSpeedLabel, cachedCenteredStyle);

			if (GUI.Button(new Rect(startX + btnW + lblW, yPos, btnW, btnH), "+", buttonStyle))
			{
				gameSpeed = Mathf.Min(3.0f, gameSpeed + 0.1f);
				Time.timeScale = gameSpeed;
				cachedSpeedValue = -1f;
			}
			GUI.color = Color.white;
		}
	}

	public void setsizeChange()
	{
		if (!isRun)
		{
			try
			{
				Screen.orientation = ScreenOrientation.AutoRotation;
				Application.runInBackground = true;
				
				// === CẤU HÌNH ĐỒ HỌA SẮC NÉT CHO MOBILE ===
				QualitySettings.vSyncCount = 0; // Tắt VSync: giảm input lag, tiết kiệm CPU
#if UNITY_ANDROID || UNITY_IOS
				// Mobile: target 60fps
				Application.targetFrameRate = 60;
				// Giữ hình ảnh 2D sắc nét, không bị nhòe
				QualitySettings.antiAliasing = 0;
				QualitySettings.shadows = ShadowQuality.Disable;
				QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
				QualitySettings.pixelLightCount = 0;
				QualitySettings.skinWeights = SkinWeights.OneBone;
				QualitySettings.softParticles = false;
				QualitySettings.softVegetation = false;
				QualitySettings.realtimeReflectionProbes = false;
				QualitySettings.billboardsFaceCameraPosition = false;
				QualitySettings.particleRaycastBudget = 4;
				QualitySettings.lodBias = 1.0f;
				// Sử dụng độ phân giải ảnh gốc sắc nét (không giảm mipmap)
				QualitySettings.globalTextureMipmapLimit = 0;
				GameCanvas.lowGraphic = false; // Tắt chế độ cấu hình thấp
#else
				// PC: 60 FPS cho 2D là quá đủ
				Application.targetFrameRate = 60;
#endif
#if UNITY_IOS
				// iOS: khởi tạo silent audio để chạy ngầm
				InitBackgroundAudio();
#endif
				Time.fixedDeltaTime = 0.033f; // 30Hz physics - đồng bộ với 30fps mobile
				Screen.sleepTimeout = SleepTimeout.NeverSleep;
				Time.timeScale = gameSpeed;
				base.useGUILayout = false;
				isCompactDevice = detectCompactDevice();
				if (main == null)
				{
					main = this;
				}
				isRun = true;
				ScaleGUI.initScaleGUI();
				IMEI = SystemInfo.deviceUniqueIdentifier; 
				if (GameMidlet.isPC)
				{
					Screen.fullScreen = false;
				}
				if (isWindowsPhone)
				{
					typeClient = 6;
				}
				if (GameMidlet.isPC)
				{
					typeClient = 4;
				}
				if (IphoneVersionApp)
				{
					typeClient = 5;
				}
				try
				{
					if (iPhoneSettings.generation == iPhoneGeneration.iPodTouch4Gen)
						isIpod = true;
					if (iPhoneSettings.generation == iPhoneGeneration.iPhone4)
						isIphone4 = true;
				}
				catch (Exception)
				{
				}
				g = new mGraphics();
				midlet = new GameMidlet();
				Key.mapKeyPC();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				isRun = false;
			}
		}
	}

	public static void setBackupIcloud(string path)
	{
	}

	public string GetMacAddress()
	{
		return SystemInfo.deviceUniqueIdentifier;
	}

	public void doClearRMS()
	{
	}

	public static void closeKeyBoard()
	{
		if (TouchScreenKeyboard.visible)
		{
			if (TField.kb != null)
			{
				TField.kb.active = false;
				TField.kb = null;
			}
			if (ipKeyboard.tk != null)
			{
				ipKeyboard.tk.active = false;
				ipKeyboard.tk = null;
			}
		}
	}

	private void FixedUpdate()
	{
		try
		{
			Rms.update();
			count++;
			if (count < 10)
			{
				return;
			}
			Image.update();
			if (!sizeChangeInitialized)
			{
				bool canInit = true;
				if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
				{
					if (Screen.width <= Screen.height || Screen.width < 300 || Screen.height < 300)
					{
						canInit = false;
					}
				}
				if (canInit)
				{
					setsizeChange();
					if (isRun)
					{
						sizeChangeInitialized = true;
					}
				}
			}
			updateCount++;
			ipKeyboard.update();
			if (GameMidlet.gameCanvas != null)
				GameMidlet.gameCanvas.update();
			DataInputStream.update(); 
			Net.update();
			f++;
			if (f > 8)
			{
				f = 0;
			}
			if (GameCanvas.isDisConnect)
			{
				GameCanvas.isDisConnect = false;
				string info = T.disconnect;
				if (GameCanvas.infoDisConnect != null && GameCanvas.infoDisConnect.Length > 10)
				{
					info = GameCanvas.infoDisConnect;
					GameCanvas.infoDisConnect = "";
				}
				bool flag = false;
				mVector mVector2 = new mVector();
				if (GameCanvas.currentScreen != GameCanvas.loginScr && GameCanvas.currentScreen != GameCanvas.loadMapScr)
				{
					mVector2.addElement(GameScreen.cmdReConnect);
					flag = true;
				}
				mVector2.addElement(GameCanvas.gameScr.cmdExit);
				if (flag)
				{
					GameCanvas.Start_ReConect_DiaLog(info, mVector2, isCmdClose: false);
				}
				else
				{
					GameCanvas.Start_Normal_DiaLog(info, mVector2, isCmdClose: false);
				}
			}
		}
		catch (System.Exception)
		{
			// Bảo vệ game loop: nếu có lỗi trong 1 frame, vẫn tiếp tục frame sau
		}
	}

	private void Awake()
	{
		main = this;
	}

	private void Update()
	{
		while (jobs.Count > 0)
		{
			StartCoroutine(jobs.Dequeue());
		}
		if (count >= 10)
		{
			Session_ME.update();
		}
		checkMouseInput();
	}

	internal void AddJob(IEnumerator newJob)
	{
		jobs.Enqueue(newJob);
	}

	private void checkMouseInput()
	{
		if (count < 10 || GameMidlet.gameCanvas == null)
		{
			return;
		}
		if (Input.GetMouseButtonDown(0) && valueKey == 0)
		{
			valueKey = 1;
			Vector3 mousePosition = Input.mousePosition;
			GameMidlet.gameCanvas.onPointerPressed((int)(mousePosition.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
			lastMousePos.x = mousePosition.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
		}
		if (Input.GetMouseButton(0))
		{
			Vector3 mousePosition2 = Input.mousePosition;
			GameMidlet.gameCanvas.onPointerDragged((int)(mousePosition2.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition2.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
			lastMousePos.x = mousePosition2.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition2.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
		}
		if (Input.GetMouseButtonUp(0) && valueKey == 1)
		{
			valueKey = 0;
			Vector3 mousePosition3 = Input.mousePosition;
			lastMousePos.x = mousePosition3.x / (float)mGraphics.zoomLevel;
			lastMousePos.y = mousePosition3.y / (float)mGraphics.zoomLevel + (float)mGraphics.addYWhenOpenKeyBoard;
			GameMidlet.gameCanvas.onPointerReleased((int)(mousePosition3.x / (float)mGraphics.zoomLevel), (int)(((float)Screen.height - mousePosition3.y) / (float)mGraphics.zoomLevel) + mGraphics.addYWhenOpenKeyBoard);
		}
	}

	private void checkKeyInput()
	{
		if (Input.anyKeyDown && Event.current.type == EventType.KeyDown)
		{
			int num = MyKeyMap.map(Event.current.keyCode);
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				switch (Event.current.keyCode)
				{
				case KeyCode.Alpha2:
					num = 64;
					break;
				case KeyCode.Minus:
					num = 95;
					break;
				}
			}
			if (num != 0)
			{
				GameMidlet.gameCanvas.keyPressed(num);
			}
		}
		if (Event.current.type == EventType.KeyUp)
		{
			int num2 = MyKeyMap.map(Event.current.keyCode);
			if (num2 != 0)
			{
				GameMidlet.gameCanvas.keyReleased(num2);
			}
		}
	}

#if UNITY_IOS
	private void InitBackgroundAudio()
	{
		if (bgAudioObject != null) return;
		bgAudioObject = new GameObject("iOS_BG_Audio");
		bgAudioObject.hideFlags = HideFlags.HideAndDontSave;
		DontDestroyOnLoad(bgAudioObject);
		bgAudioSource = bgAudioObject.AddComponent<AudioSource>();
		bgAudioSource.loop = true;
		bgAudioSource.volume = 0f; // Silent
		bgAudioSource.priority = 0; // Highest priority
		AudioClip silentClip = AudioClip.Create("Silence", 1, 1, AudioSettings.outputSampleRate, false);
		float[] samples = new float[silentClip.samples];
		silentClip.SetData(samples, 0);
		bgAudioSource.clip = silentClip;
		bgAudioSource.Play();
	}

	private void OnLowMemory()
	{
		Resources.UnloadUnusedAssets();
		System.GC.Collect();
	}
#endif

	private void OnApplicationQuit()
	{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		Debug.LogWarning("APP QUIT");
#endif
		Session_ME.gI().close();
		if (GameMidlet.isPC)
		{
			Application.Quit();
		}
	}

	private void OnApplicationPause(bool paused)
	{
		isResume = false;
		if (paused)
		{
#if UNITY_IOS
			Application.targetFrameRate = 15;
			Resources.UnloadUnusedAssets();
#endif
			isQuitApp = false;
		}
		else
		{
			isResume = true;
			Time.timeScale = gameSpeed;
#if UNITY_IOS
			Application.targetFrameRate = 60;
#endif
		}
		if (TouchScreenKeyboard.visible)
		{
			if (TField.kb != null)
			{
				TField.kb.active = false;
				TField.kb = null;
			}
			if (ipKeyboard.tk != null)
			{
				ipKeyboard.tk.active = false;
				ipKeyboard.tk = null;
			}
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			Time.timeScale = gameSpeed;
		}
	}

	public static void exit()
	{
		Session_ME.gI().close();
		Application.Quit();
	}

	public static bool detectCompactDevice()
	{
		try
		{
			iPhoneGeneration gen = iPhoneSettings.generation;
			if (gen == iPhoneGeneration.iPhone || gen == iPhoneGeneration.iPhone3G 
			    || gen == iPhoneGeneration.iPodTouch1Gen || gen == iPhoneGeneration.iPodTouch2Gen)
			{
				return false;
			}
		}
		catch (Exception)
		{
		}
		return true;
	}

	public static bool checkCanSendSMS()
	{
		try
		{
			if (iPhoneSettings.generation == iPhoneGeneration.iPhone3GS || iPhoneSettings.generation == iPhoneGeneration.iPhone4 || iPhoneSettings.generation > iPhoneGeneration.iPodTouch4Gen)
			{
				return true;
			}
		}
		catch (Exception)
		{
		}
		return false;
	}
}
