using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows each banner after its first purchase, including loaded saves.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(VerticalLayoutGroup))]
public sealed class MiddleColumnAnimations : MonoBehaviour
{
    [Serializable]
    private sealed class UpgradeBanner
    {
        public int upgradeIndex;
        public GameObject banner;
        public LayoutElement layout;
    }

    [SerializeField] private GameManager gameManager;
    [SerializeField] private UpgradeBanner[] banners = new UpgradeBanner[0];

    private RectTransform content;
    private VerticalLayoutGroup layoutGroup;
    private float previousWidth = -1f;

    private void OnEnable()
    {
        content = (RectTransform)transform;
        layoutGroup = GetComponent<VerticalLayoutGroup>();
        previousWidth = -1f;
    }

    private void Start()
    {
        // All Awake calls (including GameManager.Load) have finished by Start.
        if (Application.isPlaying)
            RefreshVisibility();
    }

    private void LateUpdate()
    {
        ResizeBanners();
        if (Application.isPlaying)
            RefreshVisibility();
    }

    private void ResizeBanners()
    {
        if (content == null || layoutGroup == null)
            return;

        float width = Mathf.Max(0f, content.rect.width - layoutGroup.padding.horizontal);
        if (width <= 0f || Mathf.Abs(width - previousWidth) < 0.01f)
            return;

        previousWidth = width;
        float height = width * (175f / 768f);
        foreach (UpgradeBanner entry in banners)
        {
            if (entry != null && entry.layout != null)
                entry.layout.preferredHeight = height;
        }
    }

    private void RefreshVisibility()
    {
        GameManager manager = gameManager != null ? gameManager : GameManager.Instance;
        if (manager == null)
            return;

        // Four inexpensive checks also cover Load/reset paths that do not emit
        // OnStateChanged. Existing animations are never restarted by later buys.
        foreach (UpgradeBanner entry in banners)
        {
            if (entry == null || entry.banner == null)
                continue;

            bool unlocked = entry.upgradeIndex >= 0
                && entry.upgradeIndex < manager.upgrades.Count
                && manager.upgrades[entry.upgradeIndex].level >= 1;

            if (entry.banner.activeSelf != unlocked)
                entry.banner.SetActive(unlocked);
        }
    }
}
