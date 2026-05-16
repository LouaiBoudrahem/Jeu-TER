using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class MatrixMinigame : MonoBehaviour
{
    public enum Symbol { None = 0, Moon = 1, Sun = 2, Star = 3 }

    [Header("Grid")]
    public int rows = 4;
    public int cols = 3;
    public GameObject cellPrefab; 
    public Transform gridParent; 

    [Header("Sprites")]
    public Sprite moonSprite;
    public Sprite sunSprite;
    public Sprite starSprite;

    [Header("UI")]
    public TextMeshProUGUI successText;

    [HideInInspector]
    public Symbol selectedSymbol = Symbol.None;

    private MatrixCell[,] cells;
    private Symbol[,] solution;
    private Symbol[,] currentGrid;
    private bool isLocked = false;
    public AudioClip placementClip;
    public AudioClip successClip;
    private AudioSource audioSource;
    [Header("Outcome")]
    public string requiredSceneName;
    public GameObject prefabToInstantiateOnSuccess;
    public Vector3 instantiateWorldPosition;
    private bool isClosing = false;

    void Start()
    {
        if (successText != null)
            successText.gameObject.SetActive(false);
        BuildGrid();
        SetSolution();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseMinigameScene();
    }

    public void BuildGrid()
    {
        if (cellPrefab == null || gridParent == null) return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridParent.GetChild(i).gameObject);

        cells = new MatrixCell[rows, cols];
        currentGrid = new Symbol[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject go = Instantiate(cellPrefab, gridParent);
                go.name = $"Cell_{r}_{c}";
                MatrixCell cell = go.GetComponent<MatrixCell>();
                if (cell == null)
                {
                    Debug.LogWarning("Cell prefab is missing MatrixCell component.");
                    continue;
                }
                cell.Init(r, c, this);
                cells[r, c] = cell;
                currentGrid[r, c] = Symbol.None;
            }
        }
    }

    public void OnCellClicked(int row, int col)
    {
        PlaceSymbol(row, col, selectedSymbol);
    }

    public void PlaceSymbol(int row, int col, Symbol sym)
    {
        if (isLocked) return;
        if (cells == null) return;
        if (row < 0 || row >= rows || col < 0 || col >= cols) return;
        Sprite s = GetSpriteForSymbol(sym);
        cells[row, col].SetSymbol(sym, s);
        currentGrid[row, col] = sym;
        if (sym != Symbol.None && placementClip != null && audioSource != null)
            audioSource.PlayOneShot(placementClip);
        CheckSolution();
    }

    public Sprite GetSpriteForSymbol(Symbol sym)
    {
        switch (sym)
        {
            case Symbol.Moon: return moonSprite;
            case Symbol.Sun: return sunSprite;
            case Symbol.Star: return starSprite;
            case Symbol.None:
            default:
                return null;
        }
    }

    public void SelectNone() => selectedSymbol = Symbol.None;
    public void SelectMoon() => selectedSymbol = Symbol.Moon;
    public void SelectSun() => selectedSymbol = Symbol.Sun;
    public void SelectStar() => selectedSymbol = Symbol.Star;

    public void SetSolution()
    {
        solution = new Symbol[4, 3]
        {
            { Symbol.Sun, Symbol.Sun, Symbol.Star },
            { Symbol.None, Symbol.None, Symbol.Sun },
            { Symbol.None, Symbol.Moon, Symbol.None },
            { Symbol.None, Symbol.Star, Symbol.Sun }
        };
    }

    public void FillWithSolution()
    {
        if (solution == null) return;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                PlaceSymbol(r, c, solution[r, c]);
            }
        }
    }

    private void CheckSolution()
    {
        if (solution == null || currentGrid == null) return;

        bool matches = true;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (currentGrid[r, c] != solution[r, c])
                {
                    matches = false;
                    break;
                }
            }
            if (!matches) break;
        }

        if (matches && successText != null)
        {
            ShowSuccess();
        }
    }

    private void ShowSuccess()
    {
        if (successText == null) return;
        successText.gameObject.SetActive(true);
        successText.text = "Success!";
        if (successClip != null && audioSource != null)
            audioSource.PlayOneShot(successClip);
        LockGrid();
        ApplyOutcome();
        StartCoroutine(HideSuccessText());
    }

    private void ApplyOutcome()
    {
        if (!string.IsNullOrEmpty(requiredSceneName))
        {
            var s = SceneManager.GetSceneByName(requiredSceneName);
            if (!s.isLoaded) return;
        }

        if (prefabToInstantiateOnSuccess != null)
            Instantiate(prefabToInstantiateOnSuccess, instantiateWorldPosition, Quaternion.identity);
    }

    private void LockGrid()
    {
        isLocked = true;
        if (cells == null) return;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = cells[r, c];
                if (cell == null) continue;
                var btn = cell.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = false;
            }
        }
    }

    private IEnumerator HideSuccessText()
    {
        yield return new WaitForSeconds(3f);
        if (successText != null)
            successText.gameObject.SetActive(false);
    }

    private void CloseMinigameScene()
    {
        if (isClosing) return;
        isClosing = true;

        MinigameManager minigameManager = FindObjectOfType<MinigameManager>();
        if (minigameManager != null)
        {
            minigameManager.ExitMinigame();
            return;
        }

        var scene = gameObject.scene;
        if (!scene.isLoaded) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                Destroy(roots[i]);
        }

        SceneManager.UnloadSceneAsync(scene);
    }
}
