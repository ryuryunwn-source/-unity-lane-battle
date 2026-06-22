using UnityEngine;

/// <summary>
/// シーン実行時にレーン対戦を初期化する。
/// 盤面のLaneCellを集めて2D配列化し、プレイヤーを生成してLaneGameManagerに渡す。
/// （Transform[,]はシーンに直列化できないため、ランタイムで組み立てる）
/// </summary>
[RequireComponent(typeof(LaneGameManager))]
public class LaneSetup : MonoBehaviour
{
    public LaneUI ui;

    private void Start()
    {
        var gm = GetComponent<LaneGameManager>();

        // セル収集
        var cells = new Transform[LaneBoard.Lanes, LaneBoard.Cells];
        foreach (var cell in FindObjectsByType<LaneCell>(FindObjectsSortMode.None))
        {
            if (cell.lane >= 0 && cell.lane < LaneBoard.Lanes &&
                cell.col >= 0 && cell.col < LaneBoard.Cells)
                cells[cell.lane, cell.col] = cell.transform;
        }

        if (ui == null) ui = FindFirstObjectByType<LaneUI>();

        var p1 = new LanePlayer("Player 1", true, LaneDeckFactory.CreateDeck());
        var p2 = new LanePlayer("Player 2", false, LaneDeckFactory.CreateDeck());

        gm.Configure(p1, p2, cells, ui);
        gm.BeginWhenReady();
    }
}
