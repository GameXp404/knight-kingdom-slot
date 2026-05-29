using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindFirstObjectByType<GameBootstrap>() != null) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects()) DestroyImmediate(root);
        var go = new GameObject("[KnightKingdomSlot]");
        go.AddComponent<GameBootstrap>();
    }

    public Color bgColor = new Color(0.10f, 0.06f, 0.04f);
    public Color frameColor = new Color(0.92f, 0.75f, 0.20f);
    public Color panelColor = new Color(0.18f, 0.10f, 0.08f, 0.96f);
    public Color reelBgColor = new Color(0.20f, 0.12f, 0.10f);
    public Color accentRed = new Color(0.85f, 0.15f, 0.15f);
    public Color accentGold = new Color(1.00f, 0.85f, 0.30f);
    public Color buttonColor = new Color(0.55f, 0.20f, 0.10f);

    public int reelCount = 5;
    public int rowsVisible = 4;
    public float symbolSize = 155f;
    public float reelSpacing = 12f;
    public float reelFramePadding = 28f;

    private Canvas mainCanvas;
    private RectTransform canvasRect;
    private GameObject menuRoot;
    private GameObject gameRoot;
    private GameObject settingsObj;
    private GameObject historyObj;
    private GameObject achievementObj;
    private GameObject paytableObj;
    private GameObject paylinesObj;
    private GameManager gameManager;
    private UIController uiController;
    private MainMenuController menuController;
    private Button payButton;
    private List<ReelController> reelControllers = new List<ReelController>();

    void Awake()
    {
        SetupCamera(); SetupEventSystem(); SetupCanvas(); SetupAudioManager();
        menuRoot = BuildMainMenu();
        gameRoot = BuildGameScene();
        gameRoot.SetActive(false);

        var gmGo = new GameObject("GameManager");
        gmGo.transform.SetParent(transform, false);
        gameManager = gmGo.AddComponent<GameManager>();
        var autoSpin = gmGo.AddComponent<AutoSpinController>();
        gameManager.autoSpin = autoSpin;
        gameManager.reels = reelControllers.ToArray();
        gameManager.ui = uiController;

        settingsObj = BuildSettingsPanel();
        historyObj = BuildHistoryPanel();
        achievementObj = BuildAchievementPanel();
        paytableObj = BuildPaytablePanel();
        paylinesObj = BuildPaylinesPanel();
        gameManager.winPopup = BuildWinPopup();
        gameManager.coinParticles = BuildCoinFx();

        var toast = BuildAchievementToast();
        uiController.achievementToastText = toast.text;
        uiController.achievementToastGroup = toast.group;

        menuController.gameRoot = gameRoot;
        menuController.settingsPanelObject = settingsObj;

        WireGameButtons();
        BuildScreenFlashLayer();
        gameRoot.AddComponent<ScreenShake>();
    }

    private void BuildPromoPopup()
    {
        var container = new GameObject("PromoPopup", typeof(RectTransform), typeof(CanvasGroup));
        container.transform.SetParent(canvasRect, false);
        var crt = container.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0, 0);
        crt.pivot = new Vector2(0, 0);
        crt.sizeDelta = new Vector2(360, 220);
        var cg = container.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image), typeof(Button));
        bgGo.transform.SetParent(container.transform, false);
        var brt = bgGo.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.sprite = GetUnifiedButtonSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.95f);
        var bgBtn = bgGo.GetComponent<Button>();

        var promoSprite = Resources.Load<Sprite>("UI/Promo_Banner");
        if (promoSprite != null) {
            var imgGo = new GameObject("PromoImage", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(container.transform, false);
            var irt = imgGo.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(8, 8); irt.offsetMax = new Vector2(-8, -8);
            var i = imgGo.GetComponent<Image>();
            i.sprite = promoSprite;
            i.preserveAspect = true;
            i.raycastTarget = false;
        } else {
            var title = MakeText(container.transform, "🎰 PROMO!", 26, accentGold, TMPro.TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1); title.rectTransform.anchoredPosition = new Vector2(0, -8);
            title.rectTransform.sizeDelta = new Vector2(0, 36);
            title.fontStyle = TMPro.FontStyles.Bold; ApplyGoldGradient(title); title.raycastTarget = false;

            var line1 = MakeText(container.transform, "3 SCATTER = <b>10</b> FREE SPINS", 18, Color.white, TMPro.TextAlignmentOptions.Center);
            line1.rectTransform.anchoredPosition = new Vector2(0, 30);
            line1.rectTransform.sizeDelta = new Vector2(340, 28);
            ApplyGoldGradient(line1); line1.raycastTarget = false;

            var line2 = MakeText(container.transform, "4 SCATTER = <b>50</b> FREE SPINS", 18, Color.white, TMPro.TextAlignmentOptions.Center);
            line2.rectTransform.anchoredPosition = new Vector2(0, 0);
            line2.rectTransform.sizeDelta = new Vector2(340, 28);
            ApplyGoldGradient(line2); line2.raycastTarget = false;

            var line3 = MakeText(container.transform, "5 SCATTER = <b>150</b> FREE SPINS", 18, Color.white, TMPro.TextAlignmentOptions.Center);
            line3.rectTransform.anchoredPosition = new Vector2(0, -30);
            line3.rectTransform.sizeDelta = new Vector2(340, 28);
            ApplyGoldGradient(line3); line3.raycastTarget = false;

            var footer = MakeText(container.transform, "x3 MULTIPLIER!", 22, accentRed, TMPro.TextAlignmentOptions.Center);
            footer.rectTransform.anchorMin = new Vector2(0, 0); footer.rectTransform.anchorMax = new Vector2(1, 0);
            footer.rectTransform.pivot = new Vector2(0.5f, 0); footer.rectTransform.anchoredPosition = new Vector2(0, 12);
            footer.rectTransform.sizeDelta = new Vector2(0, 32);
            footer.fontStyle = TMPro.FontStyles.Bold; ApplyFireGradient(footer); footer.raycastTarget = false;
        }

        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(container.transform, false);
        var clRt = closeGo.GetComponent<RectTransform>();
        clRt.anchorMin = clRt.anchorMax = new Vector2(1, 1);
        clRt.pivot = new Vector2(1, 1); clRt.sizeDelta = new Vector2(36, 36);
        clRt.anchoredPosition = new Vector2(-4, -4);
        var clImg = closeGo.GetComponent<Image>();
        clImg.color = new Color(0.55f, 0.20f, 0.20f, 0.95f);
        var clBtn = closeGo.GetComponent<Button>();
        var clTxt = MakeText(closeGo.transform, "✕", 24, Color.white, TMPro.TextAlignmentOptions.Center);
        clTxt.rectTransform.anchorMin = Vector2.zero; clTxt.rectTransform.anchorMax = Vector2.one;
        clTxt.rectTransform.offsetMin = Vector2.zero; clTxt.rectTransform.offsetMax = Vector2.zero;
        clTxt.fontStyle = TMPro.FontStyles.Bold;

        var ctrl = container.AddComponent<PromoPopupController>();
        ctrl.container = crt;
        ctrl.canvasGroup = cg;
        ctrl.closeButton = clBtn;
        ctrl.bgButton = bgBtn;
        ctrl.firstDelay = 15f;
        ctrl.intervalSeconds = 300f;
    }

    private void SetupCamera() { if (Camera.main != null) return; var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bgColor; camGo.AddComponent<AudioListener>(); }
    private void SetupEventSystem() { if (FindFirstObjectByType<EventSystem>() != null) return; new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); }
    private void SetupCanvas() { var go = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); mainCanvas = go.GetComponent<Canvas>(); mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay; var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f; canvasRect = go.GetComponent<RectTransform>(); }
    private void SetupAudioManager() { new GameObject("AudioManager").AddComponent<AudioManager>(); }

    private GameObject BuildMainMenu()
    {
        var root = MakePanel(canvasRect, "MainMenu", Vector2.zero, new Vector2(1920, 1080), bgColor, true);
        var canvasGroup = root.AddComponent<CanvasGroup>();
        var bg = MakePanel(root.transform, "BG", Vector2.zero, new Vector2(1920, 1080), bgColor);
        var sceneSprite = Resources.Load<Sprite>("UI/scene_bg");
        var bgSprite = sceneSprite != null ? sceneSprite : Resources.Load<Sprite>("Symbols/BACKGROUND");
        if (bgSprite != null) { bg.GetComponent<Image>().sprite = bgSprite; bg.GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.55f, 1f); }
        var dim = MakeImage(bg.transform, "Dim", Vector2.zero, new Vector2(1920, 1080), new Color(0.05f, 0.02f, 0.03f, 0.65f));

        var customLogoSprite = Resources.Load<Sprite>("UI/Logo_KK");
        if (customLogoSprite != null) {
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            logoGo.transform.SetParent(root.transform, false);
            var lrt = logoGo.GetComponent<RectTransform>();
            lrt.sizeDelta = new Vector2(950, 534);
            lrt.anchoredPosition = new Vector2(0, 220);
            var lImg = logoGo.GetComponent<Image>();
            lImg.sprite = customLogoSprite;
            lImg.preserveAspect = true;
            var logoPulse = logoGo.AddComponent<PulseGlow>();
            logoPulse.highlightColor = new Color(1f, 0.85f, 0.4f); logoPulse.speed = 1.5f; logoPulse.scaleAmplitude = 0.025f;
        } else {
            var title = MakeText(root.transform, "KNIGHT KINGDOM", 160, accentGold, TextAlignmentOptions.Center);
            title.rectTransform.anchoredPosition = new Vector2(0, 280);
            title.rectTransform.sizeDelta = new Vector2(1700, 220);
            title.fontStyle = FontStyles.Bold;
            ApplyGoldGradient(title);
            title.outlineWidth = 0.30f;
            title.outlineColor = new Color32(20, 5, 0, 255);
        }

        var subtitle = MakeText(root.transform, "EMPEROR'S TREASURE", 50, accentRed, TextAlignmentOptions.Center);
        subtitle.rectTransform.anchoredPosition = new Vector2(0, -80);
        subtitle.rectTransform.sizeDelta = new Vector2(1600, 80);
        subtitle.fontStyle = FontStyles.Bold;
        ApplyFireGradient(subtitle);
        subtitle.outlineWidth = 0.25f;
        subtitle.outlineColor = new Color32(40, 5, 0, 255);

        var playBtn = MakeButton(root.transform, "PLAY", new Vector2(360, 130), Color.white);
        playBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        StyleAsGoldButton(playBtn);
        var playTxt = playBtn.GetComponentInChildren<TextMeshProUGUI>();
        playTxt.fontSize = 60; playTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(playTxt);
        var playPulse = playBtn.gameObject.AddComponent<PulseGlow>();
        playPulse.highlightColor = new Color(1f, 0.65f, 0.35f); playPulse.speed = 2.5f; playPulse.scaleAmplitude = 0.05f;

        var settingsBtn = MakeButton(root.transform, "SETTINGS", new Vector2(320, 95), Color.white);
        settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -310);
        StyleAsGoldButton(settingsBtn);
        var setTxt2 = settingsBtn.GetComponentInChildren<TextMeshProUGUI>();
        setTxt2.fontSize = 36; setTxt2.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(setTxt2);

        var quitBtn = MakeButton(root.transform, "QUIT", new Vector2(320, 95), Color.white);
        quitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -420);
        StyleAsGoldButton(quitBtn);
        var quitTxt = quitBtn.GetComponentInChildren<TextMeshProUGUI>();
        quitTxt.fontSize = 36; quitTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(quitTxt);

        var menuComp = root.AddComponent<MainMenuController>();
        menuComp.menuGroup = canvasGroup;
        menuComp.playButton = playBtn;
        menuComp.settingsButton = settingsBtn;
        menuComp.quitButton = quitBtn;
        menuController = menuComp;
        return root;
    }

    private GameObject BuildGameScene()
    {
        var root = MakePanel(canvasRect, "GameRoot", Vector2.zero, new Vector2(1920, 1080), bgColor, true);
        BuildBackground(root.transform);
        uiController = root.AddComponent<UIController>();
        BuildHeader(root.transform);
        BuildReelsArea(root.transform);
        BuildBottomControls(root.transform);
        return root;
    }

    private void BuildBackground(Transform parent)
    {
        var bg = MakePanel(parent, "BG", Vector2.zero, new Vector2(1920, 1080), new Color(0.05f, 0.03f, 0.04f, 1f));
        var bgImg = bg.GetComponent<Image>();

        var bgSprite = Resources.Load<Sprite>("UI/Logo_KK");
        if (bgSprite == null) bgSprite = Resources.Load<Sprite>("UI/scene_bg");
        if (bgSprite == null) bgSprite = Resources.Load<Sprite>("Symbols/BACKGROUND");
        if (bgSprite != null) {
            bgImg.sprite = bgSprite;
            bgImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            bgImg.preserveAspect = false;
        }
        var dim = MakeImage(bg.transform, "Dim", Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, 0.35f));
    }

    private void SetupSideDragon(Transform parent, string name, string videoFile, string fallbackSprite, Vector2 pos, Vector2 size)
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFile);
        bool hasVideo = System.IO.File.Exists(videoPath);

        if (hasVideo) {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var rawImg = go.GetComponent<RawImage>();
            rawImg.raycastTarget = false;
            var renderTex = new RenderTexture(540, 960, 0, RenderTextureFormat.ARGB32);
            renderTex.Create();
            rawImg.texture = renderTex;

            var lumShader = Shader.Find("UI/LuminanceAlpha");
            if (lumShader != null) {
                var mat = new Material(lumShader);
                mat.SetFloat("_AlphaPower", 1.2f);
                mat.SetFloat("_AlphaScale", 1.6f);
                rawImg.material = mat;
            }

            var vp = go.AddComponent<UnityEngine.Video.VideoPlayer>();
            vp.source = UnityEngine.Video.VideoSource.Url;
            vp.url = videoPath;
            vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            vp.targetTexture = renderTex;
            vp.isLooping = true;
            vp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
            vp.playOnAwake = true;
            vp.Play();
        } else {
            var sprite = Resources.Load<Sprite>(fallbackSprite);
            if (sprite == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(1f, 1f, 1f, 0.85f);
            img.preserveAspect = true;
        }
    }

    private void BuildHeader(Transform parent)
    {
        var header = MakePanel(parent, "Header", new Vector2(0, 470), new Vector2(1900, 90), new Color(0f, 0f, 0f, 0f));


        var jackpot = MakeText(header.transform, "", 1, Color.clear, TextAlignmentOptions.Center);
        jackpot.rectTransform.anchoredPosition = new Vector2(-9999, -9999);
        jackpot.rectTransform.sizeDelta = new Vector2(1, 1);
        jackpot.gameObject.SetActive(false);
        uiController.jackpotText = jackpot;

        var settingsBtn = MakeButton(header.transform, "SET", new Vector2(110, 80), Color.white);
        settingsBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(880, 0);
        StyleAsGoldButton(settingsBtn);
        var setTxt = settingsBtn.GetComponentInChildren<TextMeshProUGUI>();
        setTxt.fontSize = 30; setTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(setTxt);
        uiController.settingsButton = settingsBtn;

        var infoBtn = MakeButton(header.transform, "INFO", new Vector2(110, 80), Color.white);
        infoBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(760, 0);
        StyleAsGoldButton(infoBtn);
        var infoTxt = infoBtn.GetComponentInChildren<TextMeshProUGUI>();
        infoTxt.fontSize = 30; infoTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(infoTxt);
        infoBtn.onClick.AddListener(() => { if (paylinesObj) paylinesObj.SetActive(true); });

        var freeSpinTxt = MakeText(header.transform, "", 36, accentGold, TextAlignmentOptions.Center);
        freeSpinTxt.rectTransform.anchoredPosition = new Vector2(0, -75);
        freeSpinTxt.rectTransform.sizeDelta = new Vector2(700, 50);
        freeSpinTxt.fontStyle = FontStyles.Bold;
        ApplyFireGradient(freeSpinTxt);
        freeSpinTxt.outlineWidth = 0.20f;
        freeSpinTxt.outlineColor = new Color32(40, 5, 0, 255);
        uiController.freeSpinText = freeSpinTxt;
    }

    private void BuildReelsArea(Transform parent)
    {
        float totalReelW = reelCount * symbolSize + (reelCount - 1) * reelSpacing;
        float reelH = rowsVisible * symbolSize;
        float frameW = totalReelW + reelFramePadding * 2;
        float frameH = reelH + reelFramePadding * 2;

        var frameGlow = MakePanel(parent, "ReelGlow", new Vector2(0, 30), new Vector2(frameW + 30, frameH + 30), new Color(0f, 0f, 0f, 0f));
        var frame = MakePanel(parent, "ReelFrame", new Vector2(0, 30), new Vector2(frameW, frameH), new Color(0f, 0f, 0f, 0f));
        var inner = MakePanel(frame.transform, "Inner", Vector2.zero, new Vector2(totalReelW + 8, reelH + 8), new Color(0f, 0f, 0f, 0f));

        var reelBorderGo = new GameObject("ReelBorder", typeof(RectTransform), typeof(Image));
        reelBorderGo.transform.SetParent(frame.transform, false);
        var rbRt = reelBorderGo.GetComponent<RectTransform>();
        rbRt.anchorMin = rbRt.anchorMax = new Vector2(0.5f, 0.5f);
        rbRt.pivot = new Vector2(0.5f, 0.5f);
        rbRt.anchoredPosition = Vector2.zero;
        rbRt.sizeDelta = new Vector2(totalReelW + 50, reelH + 50);
        var rbImg = reelBorderGo.GetComponent<Image>();
        rbImg.sprite = GetReelBorderSprite();
        rbImg.type = Image.Type.Sliced;
        rbImg.color = Color.white;
        rbImg.raycastTarget = false;

        for (int r = 0; r < reelCount; r++)
        {
            float x = -totalReelW * 0.5f + symbolSize * 0.5f + r * (symbolSize + reelSpacing);
            var reelGo = new GameObject($"Reel_{r}", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            reelGo.transform.SetParent(inner.transform, false);
            var rt = reelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(symbolSize, reelH); rt.anchoredPosition = new Vector2(x, 0);
            reelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var stripGo = new GameObject("Strip", typeof(RectTransform));
            stripGo.transform.SetParent(reelGo.transform, false);
            var stripRt = stripGo.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0.5f, 1f); stripRt.anchorMax = new Vector2(0.5f, 1f); stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.anchoredPosition = Vector2.zero; stripRt.sizeDelta = new Vector2(symbolSize, reelH);

            int cellCount = rowsVisible + 1;
            var images = new Image[cellCount];
            var labels = new TextMeshProUGUI[cellCount];
            var roundedCell = GetRoundedCellSprite();
            for (int c = 0; c < cellCount; c++)
            {
                var cellWrapGo = new GameObject($"CellWrap_{c}", typeof(RectTransform), typeof(Image));
                cellWrapGo.transform.SetParent(stripGo.transform, false);
                var wrapRt = cellWrapGo.GetComponent<RectTransform>();
                wrapRt.anchorMin = new Vector2(0.5f, 1f); wrapRt.anchorMax = new Vector2(0.5f, 1f); wrapRt.pivot = new Vector2(0.5f, 1f);
                wrapRt.sizeDelta = new Vector2(symbolSize - 4, symbolSize - 4); wrapRt.anchoredPosition = new Vector2(0, -c * symbolSize);
                var wrapImg = cellWrapGo.GetComponent<Image>();
                wrapImg.sprite = roundedCell;
                wrapImg.type = Image.Type.Sliced;
                wrapImg.color = Color.white;

                var cellGo = new GameObject($"Cell_{c}", typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(stripGo.transform, false);
                var crt = cellGo.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 1f); crt.anchorMax = new Vector2(0.5f, 1f); crt.pivot = new Vector2(0.5f, 1f);
                crt.sizeDelta = new Vector2(symbolSize - 16, symbolSize - 16); crt.anchoredPosition = new Vector2(0, -c * symbolSize);
                images[c] = cellGo.GetComponent<Image>();
                images[c].color = SymbolDatabase.GetColor(SymbolType.Ten);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(cellGo.transform, false);
                var lbl = labelGo.AddComponent<TextMeshProUGUI>();
                lbl.text = "10"; lbl.fontSize = 56; lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center; lbl.fontStyle = FontStyles.Bold;
                lbl.outlineWidth = 0.2f; lbl.outlineColor = Color.black;
                var lrt = lbl.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                labels[c] = lbl;
            }

            var reel = reelGo.AddComponent<ReelController>();
            reel.Initialize(SymbolDatabase.GetReelStrip(r), stripRt, images, labels, symbolSize);
            reelControllers.Add(reel);
        }


    }

    private void BuildBottomControls(Transform parent)
    {
        var backdrop = new GameObject("FooterBackdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(parent, false);
        var bdRt = backdrop.GetComponent<RectTransform>();
        bdRt.anchorMin = new Vector2(0, 0);
        bdRt.anchorMax = new Vector2(1, 0);
        bdRt.pivot = new Vector2(0.5f, 0);
        bdRt.anchoredPosition = Vector2.zero;
        bdRt.sizeDelta = new Vector2(0, 200);
        var bdImg = backdrop.GetComponent<Image>();
        bdImg.sprite = GetVerticalGoldGradientSprite();
        bdImg.type = Image.Type.Simple;
        bdImg.raycastTarget = false;

        var panel = MakePanel(parent, "Controls", new Vector2(0, -460), new Vector2(1900, 180), new Color(0f, 0f, 0f, 0f));


        var promoSprite = Resources.Load<Sprite>("UI/Promo_Banner");
        if (promoSprite != null) {
            var promoGo = new GameObject("PromoBanner", typeof(RectTransform), typeof(Image));
            promoGo.transform.SetParent(canvasRect, false);
            var prRt = promoGo.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0, 0);
            prRt.anchorMax = new Vector2(0, 0);
            prRt.pivot = new Vector2(0, 0);
            prRt.sizeDelta = new Vector2(310, 270);
            prRt.anchoredPosition = new Vector2(10, 10);
            var prImg = promoGo.GetComponent<Image>();
            prImg.sprite = promoSprite;
            prImg.preserveAspect = true;
            prImg.raycastTarget = false;
        }

        var betGroup = new GameObject("BetGroup", typeof(RectTransform), typeof(Image));
        betGroup.transform.SetParent(panel.transform, false);
        var bgRt = betGroup.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(-380, 0);
        bgRt.sizeDelta = new Vector2(380, 120);
        var bgImg = betGroup.GetComponent<Image>();
        bgImg.sprite = GetUnifiedButtonSprite();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;

        var betDown = MakeButton(betGroup.transform, "−", new Vector2(95, 100), new Color(0f, 0f, 0f, 0f));
        betDown.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, 0);
        StripButtonChildren(betDown);
        var bdT = betDown.GetComponentInChildren<TextMeshProUGUI>();
        bdT.fontSize = 78; bdT.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(bdT);
        uiController.betDownButton = betDown;

        var betText = MakeText(betGroup.transform, "BET: 10", 38, accentGold, TextAlignmentOptions.Center);
        betText.rectTransform.anchoredPosition = new Vector2(0, 0);
        betText.rectTransform.sizeDelta = new Vector2(170, 60);
        betText.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(betText);
        uiController.betText = betText;

        var betUp = MakeButton(betGroup.transform, "+", new Vector2(95, 100), new Color(0f, 0f, 0f, 0f));
        betUp.GetComponent<RectTransform>().anchoredPosition = new Vector2(130, 0);
        StripButtonChildren(betUp);
        var buT = betUp.GetComponentInChildren<TextMeshProUGUI>();
        buT.fontSize = 78; buT.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(buT);
        uiController.betUpButton = betUp;

        var coinPanel = new GameObject("CoinPanel", typeof(RectTransform), typeof(Image));
        coinPanel.transform.SetParent(panel.transform, false);
        var coinRt = coinPanel.GetComponent<RectTransform>();
        coinRt.anchorMin = coinRt.anchorMax = new Vector2(0.5f, 0.5f);
        coinRt.pivot = new Vector2(0.5f, 0.5f);
        coinRt.anchoredPosition = new Vector2(-80, 0);
        coinRt.sizeDelta = new Vector2(220, 120);
        var coinPanelImg = coinPanel.GetComponent<Image>();
        coinPanelImg.sprite = GetUnifiedButtonSprite();
        coinPanelImg.type = Image.Type.Sliced;
        coinPanelImg.color = Color.white;
        coinPanelImg.raycastTarget = false;

        var coinLabel = MakeText(coinPanel.transform, "COIN", 22, accentGold, TextAlignmentOptions.Center);
        coinLabel.rectTransform.anchoredPosition = new Vector2(0, 28);
        coinLabel.rectTransform.sizeDelta = new Vector2(220, 30);
        coinLabel.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(coinLabel);

        var coinText = MakeText(coinPanel.transform, "$ 1,000", 36, accentGold, TextAlignmentOptions.Center);
        coinText.rectTransform.anchoredPosition = new Vector2(0, -18);
        coinText.rectTransform.sizeDelta = new Vector2(220, 60);
        coinText.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(coinText);
        uiController.currencyText = coinText;

        var winText = MakeText(parent, "", 56, accentGold, TextAlignmentOptions.Center);
        winText.rectTransform.anchoredPosition = new Vector2(0, 495);
        winText.rectTransform.sizeDelta = new Vector2(900, 70);
        winText.fontStyle = FontStyles.Bold;
        winText.outlineWidth = 0.22f;
        winText.outlineColor = new Color32(20, 5, 0, 255);
        ApplyGoldGradient(winText);
        uiController.winText = winText;

        var spinBtn = MakeButton(panel.transform, "SPIN", new Vector2(280, 120), Color.white);
        spinBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, 0);
        StyleAsGoldButton(spinBtn);
        var spinTxt = spinBtn.GetComponentInChildren<TextMeshProUGUI>();
        spinTxt.fontSize = 56; spinTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(spinTxt);
        uiController.spinButton = spinBtn;
        var pulse = spinBtn.gameObject.AddComponent<PulseGlow>();
        pulse.highlightColor = new Color(1f, 0.65f, 0.35f); pulse.speed = 3.0f; pulse.scaleAmplitude = 0.05f;

        var menuBtn = MakeButton(panel.transform, "MENU", new Vector2(180, 120), Color.white);
        menuBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(450, 0);
        StyleAsGoldButton(menuBtn);
        var menuTxt = menuBtn.GetComponentInChildren<TextMeshProUGUI>();
        menuTxt.fontSize = 38; menuTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(menuTxt);

        var autoText = MakeText(panel.transform, "", 22, accentGold, TextAlignmentOptions.Center);
        autoText.rectTransform.anchoredPosition = new Vector2(450, 80);
        autoText.rectTransform.sizeDelta = new Vector2(180, 28);
        uiController.autoSpinText = autoText;

        var menuPopup = BuildMenuPopup(parent);
        menuPopup.SetActive(false);
        menuBtn.onClick.AddListener(() => {
            menuPopup.SetActive(!menuPopup.activeSelf);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        });
    }

    private static Sprite cachedVerticalGoldGradient;
    private Sprite GetVerticalGoldGradientSprite()
    {
        if (cachedVerticalGoldGradient != null) return cachedVerticalGoldGradient;
        int w = 4, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        Color goldColor = new Color(1f, 0.78f, 0.25f);
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            float alpha = (1f - t) * 0.50f;
            Color c = new Color(goldColor.r, goldColor.g, goldColor.b, alpha);
            for (int x = 0; x < w; x++) pixels[y * w + x] = c;
        }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        cachedVerticalGoldGradient = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return cachedVerticalGoldGradient;
    }

    private static Sprite cachedReelBorderSprite;
    private Sprite GetReelBorderSprite()
    {
        if (cachedReelBorderSprite != null) return cachedReelBorderSprite;
        int size = 128;
        int radius = 26;
        int borderWidth = 5;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        Color border = new Color(0.95f, 0.78f, 0.20f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = 0, dy = 0;
                if (x < radius) dx = radius - x;
                else if (x >= size - radius) dx = x - (size - radius - 1);
                if (y < radius) dy = radius - y;
                else if (y >= size - radius) dy = y - (size - radius - 1);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                    pixels[y * size + x] = new Color(0, 0, 0, 0);
                else
                {
                    float edgeFactor = Mathf.Clamp01((radius - dist));
                    bool isBorder = (dist > radius - borderWidth);
                    pixels[y * size + x] = isBorder
                        ? new Color(border.r, border.g, border.b, edgeFactor)
                        : new Color(0, 0, 0, 0);
                }
            }
        }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        int b = radius + borderWidth + 2;
        cachedReelBorderSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, new Vector4(b, b, b, b));
        return cachedReelBorderSprite;
    }

    private static Sprite cachedUnifiedButtonSprite;
    private Sprite GetUnifiedButtonSprite()
    {
        if (cachedUnifiedButtonSprite != null) return cachedUnifiedButtonSprite;
        cachedUnifiedButtonSprite = GetRoundedRectSprite(new Color(0.32f, 0.16f, 0.08f, 0.95f), new Color(0.95f, 0.78f, 0.20f, 1f), 4, 22, 128);
        return cachedUnifiedButtonSprite;
    }

    private void StyleAsGoldButton(Button btn)
    {
        var img = btn.GetComponent<Image>();
        img.sprite = GetUnifiedButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        StripButtonChildren(btn);
    }

    private void StripButtonChildren(Button btn)
    {
        foreach (Transform child in btn.transform) {
            if (child.name == "DropShadow" || child.name == "TopHl" || child.name == "BotSh" || child.name == "GoldT" || child.name == "GoldB") {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyGoldGradient(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.enableVertexGradient = true;
        txt.colorGradient = new VertexGradient(
            new Color(1.00f, 0.95f, 0.50f),
            new Color(1.00f, 0.95f, 0.50f),
            new Color(0.75f, 0.50f, 0.10f),
            new Color(0.75f, 0.50f, 0.10f)
        );
        txt.color = Color.white;
    }

    private void ApplyFireGradient(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.enableVertexGradient = true;
        txt.colorGradient = new VertexGradient(
            new Color(1.00f, 0.95f, 0.30f),
            new Color(1.00f, 0.95f, 0.30f),
            new Color(0.85f, 0.15f, 0.05f),
            new Color(0.85f, 0.15f, 0.05f)
        );
        txt.color = Color.white;
    }

    private GameObject BuildMenuPopup(Transform parent)
    {
        var popup = MakePanel(parent, "MenuPopup", new Vector2(450, -180), new Vector2(220, 560), new Color(0.05f, 0.03f, 0.05f, 0.88f));
        var border = MakeImage(popup.transform, "Border", Vector2.zero, new Vector2(228, 568), accentGold);
        border.transform.SetAsFirstSibling();

        var turboBtn = MakeButton(popup.transform, SaveSystem.TurboMode ? "TURBO: ON" : "TURBO: OFF", new Vector2(180, 65), Color.white);
        turboBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 240);
        StyleAsGoldButton(turboBtn);
        var turboTxt = turboBtn.GetComponentInChildren<TextMeshProUGUI>();
        turboTxt.fontSize = 26; turboTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(turboTxt);
        turboBtn.onClick.AddListener(() => {
            SaveSystem.TurboMode = !SaveSystem.TurboMode;
            turboTxt.text = SaveSystem.TurboMode ? "TURBO: ON" : "TURBO: OFF";
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        });

        var autoBtn = MakeButton(popup.transform, "AUTO 10", new Vector2(180, 65), Color.white);
        autoBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 165);
        StyleAsGoldButton(autoBtn);
        var autoBtnTxt = autoBtn.GetComponentInChildren<TextMeshProUGUI>();
        autoBtnTxt.fontSize = 28; autoBtnTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(autoBtnTxt);
        uiController.autoSpinButton = autoBtn;

        var stopAutoBtn = MakeButton(popup.transform, "STOP", new Vector2(180, 65), Color.white);
        stopAutoBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);
        StyleAsGoldButton(stopAutoBtn);
        var stopTxt = stopAutoBtn.GetComponentInChildren<TextMeshProUGUI>();
        stopTxt.fontSize = 28; stopTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(stopTxt);
        uiController.stopAutoButton = stopAutoBtn;

        var bonusBtn = MakeButton(popup.transform, "BONUS", new Vector2(180, 65), Color.white);
        bonusBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 15);
        StyleAsGoldButton(bonusBtn);
        var bonusTxt = bonusBtn.GetComponentInChildren<TextMeshProUGUI>();
        bonusTxt.fontSize = 28; bonusTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(bonusTxt);
        uiController.bonusButton = bonusBtn;

        var historyBtn = MakeButton(popup.transform, "LOG", new Vector2(180, 65), Color.white);
        historyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
        StyleAsGoldButton(historyBtn);
        var logTxt = historyBtn.GetComponentInChildren<TextMeshProUGUI>();
        logTxt.fontSize = 28; logTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(logTxt);
        uiController.historyButton = historyBtn;

        var achBtn = MakeButton(popup.transform, "TRO", new Vector2(180, 65), Color.white);
        achBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -135);
        StyleAsGoldButton(achBtn);
        var troTxt = achBtn.GetComponentInChildren<TextMeshProUGUI>();
        troTxt.fontSize = 28; troTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(troTxt);
        uiController.achievementButton = achBtn;

        var paytableBtn = MakeButton(popup.transform, "PAY", new Vector2(180, 65), Color.white);
        paytableBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
        StyleAsGoldButton(paytableBtn);
        var payTxt = paytableBtn.GetComponentInChildren<TextMeshProUGUI>();
        payTxt.fontSize = 28; payTxt.fontStyle = FontStyles.Bold;
        ApplyGoldGradient(payTxt);
        payButton = paytableBtn;

        return popup;
    }

    private GameObject BuildSettingsPanel()
    {
        var modal = MakeModal("SettingsModal", new Vector2(950, 880));
        var p = modal.AddComponent<SettingsPanel>();
        var title = MakeText(modal.transform, "SETTINGS", 60, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 380); title.rectTransform.sizeDelta = new Vector2(800, 80); title.fontStyle = FontStyles.Bold;
        var volLabel = MakeText(modal.transform, "Volume", 36, Color.white, TextAlignmentOptions.Left);
        volLabel.rectTransform.anchoredPosition = new Vector2(-340, 280); volLabel.rectTransform.sizeDelta = new Vector2(200, 60);
        var sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderGo.transform.SetParent(modal.transform, false);
        var srt = sliderGo.GetComponent<RectTransform>(); srt.sizeDelta = new Vector2(500, 28); srt.anchoredPosition = new Vector2(60, 280);
        var slider = sliderGo.GetComponent<Slider>(); slider.minValue = 0f; slider.maxValue = 1f; slider.value = SaveSystem.Volume;
        sliderGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(sliderGo.transform, false);
        fillGo.GetComponent<Image>().color = accentGold;
        var frt = fillGo.GetComponent<RectTransform>(); frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(0.7f, 1); frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        slider.fillRect = frt; p.volumeSlider = slider;

        var muteToggleGo = new GameObject("MuteToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        muteToggleGo.transform.SetParent(modal.transform, false);
        var mrt = muteToggleGo.GetComponent<RectTransform>(); mrt.sizeDelta = new Vector2(50, 50); mrt.anchoredPosition = new Vector2(-340, 200);
        muteToggleGo.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);
        var toggle = muteToggleGo.GetComponent<Toggle>(); toggle.isOn = SaveSystem.Muted;
        var checkGo = new GameObject("Check", typeof(RectTransform), typeof(Image));
        checkGo.transform.SetParent(muteToggleGo.transform, false);
        checkGo.GetComponent<Image>().color = accentGold;
        var crt2 = checkGo.GetComponent<RectTransform>(); crt2.anchorMin = new Vector2(0.15f, 0.15f); crt2.anchorMax = new Vector2(0.85f, 0.85f); crt2.offsetMin = Vector2.zero; crt2.offsetMax = Vector2.zero;
        toggle.graphic = checkGo.GetComponent<Image>(); p.muteToggle = toggle;
        var muteLabel = MakeText(modal.transform, "Mute", 36, Color.white, TextAlignmentOptions.Left);
        muteLabel.rectTransform.anchoredPosition = new Vector2(-260, 200); muteLabel.rectTransform.sizeDelta = new Vector2(200, 60);

        var diffLabel = MakeText(modal.transform, "MODE PERMAINAN", 40, accentGold, TextAlignmentOptions.Center);
        diffLabel.rectTransform.anchoredPosition = new Vector2(0, 90); diffLabel.rectTransform.sizeDelta = new Vector2(700, 60); diffLabel.fontStyle = FontStyles.Bold;
        var easyBtn = MakeButton(modal.transform, "EASY", new Vector2(220, 80), new Color(0.25f, 0.25f, 0.35f));
        easyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-260, 10); p.easyBtn = easyBtn;
        var medBtn = MakeButton(modal.transform, "MEDIUM", new Vector2(220, 80), new Color(0.25f, 0.25f, 0.35f));
        medBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10); p.mediumBtn = medBtn;
        var hardBtn = MakeButton(modal.transform, "HARD", new Vector2(220, 80), new Color(0.25f, 0.25f, 0.35f));
        hardBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(260, 10); p.hardBtn = hardBtn;

        var diffStatus = MakeText(modal.transform, "", 26, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Center);
        diffStatus.rectTransform.anchoredPosition = new Vector2(0, -60); diffStatus.rectTransform.sizeDelta = new Vector2(800, 60); p.difficultyLabel = diffStatus;
        var stats = MakeText(modal.transform, "", 24, new Color(0.85f, 0.85f, 0.95f), TextAlignmentOptions.Center);
        stats.rectTransform.anchoredPosition = new Vector2(0, -160); stats.rectTransform.sizeDelta = new Vector2(800, 100); p.statsText = stats;
        var resetBtn = MakeButton(modal.transform, "RESET PROGRESS", new Vector2(340, 70), new Color(0.55f, 0.20f, 0.20f));
        resetBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180, -290); p.resetButton = resetBtn;
        var closeBtn = MakeButton(modal.transform, "CLOSE", new Vector2(280, 70), new Color(0.30f, 0.30f, 0.40f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -290); p.closeButton = closeBtn;
        modal.SetActive(false);
        return modal;
    }

    private GameObject BuildHistoryPanel()
    {
        var modal = MakeModal("HistoryModal", new Vector2(800, 850));
        var title = MakeText(modal.transform, "RIWAYAT 10 SPIN TERAKHIR", 44, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 380); title.rectTransform.sizeDelta = new Vector2(700, 80); title.fontStyle = FontStyles.Bold;
        var listText = MakeText(modal.transform, "Belum ada riwayat.", 30, Color.white, TextAlignmentOptions.TopLeft);
        listText.rectTransform.anchoredPosition = new Vector2(0, -40); listText.rectTransform.sizeDelta = new Vector2(700, 600); listText.lineSpacing = 18;
        if (uiController != null) uiController.historyText = listText;
        var closeBtn = MakeButton(modal.transform, "TUTUP", new Vector2(280, 70), new Color(0.30f, 0.30f, 0.40f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -370);
        closeBtn.onClick.AddListener(() => modal.SetActive(false));
        modal.SetActive(false); return modal;
    }

    private GameObject BuildAchievementPanel()
    {
        var modal = MakeModal("AchievementModal", new Vector2(900, 850));
        var title = MakeText(modal.transform, "ACHIEVEMENTS", 52, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 380); title.rectTransform.sizeDelta = new Vector2(800, 80); title.fontStyle = FontStyles.Bold;
        var listText = MakeText(modal.transform, "", 26, Color.white, TextAlignmentOptions.TopLeft);
        listText.rectTransform.anchoredPosition = new Vector2(0, -30); listText.rectTransform.sizeDelta = new Vector2(820, 620); listText.lineSpacing = 14;
        modal.AddComponent<AchievementListBinder>().listText = listText;
        var closeBtn = MakeButton(modal.transform, "TUTUP", new Vector2(280, 70), new Color(0.30f, 0.30f, 0.40f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -370);
        closeBtn.onClick.AddListener(() => modal.SetActive(false));
        modal.SetActive(false); return modal;
    }

    private GameObject BuildPaytablePanel()
    {
        var modal = MakeModal("PaytableModal", new Vector2(1100, 920));
        var title = MakeText(modal.transform, "TABEL PEMBAYARAN", 50, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 410); title.rectTransform.sizeDelta = new Vector2(1000, 80); title.fontStyle = FontStyles.Bold;
        var info = MakeText(modal.transform, "Simbol biasa: hadiah = multiplier x (Bet/10). Crown (Scatter): bayar langsung x bet (mis. 3 Crown = 25x bet). Dragon = Wild substitute. Free Spin x3 multiplier.", 20, new Color(0.85f, 0.85f, 0.95f), TextAlignmentOptions.Center);
        info.rectTransform.anchoredPosition = new Vector2(0, 340); info.rectTransform.sizeDelta = new Vector2(1050, 70);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<color=#00ddff><b>SIMBOL                3 simbol      4 simbol      5 simbol</b></color>");
        sb.AppendLine();
        for (int s = SymbolDatabase.Count - 1; s >= 0; s--)
        {
            var sym = (SymbolType)s;
            int p3 = SymbolDatabase.GetPayout(sym, 3), p4 = SymbolDatabase.GetPayout(sym, 4), p5 = SymbolDatabase.GetPayout(sym, 5);
            string name = SymbolDatabase.GetDisplayName(sym);
            sb.AppendLine($"<color=#FFD700><b>{name,-22}</b></color>     {p3,5}x         {p4,5}x         {p5,5}x");
        }

        var listText = MakeText(modal.transform, sb.ToString(), 24, Color.white, TextAlignmentOptions.Left);
        listText.rectTransform.anchoredPosition = new Vector2(0, -40); listText.rectTransform.sizeDelta = new Vector2(1000, 620); listText.lineSpacing = 14;
        var closeBtn = MakeButton(modal.transform, "TUTUP", new Vector2(280, 70), new Color(0.30f, 0.30f, 0.40f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -400);
        closeBtn.onClick.AddListener(() => modal.SetActive(false));
        modal.SetActive(false); return modal;
    }

    private GameObject BuildPaylinesPanel()
    {
        var modal = MakeModal("PaylinesModal", new Vector2(1100, 920));
        var title = MakeText(modal.transform, "PAYLINE 1 / 40", 50, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 410);
        title.rectTransform.sizeDelta = new Vector2(1000, 80);
        title.fontStyle = FontStyles.Bold;

        var info = MakeText(modal.transform, "Pola 40 garis kemenangan. Tekan PREV / NEXT untuk lihat tiap line.", 22, new Color(0.85f, 0.85f, 0.95f), TextAlignmentOptions.Center);
        info.rectTransform.anchoredPosition = new Vector2(0, 335);
        info.rectTransform.sizeDelta = new Vector2(1050, 60);

        var grid = MakePanel(modal.transform, "Grid", new Vector2(0, 20), new Vector2(720, 460), new Color(0.05f, 0.05f, 0.08f, 0.9f));

        int reels = PaylineSystem.Reels;
        int rows = PaylineSystem.Rows;
        float cellW = 130f, cellH = 100f;
        float spacingX = 138f, spacingY = 108f;
        float originX = -(reels - 1) * spacingX * 0.5f;
        float originY = (rows - 1) * spacingY * 0.5f;

        var cells = new Image[reels * rows];
        for (int r = 0; r < reels; r++)
        {
            for (int c = 0; c < rows; c++)
            {
                var cellGo = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(grid.transform, false);
                var crt = cellGo.GetComponent<RectTransform>();
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(cellW, cellH);
                crt.anchoredPosition = new Vector2(originX + r * spacingX, originY - c * spacingY);
                var cImg = cellGo.GetComponent<Image>();
                cImg.color = new Color(0.16f, 0.16f, 0.22f, 1f);
                cImg.raycastTarget = false;
                cells[r * rows + c] = cImg;
            }
        }

        var lineGo = new GameObject("PaylineLine", typeof(RectTransform), typeof(CanvasRenderer));
        lineGo.transform.SetParent(grid.transform, false);
        var lrt = lineGo.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(720, 460);
        lrt.anchoredPosition = Vector2.zero;
        var line = lineGo.AddComponent<UILineRenderer>();
        line.thickness = 12f;
        line.color = new Color(1f, 0.85f, 0.20f);
        line.raycastTarget = false;
        lineGo.transform.SetAsLastSibling();

        var prevBtn = MakeButton(modal.transform, "PREV", new Vector2(220, 80), new Color(0.30f, 0.30f, 0.40f));
        prevBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180, -290);
        var nextBtn = MakeButton(modal.transform, "NEXT", new Vector2(220, 80), new Color(0.30f, 0.30f, 0.40f));
        nextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -290);
        var closeBtn = MakeButton(modal.transform, "TUTUP", new Vector2(280, 70), new Color(0.30f, 0.30f, 0.40f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -400);

        var ctl = modal.AddComponent<PaylinesViewerPanel>();
        ctl.cellHighlights = cells;
        ctl.line = line;
        ctl.titleText = title;
        ctl.prevButton = prevBtn;
        ctl.nextButton = nextBtn;
        ctl.closeButton = closeBtn;
        ctl.gridOrigin = new Vector2(originX, originY);
        ctl.cellSpacingX = spacingX;
        ctl.cellSpacingY = spacingY;

        modal.SetActive(false);
        return modal;
    }

    private WinPopupController BuildWinPopup()
    {
        var go = new GameObject("WinPopup", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasRect, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var cg = go.GetComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
        var dim = MakeImage(go.transform, "Dim", Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, 0.6f));
        var box = MakePanel(go.transform, "Box", Vector2.zero, new Vector2(800, 500), panelColor);
        var border = MakeImage(box.transform, "Border", Vector2.zero, new Vector2(820, 520), frameColor);
        border.transform.SetAsFirstSibling();
        var title = MakeText(box.transform, "BIG WIN!", 100, frameColor, TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0, 80); title.rectTransform.sizeDelta = new Vector2(700, 130); title.fontStyle = FontStyles.Bold;
        var amount = MakeText(box.transform, "+1,000", 90, Color.white, TextAlignmentOptions.Center);
        amount.rectTransform.anchoredPosition = new Vector2(0, -90); amount.rectTransform.sizeDelta = new Vector2(700, 140); amount.fontStyle = FontStyles.Bold;
        var comp = go.AddComponent<WinPopupController>();
        comp.canvasGroup = cg; comp.popupBox = box.GetComponent<RectTransform>(); comp.titleText = title; comp.amountText = amount;
        go.SetActive(false); return comp;
    }

    private CoinParticleEffect BuildCoinFx()
    {
        var go = new GameObject("CoinFx", typeof(RectTransform));
        go.transform.SetParent(canvasRect, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var fx = go.AddComponent<CoinParticleEffect>(); fx.spawnArea = rt; return fx;
    }

    private (TextMeshProUGUI text, CanvasGroup group) BuildAchievementToast()
    {
        var go = new GameObject("AchievementToast", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvasRect, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(600, 130);
        rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); rt.anchoredPosition = new Vector2(-30, -30);
        var bg = go.AddComponent<Image>(); bg.color = new Color(0.10f, 0.05f, 0.08f, 0.96f);
        var border = MakeImage(go.transform, "Border", Vector2.zero, new Vector2(610, 140), frameColor);
        border.transform.SetAsFirstSibling();
        var text = MakeText(go.transform, "", 30, Color.white, TextAlignmentOptions.Left);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(20, 10); text.rectTransform.offsetMax = new Vector2(-20, -10);
        var cg = go.GetComponent<CanvasGroup>(); cg.alpha = 0f; go.SetActive(false);
        return (text, cg);
    }

    private void BuildScreenFlashLayer()
    {
        var go = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRect, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.color = new Color(1f, 1f, 1f, 0f); img.raycastTarget = false;
        go.AddComponent<ScreenFlash>();
    }

    private void WireGameButtons()
    {
        uiController.spinButton.onClick.AddListener(() => { gameManager.StopAutoSpin(); gameManager.TrySpin(); });
        uiController.betUpButton.onClick.AddListener(() => gameManager.IncreaseBet());
        uiController.betDownButton.onClick.AddListener(() => gameManager.DecreaseBet());
        uiController.autoSpinButton.onClick.AddListener(() => gameManager.StartAutoSpin(10));
        uiController.stopAutoButton.onClick.AddListener(() => gameManager.StopAutoSpin());
        uiController.settingsButton.onClick.AddListener(() => settingsObj.SetActive(true));
        uiController.bonusButton.onClick.AddListener(() => gameManager.ClaimDailyBonus());
        uiController.historyButton.onClick.AddListener(() => { historyObj.SetActive(true); uiController.RefreshHistoryPanel(); });
        uiController.achievementButton.onClick.AddListener(() => { achievementObj.SetActive(true); var b = achievementObj.GetComponent<AchievementListBinder>(); if (b) b.Refresh(); });
        if (payButton != null) payButton.onClick.AddListener(() => paytableObj.SetActive(true));
    }

    private static Sprite cachedRoundedSprite;
    private static Sprite cachedGreenFeltSprite;
    private static Sprite cachedRoundedCellSprite;

    private Sprite GetRoundedRectSprite(Color fill, Color border, int borderWidth = 3, int radius = 18, int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = 0, dy = 0;
                if (x < radius) dx = radius - x;
                else if (x >= size - radius) dx = x - (size - radius - 1);
                if (y < radius) dy = radius - y;
                else if (y >= size - radius) dy = y - (size - radius - 1);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                {
                    pixels[y * size + x] = new Color(0, 0, 0, 0);
                }
                else
                {
                    float edgeFactor = Mathf.Clamp01((radius - dist));
                    bool isBorder = (dist > radius - borderWidth) || (x < borderWidth) || (x >= size - borderWidth) || (y < borderWidth) || (y >= size - borderWidth);
                    if (isBorder && (dx > 0 || dy > 0 || x < borderWidth || y < borderWidth || x >= size - borderWidth || y >= size - borderWidth))
                        pixels[y * size + x] = new Color(border.r, border.g, border.b, edgeFactor);
                    else
                        pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * edgeFactor);
                }
            }
        }
        tex.SetPixels(pixels); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp; tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    private Sprite GetGreenFeltSprite(int size = 256)
    {
        if (cachedGreenFeltSprite != null) return cachedGreenFeltSprite;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = (Mathf.PerlinNoise(x * 0.05f, y * 0.05f) - 0.5f) * 0.06f;
                float n2 = (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f) * 0.04f;
                Color baseG = new Color(0.06f, 0.20f, 0.10f);
                pixels[y * size + x] = new Color(baseG.r + n + n2, baseG.g + n + n2, baseG.b + n + n2, 1f);
            }
        tex.SetPixels(pixels); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Repeat; tex.Apply();
        cachedGreenFeltSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return cachedGreenFeltSprite;
    }

    private Sprite GetRoundedCellSprite()
    {
        if (cachedRoundedCellSprite != null) return cachedRoundedCellSprite;
        cachedRoundedCellSprite = GetRoundedRectSprite(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), 0, 16, 128);
        return cachedRoundedCellSprite;
    }

    private void ApplyButtonSprite(Button btn, string spritePath)
    {
        var sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null) return;
        var img = btn.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        foreach (Transform child in btn.transform) {
            if (child.name == "DropShadow" || child.name == "TopHl" || child.name == "BotSh" || child.name == "GoldT" || child.name == "GoldB") {
                child.gameObject.SetActive(false);
            }
        }
    }

    private GameObject MakeModal(string name, Vector2 size)
    {
        var dim = new GameObject(name, typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvasRect, false);
        var dr = dim.GetComponent<RectTransform>(); dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one; dr.offsetMin = Vector2.zero; dr.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(dim.transform, false);
        var br = box.GetComponent<RectTransform>(); br.sizeDelta = size; br.anchorMin = new Vector2(0.5f, 0.5f); br.anchorMax = new Vector2(0.5f, 0.5f); br.pivot = new Vector2(0.5f, 0.5f); br.anchoredPosition = Vector2.zero;
        box.GetComponent<Image>().color = panelColor;
        var border = MakeImage(box.transform, "Border", Vector2.zero, size + new Vector2(16, 16), frameColor);
        border.transform.SetAsFirstSibling();
        return dim;
    }

    private GameObject MakePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool stretch = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        else { rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; rt.anchoredPosition = pos; }
        go.GetComponent<Image>().color = color;
        return go;
    }

    private Image MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>(); img.color = color; return img;
    }

    private TextMeshProUGUI MakeText(Transform parent, string content, int size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(400, 60);
        var tmp = go.AddComponent<TextMeshProUGUI>(); tmp.text = content; tmp.fontSize = size; tmp.color = color; tmp.alignment = align; tmp.enableWordWrapping = false;
        tmp.outlineWidth = 0.15f; tmp.outlineColor = new Color(0, 0, 0, 0.8f);
        return tmp;
    }

    private Button MakeButton(Transform parent, string label, Vector2 size, Color bg)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = bg;
        var roundedSprite = GetRoundedRectSprite(bg, new Color(1f, 0.95f, 0.55f, 1f), 4, Mathf.RoundToInt(size.y * 0.25f), 128);
        img.sprite = roundedSprite;
        img.type = Image.Type.Sliced;

        var dropShadow = new GameObject("DropShadow", typeof(RectTransform), typeof(Image));
        dropShadow.transform.SetParent(go.transform, false);
        var dsRt = dropShadow.GetComponent<RectTransform>(); dsRt.anchorMin = Vector2.zero; dsRt.anchorMax = Vector2.one; dsRt.offsetMin = new Vector2(-3, -7); dsRt.offsetMax = new Vector2(3, -3);
        dropShadow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f); dropShadow.transform.SetAsFirstSibling();

        var topHl = new GameObject("TopHl", typeof(RectTransform), typeof(Image));
        topHl.transform.SetParent(go.transform, false);
        var thRt = topHl.GetComponent<RectTransform>(); thRt.anchorMin = new Vector2(0, 1); thRt.anchorMax = new Vector2(1, 1); thRt.pivot = new Vector2(0.5f, 1); thRt.sizeDelta = new Vector2(0, Mathf.Max(8, size.y * 0.35f)); thRt.anchoredPosition = Vector2.zero;
        topHl.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.32f); topHl.GetComponent<Image>().raycastTarget = false;

        var botSh = new GameObject("BotSh", typeof(RectTransform), typeof(Image));
        botSh.transform.SetParent(go.transform, false);
        var bsRt = botSh.GetComponent<RectTransform>(); bsRt.anchorMin = new Vector2(0, 0); bsRt.anchorMax = new Vector2(1, 0); bsRt.pivot = new Vector2(0.5f, 0); bsRt.sizeDelta = new Vector2(0, Mathf.Max(6, size.y * 0.25f)); bsRt.anchoredPosition = Vector2.zero;
        botSh.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.50f); botSh.GetComponent<Image>().raycastTarget = false;

        var goldT = new GameObject("GoldT", typeof(RectTransform), typeof(Image));
        goldT.transform.SetParent(go.transform, false);
        var gtRt = goldT.GetComponent<RectTransform>(); gtRt.anchorMin = new Vector2(0, 1); gtRt.anchorMax = new Vector2(1, 1); gtRt.pivot = new Vector2(0.5f, 1); gtRt.sizeDelta = new Vector2(0, 4); gtRt.anchoredPosition = Vector2.zero;
        goldT.GetComponent<Image>().color = new Color(1f, 0.88f, 0.35f, 1f); goldT.GetComponent<Image>().raycastTarget = false;

        var goldB = new GameObject("GoldB", typeof(RectTransform), typeof(Image));
        goldB.transform.SetParent(go.transform, false);
        var gbRt = goldB.GetComponent<RectTransform>(); gbRt.anchorMin = new Vector2(0, 0); gbRt.anchorMax = new Vector2(1, 0); gbRt.pivot = new Vector2(0.5f, 0); gbRt.sizeDelta = new Vector2(0, 4); gbRt.anchoredPosition = Vector2.zero;
        goldB.GetComponent<Image>().color = new Color(0.55f, 0.35f, 0.05f, 1f); goldB.GetComponent<Image>().raycastTarget = false;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bg;
        colors.highlightedColor = new Color(Mathf.Min(1f, bg.r * 1.25f), Mathf.Min(1f, bg.g * 1.25f), Mathf.Min(1f, bg.b * 1.25f), bg.a);
        colors.pressedColor = new Color(bg.r * 0.65f, bg.g * 0.65f, bg.b * 0.65f, bg.a);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        var lblGo = new GameObject("Label", typeof(RectTransform));
        lblGo.transform.SetParent(go.transform, false);
        var tmp = lblGo.AddComponent<TextMeshProUGUI>(); tmp.text = label; tmp.fontSize = (int)(size.y * 0.45f); tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.2f; tmp.outlineColor = new Color(0, 0, 0, 0.9f); tmp.raycastTarget = false;
        var lrt = tmp.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        return btn;
    }
}

public class AchievementListBinder : MonoBehaviour
{
    public TextMeshProUGUI listText;
    void OnEnable() { Refresh(); }
    public void Refresh()
    {
        if (listText == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var a in AchievementManager.All()) {
            string mark = a.unlocked ? "<color=#00ff66>[V]</color>" : "<color=#666666>[ ]</color>";
            sb.AppendLine($"{mark} <b>{a.title}</b> — {a.description}");
        }
        listText.text = sb.ToString();
    }
}
