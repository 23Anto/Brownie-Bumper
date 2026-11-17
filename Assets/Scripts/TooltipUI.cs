using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    [SerializeField] TMP_Text text;
    [SerializeField] float gap = 12f;   // space between button and tooltip

    RectTransform rt, target;
    CanvasGroup group;
    Func<string> source;
    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        Instance = this;
        rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);  // guard against stretched anchors
        rt.pivot = new Vector2(1f, 0.5f);
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(RectTransform anchor, Func<string> getText)
    {
        target = anchor;
        source = getText;
        group.alpha = 1f;
        Refresh();
    }

    public void Hide()
    {
        target = null;
        source = null;
        group.alpha = 0f;
    }

    void LateUpdate() { if (target != null) Refresh(); }   // live-updating numbers

    void Refresh()
    {
        text.text = source();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);   // resize to the new text now

        target.GetWorldCorners(corners);
        Vector3 leftMiddle = (corners[0] + corners[1]) * 0.5f;
        rt.position = leftMiddle + Vector3.left * gap;
    }
}