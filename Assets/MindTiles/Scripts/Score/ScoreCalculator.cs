using UnityEngine;

namespace MindTiles.Score
{
    // Pure function: turns one round's raw results into a point value.
    // Kept separate from ScoreManager so the formula can be tuned/tested
    // without touching persistence code.
    public static class ScoreCalculator
    {
        private const float BasePointsPerTile = 10f;

        public static float CalculateRoundScore(int correctTiles, int totalTiles, float timeTakenSeconds, float timeLimitSeconds, int moveCount)
        {
            if (totalTiles <= 0) return 0f;

            float accuracy = (float)correctTiles / totalTiles;
            float timeFactor = timeLimitSeconds > 0f
                ? Mathf.Clamp01(1f - (timeTakenSeconds / timeLimitSeconds))
                : 0f;
            float extraMoves = Mathf.Max(0, moveCount - totalTiles);
            float efficiencyFactor = Mathf.Clamp01(1f - (extraMoves / totalTiles));

            float score = BasePointsPerTile * totalTiles * accuracy * (0.6f + 0.25f * timeFactor + 0.15f * efficiencyFactor);
            return Mathf.Round(score);
        }
    }
}
