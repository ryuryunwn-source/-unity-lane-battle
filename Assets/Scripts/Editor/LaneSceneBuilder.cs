#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 侵攻ライン（レーン制対戦）のシーンを自動構築する。
/// メニュー Tools → Lane → Build Scene を実行すると、5×5の盤面・UI・マネージャを生成する。
/// （TCG版のSceneBuilderとは別物。実行すると現在のシーンのルートは作り直される）
/// </summary>
public static class LaneSceneBuilder
{
    [MenuItem("Tools/Lane/Build Scene")]
    public static void Build()
    {
        // 既存ルートを掃除（Main Cameraは残す）
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Main Camera") continue;
            Object.DestroyImmediate(go);
        }

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // Canvas
        Canvas canvas = CreateCanvas();

        // 背景（古代遺跡の石板テクスチャ）
        GameObject bg = NewImage(canvas.transform, "BG", Vector2.zero, new Vector2(1920, 1080),
            new Color(0.07f, 0.06f, 0.05f, 1f));
        Stretch(bg.GetComponent<RectTransform>());
        Image bgImg = bg.GetComponent<Image>();
        if (AncientArt.Board != null)
        {
            bgImg.sprite = AncientArt.Board;
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 上のUIを邪魔しないよう少し暗く
        }

        // ===== 盤面グリッド（5レーン×5セル）=====
        GameObject gridGo = new GameObject("LaneGrid");
        gridGo.transform.SetParent(canvas.transform, false);
        RectTransform gridRt = gridGo.AddComponent<RectTransform>();
        gridRt.sizeDelta = new Vector2(620f, 620f);
        gridRt.anchoredPosition = new Vector2(0f, 40f);
        GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(112f, 112f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = LaneBoard.Cells;
        grid.childAlignment = TextAnchor.MiddleCenter;

        // 行=レーン, 列=col。col0が左(P1自陣), col4が右(P2自陣)
        for (int lane = 0; lane < LaneBoard.Lanes; lane++)
        {
            for (int col = 0; col < LaneBoard.Cells; col++)
            {
                GameObject cellGo = new GameObject($"Cell_{lane}_{col}");
                cellGo.transform.SetParent(gridGo.transform, false);
                cellGo.AddComponent<RectTransform>();
                Image cellImg = cellGo.AddComponent<Image>();
                // 自陣端の列を色分け（召喚可能列を分かりやすく）
                if (col == 0) cellImg.color = new Color(0.18f, 0.28f, 0.42f, 0.55f);       // P1 home
                else if (col == LaneBoard.Cells - 1) cellImg.color = new Color(0.42f, 0.22f, 0.18f, 0.55f); // P2 home
                else cellImg.color = new Color(0.16f, 0.15f, 0.12f, 0.5f);
                Outline cellOutline = cellGo.AddComponent<Outline>();
                cellOutline.effectColor = new Color(0.6f, 0.5f, 0.25f, 0.55f);
                cellOutline.effectDistance = new Vector2(1.5f, -1.5f);

                LaneCell cell = cellGo.AddComponent<LaneCell>();
                cell.lane = lane;
                cell.col = col;
            }
        }

        // ===== ステータスUI =====
        GameObject uiGo = new GameObject("LaneUI");
        uiGo.transform.SetParent(canvas.transform, false);
        uiGo.AddComponent<RectTransform>();
        LaneUI ui = uiGo.AddComponent<LaneUI>();

        if (AncientArt.BarHp != null)
        {
            var b1 = NewImage(canvas.transform, "P1BaseBar", new Vector2(-740, 232), new Vector2(320, 64), Color.white);
            b1.GetComponent<Image>().sprite = AncientArt.BarHp;
            var b2 = NewImage(canvas.transform, "P2BaseBar", new Vector2(740, 232), new Vector2(320, 64), Color.white);
            b2.GetComponent<Image>().sprite = AncientArt.BarHp;
        }

        ui.p1BaseText = NewText(canvas.transform, "P1Base", "P1 ベース: 20/20",
            new Vector2(-740, 240), new Vector2(340, 50), 28, new Color(0.55f, 0.8f, 1f)).GetComponent<Text>();
        ui.p1MpText = NewText(canvas.transform, "P1MP", "MP: 0/10",
            new Vector2(-740, 190), new Vector2(340, 40), 24, new Color(0.5f, 0.85f, 0.95f)).GetComponent<Text>();
        ui.p2BaseText = NewText(canvas.transform, "P2Base", "P2 ベース: 20/20",
            new Vector2(740, 240), new Vector2(340, 50), 28, new Color(1f, 0.6f, 0.5f)).GetComponent<Text>();
        ui.p2MpText = NewText(canvas.transform, "P2MP", "MP: 0/10",
            new Vector2(740, 190), new Vector2(340, 40), 24, new Color(0.95f, 0.6f, 0.5f)).GetComponent<Text>();

        ui.turnText = NewText(canvas.transform, "Turn", "▶ Player 1 のターン",
            new Vector2(0, 410), new Vector2(700, 50), 30, Color.white).GetComponent<Text>();

        // ターン終了ボタン
        ui.endTurnButton = CreateButton(canvas.transform, "EndTurnBtn",
            new Vector2(780, -360), new Vector2(220, 80), "ターン終了 ▶", new Color(0.25f, 0.5f, 0.85f));

        // 手札コンテナ（下部）
        GameObject handGo = new GameObject("HandContainer");
        handGo.transform.SetParent(canvas.transform, false);
        RectTransform handRt = handGo.AddComponent<RectTransform>();
        handRt.sizeDelta = new Vector2(1400f, 170f);
        handRt.anchoredPosition = new Vector2(-80f, -380f);
        HorizontalLayoutGroup hlg = handGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        ui.handContainer = handGo.transform;

        // バナー
        GameObject banner = NewImage(canvas.transform, "Banner", Vector2.zero, new Vector2(640, 100),
            new Color(0f, 0f, 0f, 0.8f));
        ui.bannerPanel = banner;
        ui.bannerText = NewText(banner.transform, "BannerText", "",
            Vector2.zero, new Vector2(620, 90), 40, Color.yellow).GetComponent<Text>();

        // ゲームオーバー
        GameObject over = NewImage(canvas.transform, "GameOver", Vector2.zero, new Vector2(600, 300),
            new Color(0f, 0f, 0f, 0.9f));
        ui.gameOverPanel = over;
        ui.gameOverText = NewText(over.transform, "OverText", "勝利！",
            new Vector2(0, 70), new Vector2(560, 90), 46, Color.yellow).GetComponent<Text>();
        ui.restartButton = CreateButton(over.transform, "RestartBtn",
            new Vector2(0, -70), new Vector2(220, 70), "もう一度", new Color(0.25f, 0.65f, 0.3f));

        // ===== マネージャ =====
        GameObject gmGo = new GameObject("LaneGameManager");
        gmGo.transform.SetParent(null);
        LaneGameManager gm = gmGo.AddComponent<LaneGameManager>();
        LaneSetup setup = gmGo.AddComponent<LaneSetup>();
        setup.ui = ui;
        gm.ui = ui;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[LaneSceneBuilder] 侵攻ラインのシーンを構築しました。Playで対戦できます。");
    }

    // ===== ヘルパ =====
    private static Canvas CreateCanvas()
    {
        GameObject go = new GameObject("MainCanvas");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject NewImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static GameObject NewText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0.04f, 0.02f, 0.95f);
        o.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color)
    {
        GameObject go = NewImage(parent, name, pos, size, color);
        Image img = go.GetComponent<Image>();
        if (AncientArt.BtnTurnEnd != null)
        {
            img.sprite = AncientArt.BtnTurnEnd;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        Button btn = go.AddComponent<Button>();
        NewText(go.transform, "Label", label, Vector2.zero, size, 22, new Color(0.97f, 0.92f, 0.78f));
        return btn;
    }
}
#endif
