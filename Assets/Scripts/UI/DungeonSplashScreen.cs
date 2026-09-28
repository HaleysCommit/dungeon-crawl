using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DungeonSplashScreen : MonoBehaviour
{
    [Header("Artwork")]
    [SerializeField] private Sprite splashArtwork;

    [Header("Button")]
    [SerializeField] private Font buttonFont;
    [SerializeField] private Sprite playButtonSprite;

    [Header("Destination")]
    [SerializeField] private string gameSceneName = "Dungeon";

    [Header("Optional")]
    [SerializeField] private bool allowSpacebar = true;

    private bool isLoading;

    private void Awake()
    {
        BuildSplashScreen();
    }

    private void Update()
    {
        if (!allowSpacebar || isLoading)
            return;

        bool spacePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif

        if (spacePressed)
            StartGame();
    }

    private void BuildSplashScreen()
    {
        if (splashArtwork == null)
        {
            Debug.LogError(
                "DungeonSplashScreen: Assign the splash artwork in the Inspector."
            );

            return;
        }

        // ------------------------------------------------------------
        // CANVAS
        // ------------------------------------------------------------

        GameObject canvasObject = new GameObject(
            "Splash Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1641f, 958f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // ------------------------------------------------------------
        // ARTWORK CONTAINER
        //
        // Actual exported artwork is 1641 x 958. EnvelopeParent fills
        // widescreen displays and crops only the excess top/bottom
        // instead of stretching the artwork.
        // ------------------------------------------------------------

        GameObject artworkObject = new GameObject(
            "Splash Artwork",
            typeof(RectTransform),
            typeof(Image),
            typeof(AspectRatioFitter)
        );

        artworkObject.transform.SetParent(canvasObject.transform, false);

        RectTransform artworkRect =
            artworkObject.GetComponent<RectTransform>();

        artworkRect.anchorMin = new Vector2(0.5f, 0.5f);
        artworkRect.anchorMax = new Vector2(0.5f, 0.5f);
        artworkRect.pivot = new Vector2(0.5f, 0.5f);
        artworkRect.anchoredPosition = Vector2.zero;

        Image artworkImage = artworkObject.GetComponent<Image>();
        artworkImage.sprite = splashArtwork;
        artworkImage.raycastTarget = false;

        AspectRatioFitter fitter =
            artworkObject.GetComponent<AspectRatioFitter>();

        fitter.aspectMode =
            AspectRatioFitter.AspectMode.EnvelopeParent;

        fitter.aspectRatio = 1641f / 958f;

        // ------------------------------------------------------------
        // INVISIBLE BUTTON
        //
        // These coordinates line up with the PLAY button painted into
        // the artwork, expressed as fractions of its original 1536 x 1024
        // generation canvas (resolution-independent - same visual layout
        // regardless of the final exported pixel size).
        // ------------------------------------------------------------

        GameObject buttonObject = new GameObject(
            "Play Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(artworkObject.transform, false);

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>();

        /*
         * Generated image PLAY button is approximately:
         *
         * x: 593 -> 1001
         * y: 699 -> 816 (image coordinates from top)
         *
         * Converted to normalized Unity bottom-left coordinates.
         */

        buttonRect.anchorMin = new Vector2(
            593f / 1536f,
            1f - (816f / 1024f)
        );

        buttonRect.anchorMax = new Vector2(
            1001f / 1536f,
            1f - (699f / 1024f)
        );

        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // Dark rounded background (#171717) that animates to amber/gold on hover/press -
        // only used as a fallback when no playButtonSprite artwork is assigned.
        Image buttonBackground = buttonObject.GetComponent<Image>();
        Button playButton = buttonObject.GetComponent<Button>();
        playButton.targetGraphic = buttonBackground;
        playButton.transition = Selectable.Transition.None;
        playButton.onClick.AddListener(StartGame);

        if (playButtonSprite != null)
        {
            // Use the painted "PLAY" artwork directly - cleaner than a generic box + Text label.
            buttonBackground.sprite = playButtonSprite;
            buttonBackground.type = Image.Type.Simple;
            buttonBackground.preserveAspect = true;
            buttonBackground.color = Color.white;
            buttonBackground.raycastTarget = true;

            buttonObject.AddComponent<PlayButtonColorHover>().Initialize(
                buttonBackground, Color.white, new Color32(0xF5, 0xA9, 0x3B, 0xFF));
        }
        else
        {
            buttonBackground.sprite = CreateRoundedRectSprite(64, 32, 12);
            buttonBackground.type = Image.Type.Sliced;
            buttonBackground.color = new Color32(0x17, 0x17, 0x17, 0xFF);
            buttonBackground.raycastTarget = true;

            GameObject buttonTextGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            buttonTextGO.transform.SetParent(buttonObject.transform, false);
            RectTransform buttonTextRT = buttonTextGO.GetComponent<RectTransform>();
            buttonTextRT.anchorMin = Vector2.zero;
            buttonTextRT.anchorMax = Vector2.one;
            buttonTextRT.offsetMin = Vector2.zero;
            buttonTextRT.offsetMax = Vector2.zero;

            Text buttonText = buttonTextGO.GetComponent<Text>();
            buttonText.text = "PLAY";
            buttonText.font = buttonFont != null ? buttonFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 28;
            buttonText.fontStyle = FontStyle.Normal;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.raycastTarget = false;

            buttonObject.AddComponent<PlayButtonColorHover>().Initialize(
                buttonBackground, new Color32(0x17, 0x17, 0x17, 0xFF), new Color32(0xF5, 0xA9, 0x3B, 0xFF));
        }

        EnsureEventSystem();
    }

    private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[width * height];
        Color32 opaque = Color.white;
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = -1f, cy = -1f;
                if (x < radius && y < radius) { cx = radius; cy = radius; }
                else if (x >= width - radius && y < radius) { cx = width - radius - 1; cy = radius; }
                else if (x < radius && y >= height - radius) { cx = radius; cy = height - radius - 1; }
                else if (x >= width - radius && y >= height - radius) { cx = width - radius - 1; cy = height - radius - 1; }

                bool inside = true;
                if (cx >= 0f)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    inside = (dx * dx + dy * dy) <= radius * radius;
                }

                pixels[y * width + x] = inside ? opaque : clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject =
            new GameObject("EventSystem");

        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<
            UnityEngine.InputSystem.UI.InputSystemUIInputModule
        >();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    public void StartGame()
    {
        if (isLoading)
            return;

        isLoading = true;

        Debug.Log("Descending into dungeon...");

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "DungeonSplashScreen: No game scene name configured."
            );

            isLoading = false;
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}

/// <summary>
/// Animates the PLAY button's whole background between two colors on hover/unhover, instead of
/// the earlier expanding-blob effect.
/// </summary>
internal class PlayButtonColorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image target;
    private Color restColor;
    private Color hoverColor;
    private Coroutine activeRoutine;

    public void Initialize(Image targetImage, Color rest, Color hover)
    {
        target = targetImage;
        restColor = rest;
        hoverColor = hover;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Restart(AnimateTo(hoverColor, 0.25f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Restart(AnimateTo(restColor, 0.25f));
    }

    private void Restart(IEnumerator routine)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(routine);
    }

    private IEnumerator AnimateTo(Color targetColor, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Color start = target.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.color = Color.Lerp(start, targetColor, t);
            yield return null;
        }

        target.color = targetColor;
    }
}
