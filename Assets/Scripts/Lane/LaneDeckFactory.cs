using System.Collections.Generic;
using UnityEngine;

/// <summary>侵攻ライン用のサンプルデッキ（モンスターのみ）を生成する。</summary>
public static class LaneDeckFactory
{
    public static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();

        // ===== 通常モンスター =====
        for (int i = 0; i < 3; i++) deck.Add(Make("斥候ゴブリン", 1, 2, 1, "コスト1の軽量先兵"));
        for (int i = 0; i < 3; i++) deck.Add(Make("槍兵", 2, 2, 3, "コスト2のバランス型"));
        for (int i = 0; i < 2; i++) deck.Add(Make("重装兵", 3, 3, 5, "高HPで前線を支える壁"));

        // ===== 特殊効果モンスター =====
        for (int i = 0; i < 2; i++) deck.Add(Eff("疾風の狼", 2, 3, 2, LaneEffect.Charge, "召喚したターンに2マス進む"));
        for (int i = 0; i < 2; i++) deck.Add(Eff("旗手", 2, 2, 3, LaneEffect.Bond, "隣レーンに味方がいるとATK+1"));
        deck.Add(Eff("斧戦士", 3, 5, 2, LaneEffect.Trample, "倒した余剰ダメージが相手ベースへ"));
        deck.Add(Eff("守護騎士", 3, 2, 5, LaneEffect.Guard, "一度だけ致死を耐える"));
        deck.Add(Eff("爆裂ゴーレム", 4, 4, 4, LaneEffect.Explode, "破壊時、前後の敵に2ダメージ"));
        deck.Add(Eff("古竜", 5, 7, 6, LaneEffect.Trample, "コスト5の決定力。貫通持ち"));

        // ===== アイテムカード（控えめに：枚数を絞りコストを引き上げ）=====
        deck.Add(Item("火炎弾", 3, LaneItem.Firebolt, 3, "対象レーンの敵全員に3ダメージ"));
        deck.Add(Item("強化の薬", 3, LaneItem.Buff, 0, "自分のユニット1体を+2/+2"));
        deck.Add(Item("落石", 5, LaneItem.Rockfall, 0, "相手のユニット1体を破壊"));
        deck.Add(Item("退却ラッパ", 1, LaneItem.Retreat, 0, "自分のユニットを自陣端へ後退"));

        return deck;
    }

    /// <summary>時代レベルが上がった時に手札へ配られる強力カード。</summary>
    public static CardData EraCard(int level)
    {
        switch (level)
        {
            // 全体強化「鬨の声」は強すぎたため廃止し、強力モンスター中心に
            case 2: return Eff("竜騎兵", 3, 4, 3, LaneEffect.Charge, "突撃持ちの精鋭");
            case 3: return Eff("重騎士団長", 4, 4, 6, LaneEffect.Bond, "隣レーン連携で強化");
            case 4: return Eff("巨神兵", 6, 8, 8, LaneEffect.Trample, "圧倒的な巨体。貫通持ち");
            default: return Item("隕石嵐", 6, LaneItem.Meteor, 4, "盤面の敵全員に4ダメージ");
        }
    }

    private static CardData Make(string name, int cost, int atk, int hp, string desc)
        => Eff(name, cost, atk, hp, LaneEffect.None, desc);

    private static CardData Eff(string name, int cost, int atk, int hp, LaneEffect effect, string desc)
    {
        CardData d = ScriptableObject.CreateInstance<CardData>();
        d.cardName = name;
        d.cardType = CardType.Monster;
        d.cost = cost;
        d.attack = atk;
        d.defense = hp;
        d.laneEffect = effect;
        d.description = desc;
        return d;
    }

    private static CardData Item(string name, int cost, LaneItem item, int amount, string desc)
    {
        CardData d = ScriptableObject.CreateInstance<CardData>();
        d.cardName = name;
        d.cardType = CardType.Item;
        d.cost = cost;
        d.itemEffect = item;
        d.effectAmount = amount;
        d.description = desc;
        return d;
    }
}
