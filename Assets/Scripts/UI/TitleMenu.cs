using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// タイトル/モード選択画面（全編コード生成）。
/// 「1台で対戦」「オンライン:ホスト」「オンライン:コードで参加」を選べる。
/// 選択結果は GameSession に保存し、GameManager がそれを見てゲームを開始する。
/// </summary>
public class TitleMenu : MonoBehaviour
{
    private Canvas canvas;
    private GameObject rootPanel;     // メインメニュー
    private GameObject onlinePanel;   // オンライン用サブパネル
    private Text statusText;
    private Text codeDisplayText;     // ホストの参加コード表示
    private InputField codeInput;     // 参加者のコード入力

    private OnlineConnector connector;

    private void Start()
    {
        BuildUI();
    }

    // ===== UI構築 =====
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("TitleCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 最前面
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 背景（古代盤面 or 暗幕）
        GameObject bg = NewImage(canvas.transform, "BG", Vector2.zero, new Vector2(1920, 1080));
        Image bgImg = bg.GetComponent<Image>();
        if (AncientArt.Board != null) { bgImg.sprite = AncientArt.Board; bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f); }
        else bgImg.color = new Color(0.06f, 0.06f, 0.05f, 1f);
        StretchFull(bg.GetComponent<RectTransform>());

        // タイトル
        Text title = NewText(canvas.transform, "Title", "マイモン：侵攻ライン",
            new Vector2(0, 320), new Vector2(1400, 160), 76, new Color(0.95f, 0.86f, 0.55f));
        title.fontStyle = FontStyle.Bold;
        AddOutline(title, 3f);

        // ===== メインメニュー =====
        rootPanel = new GameObject("MainMenu");
        rootPanel.transform.SetParent(canvas.transform, false);
        rootPanel.AddComponent<RectTransform>();

        MakeButton(rootPanel.transform, "AIと対戦", new Vector2(0, 200), OnClickVsAI);
        MakeButton(rootPanel.transform, "1台で2人対戦", new Vector2(0, 100), OnClickLocal);
        MakeButton(rootPanel.transform, "オンライン: ホストになる", new Vector2(0, 0), OnClickHost);
        MakeButton(rootPanel.transform, "オンライン: コードで参加", new Vector2(0, -100), OnClickShowJoin);
        // 同一PCテスト（Multiplayer Play Mode用 / UGS不要のlocalhost直結）
        MakeButton(rootPanel.transform, "▷ 同PCテスト: ホスト", new Vector2(0, -200), OnClickLocalHost);
        MakeButton(rootPanel.transform, "▷ 同PCテスト: 参加", new Vector2(0, -300), OnClickLocalJoin);

        // ===== オンライン用パネル =====
        BuildOnlinePanel();
        onlinePanel.SetActive(false);

