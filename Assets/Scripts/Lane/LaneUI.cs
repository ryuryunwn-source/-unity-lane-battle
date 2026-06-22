using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>侵攻ラインのUI。ベースHP/MP/ターン表示と、現在プレイヤーの手札描画を担当。</summary>
public class LaneUI : MonoBehaviour
{
    public Text p1BaseText;
    public Text p2BaseText;
    public Text p1MpText;
    public Text p2MpText;
    public Text turnText;
    public Button endTurnButton;
    public Transform handContainer;

    public GameObject bannerPanel;
    public Text bannerText;
    public GameObject gameOverPanel;
    public Text gameOverText;
    public Button restartButton;

    private void Start()
    {
        if (endTurnButton) endTurnButton.onClick.AddListener(() => LaneGameManager.Instance?.EndTurn());
        if (restartButton) restartButton.onClick.AddListener(OnRestart);
        if (bannerPanel) bannerPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void Render()
    {
        var gm = LaneGameManager.Instance;
        if (gm == null || gm.player1 == null || gm.player2 == null) return;

        if (p1BaseText) p1BaseText.text = $"P1 ベース: {gm.player1.BaseHP}/{gm.player1.MaxBaseHP}";
        if (p2BaseText) p2BaseText.text = $"P2 ベース: {gm.player2.BaseHP}/{gm.player2.MaxBaseHP}";
        if (p1MpText) p1MpText.text = $"MP: {gm.player1.MP}/{gm.player1.MaxMP}";
        if (p2MpText) p2MpText.text = $"MP: {gm.player2.MP}/{gm.player2.MaxMP}";
        if (turnText && gm.CurrentPlayer != null)
        {
            string baseLabel = gm.CurrentPlayer.isPlayer1 ? "▶ Player 1 のターン" : "▶ Player 2 のターン";
            baseLabel = $"時代Lv.{gm.EraLevel}　" + baseLabel;
            string hint = SelectionHint(gm);
            turnText.text = string.IsNullOrEmpty(hint) ? baseLabel : baseLabel + "　" + hint;
        }

        RebuildHand(gm);
    }

    /// <summary>選択中カードに応じた操作ガイド文。</summary>
    private string SelectionHint(LaneGameManager gm)
    {
        if (gm.SelectedHandIndex < 0 || gm.CurrentPlayer == null) return "";
        if (gm.SelectedHandIndex >= gm.CurrentPlayer.hand.Count) return "";
        CardData card = gm.CurrentPlayer.hand[gm.SelectedHandIndex];

        if (card.cardType == CardType.Monster)
            return "自陣端の空きマスをクリックで召喚";

        switch (card.itemEffect)
        {
            case LaneItem.Firebolt: return "対象レーンのマスをクリック";
            case LaneItem.Buff:     return "自分のユニットをクリック";
            case LaneItem.Rockfall: return "相手のユニットをクリック";
            case LaneItem.Retreat:  return "後退させる自分のユニットをクリック";
            default: return "";
        }
    }

    private void RebuildHand(LaneGameManager gm)
    {
        if (handContainer == null || gm.CurrentPlayer == null) return;

        for (int i = handContainer.childCount - 1; i >= 0; i--)
            Destroy(handContainer.GetChild(i).gameObject);

        var hand = gm.CurrentPlayer.hand;
        for (int i = 0; i < hand.Count; i++)
        {
            CardData card = hand[i];
            bool selected = (i == gm.SelectedHandIndex);
            bool affordable = gm.CanAffordCurrent(card);
            CreateHandCard(card, i, selected, affordable);
        }
    }

    private void CreateHandCard(CardData card, int index, bool selected, bool affordable)
    {
        GameObject go = new GameObject($"Hand_{card.cardName}");
        go.transform.SetParent(handContainer, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 160f);

        Image bg = go.AddComponent<Image>();
        Sprite frame = AncientArt.CardFrame;
        if (frame != null)
        {
            bg.sprite = frame;
            bg.type = Image.Type.Sliced;
            bg.color = selected ? new Color(1f, 0.92f, 0.55f, 1f)
                                : (affordable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.85f));
        }
        else
        {
            bg.color = selected ? new Color(0.85f, 0.7f, 0.25f, 0.98f)
                                : (affordable ? new Color(0.22f, 0.3f, 0.4f, 0.95f)
                                              : new Color(0.18f, 0.18f, 0.2f, 0.7f));
        }

        Outline o = go.AddComponent<Outline>();
        o.effectColor = selected ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.75f, 0.6f, 0.3f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);

