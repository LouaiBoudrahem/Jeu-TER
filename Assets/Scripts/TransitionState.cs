using UnityEngine;

public static class TransitionState
{
    public static bool HasPending = false;
    // normalized 0..1 value where the game-scene slider should start (e.g. 0.56)
    public static float PendingStartPercent = 0f;
}
