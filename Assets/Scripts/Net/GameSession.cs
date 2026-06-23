/// <summary>
/// 選択中のゲームモードと、オンライン接続用の参加コードを保持する静的クラス。
/// タイトル画面で設定し、ゲーム本体（GameManager）が参照する。
/// </summary>
public enum GameMode
{
    None,        // 未選択（タイトル表示中）
    Local,       // 1台で2人交互プレイ（従来方式）
    OnlineHost,  // オンライン: ホスト（サーバー権威）
    OnlineClient // オンライン: 参加（コードで接続）
}

public static class GameSession
{
    public static GameMode Mode = GameMode.None;

    /// <summary>ローカル対戦で相手(player2)をAIが操作するか。</summary>
    public static bool VsAI = false;

    /// <summary>AI観戦：両プレイヤーをAIが操作する。</summary>
    public static bool SpectateAI = false;

    /// <summary>Relayの参加コード（ホストが発行 / 参加者が入力）。</summary>
    public static string RelayJoinCode = "";

    public static bool IsOnline => Mode == GameMode.OnlineHost || Mode == GameMode.OnlineClient;

    /// <summary>このクライアントがゲームロジックの権威（サーバー）か。ローカルとホストはtrue。</summary>
    public static bool IsAuthority => Mode == GameMode.Local || Mode == GameMode.OnlineHost;

    public static void Reset()
    {
        Mode = GameMode.None;
        VsAI = false;
        SpectateAI = false;
        RelayJoinCode = "";
    }
}
