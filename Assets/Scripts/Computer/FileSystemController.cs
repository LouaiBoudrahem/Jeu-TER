using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FileSystemController : MonoBehaviour
{
    [SerializeField] private DesktopData desktopData;

    public DesktopData DesktopData => desktopData;

    public IEnumerable<FolderData> GetVisibleRootFolders()
    {
        if (desktopData == null) return Enumerable.Empty<FolderData>();
        return desktopData.rootFolders.Where(f => IsUnlocked(f));
    }

    public IEnumerable<FileData> GetVisibleRootFiles()
    {
        if (desktopData == null) return Enumerable.Empty<FileData>();
        return desktopData.rootFiles.Where(f => IsUnlocked(f));
    }

    public IEnumerable<FolderData> GetVisibleSubfolders(FolderData folder)
    {
        if (folder == null || folder.subfolders == null) return Enumerable.Empty<FolderData>();
        return folder.subfolders.Where(f => IsUnlocked(f));
    }

    public IEnumerable<FileData> GetVisibleFiles(FolderData folder)
    {
        if (folder == null || folder.files == null) return Enumerable.Empty<FileData>();
        return folder.files.Where(f => IsUnlocked(f));
    }

    public bool IsUnlocked(FolderData folder)
    {
        if (folder == null) return false;
        if (folder.requiredItems == null || folder.requiredItems.Count == 0) return true;

        if (folder.requireAll)
        {
            return folder.requiredItems.All(item => item != null && InventoryManager.HasItem(item));
        }

        return folder.requiredItems.Any(item => item != null && InventoryManager.HasItem(item));
    }

    public bool IsUnlocked(FileData file)
    {
        if (file == null) return false;
        if (file.requiredItems == null || file.requiredItems.Count == 0) return true;

        if (file.requireAll)
        {
            return file.requiredItems.All(item => item != null && InventoryManager.HasItem(item));
        }

        return file.requiredItems.Any(item => item != null && InventoryManager.HasItem(item));
    }
}
