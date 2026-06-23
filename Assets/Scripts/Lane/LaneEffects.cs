/// <summary>
/// 侵攻ライン用の効果定義。
/// ユニット特殊効果(LaneEffect)とアイテム効果(LaneItem)を区別する。
/// </summary>
public enum LaneEffect
{
    None = 0,
    Charge,    // 突撃: 召喚したターンの進軍でもう1マス進む（速攻）
    Bond,      // 隣接強化: 上下どちらかの隣レーンに味方がいる間ATK+1
    Trample,   // 貫通: 戦闘で敵を倒し攻撃力が余ったら超過分を相手ベースへ
    Guard,     // 守備: 一度だけ致死ダメージを耐えHP1で残る
    Explode,   // 爆散: 破壊時、同レーンの前後の敵に2ダメージ
}

public enum LaneItem
{
    None = 0,
    Firebolt,  // 火炎弾: 指定レーンの敵ユニット全員に3ダメージ
    Buff,      // 強化の薬: 自分のユニット1体をATK+2/HP+2
    Rockfall,  // 落石: 指定セルの敵ユニット1体を破壊
    Retreat,   // 退却ラッパ: 自分のユニット1体を自陣端まで後退
    TimeSand,  // 時の砂: このターン、自軍全ユニットが進軍でもう1マス進む
    Meteor,    // 隕石嵐: 盤面の敵ユニット全員にダメージ（時代アンロック）
    WarCry,    // 鬨の声: 自軍全ユニットをATK+2/HP+2（時代アンロック）
}

/// <summary>レーンごとの地形（環境）効果。</summary>
public enum LaneTerrain
{
    Plain = 0,  // 平地: 効果なし
    Swift,      // 疾風: このレーンのユニットは毎ターン+1マス進軍
    Forge,      // 鍛冶場: このレーンにいる間ATK+2
    Thorns,     // 茨: このレーンの自軍は毎ターン1ダメージ
    Bastion,    // 砦: このレーンに召喚するとHP+2
}

public static class LaneTerrainInfo
{
    public static string Name(LaneTerrain t)
    {
        switch (t)
        {
            case LaneTerrain.Swift:   return "疾風";
            case LaneTerrain.Forge:   return "鍛冶場";
            case LaneTerrain.Thorns:  return "茨";
            case LaneTerrain.Bastion: return "砦";
            default: return "平地";
        }
    }

    /// <summary>地形を示すセルの淡い色。</summary>
    public static UnityEngine.Color Tint(LaneTerrain t)
    {
        switch (t)
        {
            case LaneTerrain.Swift:   return new UnityEngine.Color(0.25f, 0.45f, 0.6f, 0.5f);  // 青
            case LaneTerrain.Forge:   return new UnityEngine.Color(0.55f, 0.25f, 0.18f, 0.5f); // 赤
            case LaneTerrain.Thorns:  return new UnityEngine.Color(0.3f, 0.4f, 0.2f, 0.5f);    // 毒緑
            case LaneTerrain.Bastion: return new UnityEngine.Color(0.5f, 0.42f, 0.2f, 0.5f);   // 金
            default: return new UnityEngine.Color(0.16f, 0.15f, 0.12f, 0.5f);                  // 平地
        }
    }
}

public static class LaneEffectInfo
{
    public static string Keyword(LaneEffect e)
    {
        switch (e)
        {
            case LaneEffect.Charge:  return "【突撃】";
            case LaneEffect.Bond:    return "【隣接強化】";
            case LaneEffect.Trample: return "【貫通】";
            case LaneEffect.Guard:   return "【守備】";
            case LaneEffect.Explode: return "【爆散】";
            default: return "";
        }
    }

    public static string Keyword(LaneItem i)
    {
        switch (i)
        {
            case LaneItem.Firebolt: return "【火炎弾】";
            case LaneItem.Buff:     return "【強化の薬】";
            case LaneItem.Rockfall: return "【落石】";
            case LaneItem.Retreat:  return "【退却】";
            case LaneItem.TimeSand: return "【時の砂】";
            case LaneItem.Meteor:   return "【隕石嵐】";
            case LaneItem.WarCry:   return "【鬨の声】";
            default: return "";
        }
    }

    /// <summary>アイテム使用時に対象クリックが必要か（盤面全体系・時の砂は即時発動で不要）。</summary>
    public static bool NeedsTarget(LaneItem i)
        => i == LaneItem.Firebolt || i == LaneItem.Buff || i == LaneItem.Rockfall || i == LaneItem.Retreat;
}
