using System;
using UnityEngine;

/// <summary>
/// カードの特殊効果タイプ
/// AIEffectGeneratorによって生成され、ドロー時にカードへランダムに付与される
/// </summary>
public enum SpecialEffectType
{
    None = 0,
    Poison,       // 攻撃時：相手プレイヤーに毒（3ターン間毎ターン1ダメ）
    Lifesteal,    // 攻撃時：与ダメ分だけHP回復
    Explosion,    // 破壊時：相手プレイヤーに3ダメ
    Rally,        // 召喚時：味方全員ATK+1
    Grow,         // ターン終了時：ATK+1 / HP+1
    Undying,      // 一度だけ致死ダメを無効（HP1で残る）
    DoubleTap,    // 1ターンに2回攻撃可能
    ManaGain,     // 破壊時：自分のMP+2
    Regenerate,   // ターン開始時：自身HP+1（最大値まで）
    Barrier,      // 召喚時：このターンの防御+3
    Frenzy,       // 攻撃後：別のランダムな敵モンスターに1ダメ
}

[Serializable]
public class CardEffect
{
    public SpecialEffectType effectType;
    public string effectName;
    public string description;
    public int value;

    public CardEffect()
    {
        effectType = SpecialEffectType.None;
        effectName = "";
        description = "";
        value = 0;
    }

    public CardEffect(SpecialEffectType type, string name, string desc, int val = 0)
    {
        effectType = type;
        effectName = name;
        description = desc;
        value = val;
    }

    public static CardEffect None => new CardEffect(SpecialEffectType.None, "", "", 0);
    public bool HasEffect => effectType != SpecialEffectType.None;

    /// <summary>カードに表示するキーワードタグ</summary>
    public string GetKeyword()
    {
        switch (effectType)
        {
            case SpecialEffectType.Poison:     return "【毒】";
            case SpecialEffectType.Lifesteal:  return "【吸血】";
            case SpecialEffectType.Explosion:  return "【爆発】";
            case SpecialEffectType.Rally:      return "【鼓舞】";
            case SpecialEffectType.Grow:       return "【成長】";
            case SpecialEffectType.Undying:    return "【不死】";
            case SpecialEffectType.DoubleTap:  return "【連撃】";
            case SpecialEffectType.ManaGain:   return "【霊魂】";
            case SpecialEffectType.Regenerate: return "【再生】";
            case SpecialEffectType.Barrier:    return "【護壁】";
            case SpecialEffectType.Frenzy:     return "【乱打】";
            default: return "";
        }
    }
}
