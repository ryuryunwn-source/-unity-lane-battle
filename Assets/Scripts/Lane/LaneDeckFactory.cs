using System.Collections.Generic;
using UnityEngine;

/// <summary>侵攻ライン用のサンプルデッキ（モンスターのみ）を生成する。</summary>
public static class LaneDeckFactory
{
    public static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();

        // 軽量・先兵（低コスト・前線を埋める）
        for (int i = 0; i < 4; i++) deck.Add(Make("斥候ゴブリン", 1, 2, 1, "コスト1の軽量先兵"));
        for (int i = 0; i < 4; i++) deck.Add(Make("槍兵", 2, 2, 3, "コスト2のバランス型"));
        // 中堅
        for (int i = 0; i < 3; i++) deck.Add(Make("重装兵", 3, 3, 5, "高HPで前線を支える壁"));
        for (int i = 0; i < 3; i++) deck.Add(Make("斧戦士", 3, 5, 2, "高ATKの突破役"));
        // 大型
        for (int i = 0; i < 2; i++) deck.Add(Make("石像兵", 4, 4, 7, "コスト4の鈍重な大型"));
        for (int i = 0; i < 2; i++) deck.Add(Make("古竜", 5, 7, 6, "コスト5の決定力"));

        return deck;
    }

    private static CardData Make(string name, int cost, int atk, int hp, string desc)
    {
        CardData d = ScriptableObject.CreateInstance<CardData>();
        d.cardName = name;
        d.cardType = CardType.Monster;
        d.cost = cost;
        d.attack = atk;
        d.defense = hp;
        d.description = desc;
        return d;
    }
}
