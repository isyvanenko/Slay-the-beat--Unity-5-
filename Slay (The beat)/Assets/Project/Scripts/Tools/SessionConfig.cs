using UnityEngine;
using UnityEngine.InputSystem;

public static class SessionConfig
{
    // --- ARCADE STAGE SETTINGS ---
    public static int CurrentStage = 1;
    public static int MaxStages = 2; // Define how many songs a player gets per playthrough

    public static int PlayerCount = 1;
    public static InputDevice Player1Device;
    public static InputDevice Player2Device;
    public static string P1DeviceType;
    public static string P2DeviceType;

    public static void SetPlayerDevice(int playerIndex, InputDevice device, string type)
    {
        if (playerIndex == 0) { Player1Device = device; P1DeviceType = type; }
        else { Player2Device = device; P2DeviceType = type; }
    }

    public static bool IsDeviceUsed(InputDevice device)
    {
        return device == Player1Device || device == Player2Device;
    }

    // --- NEW: Reset for a new player ---
    // Call this when you load your Main Menu / Insert Coin screen
    public static void ResetSession()
    {
        CurrentStage = 1;
        PlayerCount = 1;
        Player1Device = null;
        Player2Device = null;
        P1DeviceType = "";
        P2DeviceType = "";
    }
}