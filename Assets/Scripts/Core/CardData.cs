using UnityEngine;

public enum CardType
{
    Monster,
    Heal,
    Defense,
    MPRecovery,
    Ultimate, // 必殺カード: 自分の場にモンスターが3体以上いる時だけ使用可能。相手に直接ダメージ
    Trap      // 秘策カード: セットすると伏せられ、相手の攻撃時に自動発動して無効化＋反撃ダメージ
}

[CreateAssetMenu(fileName = "NewCard", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本情報")]
    public string cardName = "カード名";
    public CardType cardType = CardType.Monster;
    public int cost = 1;
    [TextArea(2, 4)]
    public string description = "カードの説明";
    public Sprite artwork;

    [Header("モンスターステータス (モンスターカードのみ)")]
    public int attack = 1;
    public int defense = 1;

    [Header("効果量 (モンスター以外)")]
    public int effectAmount = 3; // 回復量 / 防御量 / MP回復量
}
