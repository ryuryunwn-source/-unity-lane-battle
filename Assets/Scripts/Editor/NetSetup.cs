#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// オンライン対戦に必要な NetworkManager と NetGameSync を、開いているシーンに自動配置する。
/// メニュー Tools → Net → Setup Networking を一度実行すればよい。
/// </summary>
public static class NetSetup
{
    [MenuItem("Tools/Net/Setup Networking")]
    public static void SetupNetworking()
    {
        // ===== NetworkManager =====
        NetworkManager nm = Object.FindFirstObjectByType<NetworkManager>();
        if (nm == null)
        {
            GameObject go = new GameObject("NetworkManager");
            nm = go.AddComponent<NetworkManager>();
            Debug.Log("[NetSetup] NetworkManager を作成しました");
        }

        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
            transport = nm.gameObject.AddComponent<UnityTransport>();

        // Protocolは読み取り専用。SetRelayServerData()呼び出し時に自動でRelayモードへ切り替わる。

        if (nm.NetworkConfig == null)
            nm.NetworkConfig = new NetworkConfig();
        nm.NetworkConfig.NetworkTransport = transport;
        nm.NetworkConfig.EnableSceneManagement = true;
        nm.NetworkConfig.ConnectionApproval = false;

        // ※ レーン制のオンライン同期(LaneNetSync)は後続作業で追加予定。
        //   現状はNetworkManager/UnityTransportのみ配置する（ローカル対戦は同期不要）。

        // 変更を保存
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[NetSetup] 完了。NetworkManager を配置・保存しました。");
    }
}
#endif
