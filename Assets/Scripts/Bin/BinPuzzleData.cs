using UnityEngine;

[CreateAssetMenu(fileName = "BinPuzzle", menuName = "Quiz/BinPuzzle")]
public class BinPuzzleData : ScriptableObject
{
    [TextArea(5, 15)]
    public string AlgorithmCode;        

    [TextArea]
    public string OutputAnswer;           

    public int CrashLine;                 

    public int MaxPoints = 25;
    public int PenaltyPerAttempt = 6;
}