using UnityEngine;

/// <summary>
/// 5レーン×5セルの盤面占有モデル。
/// 列0が画面左(=Player1の自陣端)、列4が画面右(=Player2の自陣端)。
/// Player1は列0→4へ、Player2は列4→0へ進軍する。
/// </summary>
public class LaneBoard
{
    public const int Lanes = 5;
    public const int Cells = 5;

    private readonly LaneUnit[,] grid = new LaneUnit[Lanes, Cells];

    public LaneUnit Get(int lane, int col)
    {
        if (lane < 0 || lane >= Lanes || col < 0 || col >= Cells) return null;
        return grid[lane, col];
    }

    public void Set(int lane, int col, LaneUnit unit)
    {
        if (lane < 0 || lane >= Lanes || col < 0 || col >= Cells) return;
        grid[lane, col] = unit;
    }

    public bool IsEmpty(int lane, int col) => Get(lane, col) == null;

    public void Clear()
    {
        for (int l = 0; l < Lanes; l++)
            for (int c = 0; c < Cells; c++)
                grid[l, c] = null;
    }
}
