using UnityEngine;

[CreateAssetMenu(fileName = "QuestionData", menuName = "Question Data")]

public class QuestionData : ScriptableObject
{
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        ShortAnswer
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public enum Subject
    {
        Math,
        Science,
        History,
        Literature,
        Geography
    }
    public QuestionType questionType;
    public DifficultyLevel difficultyLevel;
    public Subject subject;

    public string question;
    public string[] options;

    public string answer;
    public float answerIndex;
    public int correctOptionIndex;
    public int scoreReward = 1;

    public string hint;

    public void isCorrect(int selectedOptionIndex)
    {
        
        switch (questionType)
        {
            case QuestionType.MultipleChoice:
                if (selectedOptionIndex == correctOptionIndex)
                {
                    Debug.Log("Correct!");
                }
                else
                {
                    Debug.Log("Incorrect. Try again.");
                }
                break;
            case QuestionType.TrueFalse:
                bool selectedAnswer = selectedOptionIndex == 0;
                if ((answer.ToLower() == "true" && selectedAnswer) || (answer.ToLower() == "false" && !selectedAnswer))
                {
                    Debug.Log("Correct!");
                }
                else
                {
                    Debug.Log("Incorrect. Try again.");
                }
                break;
            case QuestionType.ShortAnswer:
                if (selectedOptionIndex == 0) 
                {
                    Debug.Log("Correct!");
                }
                else
                {
                    Debug.Log("Incorrect. Try again.");
                }
                break;
        }
    }


}
