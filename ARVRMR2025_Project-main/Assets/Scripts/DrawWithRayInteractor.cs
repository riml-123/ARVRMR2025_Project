using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class DrawWithRayInteractor : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // XR Ray Interactor
    private SketchBook currentSketchBook;
    public InputActionProperty triggerButton;

    private Vector2? lastUV = null; // 이전 프레임의 UV 좌표

    private void Update()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            SketchBook sketchBook = hit.collider.GetComponent<SketchBook>();

            if (sketchBook != null && triggerButton.reference.action.IsPressed())
            {
                Vector2 currentUV;

                if (TryGetUVCoordinates(hit, sketchBook, out currentUV))
                {
                    //Debug.Log($"UV Coordinates: {currentUV}");

                    if (lastUV.HasValue)
                    {
                        // 이전 UV와 현재 UV 사이를 선으로 연결
                        DrawLineBetweenUVs(sketchBook, lastUV.Value, currentUV);
                    }

                    lastUV = currentUV; // 현재 UV를 저장
                }
            }
            else
            {
                lastUV = null; // 드로잉이 끝나면 초기화
            }
        }
        else
        {
            lastUV = null; // 레이캐스트가 히트하지 않으면 초기화
        }
    }

    private void DrawLineBetweenUVs(SketchBook sketchBook, Vector2 startUV, Vector2 endUV)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(startUV, endUV) * 100); // 보간 단계 수
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps; // 보간 비율
            Vector2 interpolatedUV = Vector2.Lerp(startUV, endUV, t); // 선형 보간
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