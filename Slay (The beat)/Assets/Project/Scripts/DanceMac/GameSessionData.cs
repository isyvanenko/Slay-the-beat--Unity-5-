public static class GameSessionData
{
    public static int P1Score = 0;
    public static int P2Score = 0;
    public static int P1MaxCombo = 0; // New
    public static int P2MaxCombo = 0; // New
    public static bool IsTwoPlayer = false;
    public static string SongName = "Unknown Song";
    public static SongGradeData CurrentSongGrades;
    public static int CurrentRound = 1; 
    public static int TotalRounds = 2; 
}