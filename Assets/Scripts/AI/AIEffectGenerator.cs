using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google Gemini APIを使ってカード効果プールを起動時に生成・キャッシュする。
/// APIキー未設定またはAPI失敗時は内蔵エフェクトにフォールバック。
/// ドロー時に GetRandomEffect() を呼び出してランダムに効果を付与する。
///
/// 無料APIキー取得: https://aistudio.google.com (Googleアカウントで即取得可)
/// </summary>
public class AIEffectGenerator : MonoBehaviour
{
    public static AIEffectGenerator Instance { get; private set; }

    [Header("Gemini API設定")]
    [Tooltip("Google AI Studio APIキー (aistudio.google.com で無料取得)")]
    public string apiKey = "";

    [Tooltip("使用するモデル")]
    public string model = "gemini-2.0-flash";

    [Header("生成設定")]
    [Range(5, 30)]
    [Tooltip("生成するエフェクト種類数")]
    public int effectPoolSize = 15;

    [Range(0f, 1f)]
    [Tooltip("ドロー時にモンスターへ特殊効果が付く確率")]
    public float specialEffectChance = 0.55f;

    // 公開プロパティ
    public List<CardEffect> EffectPool { get; private set; } = new List<CardEffect>();
    public bool IsReady { get; private set; } = false;
    public string StatusMessage { get; private set; } = "初期化中...";

    // ===== 内蔵フォールバックエフェクト (APIなしでも遊べる) =====
    private static readonly CardEffect[] builtinEffects =
    {
        new CardEffect(SpecialEffectType.Poison,    "毒牙",       "攻撃時、相手プレイヤーに毒を仕込む（3ターン間毎ターン1ダメ）"),
        new CardEffect(SpecialEffectType.Poison,    "腐敗の一撃", "一撃が相手の体内で毒となり、3ターン蝕み続ける"),
        new CardEffect(SpecialEffectType.Lifesteal, "吸血鬼",     "攻撃で与えたダメージ分、自分のHPを回復する"),
        new CardEffect(SpecialEffectType.Lifesteal, "魂喰い",     "戦いながら相手の生命力を根こそぎ奪い取る"),
        new CardEffect(SpecialEffectType.Explosion, "自爆装置",   "破壊される瞬間に爆発し、相手プレイヤーへ3ダメージ"),
        new CardEffect(SpecialEffectType.Explosion, "殉教者",     "倒れる最後の瞬間、全力の爆破で3ダメージを叩き込む"),
        new CardEffect(SpecialEffectType.Rally,     "将軍",       "召喚時、味方モンスター全員の攻撃力+1"),
        new CardEffect(SpecialEffectType.Rally,     "軍旗持ち",   "登場しただけで仲間が奮い立ち攻撃力が高まる"),
        new CardEffect(SpecialEffectType.Grow,      "進化体",     "ターン終了時に攻撃力・HPが各+1ずつ増大し続ける"),
        new CardEffect(SpecialEffectType.Undying,   "不死鳳凰",   "初回の致死ダメージを無効化し、HP1で生き残る"),
        new CardEffect(SpecialEffectType.DoubleTap, "双剣士",     "1ターンに2回攻撃できる"),
        new CardEffect(SpecialEffectType.ManaGain,  "魔石の守護者","破壊時にMP+2を所有者へ還元する"),
        new CardEffect(SpecialEffectType.Regenerate,"再生の乙女", "ターン開始時にHPが1回復する（最大値まで）"),
        new CardEffect(SpecialEffectType.Barrier,   "鉄壁の騎士", "召喚時にこのターンの防御+3を付与する"),
        new CardEffect(SpecialEffectType.Frenzy,    "狂戦士",     "攻撃後、別のランダムな敵モンスターに1ダメージの余波"),
    };

    // ===== Gemini APIのJSON構造 =====
    [Serializable] private class GeminiPart    { public string text; }
    [Serializable] private class GeminiContent { public GeminiPart[] parts; }
    [Serializable] private class GeminiRequest { public GeminiContent[] contents; }

    // レスポンス解析用
    [Serializable] private class GeminiRespPart      { public string text; }
    [Serializable] private class GeminiRespContent   { public GeminiRespPart[] parts; }
    [Serializable] private class GeminiCandidate     { public GeminiRespContent content; }
    [Serializable] private class GeminiResponse      { public GeminiCandidate[] candidates; }

