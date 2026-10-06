using UnityEngine;

namespace MindTiles.Score
{
    // Pure functions that turn one level's raw result into points and a
    // performance grade. No state, so the formulas can be tuned in one place.
    public static class ScoreCalculator
    {
        // 1 = checked instantly, 0 = the timer ran out.
        public static float SpeedFactor(float timeUsed, float timeLimit)
        {
            return timeLimit > 0f ? Mathf.Clamp01(1f - timeUsed / timeLimit) : 0f;
        }

        // Points = tiles x pointsPerTile x accuracy x speed multiplier.
        // Accuracy is the decimal share of correct tiles (4/6 = 0.667); the
        // multiplier runs from 1 (time ran out) to 1 + maxSpeedBonus (instant).
        // Nothing correct = 0 points, however fast.
        public static int LevelPoints(int correct, int total, float speed, int pointsPerTile, float maxSpeedBonus)
        {
            if (correct <= 0 || total <= 0) return 0;

            float accuracy = (float)correct / total;
            return Mathf.RoundToInt(total * pointsPerTile * accuracy * (1f + maxSpeedBonus * speed));
        }

        // Level grade between 0 and 1: accuracy, with `speedShare` of it
        // depending on speed. Perfect & instant = 1; perfect at the buzzer =
        // 1 - speedShare; nothing correct = 0.
        public static float LevelPerformance(int correct, int total, float speed, float speedShare)
        {
            if (correct <= 0 || total <= 0) return 0f;

            float accuracy = (float)correct / total;
            return accuracy * ((1f - speedShare) + speedShare * speed);
        }
    }
}
