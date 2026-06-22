using UnityEngine;

/// <summary>
/// シーン読み込み後に自動実行され、タイトル画面(TitleMenu)を生成する。
/// シーンを手動編集せずにモード選択を差し込むためのフック。
/// </summary>
public static class AppBootstrap
{
    // Play開始時に必ずモードをリセット（ドメインリロード無効でも静的状態が残らないように）。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionOnPlay()
    {
        GameSession.Reset();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        // まだモード未選択のときだけタイトルを出す（再読み込み等での二重生成を防ぐ）
        if (GameSession.Mode != GameMode.None) return;
        if (Object.FindFirstObjectByType<TitleMenu>() != null) return;

        GameObject go = new GameObject("TitleMenu");
        go.AddComponent<TitleMenu>();
    }
}
