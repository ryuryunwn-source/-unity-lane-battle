using UnityEngine;

/// <summary>
/// 古代遺跡テーマの素材（Assets/Resources/Art）をキャッシュして提供する。
/// </summary>
public static class AncientArt
{
    private static Sprite cardFrame;
    private static Sprite board;
    private static Sprite btnTurnEnd;
    private static Sprite barHp;
    private static Sprite iconDeck;
    private static Sprite iconCost;

    public static Sprite CardFrame  => cardFrame  ??= Resources.Load<Sprite>("Art/card_frame");
    public static Sprite Board      => board      ??= Resources.Load<Sprite>("Art/board_bg");
    public static Sprite BtnTurnEnd => btnTurnEnd ??= Resources.Load<Sprite>("Art/btn_turn_end");
    public static Sprite BarHp      => barHp      ??= Resources.Load<Sprite>("Art/bar_hp");
    public static Sprite IconDeck   => iconDeck   ??= Resources.Load<Sprite>("Art/icon_deck");
    public static Sprite IconCost   => iconCost   ??= Resources.Load<Sprite>("Art/icon_cost");
}
