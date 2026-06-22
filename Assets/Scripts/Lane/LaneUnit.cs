using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// レーン上に配置されたモンスター1体。盤面のセルに親子付けされて表示される。
/// 進軍・戦闘はLaneGameManagerが制御し、本クラスはステータスと見た目を保持する。
/// </summary>
public class LaneUnit : MonoBehaviour
{
    public CardData data;
    public LanePlayer owner;
    public int lane;
    public int col;
    public int atk;
    public int hp;
    public bool advancedThisTurn = false;

    public bool IsAlive => hp > 0;

    private Text nameText;
    private Text statsText;
    private Image bg;

    /// <summary>UIを生成して初期化する。parentは配置先セルのTransform。</summary>
    public void Setup(CardData cardData, LanePlayer ownerPlayer, int laneIndex, int colIndex, Transform parent)
    {
        data = cardData;
        owner = ownerPlayer;
        lane = laneIndex;
        col = colIndex;
        atk = cardData.attack;
        hp = cardData.defense;

        transform.SetParent(parent, false);
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(96f, 96f);

        bg = gameObject.AddComponent<Image>();
        Sprite frame = AncientArt.CardFrame;
        if (frame != null)
        {
            bg.sprite = frame;
            bg.type = Image.Type.Sliced;
            // 石組みの質感を活かしつつ陣営色をうっすら乗せる
            bg.color = ownerPlayer.isPlayer1
                ? new Color(0.62f, 0.78f, 1f, 1f)    // P1=青みがかった石
                : new Color(1f, 0.72f, 0.62f, 1f);   // P2=赤みがかった石
        }
        else
        {
            bg.color = ownerPlayer.isPlayer1
                ? new Color(0.2f, 0.45f, 0.7f, 0.95f)
                : new Color(0.7f, 0.3f, 0.2f, 0.95f);
        }

        Outline o = gameObject.AddComponent<Outline>();
        o.effectColor = ownerPlayer.isPlayer1
            ? new Color(0.35f, 0.55f, 0.9f, 0.95f)
            : new Color(0.9f, 0.45f, 0.3f, 0.95f);
        o.effectDistance = new Vector2(2.2f, -2.2f);

        // 陣営マーカー帯（上端）
        var banner = new GameObject("OwnerBand");
        banner.transform.SetParent(transform, false);
        var brt = banner.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 1f);
        brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(96f, 8f);
        brt.anchoredPosition = new Vector2(0f, -4f);
        banner.AddComponent<Image>().color = ownerPlayer.isPlayer1
            ? new Color(0.3f, 0.55f, 0.95f, 0.95f)
            : new Color(0.9f, 0.4f, 0.3f, 0.95f);

        nameText = MakeText("Name", new Vector2(0, 28), new Vector2(90, 26), 13);
        nameText.text = cardData.cardName;
        nameText.color = new Color(0.97f, 0.92f, 0.78f);

        statsText = MakeText("Stats", new Vector2(0, -30), new Vector2(90, 30), 19);
        statsText.fontStyle = FontStyle.Bold;

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (statsText != null) statsText.text = $"⚔{atk}  ♥{hp}";
    }

    private Text MakeText(string n, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.text = "";
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0.04f, 0.02f, 0.95f);
        o.effectDistance = new Vector2(1.1f, -1.1f);
        return t;
    }

    /// <summary>セル間を移動する（見た目の親も付け替える）。</summary>
    public void MoveTo(int newLane, int newCol, Transform cellParent)
    {
        lane = newLane;
        col = newCol;
        transform.SetParent(cellParent, false);
        var rt = GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;
    }
}
