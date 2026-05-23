using System.Collections.Generic;

public class FileSystemNode
{
    public string Name;
    public bool IsDirectory;
    public string Content; 
    public FileSystemNode Parent;
    public List<FileSystemNode> Children = new();

    public FileSystemNode(string name, bool isDirectory, string content = "", FileSystemNode parent = null)
    {
        Name = name;
        IsDirectory = isDirectory;
        Content = content;
        Parent = parent;
    }

    public FileSystemNode FindChild(string name)
    {
        return Children.Find(c => c.Name == name);
    }
}
