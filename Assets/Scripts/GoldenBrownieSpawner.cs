using UnityEngine;

public class GoldenBrownieSpawner : MonoBehaviour
{
    [SerializeField] GoldenBrownie prefab;
    [SerializeField] RectTransform spawnArea;
    [SerializeField] float minInterval = 60f, maxInterval = 180f;
    [SerializeField] float edgeMargin = 80f;

    float timer;

    void Start() => timer = Random.Range(minInterval, maxInterval);

    void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        Spawn();
        timer = Random.Range(minInterval, maxInterval);
    }

    [ContextMenu("Spawn Now")] // right-click the component in Play mode to test
    void Spawn()
    {
        var b = Instantiate(prefab, spawnArea);
        Rect r = spawnArea.rect;
        ((RectTransform)b.transform).localPosition = new Vector3(
            Random.Range(r.xMin + edgeMargin, r.xMax - edgeMargin),
            Random.Range(r.yMin + edgeMargin, r.yMax - edgeMargin), 0f);
    }
}