        go.AddComponent<LaneHandCard>().handIndex = index;

        MakeText(go.transform, "Name", card.cardName, new Vector2(0, 56), new Vector2(112, 28), 14,
            new Color(0.97f, 0.92f, 0.78f));

        // コストバッジ（左上・歯車アイコン）
        GameObject costBadge = new GameObject("CostBadge");
        costBadge.transform.SetParent(go.transform, false);
        RectTransform crt = costBadge.AddComponent<RectTransform>();
        crt.sizeDelta = new Vector2(34f, 34f);
        crt.anchoredPosition = new Vector2(-42f, 56f);
        Image costImg = costBadge.AddComponent<Image>();
        if (AncientArt.IconCost != null) { costImg.sprite = AncientArt.IconCost; costImg.color = Color.white; }
        else costImg.color = new Color(0.9f, 0.7f, 0f);
        var gm = LaneGameManager.Instance;
        int effCost = gm != null ? gm.EffectiveCost(card) : card.cost;
        MakeText(costBadge.transform, "Cost", effCost.ToString(), Vector2.zero, new Vector2(34f, 34f), 18, Color.white);

        if (card.cardType == CardType.Item)
        {
            // アイテム: ステータスの代わりに効果キーワードを表示
            MakeText(go.transform, "Kind", LaneEffectInfo.Keyword(card.itemEffect),
                new Vector2(0, -6), new Vector2(112, 28), 14, new Color(1f, 0.8f, 0.4f));
        }
        else
        {
            // 時代レベルの強化を反映した実ステータスをプレビュー表示
            int atkBonus = gm != null ? gm.atkPerLevel * (gm.EraLevel - 1) : 0;
            int hpBonus = gm != null ? gm.hpPerLevel * (gm.EraLevel - 1) : 0;
            string atkStr = atkBonus > 0 ? $"{card.attack}+{atkBonus}" : card.attack.ToString();
            string hpStr = hpBonus > 0 ? $"{card.defense}+{hpBonus}" : card.defense.ToString();
            MakeText(go.transform, "Stats", $"⚔{atkStr}  ♥{hpStr}", new Vector2(0, -6), new Vector2(112, 28), 16,
                new Color(0.97f, 0.92f, 0.78f));
            // ユニット特殊効果のキーワード
            string kw = LaneEffectInfo.Keyword(card.laneEffect);
            if (!string.IsNullOrEmpty(kw))
                MakeText(go.transform, "Effect", kw, new Vector2(0, -32), new Vector2(112, 22), 11,
                    new Color(1f, 0.85f, 0.4f));
        }

        MakeText(go.transform, "Desc", card.description, new Vector2(0, -58), new Vector2(108, 40), 9,
            new Color(0.82f, 0.8f, 0.72f));
    }

    private Text MakeText(Transform parent, string n, string content, Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    public void ShowBanner(string message)
    {
        if (bannerPanel == null) return;
        if (bannerText) bannerText.text = message;
        StopAllCoroutines();
        StartCoroutine(BannerRoutine());
    }

    private IEnumerator BannerRoutine()
    {
        bannerPanel.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        bannerPanel.SetActive(false);
    }

    public void ShowGameOver(string message)
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        if (gameOverText) gameOverText.text = message;
    }

    private void OnRestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
