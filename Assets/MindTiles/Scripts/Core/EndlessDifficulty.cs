using System;
using System.Collections.Generic;
using UnityEngine;
using MindTiles.Grid;
using MindTiles.Score;

namespace MindTiles.Core
{
    // One step of the Endless difficulty ladder. Read top to bottom in the
    // Inspector: what the board looks like, how much time the player gets,
    // and what they must achieve here to move on to the next stage.
    [Serializable]
    public class DifficultyStage
    {
        public string name = "Stage";

        [Header("Board")]
        [Min(1)] public int columns = 4;
        [Min(1)] public int rows = 4;
        [Min(0)] public int easyTiles;
        [Min(0)] public int midTiles;
        [Min(0)] public int hardTiles;

        [Header("Time")]
        [Tooltip("Memorize time when the stage starts; it shrinks slowly the longer the player stays (Memorize Ramp).")]
        [Min(0.5f)] public float memorizeSeconds = 4f;
        [Tooltip("Time to rebuild the pattern. When it runs out the level is checked automatically.")]
        [Min(3f)] public float timeLimitSeconds = 30f;

        [Header("To advance from this stage")]
        [Tooltip("Stage Score: points that must be earned while in this stage.")]
        [Min(0)] public int stageScoreToAdvance = 500;
        [Tooltip("Performance Rating (0-1) the player must be at or above.")]
        [Range(0f, 1f)] public float ratingToAdvance = 0.7f;
    }

    // Endless mode difficulty director. There are no fixed level gates: how
    // fast a player climbs depends only on how well they play.
    //
    // After every level:
    //   Level Points      = tiles x 10 x accuracy x speed multiplier (0 if nothing correct)
    //   Level Performance = accuracy, partly weighted by speed (0..1)
    //   Performance Rating = running average of Level Performance
    //   Stage Score        = points earned since entering the current stage
    //
    //   Promotion: Stage Score >= stage target AND Performance Rating >= stage target
    //   Demotion:  Performance Rating < Demote Below Rating (after a few levels in the stage)
    //
    // Past the Progression Cap (level 500) the stage is frozen; only score grows.
    // All state lives in SaveData.endless, so it survives app restarts.
    public class EndlessDifficulty : MonoBehaviour
    {
        [Header("Scoring")]
        [Tooltip("Extra points multiplier for speed: 1 = up to x2 points when checked instantly, x1 when the timer runs out.")]
        [SerializeField, Range(0f, 3f)] private float maxSpeedBonus = 1f;
        [Tooltip("How much of the Level Performance grade depends on speed (the rest is accuracy).")]
        [SerializeField, Range(0f, 1f)] private float speedShareInPerformance = 0.3f;

        [Header("Performance Rating")]
        [Tooltip("How strongly each new level moves the rating (0.2 = roughly the last ~10 levels count).")]
        [SerializeField, Range(0.05f, 1f)] private float ratingWeight = 0.2f;
        [Tooltip("Drop one stage if the rating falls below this. 0 = never demote.")]
        [SerializeField, Range(0f, 1f)] private float demoteBelowRating = 0.35f;
        [Tooltip("Levels to play in a stage before a demotion can happen (avoids bouncing right after a promotion).")]
        [SerializeField, Min(1)] private int minLevelsBeforeDemotion = 5;
        [Tooltip("Share of the lower stage's Stage Score already credited after a demotion, so climbing back is quicker.")]
        [SerializeField, Range(0f, 1f)] private float demotionCredit = 0.5f;
        [Tooltip("After this level the difficulty stays where it is; only the score keeps counting.")]
        [SerializeField, Min(1)] private int progressionCapLevel = 500;

        [Header("Memorize Ramp (gradual pressure inside a stage)")]
        [Tooltip("Memorize time lost per level played in the stage (fraction of the stage's time).")]
        [SerializeField, Range(0f, 0.02f)] private float memorizeRampPerLevel = 0.005f;
        [Tooltip("Memorize time never drops below this fraction of the stage's time.")]
        [SerializeField, Range(0.5f, 1f)] private float memorizeRampFloor = 0.75f;

        [Header("Stages (easy -> hard)")]
        [SerializeField] private List<DifficultyStage> stages = DefaultStages();

