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
            turnText.text = gm.CurrentPlayer.isPlayer1 ? "▶ Player 1 のターン" : "▶ Player 2 のターン";

        RebuildHand(gm);
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
            bool affordable = gm.CurrentPlayer.CanAfford(card);
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
        bg.color = selected ? new Color(0.85f, 0.7f, 0.25f, 0.98f)
                            : (affordable ? new Color(0.22f, 0.3f, 0.4f, 0.95f)
                                          : new Color(0.18f, 0.18f, 0.2f, 0.7f));

        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0.75f, 0.6f, 0.3f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);

        go.AddComponent<LaneHandCard>().handIndex = index;

        MakeText(go.transform, "Name", card.cardName, new Vector2(0, 58), new Vector2(112, 30), 14, Color.white);
        MakeText(go.transform, "Cost", $"コスト {card.cost}", new Vector2(0, 24), new Vector2(112, 26), 14,
            new Color(0.5f, 0.85f, 1f));
        MakeText(go.transform, "Stats", $"⚔{card.attack}  ♥{card.defense}", new Vector2(0, -10), new Vector2(112, 30), 18,
            Color.white);
        MakeText(go.transform, "Desc", card.description, new Vector2(0, -52), new Vector2(112, 50), 10,
            new Color(0.85f, 0.85f, 0.8f));
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
