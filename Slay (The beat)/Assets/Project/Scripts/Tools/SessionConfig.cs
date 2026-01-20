using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public static class SessionConfig
{
    public static int PlayerCount = 1;

    // Store the actual hardware device for each player
    public static InputDevice Player1Device;
    public static InputDevice Player2Device;

    // Optional: Store if they are using a Mat or Controller
    public static string P1DeviceType;
    public static string P2DeviceType;

    public static void SetPlayerDevice(int playerIndex, InputDevice device, string type)
    {
        if (playerIndex == 0)
        {
            Player1Device = device;
            P1DeviceType = type;
        }
        else
        {
            Player2Device = device;
            P2DeviceType = type;
        }
    }

    // Helper to check if a device is already taken
    public static bool IsDeviceUsed(InputDevice device)
    {
        return device == Player1Device || device == Player2Device;
    }
}