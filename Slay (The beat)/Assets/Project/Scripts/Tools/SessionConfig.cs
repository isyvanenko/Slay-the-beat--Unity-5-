using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Static configuration class that holds session-wide settings, player device assignments, and custom input bindings.
/// </summary>
/// <remarks>
/// This class serves as the central data store for the current gameplay session, including:
/// - Arcade stage progression (current stage and maximum stages)
/// - Number of active players (1 or 2)
/// - Player device assignments (InputDevice references)
/// - Device type strings (e.g., "Gamepad", "Keyboard")
/// - Custom binding override JSON strings for each player
/// 
/// Key Features:
/// - Tripwire property for Player1Device that logs a stack trace when set to null (debugging aid)
/// - Device usage checking to prevent assigning the same device to both players
/// - Reset method to clear all session data
/// 
/// Important Notes:
/// - All fields are static, so data persists across scene loads until explicitly cleared
/// - Custom binding strings store InputAction binding overrides in JSON format
/// - The tripwire helps track down scripts that unexpectedly nullify Player1Device
/// </remarks>
public static class SessionConfig
{
    // --- ARCADE STAGE SETTINGS ---
    /// <summary>Current stage number in arcade mode (1-based index).</summary>
    public static int CurrentStage = 1;
    
    /// <summary>Total number of stages in the current arcade session.</summary>
    /// <remarks>Default is 2 stages. Used to determine when to show final results.</remarks>
    public static int MaxStages = 2; 

    /// <summary>Number of players in the session (1 or 2).</summary>
    public static int PlayerCount = 1;

    // ==========================================
    // CUSTOM BINDINGS MEMORY
    // ==========================================
    /// <summary>
    /// JSON string containing Player 1's input binding overrides.
    /// </summary>
    /// <remarks>
    /// Stores the result of InputActionMap.SaveBindingOverridesAsJson().
    /// Applied when Player 1's input is initialized in gameplay.
    /// Empty string means no custom bindings (use defaults).
    /// </remarks>
    public static string P1Bindings;
    
    /// <summary>
    /// JSON string containing Player 2's input binding overrides.
    /// </summary>
    public static string P2Bindings;

    // ==========================================
    // THE TRIPWIRE FOR PLAYER 1
    // ==========================================
    private static InputDevice _player1Device;
    
    /// <summary>
    /// InputDevice assigned to Player 1.
    /// </summary>
    /// <remarks>
    /// This property includes a tripwire that logs an error with a stack trace
    /// if any script attempts to set it to null when it wasn't already null.
    /// This helps track down unintended resets or device unpairing.
    /// 
    /// To clear the device intentionally (e.g., during full game reset), assign null directly
    /// which will still trigger the log. Use ResetSession() for a clean wipe without log noise.
    /// </remarks>
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

    /// <summary>
    /// InputDevice assigned to Player 2 (no tripwire).
    /// </summary>
    public static InputDevice Player2Device;
    
    /// <summary>String identifier for Player 1's device type (e.g., "Gamepad", "Keyboard", "DanceMat").</summary>
    public static string P1DeviceType;
    
    /// <summary>String identifier for Player 2's device type.</summary>
    public static string P2DeviceType;

    /// <summary>
    /// Assigns a device and its type to the specified player.
    /// </summary>
    /// <param name="playerIndex">0 for Player 1, 1 for Player 2.</param>
    /// <param name="device">The InputDevice to assign.</param>
    /// <param name="type">Human-readable device type string.</param>
    public static void SetPlayerDevice(int playerIndex, InputDevice device, string type)
    {
        if (playerIndex == 0) { Player1Device = device; P1DeviceType = type; }
        else { Player2Device = device; P2DeviceType = type; }
    }

    /// <summary>
    /// Checks whether a given InputDevice is already assigned to either player.
    /// </summary>
    /// <param name="device">The device to check.</param>
    /// <returns>True if the device is assigned to Player 1 or Player 2.</returns>
    public static bool IsDeviceUsed(InputDevice device)
    {
        return device == Player1Device || device == Player2Device;
    }

    /// <summary>
    /// Resets all session configuration data to default values.
    /// </summary>
    /// <remarks>
    /// Resets:
    /// - CurrentStage to 1
    /// - PlayerCount to 1
    /// - Player1Device and Player2Device to null
    /// - Device type strings to empty
    /// - Binding override JSON strings to empty
    /// 
    /// Note: The tripwire will trigger when Player1Device is set to null here,
    /// but this is intentional and can be ignored during reset.
    /// </remarks>
    public static void ResetSession()
    {
        CurrentStage = 1;
        PlayerCount = 1;
        Player1Device = null; // NOTE: The tripwire will catch if this is accidentally called!
        Player2Device = null;
        P1DeviceType = "";
        P2DeviceType = "";
        
        // Clear the bindings when the game restarts!
        P1Bindings = "";
        P2Bindings = "";
    }
}