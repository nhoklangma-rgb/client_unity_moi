using System.Threading;
using UnityEngine;

public class mSound
{
	private const int INTERVAL = 5;

	private const int MAXTIME = 100;

	public static int status;

	public static int postem;

	public static int timestart;

	private static string filenametemp;

	private static float volumetem;

	public static bool isSound = true;

	public static bool isMusic = true;

	public static bool isNotPlay = false;

	public static AudioSource SoundWater;

	public static AudioSource SoundRun;

	public static AudioSource SoundBGLoop;

	public static float volumeSound = 0.7f;

	public static float volumeMusic = 0.8f;

	public static AudioClip[] music;

	public static GameObject[] player;

	// Cached AudioSource references for player[] GameObjects
	private static AudioSource[] _playerSources;

	// Cached Main Camera reference
	private static GameObject _mainCamera;

	public static int l1;

	public static int idCurent = -1;

	// Event for cross-thread signaling instead of Thread.Sleep polling
	private static ManualResetEventSlim _statusEvent = new ManualResetEventSlim(false);

	public static void stopAll()
	{
		stopAllz();
	}

	public static bool isPlaying()
	{
		return false;
	}

	public static void init()
	{
		GameObject gameObject = new GameObject();
		gameObject.name = "Audio Player";
		gameObject.transform.position = Vector3.zero;
		gameObject.AddComponent<AudioListener>();
		SoundBGLoop = gameObject.AddComponent<AudioSource>();
		_mainCamera = GameObject.Find("Main Camera");
	}

	public static void init(int musicID, int sID)
	{
		if (player == null && music == null)
		{
			init();
			l1 = musicID;
			player = new GameObject[musicID + sID];
			music = new AudioClip[musicID + sID];
			_playerSources = new AudioSource[musicID + sID];
			for (int i = 0; i < player.Length; i++)
			{
				getAssetSoundFile((i < l1) ? ("/sound/m" + i) : ("/sound/s" + (i - l1)), i);
			}
		}
	}

	public static void playSound(int id, float volume)
	{
		if (isSound && id >= 0 && id <= music.Length - l1 - 1)
		{
			play(id + l1, volume);
		}
	}

	public static void playSound1(int id, float volume)
	{
		play(id, volume);
	}

	public static void getAssetSoundFile(string fileName, int pos)
	{
		stop(pos);
		load(Main.res + fileName, pos);
	}

	public static void stopSoundAll()
	{
		for (int i = 0; i < music.Length; i++)
		{
			stop(i);
		}
	}

	public static void stopAllz()
	{
		for (int i = 0; i < music.Length; i++)
		{
			stop(i);
		}
		for (int j = 0; j < l1; j++)
		{
			sTopSoundBG(j);
		}
	}

	public static void stopAllBg()
	{
		for (int i = 0; i < music.Length; i++)
		{
			stop(i);
		}
		sTopSoundBG(0);
		sTopSoundRun();
		stopSoundNatural(0);
	}

	public static void update()
	{
	}

	public static void stopMusic(int x)
	{
		stop(x);
	}

	public static void play(int id, float volume)
	{
		start(volume, id);
	}

	public static void playSoundRun(int id, float volume)
	{
		if (!(SoundRun == null))
		{
			SoundRun.loop = true;
			SoundRun.clip = music[id];
			SoundRun.volume = volume;
			SoundRun.Play();
		}
	}

	public static void sTopSoundRun()
	{
		SoundRun.Stop();
	}

	public static bool isPlayingSound()
	{
		if (SoundRun == null)
		{
			return false;
		}
		return SoundRun.isPlaying;
	}

	public static void playSoundNatural(int id, float volume, bool isLoop)
	{
		if (!(SoundBGLoop == null))
		{
			SoundWater.loop = isLoop;
			SoundWater.clip = music[id];
			SoundWater.volume = volume;
			SoundWater.Play();
		}
	}

	public static void stopSoundNatural(int id)
	{
		SoundWater.Stop();
	}

