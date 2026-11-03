using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrownieBumper.Cafe
{
    [Serializable] public class CafeRegion
    {
        public string name;
        public int x, y, width, height;
    }
    [Serializable] public sealed class CafeBobRegion : CafeRegion
    {
        public float amplitude, period, phase;
    }
    [Serializable] public sealed class CafeSceneConfig
    {
        public int referenceWidth, referenceHeight, framesPerSecond;
        public float loopSeconds;
        public CafeRegion steam;
        public CafeBobRegion[] marshmallows;
        public CafeRegion[] eyes;
        public int rainCell, rainCount, seed;
    }

    [DisallowMultipleComponent]
    public sealed class CafeBackgroundAnimator : MonoBehaviour
    {
        [Header("Motion")]
        public bool fallingRain = true;
        public bool wavingSteam = true;
        public bool bobbingMarshmallows = true;
        public bool brownieBlinking = true;
        [Range(0f, 3f)] public float animationSpeed = 1f;
        [Range(0f, 2f)] public float bobStrength = 1f;
        [Range(0f, 1f)] public float rainOpacity = 1f;

        [Header("Configured by Tools > Brownie Cafe")]
        [SerializeField] private RawImage artwork;
        [SerializeField] private TextAsset configuration;
        [SerializeField] private Texture2D cleanPlate, steamSource, rainMask;
        [SerializeField] private Texture2D[] marshmallowTextures, halfEyeTextures, closedEyeTextures;

        private CafeSceneConfig config;
        private RawImage rainGraphic, steamGraphic;
        private RawImage[] marshmallowGraphics, eyeGraphics;
        private Texture2D rainTexture, steamTexture;
        private Color32[] rainPixels, maskPixels, steamPixels, warpedSteamPixels;
        private Drop[] drops;
        private int rainWidth, rainHeight, lastFrame = -1;
        private float clock;
        private bool initialized;

        private struct Drop { public int x, y, cycles, length; public byte alpha; }

        public void Configure(RawImage plate, TextAsset settings, Texture2D background,
            Texture2D steam, Texture2D mask, Texture2D[] marshmallows,
            Texture2D[] halfEyes, Texture2D[] closedEyes)
        {
            artwork = plate; configuration = settings; cleanPlate = background;
            steamSource = steam; rainMask = mask; marshmallowTextures = marshmallows;
            halfEyeTextures = halfEyes; closedEyeTextures = closedEyes;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && !initialized) Initialize();
        }

        private void Initialize()
        {
            if (artwork == null || configuration == null || cleanPlate == null ||
                steamSource == null || rainMask == null)
            {
                Debug.LogError("Brownie Cafe: use Tools > Brownie Cafe > Add Animated Background to configure the assets.", this);
                return;
            }
            config = JsonUtility.FromJson<CafeSceneConfig>(configuration.text);
            if (config == null || config.referenceWidth <= 0 || config.framesPerSecond <= 0 ||
                config.loopSeconds <= 0 || config.steam == null || config.eyes == null ||
                config.marshmallows == null || marshmallowTextures == null ||
                halfEyeTextures == null || closedEyeTextures == null ||
                marshmallowTextures.Length != config.marshmallows.Length ||
                halfEyeTextures.Length != config.eyes.Length || closedEyeTextures.Length != config.eyes.Length)
            {
                Debug.LogError("Brownie Cafe: the configuration or texture arrays are incomplete.", this);
                return;
            }
            try
            {
                maskPixels = rainMask.GetPixels32();
                steamPixels = steamSource.GetPixels32();
            }
            catch (UnityException exception)
            {
                Debug.LogError("Brownie Cafe: reimport the Art folder to enable Read/Write on Steam and RainMask. " + exception.Message, this);
                return;
            }

            artwork.texture = cleanPlate;
            rainWidth = rainMask.width; rainHeight = rainMask.height;
            rainPixels = new Color32[rainWidth * rainHeight];
            rainTexture = NewTexture(rainWidth, rainHeight, "Cafe runtime rain");
            rainGraphic = CreateLayer("Rain behind window", rainTexture, null);
            warpedSteamPixels = new Color32[steamPixels.Length];
            steamTexture = NewTexture(steamSource.width, steamSource.height, "Cafe runtime steam");
            steamGraphic = CreateLayer("Waving steam", steamTexture, config.steam);
            marshmallowGraphics = new RawImage[config.marshmallows.Length];
            for (int i = 0; i < marshmallowGraphics.Length; i++)
                marshmallowGraphics[i] = CreateLayer(config.marshmallows[i].name, marshmallowTextures[i], config.marshmallows[i]);
            eyeGraphics = new RawImage[config.eyes.Length];
            for (int i = 0; i < eyeGraphics.Length; i++)
                eyeGraphics[i] = CreateLayer(config.eyes[i].name + " blink", closedEyeTextures[i], config.eyes[i]);

            uint random = (uint)config.seed;
            drops = new Drop[config.rainCount];
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = new Drop {
                    x = 6 + (int)(NextRandom(ref random) * 180),
                    y = (int)(NextRandom(ref random) * rainHeight),
                    cycles = 5 + (int)(NextRandom(ref random) * 3),
                    length = 3 + (int)(NextRandom(ref random) * 5),
                    alpha = (byte)(65 + (int)(NextRandom(ref random) * 55))
                };
            }
            initialized = true;
            ApplyFrame(0f);
        }

        private static double NextRandom(ref uint state)
        {
            unchecked { state = state * 1664525u + 1013904223u; }
            return state / 4294967296.0;
        }

        private static Texture2D NewTexture(int width, int height, string label)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false) {
                name = label, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
        }

        private RawImage CreateLayer(string label, Texture texture, CafeRegion region)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(artwork.transform, false);
            var image = go.GetComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            var rect = image.rectTransform;
            if (region == null) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; }
            else
            {
                rect.anchorMin = new Vector2(region.x / (float)config.referenceWidth,
                    1f - (region.y + region.height) / (float)config.referenceHeight);
                rect.anchorMax = new Vector2((region.x + region.width) / (float)config.referenceWidth,
                    1f - region.y / (float)config.referenceHeight);
            }
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        private void Update()
        {
            if (!initialized) return;
            clock = Mathf.Repeat(clock + Time.unscaledDeltaTime * Mathf.Max(0f, animationSpeed), config.loopSeconds);
            int frame = Mathf.FloorToInt(clock * config.framesPerSecond);
            rainGraphic.enabled = fallingRain;
            if (frame == lastFrame) return;
            lastFrame = frame;
            ApplyFrame(frame / (float)config.framesPerSecond);
        }

        private void ApplyFrame(float time)
        {
            DrawRain(time);
            DrawSteam(wavingSteam ? time : 0f);
            float scale = artwork.rectTransform.rect.height / config.referenceHeight;
            for (int i = 0; i < marshmallowGraphics.Length; i++)
            {
                var r = config.marshmallows[i];
                float offset = bobbingMarshmallows ? Mathf.Round(Mathf.Sin(time * Mathf.PI * 2f / r.period + r.phase) * r.amplitude * bobStrength / 2f) * 2f : 0f;
                marshmallowGraphics[i].rectTransform.anchoredPosition = new Vector2(0f, offset * scale);
            }
            int state = brownieBlinking ? BlinkState(time) : 0;
            for (int i = 0; i < eyeGraphics.Length; i++)
            {
                eyeGraphics[i].enabled = state != 0;
                if (state != 0) eyeGraphics[i].texture = state == 1 ? halfEyeTextures[i] : closedEyeTextures[i];
            }
        }

        private static int BlinkState(float time)
        {
            int state = BlinkAt(time, 3.6f);
            if (state == 0) state = BlinkAt(time, 8.7f);
            if (state == 0) state = BlinkAt(time, 9.08f);
            return state;
        }
        private static int BlinkAt(float time, float start)
        {
            float d = time - start;
            if (d >= 0f && d < .07f) return 1;
            if (d >= .07f && d < .17f) return 2;
            if (d >= .17f && d < .24f) return 1;
            return 0;
        }

        private void DrawSteam(float time)
        {
            Array.Clear(warpedSteamPixels, 0, warpedSteamPixels.Length);
            int width = steamSource.width, height = steamSource.height;
            for (int y = 0; y < height; y++)
            {
                float weight = 1f - y / (float)(height - 1);
                int shift = Mathf.RoundToInt(Mathf.Sin(time * Mathf.PI * 2f / 4f + y * .045f) * 7f * weight / 2f) * 2;
                int row = (height - 1 - y) * width;
                for (int x = 0; x < width; x++)
                {
                    int destinationX = x + shift;
                    if (destinationX >= 0 && destinationX < width)
                        warpedSteamPixels[row + destinationX] = steamPixels[row + x];
                }
            }
            steamTexture.SetPixels32(warpedSteamPixels);
            steamTexture.Apply(false, false);
        }

        private void DrawRain(float time)
        {
            rainGraphic.enabled = fallingRain;
            if (!fallingRain) return;
            Array.Clear(rainPixels, 0, rainPixels.Length);
            foreach (var drop in drops)
            {
                int top = Mathf.FloorToInt(Mathf.Repeat(drop.y + time * rainHeight * drop.cycles / config.loopSeconds, rainHeight));
                for (int k = 0; k < drop.length; k++)
                {
                    int x = drop.x + k / 4, y = (top + k) % rainHeight;
                    if (x < 0 || x >= rainWidth) continue;
                    int index = (rainHeight - 1 - y) * rainWidth + x;
                    if (maskPixels[index].a > 127)
                        rainPixels[index] = new Color32(125, 182, 214, (byte)(drop.alpha * rainOpacity));
                }
            }
            rainTexture.SetPixels32(rainPixels);
            rainTexture.Apply(false, false);
        }

        private void OnValidate() { lastFrame = -1; }
        private void OnDestroy()
        {
            if (rainTexture != null) Destroy(rainTexture);
            if (steamTexture != null) Destroy(steamTexture);
        }
    }
}
