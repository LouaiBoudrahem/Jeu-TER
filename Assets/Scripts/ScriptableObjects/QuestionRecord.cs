using System;
using UnityEngine;

[Serializable]
public class QuestionRecord
{
    public string id;
    public QuestionData.QuestionType questionType;
    public QuestionData.DifficultyLevel difficultyLevel;
    public QuestionData.Subject subject;
    public string question;
    public string[] options;
    public string answer;
    public int correctOptionIndex;
    public int scoreReward = 1;
    public string hint;

    public static QuestionRecord FromQuestionData(QuestionData data)
    {
        if (data == null)
        {
            return null;
        }

        return new QuestionRecord
        {
            id = string.IsNullOrWhiteSpace(data.name) ? Guid.NewGuid().ToString("N") : data.name,
            questionType = data.questionType,
            difficultyLevel = data.difficultyLevel,
            subject = data.subject,
            question = data.question,
            options = data.options != null ? (string[])data.options.Clone() : null,
            answer = data.answer,
            correctOptionIndex = data.correctOptionIndex,
            scoreReward = Mathf.Max(0, data.scoreReward),
            hint = data.hint
        };
    }

    public QuestionData ToQuestionData()
    {
        QuestionData runtimeData = ScriptableObject.CreateInstance<QuestionData>();
        runtimeData.name = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        runtimeData.questionType = questionType;
        runtimeData.difficultyLevel = difficultyLevel;
        runtimeData.subject = subject;
        runtimeData.question = question;
        runtimeData.options = options != null ? (string[])options.Clone() : new string[0];
        runtimeData.answer = answer;
        runtimeData.correctOptionIndex = correctOptionIndex;
        runtimeData.scoreReward = Mathf.Max(0, scoreReward);
        runtimeData.hint = hint;
        return runtimeData;
    }
}