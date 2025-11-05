using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SketchBook : MonoBehaviour
{
    public Texture2D texture; // 스케치북 텍스처
    public static Color drawColor; // 텍스트 색상
    public int brushSize = 5; // 기본 브러시 크기
    public bool drawTrue;
    // public FlexibleColorPicker colorPicker;

    public Color backgroundColor; // 배경색
    private int minBrushSize = 1;
    private int maxBrushSize = 50;

    [SerializeField] private InputActionReference aButtonAction; // A 버튼 입력: 브러쉬 사이즈 증가
    [SerializeField] private InputActionReference bButtonAction; // B 버튼 입력: 브러쉬 사이즈 감소

    private void Awake()
    {
        // 텍스처 초기화
        Renderer renderer = GetComponent<Renderer>();
        texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        renderer.material.mainTexture = texture;

        Shader transparentShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (transparentShader != null)
        {
            renderer.material.shader = transparentShader;
            renderer.material.SetFloat("_Surface", 1);
            renderer.material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            renderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            Debug.LogWarning("URP/Unlit Shader not found.");
        }

        drawColor = Color.black;

        // 텍스처 배경색 설정
        backgroundColor = Color.white;
        ClearTexture();
        drawTrue = false;

        // Input Actions 활성화
        if (aButtonAction != null) aButtonAction.action.Enable();
        if (bButtonAction != null) bButtonAction.action.Enable();
    }

    private void Update()
    {
        if (aButtonAction != null && aButtonAction.action.WasPressedThisFrame())
        {
            brushSize = Mathf.Clamp(brushSize + 1, minBrushSize, maxBrushSize);//사이즈 증가
        }

        if (bButtonAction != null && bButtonAction.action.WasPressedThisFrame())
        {
            brushSize = Mathf.Clamp(brushSize - 1, minBrushSize, maxBrushSize);//사이즈 감소
        }
    }

    public void DrawAtUV(Vector2 uv) // 드로잉 함수
    {
        // UV 좌표를 텍스처 픽셀 좌표로 변환
        int pixelX = (int)(uv.x * texture.width);
        int pixelY = (int)(uv.y * texture.height);

        // 브러시로 텍스처에 색칠
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                int px = pixelX + x;
                int py = pixelY + y;

                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, drawColor);
                }
            }
        }

        texture.Apply(); // 텍스처 업데이트
        drawTrue = true;
    }

    /* HEX 코드로 드로잉 색상 바꾸는 함수
    public void SetDrawColorFromHex(Text textObject)
    {
        drawColor = colorPicker.color; // 알파 포함된 최종 색상
    }  public void SetDrawColorFromHex(Text textObject)
    {
        drawColor = colorPicker.color; // 알파 포함된 최종 색상
    }*/

    
    public void ClearTexture() // 스케치북 전체 지우기
    {
        Color32[] colors = new Color32[texture.width * texture.height];
        Color32 bgColor = backgroundColor; // 캐싱

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = bgColor;
        }

        texture.SetPixels32(colors); // SetPixels32는 SetPixels보다 빠름
        texture.Apply(); // GPU에 변경 사항을 적용
        drawTrue = false;
    }
}