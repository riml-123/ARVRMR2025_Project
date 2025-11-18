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

    public Color backgroundColor; // 배경색
    private int minBrushSize = 1;
    private int maxBrushSize = 50;

    [Header("Brush Controls")]
    [SerializeField] private InputActionReference joystickAction; // 오른쪽 조이스틱 입력
    [SerializeField] private float joystickThreshold = 0.2f; // 조이스틱 민감도 (데드존)

    [SerializeField] private float brushSizeChangeSpeed = 10.0f; // 초당 브러시 크기 변경 속도
    private float currentBrushSize; // 현재 크기

    private void Awake()
    {
    
        Renderer renderer = GetComponent<Renderer>();
        texture = new Texture2D(1024, 768, TextureFormat.RGBA32, false); // 스케치북 크기
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

        if (joystickAction != null) joystickAction.action.Enable();

        currentBrushSize = brushSize;
    }

    private void Update()
    {
        // --- (수정) 조이스틱 입력으로 브러시 크기 조절 ---
        if (joystickAction != null)
        {
            // 조이스틱의 Vector2 값을 읽어옴
            Vector2 joystickValue = joystickAction.action.ReadValue<Vector2>();
            float horizontalInput = joystickValue.x; 

            // 1. 조이스틱이 데드존(Threshold) 밖에 있는지 확인
            if (Mathf.Abs(horizontalInput) > joystickThreshold)
            {
                // 2. 입력 강도와 속도, 시간에 비례하여 크기 변경
                // (horizontalInput이 음수(왼쪽)면 감소, 양수(오른쪽)면 증가)
                float changeAmount = horizontalInput * brushSizeChangeSpeed * Time.deltaTime;

                // 3. 실수(float) 크기에 변경 값을 더함
                currentBrushSize += changeAmount;

                // 4. 최소/최대치를 넘지 않도록 제한
                currentBrushSize = Mathf.Clamp(currentBrushSize, minBrushSize, maxBrushSize);

                // 5. 최종 값을 반올림하여 실제 brushSize(int)에 적용
                brushSize = Mathf.RoundToInt(currentBrushSize);
            }
        }
    }

    public void DrawAtUV(Vector2 uv) // 드로잉 함수
    {
        // (기존 코드와 동일)
        int pixelX = (int)(uv.x * texture.width);
        int pixelY = (int)(uv.y * texture.height);

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

    public void ClearTexture() // 스케치북 전체 지우기
    {
        Color32[] colors = new Color32[texture.width * texture.height];
        Color32 bgColor = backgroundColor;

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = bgColor;
        }

        texture.SetPixels32(colors);
        texture.Apply();
        drawTrue = false;
    }

    public static Color PickColorFromTexture(Renderer targetRenderer, Vector2 uv)
    {
        Texture2D texture = targetRenderer.material.mainTexture as Texture2D;

        if (texture == null)
        {
            Debug.LogError("The target Renderer's main texture is not a readable Texture2D. Cannot pick color.");
            return Color.clear;
        }

        // 픽셀 좌표 계산
        int pixelX = Mathf.FloorToInt(uv.x * texture.width);
        int pixelY = Mathf.FloorToInt(uv.y * texture.height);

        // 경계 검사
        if (pixelX < 0 || pixelX >= texture.width || pixelY < 0 || pixelY >= texture.height)
        {
            return Color.clear;
        }

        // 색상 읽기
        return texture.GetPixel(pixelX, pixelY);
    }

}