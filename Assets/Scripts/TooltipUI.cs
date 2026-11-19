using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] TMP_Text text;
    [SerializeField] Image board;
    [SerializeField, Min(80f)] float panelWidth = 270f;
    [SerializeField] float gap = 12f;

    const float SidePadding = 20f;
    const float TopPadding = 18f;
    const float BottomPadding = 24f;
    const float ScreenPadding = 8f;

    RectTransform rt, container, target;
    CanvasGroup group;
    Func<string> source;
    string measuredText;
    Vector2 panelSize;
    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        rt = (RectTransform)transform;
        container = rt.parent as RectTransform;
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
        if (text == null) text = GetComponentInChildren<TMP_Text>(true);
        if (container == null || text == null)
        {
            enabled = false;
            return;
        }

        Instance = this;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);

        // The tooltip owns its size, so the old layout cannot move the text.
        if (TryGetComponent(out ContentSizeFitter fitter)) fitter.enabled = false;
        if (TryGetComponent(out LayoutGroup layout)) layout.enabled = false;

        if (board == null)
        {
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent(out Image candidate) || candidate.sprite == null)
                    continue;
                board = candidate;
                break;
            }
        }

        if (board != null && board.rectTransform != rt)
        {
            if (TryGetComponent(out Image oldBackground)) oldBackground.enabled = false;
            Stretch(board.rectTransform, Vector2.zero, Vector2.zero);
            board.rectTransform.SetAsFirstSibling();
            board.raycastTarget = false;
            board.preserveAspect = false;
            // Keep the frame corners at a consistent size as the board grows.
            if (board.sprite != null && board.sprite.border != Vector4.zero)
            {
                board.type = Image.Type.Sliced;
                board.pixelsPerUnitMultiplier = 8f;
            }
        }

        text.rectTransform.SetAsLastSibling();
        Stretch(text.rectTransform, new Vector2(SidePadding, BottomPadding),
            new Vector2(-SidePadding, -TopPadding));
        text.raycastTarget = false;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(RectTransform anchor, Func<string> getText)
    {
        target = anchor;
        source = getText;
        Refresh();
    }

    public void Hide()
    {
        target = null;
        source = null;
        group.alpha = 0f;
    }

    void LateUpdate()
    {
        if (source != null) Refresh();
    }

    void Refresh()
    {
        if (target == null || source == null)
        {
            Hide();
            return;
        }

        string message = source() ?? string.Empty;
        if (message != measuredText)
        {
            text.text = message;
            float width = Mathf.Max(panelWidth, SidePadding * 2f + 1f);
            float textHeight = text.GetPreferredValues(message, width - SidePadding * 2f, Mathf.Infinity).y;
            panelSize = new Vector2(width, Mathf.Ceil(textHeight) + TopPadding + BottomPadding);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelSize.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelSize.y);
            measuredText = message;
        }

        Rect bounds = container.rect;
        if (bounds.width <= 0f || bounds.height <= 0f)
        {
            group.alpha = 0f;
            return;
        }

        float inset = Mathf.Min(ScreenPadding, Mathf.Min(bounds.width, bounds.height) * 0.25f);
        float scale = Mathf.Min(1f, Mathf.Min(
            (bounds.width - 2f * inset) / panelSize.x, (bounds.height - 2f * inset) / panelSize.y));
        rt.localScale = new Vector3(scale, scale, 1f);
        Vector2 visibleSize = panelSize * scale;

        target.GetWorldCorners(corners);
        Vector3 left = container.InverseTransformPoint((corners[0] + corners[1]) * 0.5f);
        Vector3 right = container.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);
        float xMin = bounds.xMin + inset;
        float xMax = bounds.xMax - inset;
        float x = left.x - gap;
        if (x - visibleSize.x < xMin && right.x + gap + visibleSize.x <= xMax)
            x = right.x + gap + visibleSize.x;

        // Position the right-middle pivot while keeping the whole box on screen.
        rt.anchoredPosition = new Vector2(
            Mathf.Clamp(x, xMin + visibleSize.x, xMax),
            Mathf.Clamp(left.y, bounds.yMin + inset + visibleSize.y * 0.5f,
                bounds.yMax - inset - visibleSize.y * 0.5f)) - bounds.center;
        group.alpha = 1f;
    }
}