        // Targets assume a typical player (~85% accuracy, ~40% of the time left)
        // needs about 10 / 20 / 30 / 40 / 50 / 55 / 65 / 75 levels per stage;
        // stronger players earn more points per level and get there sooner.
        private static List<DifficultyStage> DefaultStages()
        {
            return new List<DifficultyStage>
            {
                Stage("Easy I",   4, 4,  4, 0,  0,  4f, 20f,   480, 0.72f),
                Stage("Easy II",  4, 4,  6, 0,  0,  5f, 25f,  1400, 0.72f),
                Stage("Easy III", 4, 5,  4, 2,  0,  5f, 30f,  2100, 0.70f),
                Stage("Mid I",    4, 5,  0, 6,  0,  6f, 35f,  2900, 0.70f),
                Stage("Mid II",   5, 5,  0, 8,  0,  7f, 45f,  4800, 0.68f),
                Stage("Mid III",  5, 5,  0, 8,  2,  8f, 50f,  6500, 0.68f),
                Stage("Hard I",   5, 6,  0, 0, 10,  9f, 60f,  7700, 0.66f),
                Stage("Hard II",  5, 6,  0, 0, 12, 10f, 70f, 10700, 0.66f),
                Stage("Hard III", 6, 6,  0, 0, 15, 11f, 80f,     0, 1f),
            };
        }

        private static DifficultyStage Stage(string name, int cols, int rows, int easy, int mid, int hard,
            float memorize, float timeLimit, int stageScore, float rating)
        {
            return new DifficultyStage
            {
                name = name, columns = cols, rows = rows,
                easyTiles = easy, midTiles = mid, hardTiles = hard,
                memorizeSeconds = memorize, timeLimitSeconds = timeLimit,
                stageScoreToAdvance = stageScore, ratingToAdvance = rating
            };
        }

        private static EndlessSave State => SaveSystem.Data.endless;

        public int Level => State.level;
        public int TotalScore => State.totalScore;
        public float Rating => State.rating;
        public DifficultyStage CurrentStage => stages[StageIndex];
        private int StageIndex => Mathf.Clamp(State.stage, 0, stages.Count - 1);

        // The board for the level the player is about to play.
        public LevelSpec CurrentSpec()
        {
            DifficultyStage stage = CurrentStage;
            float ramp = Mathf.Max(memorizeRampFloor, 1f - State.levelsInStage * memorizeRampPerLevel);

            return new LevelSpec
            {
                columns = stage.columns,
                rows = stage.rows,
                easyTiles = stage.easyTiles,
                midTiles = stage.midTiles,
                hardTiles = stage.hardTiles,
                memorizeSeconds = stage.memorizeSeconds * ramp,
                timeLimitSeconds = stage.timeLimitSeconds
            };
        }

        // Called once per checked level (by the player or by the timer).
        // Scores it, updates the rating, moves the stage if earned, advances
        // to the next level (only if passed) and saves. Returns the points earned.
        public int RecordLevel(int correct, int total, float timeUsed, float timeLimit, int pointsPerTile, bool passed)
        {
            float speed = ScoreCalculator.SpeedFactor(timeUsed, timeLimit);
            // A failed level gives no points and is replayed, but still counts
            // toward the rating (so repeated fails can demote).
            int points = passed ? ScoreCalculator.LevelPoints(correct, total, speed, pointsPerTile, maxSpeedBonus) : 0;
            float performance = ScoreCalculator.LevelPerformance(correct, total, speed, speedShareInPerformance);

            EndlessSave s = State;
            s.totalScore += points;

            if (s.level <= progressionCapLevel)
            {
                s.rating += ratingWeight * (performance - s.rating);
                s.stageScore += points;
                s.levelsInStage++;
                EvaluateStage(s);
            }

            if (passed) s.level++;
            SaveSystem.Save();
            return points;
        }

        private void EvaluateStage(EndlessSave s)
        {
            int current = StageIndex;
            DifficultyStage stage = stages[current];

            if (current + 1 < stages.Count &&
                s.stageScore >= stage.stageScoreToAdvance &&
                s.rating >= stage.ratingToAdvance)
            {
                EnterStage(s, current + 1, 0);
                return;
            }

            if (current > 0 && demoteBelowRating > 0f &&
                s.levelsInStage >= minLevelsBeforeDemotion &&
                s.rating < demoteBelowRating)
            {
                int previous = current - 1;
                EnterStage(s, previous, Mathf.RoundToInt(stages[previous].stageScoreToAdvance * demotionCredit));
            }
        }

        private void EnterStage(EndlessSave s, int index, int startingStageScore)
        {
            Debug.Log($"[Endless] Level {s.level}: {stages[StageIndex].name} -> {stages[index].name} (rating {s.rating:F2}, stage score {s.stageScore})");
            s.stage = index;
            s.stageScore = startingStageScore;
            s.levelsInStage = 0;
        }

        [ContextMenu("Endless/Restore Default Stages")]
        private void RestoreDefaultStages()
        {
            stages = DefaultStages();
        }

        [ContextMenu("Endless/Reset Endless Progress")]
        private void ResetProgress()
        {
            SaveSystem.Data.endless = new EndlessSave();
            SaveSystem.Save();
        }
    }
}
