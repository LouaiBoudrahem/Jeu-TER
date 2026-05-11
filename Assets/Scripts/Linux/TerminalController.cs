using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerminalController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject terminalPanel;

    private FileSystemNode root;
    private FileSystemNode currentNode;
    private StringBuilder outputLog = new StringBuilder();

    private const string Prompt = "<color=#00ff00>user@linux</color>:<color=#5599ff>{0}</color>$ ";

    void Start()
    {
        root = TerminalFileSystem.BuildFileSystem();
        currentNode = root.FindChild("home")?.FindChild("user") ?? root;

        AppendLine("Welcome to <color=#00ff00>LinuxSim v1.0</color>");
        AppendLine("A flag is hidden somewhere in the filesystem. Find it.");
        AppendLine("Commands: <color=#ffff00>ls</color>, <color=#ffff00>cd [dir]</color>, <color=#ffff00>cat [file]</color>, <color=#ffff00>pwd</color>, <color=#ffff00>clear</color>, <color=#ffff00>exit</color>");
        AppendLine("");
        RefreshOutput();
    }

    void OnEnable()
    {
        inputField.ActivateInputField();
        inputField.onSubmit.AddListener(HandleInput);
    }

    void OnDisable()
    {
        inputField.onSubmit.RemoveListener(HandleInput);
    }

    void Update()
    {
        if (!inputField.isFocused && terminalPanel.activeSelf)
            inputField.ActivateInputField();
    }

    private void HandleInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        AppendLine(string.Format(Prompt, GetCurrentPath()) + input);

        ProcessCommand(input.Trim());

        inputField.text = "";
        inputField.ActivateInputField();
        RefreshOutput();

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void ProcessCommand(string input)
    {
        var parts = input.Split(' ');
        var cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "ls":
                CmdLs(parts);
                break;
            case "cd":
                CmdCd(parts);
                break;
            case "cat":
                CmdCat(parts);
                break;
            case "pwd":
                AppendLine(GetCurrentPath());
                break;
            case "clear":
                outputLog.Clear();
                break;
            case "exit":
                terminalPanel.SetActive(false);
                break;
            case "help":
                AppendLine("Commands: ls, cd [dir], cat [file], pwd, clear, exit");
                break;
            default:
                AppendLine($"<color=#ff4444>{cmd}: command not found</color>");
                break;
        }
    }

    private void CmdLs(string[] parts)
    {
        bool showHidden = parts.Length > 1 && parts[1] == "-a";
        var sb = new StringBuilder();

        if (currentNode.Parent != null)
            sb.Append("<color=#5599ff>../</color>  ");

        foreach (var child in currentNode.Children)
        {
            if (!showHidden && child.Name.StartsWith(".")) continue;

            if (child.IsDirectory)
                sb.Append($"<color=#5599ff>{child.Name}/</color>  ");
            else
                sb.Append($"{child.Name}  ");
        }

        AppendLine(sb.Length > 0 ? sb.ToString() : "(empty)");
    }

    private void CmdCd(string[] parts)
    {
        if (parts.Length < 2 || parts[1] == "~")
        {
            currentNode = root.FindChild("home")?.FindChild("user") ?? root;
            return;
        }

        var target = parts[1];

        if (target == "..")
        {
            if (currentNode.Parent != null)
                currentNode = currentNode.Parent;
            else
                AppendLine("<color=#ff4444>Already at root</color>");
            return;
        }

        if (target == "/")
        {
            currentNode = root;
            return;
        }

        var found = currentNode.FindChild(target);
        if (found == null)
            AppendLine($"<color=#ff4444>cd: {target}: No such file or directory</color>");
        else if (!found.IsDirectory)
            AppendLine($"<color=#ff4444>cd: {target}: Not a directory</color>");
        else
            currentNode = found;
    }

    private void CmdCat(string[] parts)
    {
        if (parts.Length < 2)
        {
            AppendLine("<color=#ff4444>cat: missing operand</color>");
            return;
        }

        var found = currentNode.FindChild(parts[1]);
        if (found == null)
            AppendLine($"<color=#ff4444>cat: {parts[1]}: No such file or directory</color>");
        else if (found.IsDirectory)
            AppendLine($"<color=#ff4444>cat: {parts[1]}: Is a directory</color>");
        else
        {
            AppendLine(found.Content);
            if (found.Name == "flag.txt")
                AppendLine("\n<color=#ffff00>🎉 Congratulations! You found the flag!</color>");
        }
    }

    private string GetCurrentPath()
    {
        var parts = new Stack<string>();
        var node = currentNode;
        while (node != null && node.Name != "/")
        {
            parts.Push(node.Name);
            node = node.Parent;
        }
        var path = "/" + string.Join("/", parts);
        return path == "/" ? "/" : path;
    }

    private void AppendLine(string line)
    {
        outputLog.AppendLine(line);
    }

    private void RefreshOutput()
    {
        outputText.text = outputLog.ToString();
    }
}