    // エフェクトJSON
    [Serializable] private class EffectEntry
    {
        public string effectType;
        public string effectName;
        public string description;
        public int value;
    }
    [Serializable] private class EffectsRoot { public EffectEntry[] effects; }

    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(apiKey))
            StartCoroutine(GenerateEffectsFromGemini());
        else
        {
            Debug.Log("[AIEffectGenerator] APIキー未設定 → 内蔵エフェクト使用");
            UseBuiltinEffects();
        }
    }

    /// <summary>ドロー時に呼び出す。specialEffectChance確率で効果を返す。</summary>
    public CardEffect GetRandomEffect()
    {
        if (!IsReady || EffectPool.Count == 0) return CardEffect.None;
        if (UnityEngine.Random.value > specialEffectChance) return CardEffect.None;
        return EffectPool[UnityEngine.Random.Range(0, EffectPool.Count)];
    }

    // ===== Gemini API 呼び出し =====
    private IEnumerator GenerateEffectsFromGemini()
    {
        StatusMessage = "AIがエフェクトを生成中...";
        Debug.Log($"[AIEffectGenerator] Gemini API ({model}) に接続中...");

        string prompt =
            $"TCGカードゲーム用の特殊効果を{effectPoolSize}種類、JSONのみで生成してください。\n" +
            "effectTypeは必ず以下から選択: Poison, Lifesteal, Explosion, Rally, Grow, Undying, DoubleTap, ManaGain, Regenerate, Barrier, Frenzy\n" +
            "effectNameとdescriptionは日本語で、個性的で面白い名前にしてください。valueは0。同じeffectTypeを複数使ってもOK。\n" +
            "```jsonや説明文など、JSON以外は一切含めず以下の形式のみで返してください:\n" +
            "{\"effects\":[{\"effectType\":\"Poison\",\"effectName\":\"毒の牙\",\"description\":\"攻撃時に毒を与える\",\"value\":0}]}";

        // Geminiのリクエスト構造
        var requestData = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    parts = new[] { new GeminiPart { text = prompt } }
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        using var www = new UnityWebRequest(url, "POST");
        www.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.timeout = 30;

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[AIEffectGenerator] Gemini応答受信、パース中...");
            ParseGeminiResponse(www.downloadHandler.text);
        }
        else
        {
            Debug.LogWarning($"[AIEffectGenerator] Gemini API失敗 ({www.responseCode}: {www.error}) → フォールバック");
            UseBuiltinEffects();
        }
    }

    private void ParseGeminiResponse(string responseJson)
    {
        try
        {
            var geminiResp = JsonUtility.FromJson<GeminiResponse>(responseJson);

            if (geminiResp?.candidates == null || geminiResp.candidates.Length == 0
                || geminiResp.candidates[0].content?.parts == null
                || geminiResp.candidates[0].content.parts.Length == 0)
            {
                Debug.LogWarning("[AIEffectGenerator] Geminiレスポンス解析失敗 → フォールバック");
                UseBuiltinEffects(); return;
            }

            string text = geminiResp.candidates[0].content.parts[0].text;

            // Geminiはたまに ```json ... ``` で囲むので取り除く
            text = text.Replace("```json", "").Replace("```", "").Trim();

            // JSON部分を抽出
            int start = text.IndexOf('{');
            int end   = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                Debug.LogWarning("[AIEffectGenerator] JSONが見つからない → フォールバック");
                Debug.LogWarning($"受信テキスト: {text}");
                UseBuiltinEffects(); return;
            }

            string effectsJson = text.Substring(start, end - start + 1);
            var root = JsonUtility.FromJson<EffectsRoot>(effectsJson);

            if (root?.effects == null || root.effects.Length == 0)
            {
                Debug.LogWarning("[AIEffectGenerator] エフェクト配列が空 → フォールバック");
                UseBuiltinEffects(); return;
            }

            EffectPool.Clear();
            int parsed = 0;
            foreach (var entry in root.effects)
            {
                if (Enum.TryParse<SpecialEffectType>(entry.effectType, out var effectType)
                    && effectType != SpecialEffectType.None)
                {
                    EffectPool.Add(new CardEffect(effectType,
                        string.IsNullOrEmpty(entry.effectName) ? entry.effectType : entry.effectName,
                        entry.description, entry.value));
                    parsed++;
                }
                else
                {
                    Debug.LogWarning($"[AIEffectGenerator] 不明なeffectType: '{entry.effectType}' (スキップ)");
                }
            }

            if (parsed == 0)
            {
                Debug.LogWarning("[AIEffectGenerator] 有効なエフェクトなし → フォールバック");
                UseBuiltinEffects(); return;
            }

            IsReady = true;
            StatusMessage = $"Gemini生成完了: {parsed}種類のエフェクト";
            Debug.Log($"[AIEffectGenerator] {StatusMessage}");

            foreach (var e in EffectPool)
                Debug.Log($"  {e.GetKeyword()} {e.effectName}: {e.description}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AIEffectGenerator] パースエラー: {ex.Message} → フォールバック");
            UseBuiltinEffects();
        }
    }

    private void UseBuiltinEffects()
    {
        EffectPool = new List<CardEffect>(builtinEffects);
        IsReady = true;
        StatusMessage = "内蔵エフェクト使用中";
        Debug.Log($"[AIEffectGenerator] 内蔵エフェクト {EffectPool.Count}種類ロード完了");
    }
}
