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
	private static GUIStyle cachedButtonStyle;
	private static int cachedButtonFontSize;
	private static VivoxManager _cachedVivoxRef;
	private static bool sizeChangeInitialized = false;
	private static int lastScreenWidth;
	private static int lastScreenHeight;

	// Pill HUD Panel State - cache tái sử dụng để tránh alloc trong OnGUI
	private static Texture2D cachedPillBg;
	private static Texture2D cachedCircleWhite;
	private static Texture2D cachedRoundedBoxWhite;
	private static int micClickFeedbackTick;
	private static bool isGameAudioMuted = false;

	// Pre-allocated layout cache để OnGUI không alloc mỗi frame
	private static readonly Rect[] _pillRectCache = new Rect[16];
	private static readonly Color[] _pillColorCache = new Color[16];
	private static int _pillRectCount = 0;
	private static int _pillColorCount = 0;
	private static Color _scratchColor = Color.white;
	private static Matrix4x4 _scratchMatrix;

	private const int PILL_RECT_PILL = 0;
	private const int PILL_RECT_SND = 1;
	private const int PILL_RECT_VOICE = 2;
	private const int PILL_RECT_MIC = 3;
	private const int PILL_RECT_DEC = 4;
	private const int PILL_RECT_LBL = 5;
	private const int PILL_RECT_INC = 6;
	private const int PILL_RECT_SEPARATOR = 7;
	private const int PILL_RECT_TMP1 = 8;
	private const int PILL_RECT_TMP2 = 9;
	private const int PILL_RECT_TMP3 = 10;
	private const int PILL_RECT_TMP4 = 11;

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
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				baseScale = Mathf.Max(1.5f, Screen.width / 640f);
				if (baseScale > 3.0f) baseScale = 3.0f;
			}
			
			DrawPillMenu(baseScale);
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
				
				// === Cáº¤U HÃŒNH Äá»’ Há»ŒA Sáº®C NÃ‰T CHO MOBILE ===
				QualitySettings.vSyncCount = 0; // Táº¯t VSync: giáº£m input lag, tiáº¿t kiá»‡m CPU
#if UNITY_ANDROID || UNITY_IOS
				// Mobile: target 60fps
				Application.targetFrameRate = 60;
				// Giá»¯ hÃ¬nh áº£nh 2D sáº¯c nÃ©t, khÃ´ng bá»‹ nhÃ²e
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
				// Sá»­ dá»¥ng Ä‘á»™ phÃ¢n giáº£i áº£nh gá»‘c sáº¯c nÃ©t (khÃ´ng giáº£m mipmap)
				QualitySettings.globalTextureMipmapLimit = 0;
				GameCanvas.lowGraphic = false; // Táº¯t cháº¿ Ä‘á»™ cáº¥u hÃ¬nh tháº¥p
#else
				// PC: 60 FPS cho 2D lÃ  quÃ¡ Ä‘á»§
				Application.targetFrameRate = 60;
#endif
#if UNITY_IOS
				// iOS: khá»Ÿi táº¡o silent audio Ä‘á»ƒ cháº¡y ngáº§m
				InitBackgroundAudio();