        // 状況テキスト
        statusText = NewText(canvas.transform, "Status", "",
            new Vector2(0, -420), new Vector2(1400, 80), 30, new Color(0.9f, 0.9f, 0.85f));
    }

    private void BuildOnlinePanel()
    {
        onlinePanel = new GameObject("OnlinePanel");
        onlinePanel.transform.SetParent(canvas.transform, false);
        RectTransform rt = onlinePanel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 500);

        // ホストの参加コード表示
        codeDisplayText = NewText(onlinePanel.transform, "Code", "",
            new Vector2(0, 120), new Vector2(800, 120), 64, new Color(1f, 0.9f, 0.5f));
        codeDisplayText.fontStyle = FontStyle.Bold;
        AddOutline(codeDisplayText, 3f);

        // 参加者のコード入力
        codeInput = NewInputField(onlinePanel.transform, "CodeInput", "参加コードを入力",
            new Vector2(0, 10), new Vector2(560, 90));

        MakeButton(onlinePanel.transform, "接続する", new Vector2(0, -110), OnClickConnect);
        MakeButton(onlinePanel.transform, "戻る", new Vector2(0, -230), OnClickBack);
    }

    // ===== ボタン挙動 =====
    private void OnClickVsAI()
    {
        GameSession.Mode = GameMode.Local;
        GameSession.VsAI = true;
        statusText.text = "AI対戦を開始します";
        Destroy(canvas.gameObject); // タイトルを閉じる→GameManagerが開始
    }

    private void OnClickLocal()
    {
        GameSession.Mode = GameMode.Local;
        GameSession.VsAI = false;
        statusText.text = "1台対戦を開始します";
        Destroy(canvas.gameObject); // タイトルを閉じる→GameManagerが開始
    }

    private void OnClickHost()
    {
        if (!EnsureNetReady()) return;
        ShowOnlinePanel(hostMode: true);
        statusText.text = "ホスト準備中...";
        SetupConnectorEvents();
        connector.StartHost();
    }

    private void OnClickShowJoin()
    {
        if (!EnsureNetReady()) return;
        ShowOnlinePanel(hostMode: false);
        statusText.text = "ホストの参加コードを入力してください";
    }

    private void OnClickLocalHost()
    {
        if (!EnsureNetReady()) return;
        SetupConnectorEvents();
        statusText.text = "同一PCホスト準備中...";
        connector.StartLocalHost();
    }

    private void OnClickLocalJoin()
    {
        if (!EnsureNetReady()) return;
        SetupConnectorEvents();
        statusText.text = "localhostへ接続中...";
        connector.StartLocalClient();
    }

    private void OnClickConnect()
    {
        if (!EnsureNetReady()) return;
        SetupConnectorEvents();
        connector.StartClient(codeInput != null ? codeInput.text : "");
    }

    private void OnClickBack()
    {
        connector?.Disconnect();
        onlinePanel.SetActive(false);
        rootPanel.SetActive(true);
        if (codeDisplayText) codeDisplayText.text = "";
        statusText.text = "";
    }

    private void ShowOnlinePanel(bool hostMode)
    {
        rootPanel.SetActive(false);
        onlinePanel.SetActive(true);
        // ホスト：コード表示＆入力欄非表示 / 参加：入力欄表示＆コード表示非表示
        codeDisplayText.gameObject.SetActive(hostMode);
        codeInput.gameObject.SetActive(!hostMode);
        codeDisplayText.text = hostMode ? "発行中..." : "";
    }

    private bool EnsureNetReady()
    {
        if (NetworkManager.Singleton == null)
        {
            statusText.text = "ネットワーク未設定: シーンにNetworkManagerがありません（Tools→Net→Setup を実行）";
            Debug.LogError("[TitleMenu] NetworkManager.Singleton が null。NetworkManagerをシーンに配置してください。");
            return false;
        }
        if (connector == null)
        {
            connector = OnlineConnector.Instance;
            if (connector == null)
            {
                GameObject go = new GameObject("OnlineConnector");
                connector = go.AddComponent<OnlineConnector>();
            }
        }
        return true;
    }

    private bool eventsHooked = false;
    private void SetupConnectorEvents()
    {
        if (eventsHooked || connector == null) return;
        eventsHooked = true;
        connector.OnStatus += msg => { if (statusText) statusText.text = msg; };
        connector.OnError  += msg => { if (statusText) statusText.text = "エラー: " + msg; };
        connector.OnHostCodeReady += code => { if (codeDisplayText) codeDisplayText.text = code; };

        // 接続確立でタイトルを閉じる（ホスト/参加どちらも）
        NetworkManager.Singleton.OnClientConnectedCallback += OnNetClientConnected;
    }

    private void OnNetClientConnected(ulong clientId)
    {
        // ホスト: 相手(2人目)が来たら開始。参加: 自分が繋がったら閉じる。
        if (GameSession.Mode == GameMode.OnlineHost)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
                CloseTitle();
        }
        else if (GameSession.Mode == GameMode.OnlineClient)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
                CloseTitle();
        }
    }

    private void CloseTitle()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnNetClientConnected;
        if (canvas != null) Destroy(canvas.gameObject);
    }

    // ===== UI生成ヘルパ =====
    private GameObject NewImage(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.AddComponent<Image>();
        return go;
    }

    private Text NewText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.text = content; t.fontSize = fontSize; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(560, 96); rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        if (AncientArt.BtnTurnEnd != null) { img.sprite = AncientArt.BtnTurnEnd; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.32f, 0.26f, 0.16f, 0.95f);
        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        Text t = NewText(go.transform, "Label", label, Vector2.zero, new Vector2(540, 90), 34, new Color(0.96f, 0.9f, 0.78f));
        t.fontStyle = FontStyle.Bold;
        AddOutline(t, 2.2f);
    }

    private InputField NewInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.09f, 0.07f, 0.95f);
        InputField field = go.AddComponent<InputField>();

        Text ph = NewText(go.transform, "Placeholder", placeholder, Vector2.zero, size - new Vector2(20, 10), 30, new Color(0.7f, 0.68f, 0.6f, 0.7f));
        ph.alignment = TextAnchor.MiddleCenter;
        Text txt = NewText(go.transform, "Text", "", Vector2.zero, size - new Vector2(20, 10), 34, Color.white);
        txt.alignment = TextAnchor.MiddleCenter;

        field.textComponent = txt;
        field.placeholder = ph;
        field.characterLimit = 8;
        return field;
    }

    private void AddOutline(Text t, float distance)
    {
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0.04f, 0.02f, 0.95f);
        o.effectDistance = new Vector2(distance, -distance);
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
