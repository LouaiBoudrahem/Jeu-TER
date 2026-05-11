using System.Collections.Generic;
using UnityEngine;

public enum NodeType { File, Folder }

[System.Serializable]
public class FileData
{
    public string id;
    public string displayName;
    public Sprite icon;
    public TextAsset content;
    public int quizQuestionIndex = -1;
    public List<InventoryItem> requiredItems;
    public bool requireAll = false;
    public bool hiddenByDefault = false;
}

[System.Serializable]
public class FolderData
{
    public string id;
    public string displayName;
    public Sprite icon;
    public List<InventoryItem> requiredItems;
    public bool requireAll = false;
    public bool hiddenByDefault = false;
    public List<FolderData> subfolders;
    public List<FileData> files;
}

[CreateAssetMenu(menuName = "Computer/DesktopData", fileName = "NewDesktopData")]
public class DesktopData : ScriptableObject
{
    public List<FolderData> rootFolders = new List<FolderData>();
    public List<FileData> rootFiles = new List<FileData>();
}
