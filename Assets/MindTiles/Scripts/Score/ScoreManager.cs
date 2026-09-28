using System;
using System.Collections.Generic;
using UnityEngine;

namespace MindTiles.Score
{
    // Multi-layered score aggregation:
    //   round score      -> added to the in-memory session list
    //   session score     -> average of this session's round scores
    //   daily score (PlayerPrefs, keyed by date) += session average, on EndSession
    //   overall score (PlayerPrefs)               += session average, on EndSession
    //
    // "Session" = one continuous play session from app start until
    // EndSession() is called (app backgrounded/quit, or player returns to
    // the main menu).
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private const string OverallScoreKey = "MT_OverallScore";
        private const string DailyScoreKeyPrefix = "MT_DailyScore_";

        private readonly List<float> sessionRoundScores = new List<float>();
        private bool sessionCommitted = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void AddRoundScore(float score)
        {
            sessionRoundScores.Add(score);
            sessionCommitted = false;
        }

        public float GetSessionAverage()
        {
            if (sessionRoundScores.Count == 0) return 0f;
            float sum = 0f;
            foreach (float s in sessionRoundScores) sum += s;
            return sum / sessionRoundScores.Count;
        }

        // Rolls the current session's average into today's daily score and
        // the overall cumulative score, then clears the session.
        public void EndSession()
        {
            if (sessionCommitted || sessionRoundScores.Count == 0) return;

            float sessionAverage = GetSessionAverage();

            float dailyScore = PlayerPrefs.GetFloat(TodayKey(), 0f);
            PlayerPrefs.SetFloat(TodayKey(), dailyScore + sessionAverage);

            float overallScore = PlayerPrefs.GetFloat(OverallScoreKey, 0f);
            PlayerPrefs.SetFloat(OverallScoreKey, overallScore + sessionAverage);

            PlayerPrefs.Save();

            sessionCommitted = true;
            sessionRoundScores.Clear();
        }

        public float GetDailyScore(DateTime date)
        {
            return PlayerPrefs.GetFloat(DailyScoreKeyPrefix + date.ToString("yyyyMMdd"), 0f);
        }

        public float GetOverallScore()
        {
            return PlayerPrefs.GetFloat(OverallScoreKey, 0f);
        }

        private static string TodayKey()
        {
            return DailyScoreKeyPrefix + DateTime.Now.ToString("yyyyMMdd");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) EndSession();
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }
    }
}
