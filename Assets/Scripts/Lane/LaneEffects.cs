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
