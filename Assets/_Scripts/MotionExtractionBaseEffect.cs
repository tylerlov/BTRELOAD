using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class MotionExtractionBaseEffect : MonoBehaviour
{
    public RenderTexture sourceRenderTexture;
    public RenderTexture delayedRenderTexture;

    public Vector2 resolution = new Vector2(960, 540);

    public int framesOfDelay;
    public Image image;

    public int gradientResolution;
    public Gradient mainGrad;
    public string mainGradTexturePath;

    public Gradient altGrad;
    public string altGradTexturePath;

    public Camera mainCamera;
    public float fadeOutSpeed;
    public float fadeInSpeed;
    public float fadeOutAlpha = 0.1f;

    private Quaternion lastFrameRotation;
    private Vector3 lastFramePosition;
    private float alpha = 1.0f;

    private Tween alphaTween;

    Texture2D[] textures;

    int currentFrameIndex;

    private void Start()
    {
        textures = new Texture2D[framesOfDelay];
        currentFrameIndex = 0;
        for (int i = 0; i < framesOfDelay; i++)
        {
            textures[i] = new Texture2D(
                (int)resolution.x,
                (int)resolution.y,
                TextureFormat.ARGB32,
                false,
                true
            );
            textures[i].filterMode = FilterMode.Point;
            textures[i].Apply();
        }

        ApplyShaderParams();

        lastFramePosition = mainCamera.transform.position;
        lastFrameRotation = mainCamera.transform.rotation;
    }

    private void ApplyShaderParams()
    {
        GenerateTextures();
    }

    [ContextMenu("Generate Textures")]
    private void GenerateTextures()
    {
        GenerateTexture(mainGrad, mainGradTexturePath);
        GenerateTexture(altGrad, altGradTexturePath);
    }

    private void GenerateTexture(Gradient grad, string path)
    {
        Texture2D tex = new Texture2D(gradientResolution, 1, TextureFormat.ARGB32, false, true);
        tex.filterMode = FilterMode.Point;

        Color[] colors = new Color[gradientResolution];

        for (int i = 0; i < gradientResolution; i++)
        {
            float t = (float)i / gradientResolution;
            colors[i] = grad.Evaluate(t);
        }

        tex.SetPixels(colors);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(Application.dataPath + path, bytes);
    }

    private void Update()
    {
        Graphics.Blit(textures[currentFrameIndex], delayedRenderTexture);

        CaptureFrame();

        currentFrameIndex++;
        if (currentFrameIndex >= framesOfDelay)
        {
            currentFrameIndex = 0;
        }

        bool isMoving = Vector3.Distance(mainCamera.transform.position, lastFramePosition) > 0.01f;
        bool isRotating = mainCamera.transform.rotation != lastFrameRotation;

        bool shouldFade = isMoving || isRotating;

        // Stop any existing alpha tween
        alphaTween.Stop();

        // Create new alpha tween
        alphaTween = Tween.Custom(
            alpha,
            shouldFade ? fadeOutAlpha : 1f,
            shouldFade ? fadeOutSpeed : fadeInSpeed,
            value => {
                alpha = value;
                image.material.SetFloat("_AlphaMultiplier", value);
            },
            Ease.OutSine
        );

        lastFramePosition = mainCamera.transform.position;
        lastFrameRotation = mainCamera.transform.rotation;
    }

    private void CaptureFrame()
    {
        RenderTexture.active = sourceRenderTexture;
        Texture2D texture2D = textures[currentFrameIndex];
        texture2D.ReadPixels(new Rect(0, 0, (int)resolution.x, (int)resolution.y), 0, 0);
        texture2D.Apply();
    }

    private void OnDestroy()
    {
        alphaTween.Stop();
    }
}
