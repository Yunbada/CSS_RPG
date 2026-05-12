using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum RoundState
{
    Waiting,        // 라운드 시작 전 (로비 대기)
    RoundStarted,   // 라운드 시작됨 (감염 전 준비 시간)
    InfectionStarted, // 감염 발동 (좀비 등장)
    RoundEnded      // 라운드 종료 (보상 처리 중)
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public NetworkVariable<RoundState> currentState = new NetworkVariable<RoundState>(RoundState.Waiting);
    public NetworkVariable<float> roundTimer = new NetworkVariable<float>(0f);

    private List<PlayerState> allPlayers = new List<PlayerState>();
    public IReadOnlyList<PlayerState> AllPlayers => allPlayers;

    // =========================================================================
    // 상수 설정
    // =========================================================================
    private const float ROUND_TIME_LIMIT = 180f;   // 3분
    private const float INFECTION_TIME = 10f;      // 라운드 시작 10초 후 감염
    private const int INFECTION_RATIO = 8;         // 8:1 비율 (8명당 1좀비)
    private const float EVOLUTION_INTERVAL = 60f;  // 좀비 진화 주기
    private float nextEvolutionTime;

    // =========================================================================
    // 라운드 후 보상 상수
    // =========================================================================
    private const float DMG_TO_EXP_RATE = 0.01f;         // DMG의 1% → EXP
    private const int HUMAN_SURVIVAL_EXP = 100;           // 생존 인간 보너스 EXP
    private const int HUMAN_SURVIVAL_GOLD = 30;           // 생존 인간 보너스 골드
    private const int HOST_ZOMBIE_CONVERT_EXP = 10;       // 숙주 좀비: 전환 1회당 EXP
    private const int NORMAL_ZOMBIE_CONVERT_EXP = 20;     // 일반 좀비: 전환 1회당 EXP

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // =========================================================================
    // 플레이어 등록/해제
    // =========================================================================
    public void RegisterPlayer(PlayerState player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
        }

        if (currentState.Value == RoundState.Waiting)
        {
            // 대기 중: (0,0,0) 좌표에 스폰
            TeleportPlayer(player, Vector3.up); // (0,1,0) 바닥 위

            // 2명 이상이면 자동 시작
            if (IsServer && allPlayers.Count >= 2)
            {
                StartRound();
            }
        }
        else if (currentState.Value == RoundState.InfectionStarted)
        {
            // 라운드 진행 중 참여 → 관전 모드 (인간 팀이지만 무적 + 비활성)
            if (IsServer)
            {
                player.currentTeam.Value = Team.Human;
                var pH = player.GetComponent<PlayerHealth>();
                if (pH != null) pH.isInvincible.Value = true;
                SetPlayerSpectatorClientRpc(player.OwnerClientId, true);
            }
        }
    }

    public void UnregisterPlayer(PlayerState player)
    {
        if (allPlayers.Contains(player))
        {
            allPlayers.Remove(player);
            CheckWinCondition();
        }
    }

    // =========================================================================
    // 라운드 시작
    // =========================================================================
    public void StartRound()
    {
        if (!IsServer) return;
        
        currentState.Value = RoundState.RoundStarted;
        roundTimer.Value = ROUND_TIME_LIMIT;
        nextEvolutionTime = ROUND_TIME_LIMIT - EVOLUTION_INTERVAL;

        // 모든 플레이어 초기화: (0,0,0) 스폰, 인간 팀, 데이터 로드 유지
        foreach (var p in allPlayers)
        {
            if (p == null) continue;
            p.currentTeam.Value = Team.Human;
            p.currentZombieType.Value = ZombieType.None;
            var pH = p.GetComponent<PlayerHealth>();
            if (pH != null)
            {
                pH.maxHealth.Value = 100;
                pH.currentHealth.Value = 100;
                pH.isInvincible.Value = false;
                pH.ResetRoundCounters();
            }

            // CombatSystem 데미지 카운터 리셋
            var combat = p.GetComponentInChildren<CombatSystem>();
            if (combat != null) combat.ResetDamageCounter();

            // 팔라딘 충전량 초기화
            var paladin = p.GetComponentInChildren<PaladinSkillExecutor>();
            if (paladin != null) paladin.ResetShieldEnergy();

            // (0,0,0) 좌표에 스폰
            TeleportPlayer(p, Vector3.up);
        }

        Debug.Log($"[RoundManager] 라운드 시작! 인원: {allPlayers.Count}명");
    }

    // =========================================================================
    // 매 프레임 업데이트
    // =========================================================================
    private void Update()
    {
        if (!IsServer || currentState.Value == RoundState.Waiting || currentState.Value == RoundState.RoundEnded) 
            return;

        roundTimer.Value -= Time.deltaTime;

        if (currentState.Value == RoundState.RoundStarted)
        {
            // 10초 후 감염 발동
            if (ROUND_TIME_LIMIT - roundTimer.Value >= INFECTION_TIME)
            {
                TriggerInfection();
            }
        }
        else if (currentState.Value == RoundState.InfectionStarted)
        {
            // 좀비 진화 (60초마다)
            if (roundTimer.Value <= nextEvolutionTime)
            {
                EvolveRandomZombie();
                nextEvolutionTime -= EVOLUTION_INTERVAL;
            }
        }

        // 시간 종료 → 인간 승리 (1명이라도 남아있으므로)
        if (roundTimer.Value <= 0)
        {
            roundTimer.Value = 0;
            EndRound(Team.Human);
        }
        else
        {
            CheckWinCondition();
        }
    }

    // =========================================================================
    // 감염 발동: 8:1 비율로 랜덤 숙주 좀비 선정
    // =========================================================================
    private void TriggerInfection()
    {
        currentState.Value = RoundState.InfectionStarted;
        
        if (allPlayers.Count == 0) return;

        // 8:1 비율 계산: 총 인원 / (INFECTION_RATIO + 1) = 좀비 수 (최소 1명)
        int zombieCount = Mathf.Max(1, allPlayers.Count / (INFECTION_RATIO + 1));
        
        // 셔플 후 앞에서 zombieCount명을 숙주 좀비로 선정
        List<int> indices = new List<int>();
        for (int i = 0; i < allPlayers.Count; i++) indices.Add(i);
        ShuffleList(indices);

        for (int i = 0; i < zombieCount && i < indices.Count; i++)
        {
            var target = allPlayers[indices[i]];
            target.currentTeam.Value = Team.HostZombie;
            // 좌표는 그대로 (감염 당시 위치에서 좀비로 변환)
            
            // 팔라딘 충전량 초기화
            var paladin = target.GetComponentInChildren<PaladinSkillExecutor>();
            if (paladin != null) paladin.ResetShieldEnergy();

            string nick = "";
                var pAuth = target.GetComponent<PlayerAuthentication>();
                if (pAuth != null) nick = pAuth.Nickname.Value.ToString();
            Debug.Log($"[Infection] {nick}이(가) 숙주 좀비로 감염!");
        }
        
        Debug.Log($"[RoundManager] 감염 시작! 숙주 좀비 {zombieCount}명 선정");
    }

    // =========================================================================
    // 좀비 진화 (일반 좀비 → Speed/Tank/Jump 랜덤 진화)
    // =========================================================================
    private void EvolveRandomZombie()
    {
        List<PlayerState> normalZombies = new List<PlayerState>();
        foreach(var player in allPlayers)
        {
            if (player.currentTeam.Value == Team.NormalZombie && player.currentZombieType.Value == ZombieType.None)
            {
                normalZombies.Add(player);
            }
        }

        if (normalZombies.Count > 0)
        {
            int randIndex = Random.Range(0, normalZombies.Count);
            int typeRand = Random.Range(1, 4); // 1: Speed, 2: Tank, 3: Jump
            normalZombies[randIndex].currentZombieType.Value = (ZombieType)typeRand;
            Debug.Log($"[Evolution] 좀비 진화 → {(ZombieType)typeRand}!");
        }
    }

    // =========================================================================
    // 승리 조건 확인
    // =========================================================================
    private void CheckWinCondition()
    {
        if (currentState.Value != RoundState.InfectionStarted) return;

        int humanCount = 0;
        foreach (var player in allPlayers)
        {
            if (player != null && player.currentTeam.Value == Team.Human)
                humanCount++;
        }

        // 모든 인간이 좀비가 됨 → 좀비 승리
        if (humanCount == 0)
        {
            EndRound(Team.HostZombie);
        }
        // 좀비가 0명 (전원 인간) → 이론상 감염 직후에는 불가능하지만 안전장치
    }

    // =========================================================================
    // 라운드 종료 + 보상 처리
    // =========================================================================
    private void EndRound(Team winningTeam)
    {
        currentState.Value = RoundState.RoundEnded;
        Debug.Log($"[RoundManager] 라운드 종료! 승리: {winningTeam}");
        
        // 라운드 종료 시 모든 드롭된 아이템 파괴
        var activeLoot = FindObjectsByType<LootDrop>(FindObjectsSortMode.None);
        foreach (var loot in activeLoot)
        {
            if (loot != null && loot.NetworkObject != null && loot.NetworkObject.IsSpawned)
            {
                loot.NetworkObject.Despawn(true);
            }
        }
        
        foreach (var p in PlayerState.AllPlayersList)
        {
            if (p == null) continue;

            var pH = p.GetComponent<PlayerHealth>();
            var pAuth = p.GetComponent<PlayerAuthentication>();
            var pExp = p.GetComponentInChildren<PlayerExperience>();
            int totalExpReward = 0;
            int totalGoldReward = 0;

            // (1) DMG의 1% → EXP
            int serverDmg = pH != null ? pH.totalDamageDealt.Value : 0;
            if (serverDmg > 0)
            {
                int dmgExp = Mathf.Max(1, Mathf.FloorToInt(serverDmg * DMG_TO_EXP_RATE));
                totalExpReward += dmgExp;
                Debug.Log($"  [{(pAuth != null ? pAuth.Nickname.Value : "?")}] DMG:{serverDmg} → EXP +{dmgExp}");
            }

            // ---------------------------------------------------------------
            // (2) 생존 인간 보너스: +100 EXP, +30 Gold
            // ---------------------------------------------------------------
            if (p.currentTeam.Value == Team.Human)
            {
                totalExpReward += HUMAN_SURVIVAL_EXP;
                totalGoldReward += HUMAN_SURVIVAL_GOLD;
                Debug.Log($"  [{(pAuth != null ? pAuth.Nickname.Value : "?")}] 생존 보너스: EXP +{HUMAN_SURVIVAL_EXP}, Gold +{HUMAN_SURVIVAL_GOLD}");
            }

            // ---------------------------------------------------------------
            // (3) 숙주 좀비: 전환 횟수 × 10 EXP
            // ---------------------------------------------------------------
            if (p.currentTeam.Value == Team.HostZombie && pH != null && pH.zombieConversionCount.Value > 0)
            {
                int convExp = pH.zombieConversionCount.Value * HOST_ZOMBIE_CONVERT_EXP;
                totalExpReward += convExp;
                Debug.Log($"  [{(pAuth != null ? pAuth.Nickname.Value : "?")}] 숙주 좀비 전환 보상: {pH.zombieConversionCount.Value}회 × {HOST_ZOMBIE_CONVERT_EXP} = EXP +{convExp}");
            }

            // ---------------------------------------------------------------
            // (4) 일반 좀비: 전환 횟수 × 20 EXP
            // ---------------------------------------------------------------
            if (p.currentTeam.Value == Team.NormalZombie && pH != null && pH.zombieConversionCount.Value > 0)
            {
                int convExp = pH.zombieConversionCount.Value * NORMAL_ZOMBIE_CONVERT_EXP;
                totalExpReward += convExp;
                Debug.Log($"  [{(pAuth != null ? pAuth.Nickname.Value : "?")}] 일반 좀비 전환 보상: {pH.zombieConversionCount.Value}회 × {NORMAL_ZOMBIE_CONVERT_EXP} = EXP +{convExp}");
            }

            // ---------------------------------------------------------------
            // 서버에서 EXP 지급
            // ---------------------------------------------------------------
            if (totalExpReward > 0 && pExp != null)
            {
                pExp.AddExp(totalExpReward);
            }

            // ---------------------------------------------------------------
            // (5) 서버에서 직접 골드 지급 및 데이터 저장 (Server-Authoritative)
            // ---------------------------------------------------------------
            if (pAuth != null)
            {
                pAuth.Gold.Value += totalGoldReward;
                pAuth.SaveDataToDatabase();
                Debug.Log($"  [{(pAuth != null ? pAuth.Nickname.Value : "?")}] 라운드 종료 자동 저장 (Server-Authoritative)");
            }

            if (pH != null) pH.ResetRoundCounters();

            // 클라이언트 측 DMG 카운터 리셋
            var combat = p.GetComponentInChildren<CombatSystem>();
            if (combat != null) combat.ResetDamageCounter();

            // 팔라딘 충전량 초기화
            var paladin = p.GetComponentInChildren<PaladinSkillExecutor>();
            if (paladin != null) paladin.ResetShieldEnergy();
        }

        // 5초 후 다음 라운드 준비
        Invoke(nameof(ResetRound), 5f);
    }

    // =========================================================================
    // 라운드 리셋 → 다음 라운드 시작
    // =========================================================================
    private void ResetRound()
    {
        if (!IsServer) return;

        foreach (var p in PlayerState.AllPlayersList)
        {
            if (p != null)
            {
                // 인간 팀으로 복원, 체력 복구
                p.currentTeam.Value = Team.Human;
                p.currentZombieType.Value = ZombieType.None;
                var pH2 = p.GetComponent<PlayerHealth>();
                if (pH2 != null)
                {
                    pH2.maxHealth.Value = 100; 
                    pH2.currentHealth.Value = 100;
                    pH2.isInvincible.Value = false;
                }

                // (0,0,0) 좌표로 리스폰
                TeleportPlayer(p, Vector3.up);
            }
        }
        
        StartRound();
    }

    // =========================================================================
    // 헬퍼: 플레이어 텔레포트
    // =========================================================================
    private void TeleportPlayer(PlayerState player, Vector3 position)
    {
        // Unity Netcode의 ClientNetworkTransform을 사용할 때, 
        // 서버에서 transform.position을 바꿔도 클라이언트(Owner) 쪽 트랜스폼 권한이 덮어쓰는 문제가 있습니다.
        // 그러므로 ClientRpc를 통해서 Client (소유주)에서 물리적 이동을 하도록 호출하여 동기화를 진행합니다.
        var pH = player.GetComponent<PlayerHealth>();
        if (pH != null) pH.TeleportClientRpc(position);
    }

    [ClientRpc]
    public void SetFreezeStateClientRpc(bool isFreeze)
    {
        Time.timeScale = isFreeze ? 0f : 1f;
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                var movement = localPlayer.GetComponentInChildren<PlayerMovement>();
                if (movement != null) movement.enabled = !isFreeze;
                var combat = localPlayer.GetComponentInChildren<CombatSystem>();
                if (combat != null) combat.enabled = !isFreeze;
            }
        }
    }

    // =========================================================================
    // 헬퍼: 리스트 셔플 (Fisher-Yates)
    // =========================================================================
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // =========================================================================
    // 관전 모드 (라운드 중 참여 시)
    // =========================================================================
    [ClientRpc]
    private void SetPlayerSpectatorClientRpc(ulong targetClientId, bool isSpectator)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        
        // 관전 모드: 이동/공격 비활성화 (간단 구현)
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            var combat = localPlayer.GetComponentInChildren<CombatSystem>();
            if (combat != null) combat.enabled = !isSpectator;
            
            Debug.Log(isSpectator ? "[관전 모드] 다음 라운드까지 관전합니다." : "[관전 해제] 참여합니다.");
        }
    }
}
