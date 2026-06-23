using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 侵攻ラインのプレイヤー。ベースHP・MP・デッキ・手札を持つ。
/// 進軍方向はPlayer1が右(+1)、Player2が左(-1)。
/// </summary>
public class LanePlayer
{
    public string playerName;
    public bool isPlayer1;

    public int MaxBaseHP = 20;
    public int BaseHP;
    public int MaxMP = 10;
    public int MP;
    public int TurnMPGain = 1;

    // 盤外の経済: ゴールドで強化レベルを上げ、自軍全体を底上げする
    public int Gold;
    public int PowerLevel;

    public List<CardData> deckData = new List<CardData>();
    public List<CardData> deckPile = new List<CardData>();
    public List<CardData> hand = new List<CardData>();

    public const int MaxHand = 7;

    /// <summary>自陣の召喚列。Player1=0(左端) / Player2=4(右端)。</summary>
    public int HomeColumn => isPlayer1 ? 0 : LaneBoard.Cells - 1;
    /// <summary>進軍方向。Player1=+1 / Player2=-1。</summary>
    public int Direction => isPlayer1 ? 1 : -1;

    private int fatigue = 0;

    public LanePlayer(string name, bool p1, List<CardData> deck)
    {
        playerName = name;
        isPlayer1 = p1;
        deckData = deck;
    }

    public void Initialize()
    {
        BaseHP = MaxBaseHP;
        MP = 0;
        TurnMPGain = 1;
        fatigue = 0;
        Gold = 0;
        PowerLevel = 0;
        hand.Clear();
        deckPile = new List<CardData>(deckData);
        Shuffle();
    }

    public void StartTurn()
    {
        TurnMPGain = Mathf.Min(TurnMPGain + 1, MaxMP);
        MP = Mathf.Min(MP + TurnMPGain, MaxMP);
        DrawCard();
    }

    public void DrawCard()
    {
        if (hand.Count >= MaxHand) return;
        if (deckPile.Count == 0)
        {
            // 山札切れ：疲労ダメージでベースHPを削る（膠着防止）
            fatigue++;
            BaseHP = Mathf.Max(0, BaseHP - fatigue);
            Debug.Log($"{playerName}: 山札切れ 疲労ダメージ-{fatigue} → ベースHP{BaseHP}");
            return;
        }
        hand.Add(deckPile[0]);
        deckPile.RemoveAt(0);
    }

    public bool CanAfford(CardData card) => MP >= card.cost;

    public void Spend(int amount) => MP = Mathf.Max(0, MP - amount);

    public void TakeBaseDamage(int dmg)
    {
        BaseHP = Mathf.Max(0, BaseHP - dmg);
    }

    private void Shuffle()
    {
        for (int i = deckPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deckPile[i], deckPile[j]) = (deckPile[j], deckPile[i]);
        }
    }
}
