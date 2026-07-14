using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VivoxManager : MonoBehaviour
{
    private static VivoxManager _instance;
    private static readonly object _instanceLock = new object();

    public static VivoxManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            if (!Application.isPlaying) return null;
            lock (_instanceLock)
            {
                if (_instance != null) return _instance;
                VivoxManager existing = FindObjectOfType<VivoxManager>();
                if (existing != null)
                {
                    _instance = existing;
                    if (_instance.gameObject != null)
                        DontDestroyOnLoad(_instance.gameObject);
                }
                else
                {
                    GameObject go = new GameObject("VivoxManager");
                    _instance = go.AddComponent<VivoxManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
    }

    private bool _isInitialized = false;
    private bool _isLoggedIn = false;
    private bool _isMuted = false;
    private bool _isOutputMuted = false;
    private bool _isJoining = false;
    private bool _isLoggingIn = false;
    private bool _isLeaving = false;
    private bool _isRequestingMicrophonePermission = false;
    private string _currentChannelName = null;
    private string _currentUsername = null;

    public bool IsInitialized => _isInitialized;
    public bool IsLoggedIn => _isLoggedIn;
    public bool IsMuted => _isMuted;
    public bool IsOutputMuted => _isOutputMuted;
    public string CurrentChannelName => _currentChannelName;
    public string CurrentUsername => _currentUsername;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public async Task InitAsync()
    {
        if (_isInitialized) return;

        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await VivoxService.Instance.InitializeAsync();
            _isInitialized = true;
        }
        catch (Exception)
        {
            _isInitialized = false;
        }
    }

    public async Task LoginAsync(string username)
    {
        if (_isLoggedIn) return;
        if (_isLoggingIn) return;

        await InitAsync();
        if (!_isInitialized) return;

        try
        {
            _isLoggingIn = true;
            string authPlayerId = AuthenticationService.Instance.PlayerId;
            string displayName = string.IsNullOrEmpty(username) ? ("Player_" + authPlayerId.Substring(0, 8)) : username;
            var loginOptions = new LoginOptions { DisplayName = displayName };
            await VivoxService.Instance.LoginAsync(loginOptions);
            _isLoggedIn = true;
            _currentUsername = displayName;
        }
        catch (Exception)
        {
            _isLoggedIn = false;
            _currentUsername = null;
        }
        finally
        {
            _isLoggingIn = false;
        }
    }

    public async Task JoinVoiceChannelAsync(string channelName)
    {
        if (!string.IsNullOrEmpty(_currentChannelName)) return;
        if (_isJoining) return;

        try
        {
            _isJoining = true;
            if (!EnsureMicrophonePermission(channelName))
                return;

            if (!_isLoggedIn)
            {
                string guestName = "Guest_" + UnityEngine.Random.Range(1000, 9999);
                await LoginAsync(guestName);
            }

            if (!_isLoggedIn) return;

            string cleanChan = CleanString(channelName);
            await VivoxService.Instance.JoinGroupChannelAsync(cleanChan, ChatCapability.AudioOnly);
            _currentChannelName = cleanChan;

            if (_isMuted)
                VivoxService.Instance.MuteInputDevice();
            else
            {
                VivoxService.Instance.UnmuteInputDevice();
                try { VivoxService.Instance.UnmuteOutputDevice(); } catch (Exception) { }
            }
        }
        catch (Exception)
        {
            _currentChannelName = null;
        }
        finally
        {
            _isJoining = false;
        }
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        if (!_isLoggedIn) return;
        if (_isMuted)
            VivoxService.Instance.MuteInputDevice();
        else
            VivoxService.Instance.UnmuteInputDevice();
    }

    public void ToggleOutputMute()
    {
        _isOutputMuted = !_isOutputMuted;
        if (!_isLoggedIn) return;
        if (_isOutputMuted)
            VivoxService.Instance.MuteOutputDevice();
        else
            VivoxService.Instance.UnmuteOutputDevice();
    }

    public async Task LeaveChannelAsync()
    {
        if (string.IsNullOrEmpty(_currentChannelName)) return;
        if (_isLeaving) return;

        string channelToLeave = _currentChannelName;
        try
        {
            _isLeaving = true;
            _currentChannelName = null;
            await VivoxService.Instance.LeaveChannelAsync(channelToLeave);
        }
        catch (Exception)
        {
        }
        finally
        {
            _isLeaving = false;
        }
    }

    public async Task LogoutAsync()
    {
        if (!_isLoggedIn) return;
        try
        {
            await LeaveChannelAsync();
            await VivoxService.Instance.LogoutAsync();
        }
        catch (Exception)
        {
        }
        finally
        {
            _isLoggedIn = false;
            _currentUsername = null;
        }
    }

    public bool HasMicrophonePermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return true;
#endif
    }

    public void RequestMicrophonePermission()
    {
#if UNITY_ANDROID
        if (!HasMicrophonePermission())
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

    private bool EnsureMicrophonePermission(string channelNameAfterGrant)
    {
#if UNITY_ANDROID
        if (HasMicrophonePermission()) return true;
        if (_isRequestingMicrophonePermission) return false;

        _isRequestingMicrophonePermission = true;
        PermissionCallbacks callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permissionName =>
        {
            _isRequestingMicrophonePermission = false;
            if (permissionName == Permission.Microphone)
                _ = JoinVoiceChannelAsync(channelNameAfterGrant);
        };
        callbacks.PermissionDenied += permissionName =>
        {
            _isRequestingMicrophonePermission = false;
        };
        callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
        {
            _isRequestingMicrophonePermission = false;
        };
        Permission.RequestUserPermission(Permission.Microphone, callbacks);
        return false;
#else
        return true;
#endif
    }

    private string CleanString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "default";
        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == '@' || c == '=' || c == '+')
                sb.Append(c);
        }
        string clean = sb.ToString();
        return clean.Length > 0 ? clean : "default";
    }

    private void OnApplicationQuit()
    {
        if (!_isLoggedIn) return;
        _ = LogoutWithTimeoutAsync(2000);
    }

    private async Task LogoutWithTimeoutAsync(int timeoutMs)
    {
        try
        {
            var logoutTask = LogoutAsync();
            var timeoutTask = Task.Delay(timeoutMs);
            await Task.WhenAny(logoutTask, timeoutTask);
        }
        catch (Exception)
        {
        }
    }
}