#endif
				Time.fixedDeltaTime = 0.01667f; // 60Hz - game logic chạy đồng bộ 60fps
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
				if (MotherCanvas.instance != null)
				{
					MotherCanvas.instance.checkZoomLevel();
				}
				lastScreenWidth = Screen.width;
				lastScreenHeight = Screen.height;
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
			// Decrement mic click feedback in FixedUpdate (1x/frame) instead of OnGUI (3-5x/frame)
			if (micClickFeedbackTick > 0) micClickFeedbackTick--;
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
			else if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
			{
				ScaleGUI.initScaleGUI();
				if (MotherCanvas.instance != null)
				{
					MotherCanvas.instance.checkZoomLevel();
				}
				lastScreenWidth = Screen.width;
				lastScreenHeight = Screen.height;
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
			// Báº£o vá»‡ game loop: náº¿u cÃ³ lá»—i trong 1 frame, váº«n tiáº¿p tá»¥c frame sau
		}
	}

	private void Awake()
	{
		main = this;
	}

	private static void EnsurePillMenuAssets()
	{
		if (cachedPillBg == null)
		{
			cachedPillBg = CreateRoundedRectTexture(250, 36, 12f, new Color(0.12f, 0.12f, 0.12f, 0.75f), new Color(1f, 1f, 1f, 0.15f), 1f);
			cachedCircleWhite = CreateRoundedRectTexture(32, 32, 16f, Color.white, Color.clear, 0f);
			cachedRoundedBoxWhite = CreateRoundedRectTexture(32, 32, 6f, Color.white, Color.clear, 0f);
		}
	}

	private static Texture2D CreateRoundedRectTexture(int w, int h, float r, Color fillColor, Color borderColor, float borderWidth)
	{
		Texture2D texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				float cx = (x < r) ? r : ((x > w - r - 1) ? (w - r - 1) : x);
				float cy = (y < r) ? r : ((y > h - r - 1) ? (h - r - 1) : y);
				float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
				
				Color pixelColor = Color.clear;
				if (dist <= r)
				{
					if (borderWidth > 0.05f && dist >= r - borderWidth)
					{
						float alpha = (r - dist) / borderWidth;
						pixelColor = Color.Lerp(borderColor, fillColor, alpha);
					}
					else
					{
						pixelColor = fillColor;
					}
				}
				else if (dist <= r + 1f)
				{
					float alpha = 1f - (dist - r);
					Color edgeColor = borderColor;
					edgeColor.a *= alpha;
					pixelColor = edgeColor;
				}
				texture.SetPixel(x, y, pixelColor);
			}
		}
		texture.Apply();
		return texture;
	}

	private static void DrawPillMenu(float baseScale)
	{
		EnsurePillMenuAssets();

		if (_cachedVivoxRef == null) _cachedVivoxRef = VivoxManager.Instance;

		float pillWidth = 250f * baseScale;
		float pillHeight = 36f * baseScale;

		float startX = Screen.width / 2f - pillWidth / 2f;
		float startY = 8f * baseScale;
#if UNITY_IOS
		Rect safeArea = Screen.safeArea;
		startX = safeArea.x + safeArea.width / 2f - pillWidth / 2f;
		float topOffset = Screen.height - safeArea.yMax;
		startY = topOffset + 12f;
#endif

		Rect pillRect = _pillRectCache[PILL_RECT_PILL];
		pillRect.x = startX;
		pillRect.y = startY;
		pillRect.width = pillWidth;
		pillRect.height = pillHeight;
		_pillRectCache[PILL_RECT_PILL] = pillRect;
		GUI.DrawTexture(pillRect, cachedPillBg);

		float pad = 6f * baseScale;
		float btnSize = 24f * baseScale;
		float curX = startX + 14f * baseScale;
		float curY = startY + (pillHeight - btnSize) / 2f;

		// 1. Draw Game Sound Button (Loa Game)
		Rect sndRect = _pillRectCache[PILL_RECT_SND];
		sndRect.x = curX;
		sndRect.y = curY;
		sndRect.width = btnSize;
		sndRect.height = btnSize;
		_pillRectCache[PILL_RECT_SND] = sndRect;
		SetCachedColor(isGameAudioMuted ? 0 : 1, isGameAudioMuted ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : new Color(0.2f, 0.55f, 0.9f, 0.9f));
		GUI.color = _pillColorCache[isGameAudioMuted ? 0 : 1];
		GUI.DrawTexture(sndRect, cachedCircleWhite);

		GUI.color = Color.white;
		DrawSpeakerIcon(sndRect, isGameAudioMuted);

		if (GUI.Button(sndRect, GUIContent.none, GUIStyle.none))
		{
			isGameAudioMuted = !isGameAudioMuted;
			AudioListener.pause = isGameAudioMuted;
			Interface_Game.addInfoPlayerNormal(isGameAudioMuted ? "Đã tắt âm thanh game" : "Đã bật âm thanh game", mFont.tahoma_7_yellow);
		}

		curX = sndRect.xMax + pad;

		// 2. Draw Voice Chat Speaker Button (Loa Mic)
		Rect voiceSndRect = _pillRectCache[PILL_RECT_VOICE];
		voiceSndRect.x = curX;
		voiceSndRect.y = curY;
		voiceSndRect.width = btnSize;
		voiceSndRect.height = btnSize;
		_pillRectCache[PILL_RECT_VOICE] = voiceSndRect;
		bool isConnected = _cachedVivoxRef != null && !string.IsNullOrEmpty(_cachedVivoxRef.CurrentChannelName);
		bool isVoiceMuted = _cachedVivoxRef != null && _cachedVivoxRef.IsOutputMuted;

		Color voiceSndBgColor;
		if (isConnected)
			voiceSndBgColor = isVoiceMuted ? new Color(0.85f, 0.2f, 0.2f, 0.9f) : new Color(0.1f, 0.7f, 0.8f, 0.9f);
		else
			voiceSndBgColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);

		GUI.color = voiceSndBgColor;
		GUI.DrawTexture(voiceSndRect, cachedCircleWhite);

		GUI.color = Color.white;
		DrawSpeakerIcon(voiceSndRect, !isConnected || isVoiceMuted);

		if (GUI.Button(voiceSndRect, GUIContent.none, GUIStyle.none))
		{
			if (isConnected)
			{
				_cachedVivoxRef.ToggleOutputMute();
				Interface_Game.addInfoPlayerNormal(_cachedVivoxRef.IsOutputMuted ? "Đã tắt loa đàm thoại" : "Đã bật loa đàm thoại", mFont.tahoma_7_yellow);
			}
			else
			{
				Interface_Game.addInfoPlayerNormal("Đang kết nối thoại vui lòng chờ 5 giây...", mFont.tahoma_7_yellow);
				_ = _cachedVivoxRef.JoinVoiceChannelAsync("GameRoom_1");
			}
		}

		curX = voiceSndRect.xMax + pad;

		// 3. Draw Mic Button
		Rect micRect = _pillRectCache[PILL_RECT_MIC];
		micRect.x = curX;
		micRect.y = curY;
		micRect.width = btnSize;
		micRect.height = btnSize;
		_pillRectCache[PILL_RECT_MIC] = micRect;
		bool isMuted = _cachedVivoxRef != null && _cachedVivoxRef.IsMuted;

		Color micBgColor;
		if (micClickFeedbackTick > 0)
			micBgColor = new Color(0.95f, 0.6f, 0.1f, 0.8f);
		else if (isConnected)
			micBgColor = isMuted ? new Color(0.85f, 0.2f, 0.2f, 0.9f) : new Color(0.1f, 0.75f, 0.3f, 0.9f);
		else
			micBgColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);

		GUI.color = micBgColor;
		GUI.DrawTexture(micRect, cachedCircleWhite);

		GUI.color = Color.white;
		DrawMicIcon(micRect, !isConnected || isMuted);

		if (GUI.Button(micRect, GUIContent.none, GUIStyle.none))
		{
			micClickFeedbackTick = 45;
			if (isConnected)
			{
				_cachedVivoxRef.ToggleMute();
				Interface_Game.addInfoPlayerNormal(_cachedVivoxRef.IsMuted ? "Đã tắt mic truyền giọng" : "Đã bật mic truyền giọng", mFont.tahoma_7_yellow);
			}
			else
			{
				Interface_Game.addInfoPlayerNormal("Đang khởi động mic vui lòng chờ 5 giây...", mFont.tahoma_7_yellow);
				_ = _cachedVivoxRef.JoinVoiceChannelAsync("GameRoom_1");
			}
		}

		curX = micRect.xMax + 10f * baseScale;

		// Vertical Separator
		GUI.color = new Color(1f, 1f, 1f, 0.15f);
		Rect sepRect = _pillRectCache[PILL_RECT_SEPARATOR];
		sepRect.x = curX;
		sepRect.y = startY + pad;
		sepRect.width = 1f;
		sepRect.height = pillHeight - pad * 2f;
		_pillRectCache[PILL_RECT_SEPARATOR] = sepRect;
		GUI.DrawTexture(sepRect, Texture2D.whiteTexture);
		GUI.color = Color.white;

		curX += 10f * baseScale;

		// Speed Decrease [-]
		Rect decRect = _pillRectCache[PILL_RECT_DEC];
		decRect.x = curX;
		decRect.y = curY;
		decRect.width = btnSize;
		decRect.height = btnSize;
		_pillRectCache[PILL_RECT_DEC] = decRect;

		bool isHoverDec = decRect.Contains(Event.current.mousePosition);
		GUI.color = isHoverDec ? new Color(1f, 1f, 1f, 0.25f) : new Color(1f, 1f, 1f, 0.12f);
		GUI.DrawTexture(decRect, cachedRoundedBoxWhite);

		GUI.color = Color.white;
		Rect decLabelRect = _pillRectCache[PILL_RECT_TMP1];
		decLabelRect.x = decRect.x;
		decLabelRect.y = decRect.y - 1f * baseScale;
		decLabelRect.width = decRect.width;
		decLabelRect.height = decRect.height;
		_pillRectCache[PILL_RECT_TMP1] = decLabelRect;
		GUI.Label(decLabelRect, "-", GetButtonStyle((int)(14f * baseScale)));

		if (GUI.Button(decRect, GUIContent.none, GUIStyle.none))
		{
			gameSpeed = Mathf.Max(0.1f, gameSpeed - 0.1f);
			Time.timeScale = gameSpeed;
			cachedSpeedValue = -1f;
			Interface_Game.addInfoPlayerNormal("Tốc độ game: " + System.Math.Round(gameSpeed, 1) + "x", mFont.tahoma_7_yellow);
		}

		curX = decRect.xMax + pad;

		// Speed Label
		float lblWidth = 55f * baseScale;
		Rect lblRect = _pillRectCache[PILL_RECT_LBL];
		lblRect.x = curX;
		lblRect.y = curY;
		lblRect.width = lblWidth;
		lblRect.height = btnSize;
		_pillRectCache[PILL_RECT_LBL] = lblRect;

		if (cachedSpeedValue != gameSpeed)
		{
			cachedSpeedValue = gameSpeed;
			string platformStr = GameMidlet.isPC ? "PC" : "Mobile";
			cachedSpeedLabel = System.Math.Round(gameSpeed, 1) + "x (" + platformStr + ")";
		}

		GUI.Label(lblRect, cachedSpeedLabel, GetCenteredStyle((int)(11f * baseScale)));

		curX = lblRect.xMax + pad;

		// Speed Increase [+]
		Rect incRect = _pillRectCache[PILL_RECT_INC];
		incRect.x = curX;
		incRect.y = curY;
		incRect.width = btnSize;
		incRect.height = btnSize;
		_pillRectCache[PILL_RECT_INC] = incRect;

		bool isHoverInc = incRect.Contains(Event.current.mousePosition);
		GUI.color = isHoverInc ? new Color(1f, 1f, 1f, 0.25f) : new Color(1f, 1f, 1f, 0.12f);
		GUI.DrawTexture(incRect, cachedRoundedBoxWhite);

		GUI.color = Color.white;
		Rect incLabelRect = _pillRectCache[PILL_RECT_TMP2];
		incLabelRect.x = incRect.x;
		incLabelRect.y = incRect.y - 1f * baseScale;
		incLabelRect.width = incRect.width;
		incLabelRect.height = incRect.height;
		_pillRectCache[PILL_RECT_TMP2] = incLabelRect;
		GUI.Label(incLabelRect, "+", GetButtonStyle((int)(13f * baseScale)));

		if (GUI.Button(incRect, GUIContent.none, GUIStyle.none))
		{
			gameSpeed = Mathf.Min(3.0f, gameSpeed + 0.1f);
			Time.timeScale = gameSpeed;
			cachedSpeedValue = -1f;
			Interface_Game.addInfoPlayerNormal("Tốc độ game: " + System.Math.Round(gameSpeed, 1) + "x", mFont.tahoma_7_yellow);
		}
	}

	private static void SetCachedColor(int slot, Color c)
	{
		_pillColorCache[slot] = c;
	}

	private static GUIStyle GetCenteredStyle(int fontSize)
	{
		if (cachedCenteredStyle == null)
		{
			cachedCenteredStyle = new GUIStyle(GUI.skin.label);
			cachedCenteredStyle.alignment = TextAnchor.MiddleCenter;
			cachedCenteredStyle.fontStyle = FontStyle.Bold;
			cachedCenteredStyle.normal.textColor = Color.white;
		}
		cachedCenteredStyle.fontSize = fontSize;
		return cachedCenteredStyle;
	}

	private static GUIStyle GetButtonStyle(int fontSize)
	{
		if (cachedButtonStyle == null)
		{
			cachedButtonStyle = new GUIStyle(GUI.skin.label);
			cachedButtonStyle.alignment = TextAnchor.MiddleCenter;
			cachedButtonStyle.fontStyle = FontStyle.Bold;
			cachedButtonStyle.normal.textColor = Color.white;
		}
		cachedButtonStyle.fontSize = fontSize;
		return cachedButtonStyle;
	}

	private static void DrawSpeakerIcon(Rect rect, bool drawSlash)
	{
		Color savedColor = GUI.color;
		GUI.color = Color.white;
		float u = rect.width / 52f;

		Rect r1 = _pillRectCache[PILL_RECT_TMP1];
		r1.x = rect.x + 14f * u; r1.y = rect.y + 18f * u; r1.width = 10f * u; r1.height = 16f * u;
		_pillRectCache[PILL_RECT_TMP1] = r1;
		GUI.DrawTexture(r1, Texture2D.whiteTexture);

		Rect r2 = _pillRectCache[PILL_RECT_TMP2];
		r2.x = rect.x + 24f * u; r2.y = rect.y + 14f * u; r2.width = 4f * u; r2.height = 24f * u;
		_pillRectCache[PILL_RECT_TMP2] = r2;
		GUI.DrawTexture(r2, Texture2D.whiteTexture);

		Rect r3 = _pillRectCache[PILL_RECT_TMP3];
		r3.x = rect.x + 28f * u; r3.y = rect.y + 11f * u; r3.width = 4f * u; r3.height = 30f * u;
		_pillRectCache[PILL_RECT_TMP3] = r3;
		GUI.DrawTexture(r3, Texture2D.whiteTexture);

		if (!drawSlash)
		{
			Rect r4 = _pillRectCache[PILL_RECT_TMP4];
			r4.x = rect.x + 35f * u; r4.y = rect.y + 20f * u; r4.width = 3f * u; r4.height = 12f * u;
			_pillRectCache[PILL_RECT_TMP4] = r4;
			GUI.DrawTexture(r4, Texture2D.whiteTexture);

			// Tái sử dụng TMP1 slot cho wave 2
			Rect r5 = _pillRectCache[PILL_RECT_TMP1];
			r5.x = rect.x + 40f * u; r5.y = rect.y + 16f * u; r5.width = 3f * u; r5.height = 20f * u;
			_pillRectCache[PILL_RECT_TMP1] = r5;
			GUI.DrawTexture(r5, Texture2D.whiteTexture);
		}

		if (drawSlash)
		{
			_scratchMatrix = GUI.matrix;
			GUIUtility.RotateAroundPivot(-36f, rect.center);
			Rect rs = _pillRectCache[PILL_RECT_TMP4];
			rs.x = rect.x + 10f * u; rs.y = rect.y + 24f * u; rs.width = 32f * u; rs.height = 4f * u;
			_pillRectCache[PILL_RECT_TMP4] = rs;
			GUI.DrawTexture(rs, Texture2D.whiteTexture);
			GUI.matrix = _scratchMatrix;
		}
		GUI.color = savedColor;
	}

	private static void DrawMicIcon(Rect micRect, bool drawSlash)
	{
		Color savedColor = GUI.color;
		GUI.color = Color.white;
		float unit = micRect.width / 52f;

		Rect head = _pillRectCache[PILL_RECT_TMP1];
		head.x = micRect.x + 20f * unit; head.y = micRect.y + 12f * unit; head.width = 12f * unit; head.height = 18f * unit;
		_pillRectCache[PILL_RECT_TMP1] = head;

		Rect body = _pillRectCache[PILL_RECT_TMP2];
		body.x = head.x; body.y = head.y + 6f * unit; body.width = head.width; body.height = 13f * unit;
		_pillRectCache[PILL_RECT_TMP2] = body;

		GUI.DrawTexture(head, cachedCircleWhite);
		GUI.DrawTexture(body, Texture2D.whiteTexture);

		Rect r1 = _pillRectCache[PILL_RECT_TMP3];
		r1.x = micRect.x + 16f * unit; r1.y = micRect.y + 26f * unit; r1.width = 20f * unit; r1.height = 4f * unit;
		_pillRectCache[PILL_RECT_TMP3] = r1;
		GUI.DrawTexture(r1, Texture2D.whiteTexture);

		Rect r2 = _pillRectCache[PILL_RECT_TMP4];
		r2.x = micRect.x + 24f * unit; r2.y = micRect.y + 29f * unit; r2.width = 4f * unit; r2.height = 9f * unit;
		_pillRectCache[PILL_RECT_TMP4] = r2;
		GUI.DrawTexture(r2, Texture2D.whiteTexture);

		Rect r3 = _pillRectCache[PILL_RECT_TMP1];
		r3.x = micRect.x + 18f * unit; r3.y = micRect.y + 38f * unit; r3.width = 16f * unit; r3.height = 4f * unit;
		_pillRectCache[PILL_RECT_TMP1] = r3;
		GUI.DrawTexture(r3, Texture2D.whiteTexture);

		if (drawSlash)
		{
			_scratchMatrix = GUI.matrix;
			GUIUtility.RotateAroundPivot(-36f, micRect.center);
			Rect rs = _pillRectCache[PILL_RECT_TMP4];
			rs.x = micRect.x + 10f * unit; rs.y = micRect.y + 24f * unit; rs.width = 32f * unit; rs.height = 5f * unit;
			_pillRectCache[PILL_RECT_TMP4] = rs;
			GUI.DrawTexture(rs, Texture2D.whiteTexture);
			GUI.matrix = _scratchMatrix;
		}
		GUI.color = savedColor;
	}

	private void HandleVoiceChatInput()
	{
		try
		{
			if (Input.GetKeyDown(KeyCode.J))
			{
				if (_cachedVivoxRef == null) _cachedVivoxRef = VivoxManager.Instance;
				if (_cachedVivoxRef == null) return;
				if (string.IsNullOrEmpty(_cachedVivoxRef.CurrentChannelName))
					_ = _cachedVivoxRef.JoinVoiceChannelAsync("GameRoom_1");
				else
					_ = _cachedVivoxRef.LeaveChannelAsync();
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				if (_cachedVivoxRef != null)
				{
					_cachedVivoxRef.ToggleMute();
					Interface_Game.addInfoPlayerNormal(_cachedVivoxRef.IsMuted ? "Đã tắt mic truyền giọng" : "Đã bật mic truyền giọng", mFont.tahoma_7_yellow);
				}
			}
			if (Input.GetKeyDown(KeyCode.B))
			{
				if (_cachedVivoxRef != null)
				{
					_cachedVivoxRef.ToggleOutputMute();
					Interface_Game.addInfoPlayerNormal(_cachedVivoxRef.IsOutputMuted ? "Đã tắt loa đàm thoại" : "Đã bật loa đàm thoại", mFont.tahoma_7_yellow);
				}
			}
			if (Input.GetKeyDown(KeyCode.M))
			{
				mSound.isMusic = !mSound.isMusic;
				if (!mSound.isMusic)
				{
					mSound.stopAll();
				}
				else
				{
					mSound.idCurent = -1;
					LoadMapScreen.PlayMusicLang();
				}
				Interface_Game.addInfoPlayerNormal(mSound.isMusic ? "Đã bật nhạc nền game" : "Đã tắt nhạc nền game", mFont.tahoma_7_yellow);
				CRes.saveRMS("MAIN_SOUND", new sbyte[2]
				{
					(sbyte)(mSound.isMusic ? 1 : 0),
					(sbyte)(mSound.isSound ? 1 : 0)
				});
			}
			if (Input.GetKeyDown(KeyCode.N))
			{
				mSound.isSound = !mSound.isSound;
				Interface_Game.addInfoPlayerNormal(mSound.isSound ? "Đã bật âm thanh hiệu ứng" : "Đã tắt âm thanh hiệu ứng", mFont.tahoma_7_yellow);
				CRes.saveRMS("MAIN_SOUND", new sbyte[2]
				{
					(sbyte)(mSound.isMusic ? 1 : 0),
					(sbyte)(mSound.isSound ? 1 : 0)
				});
			}
		}
		catch (Exception) {}
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
			HandleVoiceChatInput();
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
			GameMidlet.gameCanvas.onPointerPressed(ScaleGUI.toGameX(mousePosition.x), ScaleGUI.toGameY(mousePosition.y));
			lastMousePos.x = ScaleGUI.toGameX(mousePosition.x);
			lastMousePos.y = ScaleGUI.toGameY(mousePosition.y);
		}
		if (Input.GetMouseButton(0))
		{
			Vector3 mousePosition2 = Input.mousePosition;
			GameMidlet.gameCanvas.onPointerDragged(ScaleGUI.toGameX(mousePosition2.x), ScaleGUI.toGameY(mousePosition2.y));
			lastMousePos.x = ScaleGUI.toGameX(mousePosition2.x);
			lastMousePos.y = ScaleGUI.toGameY(mousePosition2.y);
		}
		if (Input.GetMouseButtonUp(0) && valueKey == 1)
		{
			valueKey = 0;
			Vector3 mousePosition3 = Input.mousePosition;
			lastMousePos.x = ScaleGUI.toGameX(mousePosition3.x);
			lastMousePos.y = ScaleGUI.toGameY(mousePosition3.y);
			GameMidlet.gameCanvas.onPointerReleased(ScaleGUI.toGameX(mousePosition3.x), ScaleGUI.toGameY(mousePosition3.y));
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
		Image.clearCache();
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
#if UNITY_IOS || UNITY_ANDROID
			Application.targetFrameRate = 15;
			Resources.UnloadUnusedAssets();
#endif
			isQuitApp = false;
			// Khi background: tắt mic input + voice output để tiết kiệm pin & tránh leak native
			if (_cachedVivoxRef != null && _cachedVivoxRef.IsLoggedIn)
			{
				try { Unity.Services.Vivox.VivoxService.Instance.MuteInputDevice(); } catch (Exception) { }
				try { Unity.Services.Vivox.VivoxService.Instance.MuteOutputDevice(); } catch (Exception) { }
			}
		}
		else
		{
			isResume = true;
			Time.timeScale = gameSpeed;
#if UNITY_IOS || UNITY_ANDROID
			Application.targetFrameRate = 60;
#endif
			// Resume: khôi phục theo đúng trạng thái mute trước đó
			if (_cachedVivoxRef != null && _cachedVivoxRef.IsLoggedIn)
			{
				if (!_cachedVivoxRef.IsMuted)
				{
					try { Unity.Services.Vivox.VivoxService.Instance.UnmuteInputDevice(); } catch (Exception) { }
				}
				if (!_cachedVivoxRef.IsOutputMuted)
				{
					try { Unity.Services.Vivox.VivoxService.Instance.UnmuteOutputDevice(); } catch (Exception) { }
				}
			}
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









