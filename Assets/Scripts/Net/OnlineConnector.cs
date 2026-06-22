using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

/// <summary>
/// Unity Gaming Services の初期化・匿名認証・Relayでのホスト割り当て/参加を担当する。
/// NetworkManager（UnityTransport付き）がシーンに存在している前提。
/// UGS連携（Project Settings → Services でのサインイン）が済んでいないと実際の接続は失敗する。
/// </summary>
public class OnlineConnector : MonoBehaviour
{
    public static OnlineConnector Instance { get; private set; }

    [Tooltip("ホスト含め最大接続人数（このゲームは2人対戦なので相手1人ぶん）。")]
    public int maxConnections = 1;

    public const string ConnectionType = "dtls";

    // 進捗・結果を画面に伝えるためのコールバック
    public event Action<string> OnStatus;          // 状況メッセージ
    public event Action<string> OnHostCodeReady;   // ホスト：発行された参加コード
    public event Action<string> OnError;           // エラー文言

    private bool servicesReady = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>UGS初期化＋匿名サインイン（多重呼び出し安全）。</summary>
    public async Task EnsureServicesAsync()
    {
        if (servicesReady) return;

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            OnStatus?.Invoke("サービス初期化中...");
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            OnStatus?.Invoke("サインイン中...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        servicesReady = true;
    }

    /// <summary>ホストとして開始し、参加コードを発行する。</summary>
    public async void StartHost()
    {
        try
        {
            await EnsureServicesAsync();

            OnStatus?.Invoke("ホスト枠を確保中...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            GameSession.RelayJoinCode = joinCode;

            UnityTransport transport = GetTransport();
            transport.SetRelayServerData(new RelayServerData(allocation, ConnectionType));

            GameSession.Mode = GameMode.OnlineHost;

            if (!NetworkManager.Singleton.StartHost())
            {
                Fail("ホスト開始に失敗しました");
                return;
            }

            OnHostCodeReady?.Invoke(joinCode);
            OnStatus?.Invoke($"参加コード: {joinCode}（相手の接続待ち）");
        }
        catch (Exception e)
        {
            Fail("ホスト開始エラー: " + e.Message);
        }
    }

    /// <summary>参加コードを使ってクライアントとして接続する。</summary>
    public async void StartClient(string joinCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Fail("参加コードを入力してください");
                return;
            }
            joinCode = joinCode.Trim().ToUpperInvariant();

            await EnsureServicesAsync();

            OnStatus?.Invoke("接続中...");
            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = GetTransport();
            transport.SetRelayServerData(new RelayServerData(join, ConnectionType));

            GameSession.Mode = GameMode.OnlineClient;
            GameSession.RelayJoinCode = joinCode;

            if (!NetworkManager.Singleton.StartClient())
            {
                Fail("接続開始に失敗しました");
                return;
            }

            OnStatus?.Invoke("ホストに接続しました");
        }
        catch (Exception e)
        {
            Fail("接続エラー: " + e.Message);
        }
    }

    // ===== 同一PC直結（Multiplayer Play Mode テスト用 / UGS不要） =====
    // localhost(127.0.0.1)で直接ホスト/参加する。Relayを介さないのでUGS連携なしで動く。

    public void StartLocalHost()
    {
        try
        {
            GetTransport().SetConnectionData("127.0.0.1", 7777);
            GameSession.Mode = GameMode.OnlineHost;
            if (!NetworkManager.Singleton.StartHost())
            {
                Fail("ローカルホスト開始に失敗しました");
                return;
            }
            OnStatus?.Invoke("同一PCホスト開始（相手の参加待ち / localhost:7777）");
        }
        catch (Exception e) { Fail("ローカルホストエラー: " + e.Message); }
    }

    public void StartLocalClient()
    {
        try
        {
            GetTransport().SetConnectionData("127.0.0.1", 7777);
            GameSession.Mode = GameMode.OnlineClient;
            if (!NetworkManager.Singleton.StartClient())
            {
                Fail("ローカル参加開始に失敗しました");
                return;
            }
            OnStatus?.Invoke("localhost:7777 に接続中...");
        }
        catch (Exception e) { Fail("ローカル参加エラー: " + e.Message); }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    private UnityTransport GetTransport()
    {
        UnityTransport t = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (t == null)
            throw new Exception("NetworkManagerにUnityTransportがありません");
        return t;
    }

    private void Fail(string msg)
    {
        Debug.LogError("[OnlineConnector] " + msg);
        OnError?.Invoke(msg);
    }
}
