using UnityEngine;
using UnityEngine.UI;

/// <summary>Plays a grid sheet from left to right, starting at the top row.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class UpgradeAnimationRow : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Texture2D spriteSheet;
    [SerializeField, Min(1)] private int columns = 4;
    [SerializeField, Min(1)] private int rows = 8;
    [SerializeField, Min(1)] private int frameCount = 32;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;

    private double elapsed;
    private int displayedFrame = -1;

    private void OnEnable()
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();

        targetImage.texture = spriteSheet;
        targetImage.raycastTarget = false;
        elapsed = 0;
        displayedFrame = -1;
        ShowFrame(0);
    }

    private void Update()
    {
        int count = Mathf.Clamp(frameCount, 1, Mathf.Max(1, columns) * Mathf.Max(1, rows));
        double rate = Mathf.Max(1f, framesPerSecond);
        elapsed = (elapsed + Time.unscaledDeltaTime) % (count / rate);
        ShowFrame((int)(elapsed * rate));
    }

    private void ShowFrame(int frame)
    {
        if (targetImage == null || frame == displayedFrame)
            return;

        int gridWidth = Mathf.Max(1, columns);
        int gridHeight = Mathf.Max(1, rows);
        float width = 1f / gridWidth;
        float height = 1f / gridHeight;
        // Texture UVs start at the bottom; the supplied sheets start at the top.
        targetImage.uvRect = new Rect(
            (frame % gridWidth) * width,
            1f - ((frame / gridWidth) + 1) * height,
            width,
            height);
        displayedFrame = frame;
    }
}
