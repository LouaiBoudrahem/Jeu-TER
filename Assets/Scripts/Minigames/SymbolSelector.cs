using UnityEngine;

public class SymbolSelector : MonoBehaviour
{
    public MatrixMinigame gameController;

    public void SelectNone()
    {
        if (gameController != null) gameController.SelectNone();
    }

    public void SelectMoon()
    {
        if (gameController != null) gameController.SelectMoon();
    }

    public void SelectSun()
    {
        if (gameController != null) gameController.SelectSun();
    }

    public void SelectStar()
    {
        if (gameController != null) gameController.SelectStar();
    }
}
