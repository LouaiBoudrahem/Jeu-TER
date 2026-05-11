using System;
using System.Reflection;
using UnityEngine;

public class BoardCameraAutoFit : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private Transform boardCenter;
    [SerializeField] private Renderer[] boardRenderers;

    [Header("Fit")]
    [SerializeField] private bool useOrthographic = true;
    [SerializeField] [Range(0f, 1f)] private float paddingRatio = 0.08f;
    [SerializeField] [Min(0f)] private float extraDistance = 0.05f;
    [SerializeField] [Min(0.01f)] private float minDistance = 0.2f;
    [SerializeField] [Range(1f, 179f)] private float defaultVerticalFov = 50f;

    [Header("Update")]
    [SerializeField] private bool fitOnStart = true;
    [SerializeField] private bool fitOnResolutionChange = true;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Start()
    {
        CacheResolution();

        if (fitOnStart)
        {
            FitNow();
        }
    }

    private void LateUpdate()
    {
        if (!fitOnResolutionChange)
        {
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            CacheResolution();
            FitNow();
        }
    }

    [ContextMenu("Fit Now")]
    public void FitNow()
    {
        if (!TryGetBoardBounds(out Bounds bounds))
        {
            Debug.LogWarning("BoardCameraAutoFit: no board bounds found. Assign boardRenderers or boardCenter.");
            return;
        }

        float aspect = GetAspect();
        if (aspect <= 0f)
        {
            return;
        }

        Vector3 center = boardCenter != null ? boardCenter.position : bounds.center;
        Vector3 extents = bounds.extents;
        float paddedHalfHeight = extents.y * (1f + paddingRatio);
        float paddedHalfWidth = extents.x * (1f + paddingRatio);
        Vector3 fromCenter = transform.position - center;
        Vector3 sideDirection = fromCenter.sqrMagnitude > 0.000001f
            ? fromCenter.normalized
            : -transform.forward;

        if (useOrthographic)
        {
            float orthoSize = Mathf.Max(paddedHalfHeight, paddedHalfWidth / Mathf.Max(0.0001f, aspect));
            float distance = Mathf.Max(minDistance, fromCenter.magnitude);
            transform.position = center + sideDirection * distance;
            SetLens(true, orthoSize, GetCurrentVerticalFov());
            return;
        }

        float verticalFov = Mathf.Clamp(GetCurrentVerticalFov(), 1f, 179f);
        float halfVerticalRadians = 0.5f * verticalFov * Mathf.Deg2Rad;

        float dHeight = paddedHalfHeight / Mathf.Max(0.0001f, Mathf.Tan(halfVerticalRadians));
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(halfVerticalRadians) * aspect);
        float dWidth = paddedHalfWidth / Mathf.Max(0.0001f, Mathf.Tan(0.5f * horizontalFov));

        float distanceToFit = Mathf.Max(dHeight, dWidth) + extraDistance;
        distanceToFit = Mathf.Max(minDistance, distanceToFit);

        transform.position = center + sideDirection * distanceToFit;
        SetLens(false, 0f, verticalFov);
    }

    private bool TryGetBoardBounds(out Bounds bounds)
    {
        bounds = default;

        if (boardRenderers != null && boardRenderers.Length > 0)
        {
            bool initialized = false;
            for (int i = 0; i < boardRenderers.Length; i++)
            {
                Renderer renderer = boardRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (initialized)
            {
                return true;
            }
        }

        if (boardCenter != null)
        {
            bounds = new Bounds(boardCenter.position, Vector3.one);
            return true;
        }

        return false;
    }

    private float GetAspect()
    {
        Camera unityCamera = GetComponent<Camera>();
        if (unityCamera != null)
        {
            return unityCamera.aspect;
        }

        if (Screen.height <= 0)
        {
            return 16f / 9f;
        }

        return (float)Screen.width / Screen.height;
    }

    private float GetCurrentVerticalFov()
    {
        Camera unityCamera = GetComponent<Camera>();
        if (unityCamera != null)
        {
            return unityCamera.fieldOfView;
        }

        if (TryGetCinemachineLensValue("FieldOfView", out float fov))
        {
            return fov;
        }

        return defaultVerticalFov;
    }

    private void SetLens(bool orthographic, float orthographicSize, float verticalFov)
    {
        Camera unityCamera = GetComponent<Camera>();
        if (unityCamera != null)
        {
            unityCamera.orthographic = orthographic;
            if (orthographic)
            {
                unityCamera.orthographicSize = orthographicSize;
            }
            else
            {
                unityCamera.fieldOfView = verticalFov;
            }
        }

        TrySetCinemachineLens(orthographic, orthographicSize, verticalFov);
    }

    private bool TryGetCinemachineLensValue(string propertyName, out float value)
    {
        value = 0f;

        Component[] components = GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();
            string namespaceName = type.Namespace ?? string.Empty;
            if (!namespaceName.Contains("Cinemachine"))
            {
                continue;
            }

            if (TryGetLensFromComponent(component, out object lens, out MemberInfo lensMember))
            {
                PropertyInfo property = lens.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(float) && property.CanRead)
                {
                    value = (float)property.GetValue(lens);
                    return true;
                }

                
                _ = lensMember;
            }
        }

        return false;
    }

    private void TrySetCinemachineLens(bool orthographic, float orthographicSize, float verticalFov)
    {
        Component[] components = GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();
            string namespaceName = type.Namespace ?? string.Empty;
            if (!namespaceName.Contains("Cinemachine"))
            {
                continue;
            }

            if (!TryGetLensFromComponent(component, out object lens, out MemberInfo lensMember))
            {
                continue;
            }

            SetLensProperty(lens, "Orthographic", orthographic);
            SetLensProperty(lens, "OrthographicSize", orthographicSize);
            SetLensProperty(lens, "FieldOfView", verticalFov);

            switch (lensMember)
            {
                case PropertyInfo lensProperty:
                    lensProperty.SetValue(component, lens);
                    break;
                case FieldInfo lensField:
                    lensField.SetValue(component, lens);
                    break;
            }
        }
    }

    private static bool TryGetLensFromComponent(Component component, out object lens, out MemberInfo lensMember)
    {
        lens = null;
        lensMember = null;

        Type type = component.GetType();

        PropertyInfo lensProperty = type.GetProperty("Lens", BindingFlags.Public | BindingFlags.Instance);
        if (lensProperty != null && lensProperty.CanRead && lensProperty.CanWrite)
        {
            lens = lensProperty.GetValue(component);
            lensMember = lensProperty;
            return lens != null;
        }

        FieldInfo lensField = type.GetField("m_Lens", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (lensField != null)
        {
            lens = lensField.GetValue(component);
            lensMember = lensField;
            return lens != null;
        }

        return false;
    }

    private static void SetLensProperty(object lens, string propertyName, object value)
    {
        PropertyInfo property = lens.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null || !property.CanWrite)
        {
            return;
        }

        if (property.PropertyType == typeof(bool) && value is bool boolValue)
        {
            property.SetValue(lens, boolValue);
            return;
        }

        if (property.PropertyType == typeof(float) && value is float floatValue)
        {
            property.SetValue(lens, floatValue);
        }
    }

    private void CacheResolution()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }
}
