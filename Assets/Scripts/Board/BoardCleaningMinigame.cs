using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BoardCleaningMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage symbolsDrawingImage;
    [SerializeField] private RectTransform brushCursorVisual;
    [SerializeField] private RectTransform brushCursorLayer;
    [SerializeField] private TMP_Text symbolsText;
    [SerializeField] private TMP_Text passwordText;
    [SerializeField] private TMP_Text instructionText;

    [Header("Content")]
    [SerializeField] private Texture2D symbolsDrawingTexture;
    [SerializeField] [TextArea] private string symbolsContent = "@ # % ? * + /\\ // < >";
    [SerializeField] private string password = "PASSWORD-042";

    [Header("Cleaning")]
    [SerializeField] [Min(0.0001f)] private float cleanPerMousePixel = 0.0025f;
    [SerializeField] private bool requireMouseHold = true;
    [SerializeField] [Min(1f)] private float brushRadiusPixels = 26f;
    [SerializeField] [Range(0.1f, 1f)] private float completionThreshold = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float sourceAlphaThreshold = 0.1f;
    [SerializeField] private bool smoothBrushStrokes = true;
    [SerializeField] private bool debugEraseHits = true;
    [SerializeField] private bool preserveErasingState = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onPasswordRevealed;

    private float cleanProgress;
    private bool passwordRevealed;
    private bool active;
    private Player player;
    private Action onClosed;
    private Texture2D runtimeDrawingTexture;
    private Color32[] runtimePixels;
    private bool[] drawableMask;
    private int textureWidth;
    private int textureHeight;
    private int totalDrawablePixels;
    private int clearedDrawablePixels;
    private bool hasActiveStroke;
    private Vector2 lastStrokePixel;
    private Camera uiCamera;
    private bool cleaningEnabled;
    private bool cleaningDisabledLogged;
    private bool hasSavedEraseState;
    private Camera fallbackPointerCamera;

    private void Awake()
    {
        InitializeBoardVisualsAtStartup();
    }

    public void Begin(Player interactingPlayer, Action closedCallback)
    {
        Begin(interactingPlayer, closedCallback, true, null);
    }

    public void Begin(Player interactingPlayer, Action closedCallback, bool allowCleaning, string lockedInstruction)
    {
        player = interactingPlayer;
        onClosed = closedCallback;
        active = true;
        cleaningEnabled = allowCleaning;

        bool reuseSavedState = preserveErasingState && hasSavedEraseState && runtimeDrawingTexture != null && runtimePixels != null;

        if (!reuseSavedState)
        {
            cleanProgress = 0f;
            passwordRevealed = false;
            InitializeDrawingSurface();
            hasSavedEraseState = runtimeDrawingTexture != null;
        }
        else
        {
            BindRuntimeTextureToUI();
        }

        hasActiveStroke = false;
        cleaningDisabledLogged = false;

        if (symbolsText != null)
        {
            symbolsText.text = symbolsContent;
        }

        if (passwordText != null)
        {
            passwordText.text = password;
            passwordText.gameObject.SetActive(true);
        }

        bool usingDrawingTexture = runtimeDrawingTexture != null;

        if (symbolsText != null)
        {
            symbolsText.gameObject.SetActive(!usingDrawingTexture);
            if (!usingDrawingTexture)
            {
                symbolsText.text = symbolsContent;
            }
            else
            {
                symbolsText.text = string.Empty;
            }
        }

        if (instructionText != null)
        {
            if (cleaningEnabled)
            {
                instructionText.text = passwordRevealed
                    ? "Password revealed. Press Esc."
                    : (requireMouseHold
                        ? "Hold left mouse and drag the brush over symbols."
                        : "Drag the brush over symbols to erase them.");
            }
            else
            {
                instructionText.text = string.IsNullOrWhiteSpace(lockedInstruction)
                    ? "You can read the symbols, but you need a brush to clean them."
                    : lockedInstruction;
            }
        }

        if (brushCursorVisual != null)
        {
            brushCursorVisual.gameObject.SetActive(cleaningEnabled);
        }

        ApplyVisuals();
    }

    private void InitializeBoardVisualsAtStartup()
    {
        if (passwordText != null)
        {
            passwordText.text = password;
            passwordText.gameObject.SetActive(true);
        }

        if (runtimeDrawingTexture == null)
        {
            InitializeDrawingSurface();
            hasSavedEraseState = runtimeDrawingTexture != null;
        }
        else
        {
            BindRuntimeTextureToUI();
        }

        bool usingDrawingTexture = runtimeDrawingTexture != null;
        if (symbolsText != null)
        {
            symbolsText.gameObject.SetActive(!usingDrawingTexture);
            symbolsText.text = usingDrawingTexture ? string.Empty : symbolsContent;
        }
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMinigame();
            return;
        }

        if (passwordRevealed)
        {
            return;
        }

        if (!cleaningEnabled)
        {
            if (debugEraseHits && !cleaningDisabledLogged)
            {
                Debug.Log("BoardCleaningMinigame: cleaning is disabled (player likely missing required brush item).");
                cleaningDisabledLogged = true;
            }
            return;
        }

        UpdateBrushCursorVisual();

        bool usedDrawingErase = TryEraseWithBrush();
        if (usedDrawingErase)
        {
            return;
        }

        if (!CanCleanNow())
        {
            return;
        }

        Vector2 mouseDelta = GetPointerDelta();
        if (mouseDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        cleanProgress = Mathf.Clamp01(cleanProgress + (mouseDelta.magnitude * cleanPerMousePixel));
        ApplyVisuals();

        if (cleanProgress >= completionThreshold)
        {
            RevealPassword();
        }
    }

    private void InitializeDrawingSurface()
    {
        if (symbolsDrawingImage == null || symbolsDrawingTexture == null)
        {
            if (debugEraseHits)
            {
                Debug.LogWarning("BoardCleaningMinigame: missing symbolsDrawingImage or symbolsDrawingTexture.");
            }
            runtimeDrawingTexture = null;
            runtimePixels = null;
            drawableMask = null;
            totalDrawablePixels = 0;
            clearedDrawablePixels = 0;
            return;
        }

        textureWidth = symbolsDrawingTexture.width;
        textureHeight = symbolsDrawingTexture.height;

        runtimeDrawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        runtimeDrawingTexture.wrapMode = TextureWrapMode.Clamp;
        runtimeDrawingTexture.filterMode = symbolsDrawingTexture.filterMode;

        runtimePixels = TryGetSourcePixels(symbolsDrawingTexture);
        if (runtimePixels == null || runtimePixels.Length == 0)
        {
            Debug.LogWarning("BoardCleaningMinigame: could not read symbolsDrawingTexture pixels. Falling back to text-only cleaning mode.");
            runtimeDrawingTexture = null;
            drawableMask = null;
            totalDrawablePixels = 0;
            clearedDrawablePixels = 0;
            return;
        }

        drawableMask = new bool[runtimePixels.Length];

        totalDrawablePixels = 0;
        clearedDrawablePixels = 0;

        byte alphaThreshold = (byte)Mathf.RoundToInt(sourceAlphaThreshold * 255f);
        for (int i = 0; i < runtimePixels.Length; i++)
        {
            bool drawable = runtimePixels[i].a > alphaThreshold;
            drawableMask[i] = drawable;
            if (drawable)
            {
                totalDrawablePixels++;
            }
        }

        runtimeDrawingTexture.SetPixels32(runtimePixels);
        runtimeDrawingTexture.Apply(false, false);

        cleanProgress = totalDrawablePixels <= 0 ? 1f : 0f;
        BindRuntimeTextureToUI();

        if (debugEraseHits)
        {
            Debug.Log($"BoardCleaningMinigame: initialized drawing surface {textureWidth}x{textureHeight} on {symbolsDrawingImage.name}.");
        }
    }

    private void BindRuntimeTextureToUI()
    {
        if (symbolsDrawingImage == null || runtimeDrawingTexture == null)
        {
            return;
        }

        symbolsDrawingImage.texture = runtimeDrawingTexture;
        symbolsDrawingImage.color = Color.white;
        symbolsDrawingImage.enabled = true;
        symbolsDrawingImage.raycastTarget = true;

        Canvas canvas = symbolsDrawingImage.canvas;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = GetActiveEnabledCamera();
            }

            uiCamera = canvas.worldCamera;
        }
        else
        {
            uiCamera = null;
        }

        fallbackPointerCamera = GetActiveEnabledCamera();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(symbolsDrawingImage.rectTransform);
    }

    private Color32[] TryGetSourcePixels(Texture2D sourceTexture)
    {
        if (sourceTexture == null)
        {
            return null;
        }

        try
        {
            return sourceTexture.GetPixels32();
        }
        catch (UnityException)
        {
            
        }
        catch (ArgumentException)
        {
            
        }

        Texture2D readableCopy = CreateReadableCopy(sourceTexture);
        if (readableCopy == null)
        {
            return null;
        }

        try
        {
            return readableCopy.GetPixels32();
        }
        finally
        {
            Destroy(readableCopy);
        }
    }

    private Texture2D CreateReadableCopy(Texture2D sourceTexture)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);

        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(sourceTexture, temporary);
            RenderTexture.active = temporary;

            Texture2D readableTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
            readableTexture.Apply(false, false);
            readableTexture.filterMode = sourceTexture.filterMode;
            readableTexture.wrapMode = TextureWrapMode.Clamp;
            return readableTexture;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"BoardCleaningMinigame: failed to create readable texture copy for '{sourceTexture.name}'. {ex.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private bool TryEraseWithBrush()
    {
        if (runtimeDrawingTexture == null || runtimePixels == null || drawableMask == null)
        {
            return false;
        }

        if (!CanCleanNow())
        {
            hasActiveStroke = false;
            return true;
        }

        if (!TryGetMousePixelOnDrawing(out Vector2 currentPixel))
        {
            if (debugEraseHits)
            {
                Debug.Log("BoardCleaningMinigame: click detected but pointer did not map to the drawing image.");
            }

            hasActiveStroke = false;
            return true;
        }

        bool changed;
        if (!hasActiveStroke || !smoothBrushStrokes)
        {
            changed = EraseAtPixel(currentPixel);
        }
        else
        {
            changed = EraseLine(lastStrokePixel, currentPixel);
        }

        hasActiveStroke = true;
        lastStrokePixel = currentPixel;

        if (changed)
        {
            if (debugEraseHits)
            {
                Debug.Log($"BoardCleaningMinigame: erased at {currentPixel.x:0.0}, {currentPixel.y:0.0}");
            }

            runtimeDrawingTexture.SetPixels32(runtimePixels);
            runtimeDrawingTexture.Apply(false, false);

            cleanProgress = totalDrawablePixels <= 0 ? 1f : Mathf.Clamp01((float)clearedDrawablePixels / totalDrawablePixels);
            ApplyVisuals();

            if (cleanProgress >= completionThreshold)
            {
                RevealPassword();
            }
        }

        return true;
    }

    private bool TryGetMousePixelOnDrawing(out Vector2 pixel)
    {
        pixel = Vector2.zero;

        if (!TryGetPointerPosition(out Vector2 screenPosition) || symbolsDrawingImage == null)
        {
            return false;
        }
        RectTransform targetRect = symbolsDrawingImage.rectTransform;

        Camera[] cameraCandidates = new Camera[]
        {
            uiCamera,
            symbolsDrawingImage.canvas != null ? symbolsDrawingImage.canvas.worldCamera : null,
            fallbackPointerCamera,
            GetActiveEnabledCamera(),
            null
        };

        bool mapped = false;
        Vector2 localPoint = Vector2.zero;

        for (int i = 0; i < cameraCandidates.Length; i++)
        {
            Camera candidate = cameraCandidates[i];
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, candidate, out localPoint))
            {
                continue;
            }

            Rect rectForCheck = targetRect.rect;
            if (rectForCheck.Contains(localPoint))
            {
                mapped = true;
                break;
            }
        }

        if (!mapped)
        {
            if (debugEraseHits)
            {
                Debug.Log("BoardCleaningMinigame: mouse is outside the symbols image.");
            }

            return false;
        }

        Rect rect = targetRect.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        float normalizedX = Mathf.Clamp01(Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x));
        float normalizedY = Mathf.Clamp01(Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

        float textureX = normalizedX * (textureWidth - 1);
        float textureY = normalizedY * (textureHeight - 1);
        pixel = new Vector2(textureX, textureY);

        if (debugEraseHits)
        {
            Debug.Log($"BoardCleaningMinigame: erase hit at {pixel.x:0.0}, {pixel.y:0.0}");
        }

        return true;
    }

    private bool EraseLine(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(1f, brushRadiusPixels * 0.35f)));

        bool changed = false;
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 1f : (float)i / steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            if (EraseAtPixel(point))
            {
                changed = true;
            }
        }

        return changed;
    }

    private bool EraseAtPixel(Vector2 pixelCenter)
    {
        int radius = Mathf.CeilToInt(brushRadiusPixels);
        int radiusSq = radius * radius;
        int centerX = Mathf.RoundToInt(pixelCenter.x);
        int centerY = Mathf.RoundToInt(pixelCenter.y);

        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(textureWidth - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(textureHeight - 1, centerY + radius);

        bool changed = false;
        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - centerY;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                if ((dx * dx) + (dy * dy) > radiusSq)
                {
                    continue;
                }

                int pixelIndex = y * textureWidth + x;
                Color32 pixelColor = runtimePixels[pixelIndex];
                if (pixelColor.a == 0)
                {
                    continue;
                }

                if (drawableMask[pixelIndex])
                {
                    clearedDrawablePixels++;
                }

                pixelColor.a = 0;
                runtimePixels[pixelIndex] = pixelColor;
                changed = true;
            }
        }

        return changed;
    }

    private void UpdateBrushCursorVisual()
    {
        if (brushCursorVisual == null)
        {
            return;
        }

        RectTransform targetRect = symbolsDrawingImage != null ? symbolsDrawingImage.rectTransform : null;

        if (targetRect == null)
        {
            return;
        }

        if (!TryGetPointerPosition(out Vector2 screenPosition))
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, uiCamera, out Vector2 localPoint))
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, null, out localPoint))
            {
                return;
            }
        }

        RectTransform cursorParent = brushCursorLayer != null ? brushCursorLayer : targetRect.parent as RectTransform;
        if (cursorParent == null)
        {
            cursorParent = targetRect;
        }

        brushCursorVisual.SetParent(cursorParent, false);
        brushCursorVisual.SetAsLastSibling();

        Vector3 worldPoint = targetRect.TransformPoint(localPoint);
        Vector2 cursorLocalPoint = cursorParent.InverseTransformPoint(worldPoint);
        brushCursorVisual.anchoredPosition = cursorLocalPoint;

        float diameter = brushRadiusPixels * 2f;
        brushCursorVisual.sizeDelta = new Vector2(diameter, diameter);
    }

    private bool CanCleanNow()
    {
        if (!requireMouseHold)
        {
            return true;
        }

        return IsPointerPressed() || WasPointerPressedThisFrame();
    }

    private bool TryGetPointerPosition(out Vector2 screenPosition)
    {
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = Input.mousePosition;
        return screenPosition.sqrMagnitude > 0f;
    }

    private Vector2 GetPointerDelta()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }

        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    private bool IsPointerPressed()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.isPressed;
        }

        return Input.GetMouseButton(0);
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        return Input.GetMouseButtonDown(0);
    }

    private static Camera GetActiveEnabledCamera()
    {
        Camera[] cameras = Camera.allCameras;
        Camera best = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (best == null || camera.depth > best.depth)
            {
                best = camera;
            }
        }

        return best;
    }

    private void ApplyVisuals()
    {
        
    }

    private void RevealPassword()
    {
        passwordRevealed = true;

        if (instructionText != null)
        {
            instructionText.text = "Password revealed. Press Esc.";
        }

        onPasswordRevealed?.Invoke();
    }

    public void CloseMinigame()
    {
        if (!active)
        {
            return;
        }

        active = false;
        hasActiveStroke = false;

        if (player != null)
        {
            player.EndComputerInteraction();
        }

        hasSavedEraseState = preserveErasingState && runtimeDrawingTexture != null && runtimePixels != null;

        onClosed?.Invoke();
        onClosed = null;
        player = null;
    }
}
