using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class DrawWithRayInteractor : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public InputActionProperty triggerButton;

    // public GameObject cursorPrefab;
    // private GameObject cursorInstance;

    private Vector2? lastUV = null;
    private int colorLayer;
    private XRInteractorLineVisual lineVisual; // LineVisual 컴포넌트
    private Gradient originalValidGradient;
    private Gradient hoverColorGradient;
    private Renderer cachedSketchBookRenderer;

    private void Start()
    {
        colorLayer = LayerMask.NameToLayer("color");
        if (colorLayer == -1)
        {
            Debug.LogWarning("Layer 'color' not found.");
        }

        
        lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
        // --- ---

        if (lineVisual != null)
        {
            originalValidGradient = lineVisual.validColorGradient;
        }
        else
        {
            // XRRayInteractor에는 XRInteractorLineVisual이 기본으로 붙어있습니다.
            // 이게 null이면 설정이 잘못된 것이므로 Error를 띄웁니다.
            Debug.LogError("XRInteractorLineVisual component not found on the ray interactor!");
        }
        hoverColorGradient = new Gradient();

        /*
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
            cursorInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Cursor Prefab is not assigned in DrawWithRayInteractor.");
        }
        */
    }

    private void Update()
    {
        /*
        if (cursorInstance != null)
        {
            cursorInstance.SetActive(false);
        }
        */

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (lineVisual != null)
            {
                lineVisual.validColorGradient = originalValidGradient;
            }
            if (triggerButton.reference.action.WasPressedThisFrame())
            {
                if (colorLayer != -1 && hit.collider.gameObject.layer == colorLayer)
                {
                    Renderer colorRenderer = hit.collider.GetComponent<Renderer>();
                    /*
                    if (colorRenderer != null && colorRenderer.material != null)
                    {
                        SketchBook.drawColor = colorRenderer.material.color;
                        lastUV = null;
                        SetLaserToColor(colorRenderer.material.color);
                        return;
                    }
                    */
                    if (colorRenderer != null)
                    {
                        // 새롭게 추가한 정적 메서드를 호출하여 픽셀 색상을 가져옵니다.
                        Color pickedColor = SketchBook.PickColorFromTexture(colorRenderer, hit.textureCoord);

                        if (pickedColor.a > 0.01f) // 유효한 색상(투명하지 않은 부분)인 경우
                        {
                            SketchBook.drawColor = pickedColor;
                            lastUV = null;
                            SetLaserToColor(pickedColor);
                            return;
                        }
                    }
                }
            }
            if (colorLayer != -1 && hit.collider.gameObject.layer == colorLayer)
            {
                Renderer colorRenderer = hit.collider.GetComponent<Renderer>();
                /*
                if (colorRenderer != null && colorRenderer.material != null)
                {
                    SetLaserToColor(colorRenderer.material.color);
                }
                */
                if (colorRenderer != null)
                {
                    Color hoverColor = SketchBook.PickColorFromTexture(colorRenderer, hit.textureCoord);
                    SetLaserToColor(hoverColor.a > 0.01f ? hoverColor : originalValidGradient.colorKeys[0].color);
                }
            }

            SketchBook sketchBook = hit.collider.GetComponent<SketchBook>();

            if (sketchBook != null)
            {
                if (cachedSketchBookRenderer == null || cachedSketchBookRenderer.gameObject != sketchBook.gameObject)
                {
                    cachedSketchBookRenderer = sketchBook.GetComponent<Renderer>();
                }

                if (triggerButton.reference.action.IsPressed())
                {
                    // --- 1. 드로잉 중 ---
                    Vector2 currentUV;
                    if (TryGetUVCoordinates(hit, sketchBook, out currentUV))
                    {
                        if (lastUV.HasValue)
                        {
                            DrawLineBetweenUVs(sketchBook, lastUV.Value, currentUV);
                        }
                        lastUV = currentUV;
                    }
                }
                else
                {
                    // --- 2. 드로잉 중이 아님 (단순 호버링) ---
                    lastUV = null;
                    /*
                    if (cachedSketchBookRenderer != null)
                    {
                        UpdateCursor(hit, sketchBook, cachedSketchBookRenderer);
                    }
                    */
                }
            }
            else
            {
                lastUV = null;
                cachedSketchBookRenderer = null;
            }
        }
        else
        {
            lastUV = null;
            if (lineVisual != null)
            {
                lineVisual.validColorGradient = originalValidGradient;
            }
            cachedSketchBookRenderer = null;
        }
    }

    /*
    private void UpdateCursor(RaycastHit hit, SketchBook sketchBook, Renderer sketchBookRenderer)
    {
        if (cursorInstance == null) return;

        cursorInstance.SetActive(true);
        cursorInstance.transform.position = hit.point + hit.normal * 0.01f;
        cursorInstance.transform.rotation = Quaternion.LookRotation(-hit.normal);

        float brushDiameterPixels = sketchBook.brushSize * 2.0f;
        float textureWidthPixels = sketchBook.texture.width;
        if (textureWidthPixels == 0) return;

        float brushRatio = brushDiameterPixels / textureWidthPixels;

        if (sketchBookRenderer == null) return;

        float sketchBookWorldWidth = sketchBookRenderer.bounds.size.x;
        float cursorWorldDiameter = brushRatio * sketchBookWorldWidth;
        cursorInstance.transform.localScale = new Vector3(cursorWorldDiameter, cursorWorldDiameter, 0.001f);
    }
    */

    private void SetLaserToColor(Color color)
    {
        if (lineVisual == null) return;
        hoverColorGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) }
        );
        lineVisual.validColorGradient = hoverColorGradient;
    }

    private void DrawLineBetweenUVs(SketchBook sketchBook, Vector2 startUV, Vector2 endUV)
    {
        float texWidth = sketchBook.texture.width;
        float texHeight = sketchBook.texture.height;

        // --- (수정 2) startUV.Y -> startUV.y ---
        Vector2 startPixel = new Vector2(startUV.x * texWidth, startUV.y * texHeight);
        Vector2 endPixel = new Vector2(endUV.x * texWidth, endUV.y * texHeight);

        float pixelDistance = Vector2.Distance(startPixel, endPixel);

        float stepSize = 1.0f;
        int steps = Mathf.CeilToInt(pixelDistance / stepSize);
        steps = Mathf.Max(1, steps);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / (float)steps;
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t);
            sketchBook.DrawAtUV(interpolatedUV);
        }
    }

    private bool TryGetUVCoordinates(RaycastHit hit, SketchBook sketchBook, out Vector2 uv)
    {
        uv = Vector2.zero;
        Renderer renderer = sketchBook.GetComponent<Renderer>();
        if (renderer != null && renderer.material.mainTexture != null)
        {
            Vector2 pixelUV = hit.textureCoord;
            uv = pixelUV;
            return true;
        }
        return false;
    }
}