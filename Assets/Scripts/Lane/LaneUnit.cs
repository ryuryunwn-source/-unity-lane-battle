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
        bg.color = ownerPlayer.isPlayer1
            ? new Color(0.2f, 0.45f, 0.7f, 0.95f)   // P1=青
            : new Color(0.7f, 0.3f, 0.2f, 0.95f);   // P2=赤

        Outline o = gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.85f, 0.7f, 0.3f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);

        nameText = MakeText("Name", new Vector2(0, 30), new Vector2(92, 28), 13);
        nameText.text = cardData.cardName;

        statsText = MakeText("Stats", new Vector2(0, -28), new Vector2(92, 30), 18);
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
