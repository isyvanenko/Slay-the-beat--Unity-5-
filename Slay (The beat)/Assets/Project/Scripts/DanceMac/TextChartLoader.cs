using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextChartLoader : MonoBehaviour
{
    public TextAsset chartFile; // Drag your .txt file here

    public List<NoteEvent> LoadChart()
    {
        List<NoteEvent> newChart = new List<NoteEvent>();

        if (chartFile == null)
        {
            Debug.LogError("No Chart File assigned!");
            return newChart;
        }

        // Split the file into lines
        string[] lines = chartFile.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 1. Split by TAB first
            // Format: "16.125 [TAB] Right,3.188"
            string[] parts = line.Split('\t');
            
            // If tab split fails, try space (just in case)
            if (parts.Length < 2) parts = line.Split(' ');
            if (parts.Length < 2) continue;

            // 2. Parse Time
            if (!float.TryParse(parts[0].Trim(), out float time)) continue;

            // 3. Parse Data (Direction + Hold)
            // Example: "Right,3.188" OR just "Left"
            string dataPart = parts[1].Trim();
            string[] dataSplit = dataPart.Split(',');

            string directionStr = dataSplit[0].Trim();
            float holdDuration = 0f;

            // Check if there is a hold duration (the part after the comma)
            if (dataSplit.Length > 1)
            {
                float.TryParse(dataSplit[1], out holdDuration);
            }

            // 4. Convert "Left/Right/Up/Down" to ID (0-3)
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

        // Sort just in case the file lines are out of order
        newChart.Sort((a, b) => a.time.CompareTo(b.time));

        Debug.Log($"Loaded {newChart.Count} notes from Text format.");
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