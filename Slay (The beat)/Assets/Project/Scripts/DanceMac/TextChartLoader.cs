using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parses text-based chart files into lists of NoteEvent objects for gameplay.
/// </summary>
/// <remarks>
/// This component bridges the gap between plain text chart files and the game's runtime
/// note representation. It reads chart data from SongGradeData assets based on the
/// selected difficulty and converts formatted text lines into NoteEvent structures.
/// 
/// Chart File Format:
/// - Tab or space-separated values
/// - Column 1: Time in seconds (float)
/// - Column 2: Direction and optional hold length (comma-separated)
/// 
/// Example lines:
/// - Tap note:    "1.23    left"
/// - Hold note:   "2.45    up,2.5"    (2.5 second hold duration)
/// - Alternative: "3.67    right"
/// 
/// The parser supports both tab and space delimiters for flexibility with
/// different text editor configurations.
/// </remarks>
public class TextChartLoader : MonoBehaviour
{
    /// <summary>
    /// Loads and parses the chart file for the currently selected song and difficulty.
    /// </summary>
    /// <returns>
    /// List of NoteEvent objects containing all notes from the chart, sorted by time.
    /// Returns an empty list if loading fails (no data, missing file, or parsing errors).
    /// </returns>
    /// <remarks>
    /// Loading process:
    /// 1. Retrieve selected song data from GameDataBridge
    /// 2. Select the appropriate chart file based on difficulty index
    /// 3. Read and parse each line of the chart file
    /// 4. Convert direction strings to lane indices (0-3)
    /// 5. Parse optional hold durations for long notes
    /// 6. Sort all notes chronologically
    /// 
    /// Error handling:
    /// - Logs error if no song data is present (likely launched outside normal flow)
    /// - Logs error if chart file is missing for the selected difficulty
    /// - Skips malformed lines without crashing
    /// - Returns empty list on failure to prevent null reference exceptions
    /// </remarks>
    public List<NoteEvent> LoadChart()
    {
        List<NoteEvent> newChart = new List<NoteEvent>();

        // 1. Grab data from the Bridge
        SongGradeData data = GameDataBridge.SelectedSong;
        int diff = GameDataBridge.SelectedDifficulty;

        if (data == null)
        {
            Debug.LogError("<color=red>CHART ERROR:</color> No Song Data in Bridge! Did you start from the Menu?");
            return newChart;
        }

        // 2. Select the correct file based on difficulty
        TextAsset chartFile = null;
        string diffName = "";

        switch (diff)
        {
            case 0: chartFile = data.easyChart; diffName = "EASY"; break;
            case 1: chartFile = data.mediumChart; diffName = "MEDIUM"; break;
            case 2: chartFile = data.hardChart; diffName = "HARD"; break;
        }

        if (chartFile == null)
        {
            Debug.LogError($"<color=red>CHART ERROR:</color> The {diffName} .txt file is missing in SongGradeData!");
            return newChart;
        }

        Debug.Log($"<color=cyan>CHART LOADER:</color> Parsing {data.songName} [{diffName}] - File: {chartFile.name}");

        // 3. FULL PARSING LOGIC (Your Key Logic Restored)
        string[] lines = chartFile.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Split by TAB, fallback to Space
            string[] parts = line.Split('\t');
            if (parts.Length < 2) parts = line.Split(' ');
            if (parts.Length < 2) continue;

            // Parse Time
            if (!float.TryParse(parts[0].Trim(), out float time)) continue;

            // Parse Data (Direction + Hold)
            string dataPart = parts[1].Trim();
            string[] dataSplit = dataPart.Split(',');

            string directionStr = dataSplit[0].Trim();
            float holdDuration = 0f;

            // Check for hold duration
            if (dataSplit.Length > 1)
            {
                float.TryParse(dataSplit[1], out holdDuration);
            }

            // Convert String to Index
            int laneIndex = GetLaneIndex(directionStr);

            if (laneIndex != -1)
            {
                newChart.Add(new NoteEvent
                {
                    time = time,
                    laneIndex = laneIndex,
                    holdLength = holdDuration
                });
            }
        }

        // Sort just in case
        newChart.Sort((a, b) => a.time.CompareTo(b.time));

        Debug.Log($"<color=green>CHART SUCCESS:</color> Generated {newChart.Count} notes.");
        return newChart;
    }

    /// <summary>
    /// Converts a direction string to its corresponding lane index.
    /// </summary>
    /// <param name="dir">Direction string (case-insensitive). Valid values: "left", "down", "up", "right".</param>
    /// <returns>
    /// Lane index mapping:
    /// - "left" → 0
    /// - "down" → 1
    /// - "up" → 2
    /// - "right" → 3
    /// - Any other value → -1 (invalid)
    /// </returns>
    /// <remarks>
    /// The method uses ToLower() to ensure case-insensitive matching, making the chart
    /// files more forgiving (e.g., "LEFT", "Left", or "left" all work correctly).
    /// Returns -1 for invalid directions, which causes the parser to skip that note.
    /// </remarks>
    private int GetLaneIndex(string dir)
    {
        switch (dir.ToLower())
        {
            case "left": return 0;
            case "down": return 1;
            case "up": return 2;
            case "right": return 3;
            default: return -1;
        }
    }
}