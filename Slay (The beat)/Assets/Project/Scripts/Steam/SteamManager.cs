#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Starts the Steam API when the game launches, keeps its callbacks running, and shuts it down on quit.
/// Creates itself before the first scene loads, so it doesn't need to be placed in any scene.
/// </summary>
public class SteamManager : MonoBehaviour
{
    /// <summary>The game's Steam App ID from the Steamworks partner site.</summary>
    public const uint AppId = 4665540;

    /// <summary>The single running instance.</summary>
    private static SteamManager instance;

    /// <summary>Whether the Steam API started successfully. Steam calls are only safe when this is true.</summary>
    public static bool Initialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("[SteamManager]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SteamManager>();
    }

#if !DISABLESTEAMWORKS
    private void Awake()
    {
        if (!Packsize.Test())
            Debug.LogError("[Steamworks.NET] Packsize test failed: the wrong Steamworks.NET build is in use for this platform.");
        if (!DllCheck.Test())
            Debug.LogError("[Steamworks.NET] DllCheck test failed: one or more Steamworks binaries are the wrong version.");

        try
        {
            // If the game was launched outside Steam, relaunch it through Steam and close this copy.
            // Skipped while steam_appid.txt is next to the executable (always the case in the Editor).
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
            {
                Application.Quit();
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steamworks.NET] Couldn't load steam_api: " + e);
            Application.Quit();
            return;
        }

        Initialized = SteamAPI.Init();
        if (!Initialized)
        {
            Debug.LogError("[Steamworks.NET] SteamAPI.Init() failed. Is the Steam client running and logged in?");
            return;
        }

        Debug.Log("[Steamworks.NET] Logged in as " + SteamFriends.GetPersonaName());
    }

    private void Update()
    {
        if (Initialized) SteamAPI.RunCallbacks();
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        if (!Initialized) return;
        SteamAPI.Shutdown();
        Initialized = false;
    }

    /// <summary>
    /// Unlocks an achievement by its API name (as set up on the Steamworks site) and uploads it right away.
    /// </summary>
    public static void UnlockAchievement(string apiName)
    {
        if (!Initialized) return;
        SteamUserStats.SetAchievement(apiName);
        SteamUserStats.StoreStats();
    }

    /// <summary>The player's Steam display name, or an empty string when Steam isn't running.</summary>
    public static string PlayerName => Initialized ? SteamFriends.GetPersonaName() : string.Empty;
#else
    public static void UnlockAchievement(string apiName) { }
    public static string PlayerName => string.Empty;
#endif
}
