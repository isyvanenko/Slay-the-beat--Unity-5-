using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextChartLoader : MonoBehaviour
{
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