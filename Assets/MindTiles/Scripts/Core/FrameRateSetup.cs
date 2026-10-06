using UnityEngine;

namespace MindTiles.Core
{
    // Mobile builds default to 30 fps; this runs once before the first scene
    // loads and asks for 60 so drag-and-drop and animations feel smooth.
    public static class FrameRateSetup
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
