using System;
using System.Collections.Generic;
using UnityEngine;

public static class ComputerSolveOrderState
{
    private static int[] solveOrder;
    private static int currentSolveIndex;
    private static int solveCount;

    public static bool HasOrder => solveOrder != null && solveOrder.Length > 0;

    public static int CurrentExpectedNumber
    {
        get
        {
            if (solveCount <= 0 || currentSolveIndex < 0 || currentSolveIndex >= solveCount)
            {
                return -1;
            }

            return currentSolveIndex + 1;
        }
    }

    public static void SetOrder(IEnumerable<int> order)
    {
        if (order == null)
        {
            solveOrder = null;
            currentSolveIndex = 0;
            solveCount = 0;
            return;
        }

        List<int> values = new List<int>();
        foreach (int value in order)
        {
            values.Add(value);
        }

        solveOrder = values.Count > 0 ? values.ToArray() : null;
        solveCount = values.Count;
        currentSolveIndex = 0;

        if (solveOrder != null)
        {
            TransientDebugConsoleUI.Log($"ComputerSolveOrderState: order set for {solveCount} computers; values=[{string.Join(",", solveOrder)}], expected sequence=1..{solveCount}");
        }
    }

    public static bool CanAttempt(int solveNumber)
    {
        if (!HasOrder || solveNumber <= 0)
        {
            return true;
        }

        return solveNumber == CurrentExpectedNumber;
    }

    public static bool CompleteCurrent(int solveNumber)
    {
        if (!CanAttempt(solveNumber))
        {
            return false;
        }

        if (solveCount <= 0)
        {
            return true;
        }

        currentSolveIndex = Mathf.Min(currentSolveIndex + 1, solveCount);
        TransientDebugConsoleUI.Log($"ComputerSolveOrderState: solved {solveNumber}, next expected = {CurrentExpectedNumber}");
        return true;
    }

    public static void Reset()
    {
        solveOrder = null;
        currentSolveIndex = 0;
        solveCount = 0;
    }
}