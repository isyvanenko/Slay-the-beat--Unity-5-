using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks a Tug of Slay match across scenes: first to three round wins, with a new song every round.
/// </summary>
/// <remarks>
/// Round 1 is Easy, round 2 is Medium, and every round after that is Hard.
/// </remarks>
public static class TugOfSlayMatch
{
    public const int WinsToTakeMatch = 3;

    /// <summary>Scene shown before each round. Must be in Build Settings.</summary>
    public const string RoundIntroScene = "TugOfSlayRoundIntro";

    private const int HardDifficulty = 2;

    private static readonly string[] DifficultyNames = { "EASY", "MEDIUM", "HARD" };

    private static List<SongGradeData> songs;
    private static readonly HashSet<string> playedSongs = new HashSet<string>();

    public static bool IsActive { get; private set; }
    public static int Round { get; private set; }
    public static int P1Wins { get; private set; }
    public static int P2Wins { get; private set; }

    public static bool IsOver => P1Wins >= WinsToTakeMatch || P2Wins >= WinsToTakeMatch;

    /// <summary>The player with the most round wins: 1, 2, or 0 if level.</summary>
    public static int Leader => P1Wins > P2Wins ? 1 : P2Wins > P1Wins ? 2 : 0;

    /// <summary>Difficulty index of the current round.</summary>
    public static int RoundDifficulty => Mathf.Min(Round - 1, HardDifficulty);

    /// <summary>Display name of the current round's difficulty.</summary>
    public static string RoundDifficultyName => DifficultyNames[RoundDifficulty];

    /// <summary>True when both players are one win away, so this round decides the match.</summary>
    public static bool IsFinalRound => P1Wins == WinsToTakeMatch - 1 && P2Wins == WinsToTakeMatch - 1;

    /// <summary>
    /// Starts a new match and picks the first song.
    /// </summary>
    /// <param name="songPool">Songs the match can pick from.</param>
    /// <returns>Scene to load to begin the first round, or null if no song can be played.</returns>
    public static string Start(List<SongGradeData> songPool)
    {
        End();
        songs = songPool;
        IsActive = true;
        return StartNextRound();
    }

    /// <summary>
    /// Advances to the next round and selects its song.
    /// </summary>
    /// <returns>Scene to load, which is the round intro if it's available, or null if no song can be played.</returns>
    public static string StartNextRound()
    {
        Round++;

        SongGradeData song = PickSong();
        if (song == null) return null;

        playedSongs.Add(song.songName);
        playedSongs.Add(song.gameplaySceneName);

        GameDataBridge.SelectedSong = song;
        GameDataBridge.SelectedDifficulty = RoundDifficulty;

        if (Application.CanStreamedLevelBeLoaded(RoundIntroScene)) return RoundIntroScene;

        Debug.LogWarning($"{RoundIntroScene} isn't in Build Settings, so the round intro was skipped.");
        return song.gameplaySceneName;
    }

    /// <summary>Records who won the current round: 1, 2, or 0 for a draw that gives nobody a win.</summary>
    public static void RecordRoundWinner(int winner)
    {
        if (winner == 1) P1Wins++;
        else if (winner == 2) P2Wins++;
    }

    /// <summary>Ends the match and clears its state.</summary>
    public static void End()
    {
        IsActive = false;
        Round = 0;
        P1Wins = 0;
        P2Wins = 0;
        songs = null;
        playedSongs.Clear();
    }

    private static SongGradeData PickSong()
    {
        // Avoid repeats, but a match outlasting the song list can start reusing songs.
        List<SongGradeData> candidates = GetPlayableSongs(true);
        if (candidates.Count == 0) candidates = GetPlayableSongs(false);

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private static List<SongGradeData> GetPlayableSongs(bool skipPlayed)
    {
        List<SongGradeData> playable = new List<SongGradeData>();
        if (songs == null) return playable;

        foreach (SongGradeData song in songs)
        {
            if (song == null || song.isRandomOption || song.excludeFromTugOfSlayRounds) continue;
            if (string.IsNullOrEmpty(song.gameplaySceneName) || song.GetChart(RoundDifficulty) == null) continue;
            if (skipPlayed && (playedSongs.Contains(song.songName) || playedSongs.Contains(song.gameplaySceneName))) continue;

            playable.Add(song);
        }

        return playable;
    }
}
