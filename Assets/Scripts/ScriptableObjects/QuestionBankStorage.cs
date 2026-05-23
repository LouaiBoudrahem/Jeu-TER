using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class QuestionBankStorage
{
    private const string FileName = "question_bank.json";
    private static string CachedFilePath;

    public static string FilePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CachedFilePath))
            {
                CachedFilePath = Path.Combine(Application.persistentDataPath, FileName);
            }

            return CachedFilePath;
        }
    }

    public static QuestionBank Load()
    {
        if (!File.Exists(FilePath))
        {
            return new QuestionBank();
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            QuestionBank bank = JsonUtility.FromJson<QuestionBank>(json);
            return bank ?? new QuestionBank();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"QuestionBankStorage.Load failed: {exception.Message}");
            return new QuestionBank();
        }
    }

    public static void Save(QuestionBank bank)
    {
        if (bank == null)
        {
            bank = new QuestionBank();
        }

        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonUtility.ToJson(bank, true);
        File.WriteAllText(FilePath, json);
    }

    public static QuestionData CreateRuntimeQuestion(QuestionRecord record)
    {
        return record != null ? record.ToQuestionData() : null;
    }

    public static List<QuestionData> LoadAllQuestions()
    {
        QuestionBank bank = Load();
        List<QuestionData> questions = new List<QuestionData>();

        foreach (QuestionRecord record in bank.questions)
        {
            QuestionData runtimeQuestion = CreateRuntimeQuestion(record);
            if (runtimeQuestion != null)
            {
                questions.Add(runtimeQuestion);
            }
        }

        return questions;
    }

    public static QuestionData GetRandomQuestion()
    {
        List<QuestionData> questions = LoadAllQuestions();
        if (questions.Count == 0)
        {
            return null;
        }

        return questions[Random.Range(0, questions.Count)];
    }

    public static QuestionData GetQuestionById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        QuestionBank bank = Load();
        for (int i = 0; i < bank.questions.Count; i++)
        {
            QuestionRecord record = bank.questions[i];
            if (record == null)
            {
                continue;
            }

            if (string.Equals(record.id, id, System.StringComparison.OrdinalIgnoreCase))
            {
                return CreateRuntimeQuestion(record);
            }
        }

        return null;
    }

    public static QuestionData GetQuestionByIndex(int index)
    {
        QuestionBank bank = Load();
        if (index < 0 || index >= bank.questions.Count)
        {
            return null;
        }

        QuestionRecord record = bank.questions[index];
        return CreateRuntimeQuestion(record);
    }

    public static void AddOrUpdate(QuestionRecord record)
    {
        if (record == null)
        {
            return;
        }

        QuestionBank bank = Load();

        if (string.IsNullOrWhiteSpace(record.id))
        {
            record.id = System.Guid.NewGuid().ToString("N");
        }

        int existingIndex = bank.questions.FindIndex(question => question != null && question.id == record.id);
        if (existingIndex >= 0)
        {
            bank.questions[existingIndex] = record;
        }
        else
        {
            bank.questions.Add(record);
        }

        Save(bank);
    }

    public static bool Remove(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        QuestionBank bank = Load();
        int removed = bank.questions.RemoveAll(question => question != null && question.id == id);

        if (removed > 0)
        {
            Save(bank);
            return true;
        }

        return false;
    }

    public static void OverwriteAll(IEnumerable<QuestionRecord> records)
    {
        QuestionBank bank = new QuestionBank();

        if (records != null)
        {
            foreach (QuestionRecord record in records)
            {
                if (record == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(record.id))
                {
                    record.id = System.Guid.NewGuid().ToString("N");
                }

                bank.questions.Add(record);
            }
        }

        Save(bank);
    }
}