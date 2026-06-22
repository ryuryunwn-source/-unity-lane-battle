using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 侵攻ライン（レーン制対戦）の進行管理。
/// ・自分のターン: MP獲得 → ドロー → 手札を自陣端セルに召喚 → ターン終了
/// ・ターン終了時: 自軍ユニットが敵方向へ1セル前進。敵ユニットに当たれば自動戦闘、
///   敵陣を突破すると相手ベースHPにATK分ダメージ。
/// ・相手ベースHPを0にしたら勝利。
/// 現状はローカル2人対戦（ホットシート）。オンライン同期は後続作業。
/// </summary>
public class LaneGameManager : MonoBehaviour
{
    public static LaneGameManager Instance { get; private set; }

    [Header("初期設定")]
    public int initialHandSize = 4;

    // シーンビルダーが設定する
    public Transform[,] cells;          // [lane, col] のセルTransform
    public Transform unitLayer;         // ユニット生成の親（未使用: セルに直接付ける）
    public LaneUI ui;

    public LaneBoard board = new LaneBoard();
    public LanePlayer player1;
    public LanePlayer player2;
    public LanePlayer CurrentPlayer { get; private set; }
    public LanePlayer OpponentPlayer { get; private set; }

    public int SelectedHandIndex { get; private set; } = -1;
    public bool GameOver { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>シーンビルダーから呼ばれ、プレイヤーとセル参照を受け取って開始準備する。</summary>
    public void Configure(LanePlayer p1, LanePlayer p2, Transform[,] cellTransforms, LaneUI laneUI)
    {
        player1 = p1;
        player2 = p2;
        cells = cellTransforms;
        ui = laneUI;
    }

    public void BeginWhenReady()
    {
        StartCoroutine(WaitThenStart());
    }

    private IEnumerator WaitThenStart()
    {
        while (GameSession.Mode == GameMode.None)
            yield return null;

        // オンラインは後続作業。現状はローカルのみ正式対応。
        StartGame();
    }

    public void StartGame()
    {
        GameOver = false;
        board.Clear();
        player1.Initialize();
        player2.Initialize();

        for (int i = 0; i < initialHandSize; i++)
        {
            player1.DrawCard();
            player2.DrawCard();
        }

        StartTurn(player1, player2);
    }

    public void StartTurn(LanePlayer current, LanePlayer opponent)
    {
        CurrentPlayer = current;
        OpponentPlayer = opponent;
        SelectedHandIndex = -1;

        current.StartTurn();
        ui?.Render();
        ui?.ShowBanner(current.isPlayer1 ? "Player 1 のターン" : "Player 2 のターン");
        Debug.Log($"=== {current.playerName} のターン ===");
    }

    // ===== 入力 =====
    public void OnHandCardClicked(int index)
    {
        if (GameOver) return;
        if (index < 0 || index >= CurrentPlayer.hand.Count) return;

        CardData card = CurrentPlayer.hand[index];
        if (!CurrentPlayer.CanAfford(card))
        {
            Debug.Log($"MPが足りません (必要:{card.cost} 現在:{CurrentPlayer.MP})");
            return;
        }
        SelectedHandIndex = (SelectedHandIndex == index) ? -1 : index;
        ui?.Render();
    }

    public void OnCellClicked(int lane, int col)
    {
        if (GameOver) return;
        if (SelectedHandIndex < 0) return;
        // 召喚は自陣の端列のみ
        if (col != CurrentPlayer.HomeColumn)
        {
            Debug.Log("召喚は自陣の端の列にのみ可能です");
            return;
        }
        if (!board.IsEmpty(lane, col))
        {
            Debug.Log("そのセルは埋まっています");
            return;
        }

        CardData card = CurrentPlayer.hand[SelectedHandIndex];
        if (!CurrentPlayer.CanAfford(card)) return;

        CurrentPlayer.Spend(card.cost);
        CurrentPlayer.hand.RemoveAt(SelectedHandIndex);
        SelectedHandIndex = -1;

        SpawnUnit(card, CurrentPlayer, lane, col);
        ui?.Render();
    }

    private LaneUnit SpawnUnit(CardData card, LanePlayer owner, int lane, int col)
    {
        GameObject go = new GameObject($"Unit_{card.cardName}");
        LaneUnit unit = go.AddComponent<LaneUnit>();
        unit.Setup(card, owner, lane, col, cells[lane, col]);
        board.Set(lane, col, unit);
        Debug.Log($"{owner.playerName}: {card.cardName} をレーン{lane}に召喚");
        return unit;
    }

    // ===== ターン終了 → 進軍フェーズ =====
    public void EndTurn()
    {
        if (GameOver) return;
        SelectedHandIndex = -1;

        AdvancePhase(CurrentPlayer);
        ui?.Render();

        if (CheckWin()) return;

        if (CurrentPlayer == player1) StartTurn(player2, player1);
        else StartTurn(player1, player2);
    }

    private void AdvancePhase(LanePlayer player)
    {
        int dir = player.Direction;

        // 前方のユニットから処理して移動先を空ける。
        // P1(右進行)は列が大きい方から、P2(左進行)は列が小さい方から。
        for (int lane = 0; lane < LaneBoard.Lanes; lane++)
        {
            if (dir > 0)
            {
                for (int col = LaneBoard.Cells - 1; col >= 0; col--)
                    AdvanceUnit(player, lane, col);
            }
            else
            {
                for (int col = 0; col < LaneBoard.Cells; col++)
                    AdvanceUnit(player, lane, col);
            }
        }
    }

    private void AdvanceUnit(LanePlayer player, int lane, int col)
    {
        LaneUnit unit = board.Get(lane, col);
        if (unit == null || unit.owner != player || !unit.IsAlive) return;

        int target = col + player.Direction;

        // 敵陣を突破 → 相手ベースへ直接ダメージ
        bool reachedEnemyBase = (player.Direction > 0) ? (target >= LaneBoard.Cells) : (target < 0);
        if (reachedEnemyBase)
        {
            OpponentOf(player).TakeBaseDamage(unit.atk);
            Debug.Log($"{unit.data.cardName} が相手ベースに{unit.atk}ダメージ！");
            return;
        }

        LaneUnit occupant = board.Get(lane, target);
        if (occupant == null)
        {
            // 前進
            board.Set(lane, col, null);
            board.Set(lane, target, unit);
            unit.MoveTo(lane, target, cells[lane, target]);
        }
        else if (occupant.owner != unit.owner)
        {
            // 自動戦闘
            Combat(unit, occupant);
        }
        // 味方が前にいる場合は前進できず待機
    }

    private void Combat(LaneUnit a, LaneUnit b)
    {
        Debug.Log($"戦闘: {a.data.cardName}(⚔{a.atk}/♥{a.hp}) vs {b.data.cardName}(⚔{b.atk}/♥{b.hp})");
        int da = a.atk, db = b.atk;
        a.hp -= db;
        b.hp -= da;
        a.RefreshVisual();
        b.RefreshVisual();
        if (!a.IsAlive) RemoveUnit(a);
        if (!b.IsAlive) RemoveUnit(b);
    }

    private void RemoveUnit(LaneUnit u)
    {
        if (board.Get(u.lane, u.col) == u) board.Set(u.lane, u.col, null);
        Debug.Log($"{u.data.cardName} が破壊された");
        Destroy(u.gameObject);
    }

    private LanePlayer OpponentOf(LanePlayer p) => (p == player1) ? player2 : player1;

    private bool CheckWin()
    {
        if (player1.BaseHP <= 0)
        {
            GameOver = true;
            ui?.ShowGameOver("Player 2 の勝利！");
            return true;
        }
        if (player2.BaseHP <= 0)
        {
            GameOver = true;
            ui?.ShowGameOver("Player 1 の勝利！");
            return true;
        }
        return false;
    }
}