	public static bool isPlayingSoundatural(int id)
	{
		if (SoundWater == null)
		{
			return false;
		}
		return SoundWater.isPlaying;
	}

	public static void playMus(int type, float vl, bool loop)
	{
		if (isMusic && type >= 0 && type <= l1 - 1)
		{
			playSoundBGLoop(type, vl);
		}
	}

	public static void playSoundBGLoop(int id, float volume)
	{
		if (!(SoundBGLoop == null) && id != idCurent)
		{
			SoundBGLoop.loop = true;
			SoundBGLoop.clip = music[id];
			SoundBGLoop.volume = volume;
			SoundBGLoop.Play();
			idCurent = id;
		}
	}

	public static void sTopSoundBG(int id)
	{
		SoundBGLoop.Stop();
	}

	public static bool isPlayingSoundBG(int id)
	{
		if (SoundBGLoop == null)
		{
			return false;
		}
		return SoundBGLoop.isPlaying;
	}

	public static void load(string filename, int pos)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			__load(filename, pos);
		}
		else
		{
			_load(filename, pos);
		}
	}

	private static void _load(string filename, int pos)
	{
		if (status != 0)
		{
			Cout.LogError("CANNOT LOAD AUDIO " + filename + " WHEN LOADING " + filenametemp);
			return;
		}
		filenametemp = filename;
		postem = pos;
		_statusEvent.Reset();
		status = 2;
		if (!_statusEvent.Wait(500))
		{
			if (status != 0)
			{
				Cout.LogError("TOO LONG FOR LOAD AUDIO " + filename);
				return;
			}
		}
		Cout.Log("Load Audio " + filename + " done");
	}

	private static void __load(string filename, int pos)
	{
		music[pos] = (AudioClip)Resources.Load(filename, typeof(AudioClip));
		if (_mainCamera == null)
			_mainCamera = GameObject.Find("Main Camera");
		_mainCamera.AddComponent<AudioSource>();
		player[pos] = _mainCamera;
		// Cache the AudioSource for this player slot
		_playerSources[pos] = _mainCamera.GetComponent<AudioSource>();
	}

	/// <summary>
	/// Called from the main thread update loop to signal status completion.
	/// </summary>
	public static void signalStatusComplete()
	{
		_statusEvent.Set();
	}

	public static void start(float volume, int pos)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			__start(volume, pos);
		}
		else
		{
			_start(volume, pos);
		}
	}

	public static void _start(float volume, int pos)
	{
		if (status != 0)
		{
			// Debug.Log("CANNOT START AUDIO WHEN STARTING");
			return;
		}
		volumetem = volume;
		postem = pos;
		_statusEvent.Reset();
		status = 3;
		if (!_statusEvent.Wait(500))
		{
			if (status != 0)
			{
				// Debug.Log("TOO LONG FOR START AUDIO");
			}
		}
	}

	public static void __start(float volume, int pos)
	{
		if (!(player[pos] == null))
		{
			AudioSource src = _playerSources != null && pos < _playerSources.Length && _playerSources[pos] != null
				? _playerSources[pos]
				: player[pos].GetComponent<AudioSource>();
			src.PlayOneShot(music[pos], volume);
		}
	}

	public static void stop(int pos)
	{
		if (Thread.CurrentThread.Name == Main.mainThreadName)
		{
			__stop(pos);
		}
		else
		{
			_stop(pos);
		}
	}

	public static void _stop(int pos)
	{
		if (status != 0)
		{
			// Debug.Log("CANNOT STOP AUDIO WHEN STOPPING");
			return;
		}
		postem = pos;
		_statusEvent.Reset();
		status = 4;
		if (!_statusEvent.Wait(500))
		{
			if (status != 0)
			{
				// Debug.Log("TOO LONG FOR STOP AUDIO");
			}
		}
	}

	public static void __stop(int pos)
	{
		if (player[pos] != null)
		{
			AudioSource src = _playerSources != null && pos < _playerSources.Length && _playerSources[pos] != null
				? _playerSources[pos]
				: player[pos].GetComponent<AudioSource>();
			src.Stop();
		}
	}
}
