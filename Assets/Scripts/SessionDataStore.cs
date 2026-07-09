using System.Collections.Generic;

public static class SessionDataStore
{
    public static string sessionId = "";

    public static int anxietyScore = 0;
    public static int quizScore = 0;

    // --- The Array Baskets for the Web Dashboard ---
    public static List<int> anxietyAnswers = new List<int>();
    public static List<int> quizAnswers = new List<int>();

    public static List<MedRecord> medications = new List<MedRecord>();

    public class MedRecord
    {
        public string drugCode;
        public string status;
    }

    public static void ClearSession()
    {
        sessionId = "";
        anxietyScore = 0;
        quizScore = 0;
        anxietyAnswers.Clear();
        quizAnswers.Clear();
        medications.Clear();
    }
}