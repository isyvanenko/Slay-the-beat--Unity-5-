using UnityEngine;
using UnityEngine.InputSystem;

public static class SessionConfig
{
    // --- ARCADE STAGE SETTINGS ---
    public static int CurrentStage = 1;
    public static int MaxStages = 2; 

    public static int PlayerCount = 1;

    // ==========================================
    // THE TRIPWIRE FOR PLAYER 1
    // ==========================================
    private static InputDevice _player1Device;
    public static InputDevice Player1Device
    {
        get { return _player1Device; }
        set 
        { 
            // If someone tries to set it to NULL, and it wasn't already NULL... ALARM!
            if (value == null && _player1Device != null)
            {
                Debug.LogError("<color=red>🚨 CAUGHT THE ASSASSIN! Player 1 Device was just set to NULL by this script:</color>\n" + System.Environment.StackTrace);
            }
            _player1Device = value; 
        }
    }

    // Player 2 can stay normal
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
    public static void ResetSession()
    {
        CurrentStage = 1;
        PlayerCount = 1;
        Player1Device = null; // NOTE: The tripwire will catch if this is accidentally called!
        Player2Device = null;
        P1DeviceType = "";
        P2DeviceType = "";
    }
}