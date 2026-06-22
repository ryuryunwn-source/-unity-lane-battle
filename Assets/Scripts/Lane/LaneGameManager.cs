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

    [Header("時代レベル（試合のペース調整）")]
    public int turnsPerLevel = 4;   // 何ターンごとにレベルが上がるか（4=2ラウンド）
    public int maxEraLevel = 5;     // レベル上限
    public int costPerLevel = 1;    // レベルごとに全カードのコスト+1（置き放題の抑制）
    public int atkPerLevel = 2;     // 召喚ユニットのATKをレベルごとに+2（攻撃が防御を上回り決着へ）
    public int hpPerLevel = 1;      // 召喚ユニットのHPをレベルごとに+1

    public int EraLevel { get; private set; } = 1;
    private int turnCounter = 0;

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

    [Header("中立NPC（妨害モンスター）")]
    public bool enableNeutral = true;
    public int neutralEveryTurns = 5;   // 何ターンごとに中立が出現するか
    public int neutralBaseAtk = 3;       // 中立の基礎ATK（+時代Lv）
    public int neutralBaseHp = 5;        // 中立の基礎HP（+時代Lv）

    [Header("AI対戦")]
    public bool player2IsAI = false;
    private bool aiActing = false; // AIが操作中（人間入力をロックするため）

    /// <summary>今がAI(player2)の手番か。</summary>
    private bool IsAiTurn => player2IsAI && CurrentPlayer == player2 && !GameOver;

    // このターン「時の砂」で全味方が+1マス進軍するか
    private bool timeSandActive = false;

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
        EraLevel = 1;
        turnCounter = 0;
        // モード選択後に確定するため、ここでAIフラグを読み直す
        player2IsAI = GameSession.VsAI;
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
        timeSandActive = false;

        // 時代レベルの更新（ターン経過で上昇）
        turnCounter++;
        int newLevel = Mathf.Min(maxEraLevel, 1 + turnCounter / turnsPerLevel);
        bool leveledUp = newLevel > EraLevel;
        EraLevel = newLevel;

        // レベルアップ時、その時代の強力な新カードを両者の手札へ配る
        if (leveledUp)
        {
            GrantEraCard(player1);
            GrantEraCard(player2);
        }

        // 中立NPCの出現（数ターンごと）
        if (enableNeutral && turnCounter > 0 && turnCounter % neutralEveryTurns == 0)
            SpawnNeutral();

        current.StartTurn();
        if (CheckWin()) return; // 山札切れの疲労ダメージで決着する場合がある
        ui?.Render();
        if (leveledUp)
            ui?.ShowBanner($"時代が進んだ！ 時代Lv.{EraLevel}");
        else
            ui?.ShowBanner(current.isPlayer1 ? "Player 1 のターン" : "Player 2 のターン");
        Debug.Log($"=== {current.playerName} のターン (時代Lv.{EraLevel}) ===");

        // AIの手番なら自動進行
        if (player2IsAI && current == player2 && !GameOver)
            StartCoroutine(RunAi());
    }

    private void GrantEraCard(LanePlayer player)
    {
        CardData card = LaneDeckFactory.EraCard(EraLevel);
        if (card == null) return;
        if (player.hand.Count >= LanePlayer.MaxHand)
        {
            Debug.Log($"{player.playerName}: 手札満杯で時代カードを受け取れず（{card.cardName}）");
            return;
        }
        player.hand.Add(card);
        Debug.Log($"{player.playerName}: 時代カード「{card.cardName}」を獲得");
    }

    /// <summary>時代レベルを加味したカードの実コスト。</summary>
    public int EffectiveCost(CardData card) => card.cost + costPerLevel * (EraLevel - 1);

    /// <summary>現在プレイヤーがこのカードを支払えるか（実コスト基準）。</summary>
    public bool CanAffordCurrent(CardData card) => CurrentPlayer != null && CurrentPlayer.MP >= EffectiveCost(card);

    // ===== 入力 =====
    public void OnHandCardClicked(int index)
    {
        if (GameOver) return;
        if (IsAiTurn && !aiActing) return; // AIの手番中は人間入力を無視
        if (index < 0 || index >= CurrentPlayer.hand.Count) return;

        CardData card = CurrentPlayer.hand[index];
        if (!CanAffordCurrent(card))
        {
            Debug.Log($"MPが足りません (必要:{EffectiveCost(card)} 現在:{CurrentPlayer.MP})");
            return;
        }

        // 対象不要のアイテム（時の砂）はクリックで即発動
        if (card.cardType == CardType.Item && !LaneEffectInfo.NeedsTarget(card.itemEffect))
        {
            ApplyItem(card, -1, -1);
            CurrentPlayer.Spend(EffectiveCost(card));
            CurrentPlayer.hand.RemoveAt(index);
            SelectedHandIndex = -1;
            ui?.Render();
            return;
        }

        SelectedHandIndex = (SelectedHandIndex == index) ? -1 : index;
        ui?.Render();
    }

    public void OnCellClicked(int lane, int col)
    {
        if (GameOver) return;
        if (IsAiTurn && !aiActing) return; // AIの手番中は人間入力を無視
        if (SelectedHandIndex < 0) return;

        CardData card = CurrentPlayer.hand[SelectedHandIndex];
        if (!CanAffordCurrent(card)) return;

        if (card.cardType == CardType.Item)
        {
            // アイテム: 対象セル/レーンに効果を適用
            if (!ApplyItem(card, lane, col))
            {
                Debug.Log("そのアイテムはその対象には使えません");
                return;
            }
            CurrentPlayer.Spend(EffectiveCost(card));
            CurrentPlayer.hand.RemoveAt(SelectedHandIndex);
            SelectedHandIndex = -1;
            ui?.Render();
            return;
        }

        // モンスター: 自陣の端列の空きセルに召喚
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

        CurrentPlayer.Spend(EffectiveCost(card));
        CurrentPlayer.hand.RemoveAt(SelectedHandIndex);
        SelectedHandIndex = -1;

        SpawnUnit(card, CurrentPlayer, lane, col);
        RefreshAllUnits();
        ui?.Render();
    }

    private LaneUnit SpawnUnit(CardData card, LanePlayer owner, int lane, int col)
    {
        GameObject go = new GameObject($"Unit_{card.cardName}");
        LaneUnit unit = go.AddComponent<LaneUnit>();
        unit.Setup(card, owner, lane, col, cells[lane, col]);

        // 時代レベルによる強化（召喚時に固定）
        int atkBonus = atkPerLevel * (EraLevel - 1);
        int hpBonus = hpPerLevel * (EraLevel - 1);
        if (atkBonus > 0 || hpBonus > 0)
        {
            unit.atk += atkBonus;
            unit.hp += hpBonus;
            unit.RefreshVisual();
        }

        board.Set(lane, col, unit);
        Debug.Log($"{owner.playerName}: {card.cardName} をレーン{lane}に召喚 (時代Lv.{EraLevel} +{atkBonus}/+{hpBonus})");
        return unit;
    }

    // ===== 中立NPC =====
    private void SpawnNeutral()
    {
        int dir = (Random.value < 0.5f) ? 1 : -1;
        int startCol = dir > 0 ? 0 : LaneBoard.Cells - 1;

        // 入口セルが空いているレーンを探す
        var candidates = new List<int>();
        for (int lane = 0; lane < LaneBoard.Lanes; lane++)
            if (board.IsEmpty(lane, startCol)) candidates.Add(lane);
        if (candidates.Count == 0) return;

        int chosen = candidates[Random.Range(0, candidates.Count)];

        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = "災厄の番兵";
        card.cardType = CardType.Monster;
        card.attack = neutralBaseAtk + EraLevel;
        card.defense = neutralBaseHp + EraLevel;

        GameObject go = new GameObject("Unit_Neutral");
        LaneUnit unit = go.AddComponent<LaneUnit>();
        unit.Setup(card, null, chosen, startCol, cells[chosen, startCol]);
        unit.isNeutral = true;
        unit.neutralDir = dir;
        board.Set(chosen, startCol, unit);

        string side = dir > 0 ? "左" : "右";
        ui?.ShowBanner($"⚠ 中立モンスター出現！ レーン{chosen + 1}（{side}から）");
        Debug.Log($"[中立] 災厄の番兵がレーン{chosen}に出現 (⚔{card.attack}/♥{card.defense}, dir={dir})");
    }

    /// <summary>中立ユニットを1マスずつ進め、ぶつかった両陣営のユニットと戦闘させる。</summary>
    private void NeutralPhase()
    {
        // 盤面から中立を収集
        var neutrals = new List<LaneUnit>();
        for (int l = 0; l < LaneBoard.Lanes; l++)
            for (int c = 0; c < LaneBoard.Cells; c++)
            {
                LaneUnit u = board.Get(l, c);
                if (u != null && u.isNeutral) neutrals.Add(u);
            }

        foreach (var n in neutrals)
        {
            if (n == null || !n.IsAlive) continue;
            if (board.Get(n.lane, n.col) != n) continue; // 既に消滅/移動済み

            int target = n.col + n.neutralDir;
            bool offBoard = (n.neutralDir > 0) ? (target >= LaneBoard.Cells) : (target < 0);
            if (offBoard)
            {
                // 渡りきって消滅（ベースには無害）
                board.Set(n.lane, n.col, null);
                Debug.Log("[中立] 災厄の番兵が立ち去った");
                Destroy(n.gameObject);
                continue;
            }

            LaneUnit occupant = board.Get(n.lane, target);
            if (occupant == null)
            {
                board.Set(n.lane, n.col, null);
                board.Set(n.lane, target, n);
                n.MoveTo(n.lane, target, cells[n.lane, target]);
            }
            else
            {
                // 両陣営問わず戦闘
                Combat(n, occupant);
            }
        }
        RefreshAllUnits();
    }

    // ===== ターン終了 → 進軍フェーズ =====
    public void EndTurn()
    {
        if (GameOver) return;
        if (IsAiTurn && !aiActing) return; // AIの手番中は人間のターン終了を無視
        SelectedHandIndex = -1;

        AdvancePhase(CurrentPlayer);
        NeutralPhase();
        ui?.Render();

        if (CheckWin()) return;

        if (CurrentPlayer == player1) StartTurn(player2, player1);
        else StartTurn(player1, player2);
    }

    private void AdvancePhase(LanePlayer player)
    {
        int dir = player.Direction;

        // 各ユニットの進軍可能歩数を決める（基本1＋時の砂＋突撃）
        var units = AllUnitsOf(player);
        foreach (var u in units) u.doneThisPhase = false;

        // 最大歩数ぶんパスを回す（基本1 + 時の砂1 + 突撃1 = 最大3）
        int maxSteps = 1 + (timeSandActive ? 1 : 0) + 1;
        for (int step = 0; step < maxSteps; step++)
        {
            for (int lane = 0; lane < LaneBoard.Lanes; lane++)
            {
                if (dir > 0)
                    for (int col = LaneBoard.Cells - 1; col >= 0; col--)
                        TryAdvanceOnce(player, lane, col, step);
                else
                    for (int col = 0; col < LaneBoard.Cells; col++)
                        TryAdvanceOnce(player, lane, col, step);
            }
        }

        // フラグ整理: 召喚済み扱いに、処理フラグをクリア
        foreach (var u in AllUnitsOf(player))
        {
            u.justSummoned = false;
            u.doneThisPhase = false;
        }
        RefreshAllUnits();
    }

    /// <summary>stepパス目に、このユニットが1マス進軍できるなら行う。</summary>
    private void TryAdvanceOnce(LanePlayer player, int lane, int col, int step)
    {
        LaneUnit unit = board.Get(lane, col);
        if (unit == null || unit.owner != player || !unit.IsAlive) return;
        if (unit.doneThisPhase) return;

        // このユニットの許容歩数
        int allowed = 1 + (timeSandActive ? 1 : 0) + (unit.effect == LaneEffect.Charge && unit.justSummoned ? 1 : 0);
        if (step >= allowed) return;

        int target = col + player.Direction;

        // 敵陣突破 → 相手ベースへ一撃を与えて消滅（居座らず消費。テンポ確保＆居座り問題の解消）
        bool reachedEnemyBase = (player.Direction > 0) ? (target >= LaneBoard.Cells) : (target < 0);
        if (reachedEnemyBase)
        {
            int dmg = EffAtk(unit);
            OpponentOf(player).TakeBaseDamage(dmg);
            Debug.Log($"{unit.data.cardName} が相手ベースを突破し{dmg}ダメージ！（消滅）");
            board.Set(lane, col, null);
            Destroy(unit.gameObject);
            return;
        }

        LaneUnit occupant = board.Get(lane, target);
        if (occupant == null)
        {
            board.Set(lane, col, null);
            board.Set(lane, target, unit);
            unit.MoveTo(lane, target, cells[lane, target]);
        }
        else if (occupant.owner != unit.owner)
        {
            Combat(unit, occupant);
            unit.doneThisPhase = true; // 戦闘したらこのターンは前進終了
        }
        else
        {
            // 味方が前にいる→待機（次パスで前が空けば進める）
        }
    }

    private void Combat(LaneUnit attacker, LaneUnit defender)
    {
        int atkDmg = EffAtk(attacker);
        int defDmg = EffAtk(defender);
        int defHpBefore = defender.hp;
        Debug.Log($"戦闘: {attacker.data.cardName}(⚔{atkDmg}/♥{attacker.hp}) vs {defender.data.cardName}(⚔{defDmg}/♥{defender.hp})");

        DamageUnit(defender, atkDmg, fromEffect: false);
        DamageUnit(attacker, defDmg, fromEffect: false);

        // 貫通: 攻撃側が敵を倒し攻撃力が余れば超過分を相手ベースへ
        if (attacker.effect == LaneEffect.Trample && !defender.IsAlive && atkDmg > defHpBefore)
        {
            int overflow = atkDmg - defHpBefore;
            OpponentOf(attacker.owner).TakeBaseDamage(overflow);
            Debug.Log($"【貫通】{attacker.data.cardName} の超過{overflow}ダメージが相手ベースへ！");
        }
    }

    /// <summary>ユニットにダメージ。守備（致死耐性）を考慮し、死亡時は破壊処理。</summary>
    private void DamageUnit(LaneUnit u, int dmg, bool fromEffect)
    {
        if (u == null || !u.IsAlive || dmg <= 0) return;

        // 守備: 一度だけ致死を耐えてHP1
        if (u.effect == LaneEffect.Guard && !u.guardUsed && (u.hp - dmg) <= 0)
        {
            u.guardUsed = true;
            u.hp = 1;
            Debug.Log($"【守備】{u.data.cardName} が致死を耐えHP1で残存");
            u.RefreshVisual();
            return;
        }

        u.hp -= dmg;
        if (u.hp <= 0) KillUnit(u, fromEffect);
        else u.RefreshVisual();
    }

    private void KillUnit(LaneUnit u, bool fromEffect)
    {
        if (board.Get(u.lane, u.col) == u) board.Set(u.lane, u.col, null);
        Debug.Log($"{u.data.cardName} が破壊された");

        // 爆散: 同レーンの前後の敵に2ダメージ（効果ダメージなので連鎖はしない）
        if (!fromEffect && u.effect == LaneEffect.Explode)
        {
            DamageNeighborEnemy(u, u.lane, u.col - 1);
            DamageNeighborEnemy(u, u.lane, u.col + 1);
            Debug.Log($"【爆散】{u.data.cardName} が前後の敵を巻き込む！");
        }

        Destroy(u.gameObject);
    }

    private void DamageNeighborEnemy(LaneUnit source, int lane, int col)
    {
        LaneUnit n = board.Get(lane, col);
        if (n != null && n.owner != source.owner)
            DamageUnit(n, 2, fromEffect: true);
    }

    // ===== 効果ヘルパ =====
    /// <summary>隣接強化など補正後の実効ATK。</summary>
    private int EffAtk(LaneUnit u)
    {
        int a = u.atk;
        if (u.effect == LaneEffect.Bond &&
            (HasFriendlyInLane(u.owner, u.lane - 1) || HasFriendlyInLane(u.owner, u.lane + 1)))
            a += 1;
        return a;
    }

    private int BondBonus(LaneUnit u)
    {
        if (u.effect == LaneEffect.Bond &&
            (HasFriendlyInLane(u.owner, u.lane - 1) || HasFriendlyInLane(u.owner, u.lane + 1)))
            return 1;
        return 0;
    }

    private bool HasFriendlyInLane(LanePlayer owner, int lane)
    {
        if (lane < 0 || lane >= LaneBoard.Lanes) return false;
        for (int c = 0; c < LaneBoard.Cells; c++)
        {
            LaneUnit u = board.Get(lane, c);
            if (u != null && u.owner == owner && u.IsAlive) return true;
        }
        return false;
    }

    private List<LaneUnit> AllUnitsOf(LanePlayer player)
    {
        var list = new List<LaneUnit>();
        for (int l = 0; l < LaneBoard.Lanes; l++)
            for (int c = 0; c < LaneBoard.Cells; c++)
            {
                LaneUnit u = board.Get(l, c);
                if (u != null && u.owner == player) list.Add(u);
            }
        return list;
    }

    /// <summary>盤面全ユニットの表示を更新（隣接強化の増減を反映）。</summary>
    private void RefreshAllUnits()
    {
        for (int l = 0; l < LaneBoard.Lanes; l++)
            for (int c = 0; c < LaneBoard.Cells; c++)
            {
                LaneUnit u = board.Get(l, c);
                if (u != null) u.RefreshVisual(BondBonus(u));
            }
    }

    // ===== アイテム =====
    /// <summary>アイテムを適用する。成功でtrue。lane/colは対象（時の砂は-1,-1）。</summary>
    private bool ApplyItem(CardData card, int lane, int col)
    {
        LaneUnit target = (lane >= 0 && col >= 0) ? board.Get(lane, col) : null;

        switch (card.itemEffect)
        {
            case LaneItem.Firebolt:
                // 指定レーンの敵ユニット全員に effectAmount ダメージ
                if (lane < 0 || lane >= LaneBoard.Lanes) return false;
                for (int c = 0; c < LaneBoard.Cells; c++)
                {
                    LaneUnit u = board.Get(lane, c);
                    if (u != null && u.owner != CurrentPlayer)
                        DamageUnit(u, card.effectAmount, fromEffect: true);
                }
                Debug.Log($"【火炎弾】レーン{lane}の敵に{card.effectAmount}ダメージ");
                RefreshAllUnits();
                return true; // レーンが空でも消費（演出として許容）

            case LaneItem.Buff:
                if (target == null || target.owner != CurrentPlayer) return false;
                target.atk += 2;
                target.hp += 2;
                target.RefreshVisual(BondBonus(target));
                Debug.Log($"【強化の薬】{target.data.cardName} を+2/+2");
                return true;

            case LaneItem.Rockfall:
                if (target == null || target.owner == CurrentPlayer) return false;
                Debug.Log($"【落石】{target.data.cardName} を破壊");
                KillUnit(target, fromEffect: true);
                RefreshAllUnits();
                return true;

            case LaneItem.Retreat:
                if (target == null || target.owner != CurrentPlayer) return false;
                int home = CurrentPlayer.HomeColumn;
                if (target.col == home) return false; // 既に最後方
                if (!board.IsEmpty(target.lane, home)) return false; // 後退先が埋まっている
                board.Set(target.lane, target.col, null);
                board.Set(target.lane, home, target);
                target.MoveTo(target.lane, home, cells[target.lane, home]);
                Debug.Log($"【退却】{target.data.cardName} を自陣端へ後退");
                RefreshAllUnits();
                return true;

            case LaneItem.TimeSand:
                timeSandActive = true;
                Debug.Log("【時の砂】このターン、自軍は進軍でもう1マス進む");
                return true;

            case LaneItem.Meteor:
                // 盤面の敵ユニット全員にダメージ
                for (int l = 0; l < LaneBoard.Lanes; l++)
                    for (int c = 0; c < LaneBoard.Cells; c++)
                    {
                        LaneUnit u = board.Get(l, c);
                        if (u != null && u.owner != CurrentPlayer)
                            DamageUnit(u, card.effectAmount, fromEffect: true);
                    }
                Debug.Log($"【隕石嵐】敵全体に{card.effectAmount}ダメージ");
                RefreshAllUnits();
                return true;

            case LaneItem.WarCry:
                // 自軍全ユニットを+2/+2
                foreach (var u in AllUnitsOf(CurrentPlayer))
                {
                    u.atk += 2;
                    u.hp += 2;
                    u.RefreshVisual(BondBonus(u));
                }
                Debug.Log("【鬨の声】自軍全ユニットを+2/+2");
                return true;
        }
        return false;
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

    // ===== AI（player2 自動操作） =====
    private class AiMove
    {
        public int handIndex;
        public bool needsCell; // true=セルクリックが必要（召喚/対象アイテム）
        public int lane;
        public int col;
    }

    private IEnumerator RunAi()
    {
        aiActing = true;
        yield return new WaitForSeconds(0.6f); // 思考の間

        int safety = 0;
        while (!GameOver && safety++ < 30)
        {
            AiMove move = ChooseAiAction();
            if (move == null) break;

            int mpBefore = player2.MP;
            int handBefore = player2.hand.Count;

            OnHandCardClicked(move.handIndex);
            if (move.needsCell)
                OnCellClicked(move.lane, move.col);

            yield return new WaitForSeconds(0.45f);
            if (GameOver) { aiActing = false; yield break; }

            // 進展がなければ中断（無限ループ防止）
            if (player2.MP == mpBefore && player2.hand.Count == handBefore) break;
        }

        yield return new WaitForSeconds(0.3f);
        EndTurn();
        aiActing = false;
    }

    private AiMove ChooseAiAction()
    {
        var enemies = AllUnitsOf(player1);
        var mine = AllUnitsOf(player2);

        // 1. 落石: 強い敵(ATK4以上)を破壊
        int rockIdx = FindAffordableItem(LaneItem.Rockfall);
        if (rockIdx >= 0 && enemies.Count > 0)
        {
            LaneUnit t = Strongest(enemies);
            if (t != null && t.atk >= 4)
                return new AiMove { handIndex = rockIdx, needsCell = true, lane = t.lane, col = t.col };
        }

        // 2. 火炎弾: 敵が2体以上いるレーンを焼く
        int fireIdx = FindAffordableItem(LaneItem.Firebolt);
        if (fireIdx >= 0)
        {
            int bestLane = -1, bestCnt = 0;
            for (int lane = 0; lane < LaneBoard.Lanes; lane++)
            {
                int cnt = EnemyCountInLane(lane);
                if (cnt > bestCnt) { bestCnt = cnt; bestLane = lane; }
            }
            if (bestLane >= 0 && bestCnt >= 2)
            {
                int col = FirstEnemyColInLane(bestLane);
                if (col >= 0)
                    return new AiMove { handIndex = fireIdx, needsCell = true, lane = bestLane, col = col };
            }
        }

        // 3. 隕石嵐: 敵が3体以上なら全体攻撃（即発動）
        int meteorIdx = FindAffordableItem(LaneItem.Meteor);
        if (meteorIdx >= 0 && enemies.Count >= 3)
            return new AiMove { handIndex = meteorIdx, needsCell = false };

        // 4. 鬨の声: 自軍2体以上なら全体強化（即発動）
        int warIdx = FindAffordableItem(LaneItem.WarCry);
        if (warIdx >= 0 && mine.Count >= 2)
            return new AiMove { handIndex = warIdx, needsCell = false };

        // 5. 召喚: 出せる最強モンスターを最適レーンへ
        int monIdx = BestAffordableMonster();
        if (monIdx >= 0)
        {
            int lane = ChooseSummonLane();
            if (lane >= 0)
                return new AiMove { handIndex = monIdx, needsCell = true, lane = lane, col = player2.HomeColumn };
        }

        // 6. 強化の薬: 自分の最強ユニットを強化
        int buffIdx = FindAffordableItem(LaneItem.Buff);
        if (buffIdx >= 0 && mine.Count > 0)
        {
            LaneUnit t = Strongest(mine);
            if (t != null)
                return new AiMove { handIndex = buffIdx, needsCell = true, lane = t.lane, col = t.col };
        }

        // 7. 時の砂: 自軍がいれば押し込む（即発動）
        int sandIdx = FindAffordableItem(LaneItem.TimeSand);
        if (sandIdx >= 0 && mine.Count > 0)
            return new AiMove { handIndex = sandIdx, needsCell = false };

        return null;
    }

    private int FindAffordableItem(LaneItem item)
    {
        for (int i = 0; i < player2.hand.Count; i++)
        {
            CardData c = player2.hand[i];
            if (c.cardType == CardType.Item && c.itemEffect == item && CanAffordCurrent(c))
                return i;
        }
        return -1;
    }

    private int BestAffordableMonster()
    {
        // 空き自陣セルが無いなら召喚不可
        bool anyEmptyHome = false;
        for (int lane = 0; lane < LaneBoard.Lanes; lane++)
            if (board.IsEmpty(lane, player2.HomeColumn)) { anyEmptyHome = true; break; }
        if (!anyEmptyHome) return -1;

        int best = -1, bestScore = -1;
        for (int i = 0; i < player2.hand.Count; i++)
        {
            CardData c = player2.hand[i];
            if (c.cardType != CardType.Monster || !CanAffordCurrent(c)) continue;
            int score = c.attack + c.defense;
            if (score > bestScore) { bestScore = score; best = i; }
        }
        return best;
    }

    private int ChooseSummonLane()
    {
        int bestLane = -1, bestEnemy = -1;
        for (int lane = 0; lane < LaneBoard.Lanes; lane++)
        {
            if (!board.IsEmpty(lane, player2.HomeColumn)) continue;
            int enemyCnt = EnemyCountInLane(lane);
            // 敵がいるレーンを優先（防衛）。同点なら最初の空きレーン。
            if (enemyCnt > bestEnemy) { bestEnemy = enemyCnt; bestLane = lane; }
        }
        return bestLane;
    }

    private LaneUnit Strongest(List<LaneUnit> units)
    {
        LaneUnit best = null;
        foreach (var u in units)
            if (u != null && (best == null || u.atk > best.atk)) best = u;
        return best;
    }

    private int EnemyCountInLane(int lane)
    {
        int n = 0;
        for (int c = 0; c < LaneBoard.Cells; c++)
        {
            LaneUnit u = board.Get(lane, c);
            if (u != null && u.owner == player1) n++;
        }
        return n;
    }

    private int FirstEnemyColInLane(int lane)
    {
        for (int c = 0; c < LaneBoard.Cells; c++)
        {
            LaneUnit u = board.Get(lane, c);
            if (u != null && u.owner == player1) return c;
        }
        return -1;
    }
}
