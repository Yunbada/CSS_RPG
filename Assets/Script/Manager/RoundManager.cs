using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum RoundState
{
    Waiting,
    RoundStarted,
    InfectionStarted,
    RoundEnded
}

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public NetworkVariable<RoundState> currentState = new NetworkVariable<RoundState>(RoundState.Waiting);
    public NetworkVariable<float> roundTimer = new NetworkVariable<float>(0f);

    private List<PlayerState> allPlayers = new List<PlayerState>();
    public IReadOnlyList<PlayerState> AllPlayers => allPlayers;

    private const float ROUND_TIME_LIMIT = 180f; // 3 minutes
    private const float INFECTION_TIME = 10f; // 10 seconds after round starts
    private float nextEvolutionTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPlayer(PlayerState player)
    {
        if (!allPlayers.Contains(player))
        {
            allPlayers.Add(player);
        }

        // 2명 이상일 때 게임 자동 시작 테스트 (원하는 조건으로 변경 가능)
        if (currentState.Value == RoundState.Waiting && allPlayers.Count >= 2)
        {
            StartRound();
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

    public void StartRound()
    {
        if (!IsServer) return;
        
        currentState.Value = RoundState.RoundStarted;
        roundTimer.Value = ROUND_TIME_LIMIT;
        nextEvolutionTime = ROUND_TIME_LIMIT - 60f;
    }

    private void Update()
    {
        if (!IsServer || currentState.Value == RoundState.Waiting || currentState.Value == RoundState.RoundEnded) 
            return;

        roundTimer.Value -= Time.deltaTime;

        if (currentState.Value == RoundState.RoundStarted)
        {
            // 감염 시작 타이밍 확인 (3분 - 10초 = 2분 50초)
            if (ROUND_TIME_LIMIT - roundTimer.Value >= INFECTION_TIME)
            {
                TriggerInfection();
            }
        }
        else if (currentState.Value == RoundState.InfectionStarted)
        {
            if (roundTimer.Value <= nextEvolutionTime)
            {
                EvolveRandomZombie();
                nextEvolutionTime -= 60f;
            }
        }

        if (roundTimer.Value <= 0)
        {
            roundTimer.Value = 0;
            EndRound(Team.Human); // 시간 초과 시 인간 승리
        }
        else
        {
            CheckWinCondition();
        }
    }

    private void TriggerInfection()
    {
        currentState.Value = RoundState.InfectionStarted;
        
        if (allPlayers.Count == 0) return;

        int hostIndex = Random.Range(0, allPlayers.Count);
        allPlayers[hostIndex].currentTeam.Value = Team.HostZombie;
        
        Debug.Log("Infection Started! Host selected.");
    }

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
            Debug.Log($"Zombie Evolved into {(ZombieType)typeRand}!");
        }
    }

    private void CheckWinCondition()
    {
        if (currentState.Value != RoundState.InfectionStarted) return;

        int humanCount = 0;
        int zombieCount = 0;

        foreach (var player in allPlayers)
        {
            if (player.currentTeam.Value == Team.Human) humanCount++;
            else zombieCount++;
        }

        if (humanCount == 0)
        {
            EndRound(Team.HostZombie); // 좀비 진영 승리
        }
        else if (zombieCount == 0)
        {
            EndRound(Team.Human); // 인간 진영 승리
        }
    }

    private void EndRound(Team winningTeam)
    {
        currentState.Value = RoundState.RoundEnded;
        Debug.Log("Round Ended! Winner: " + winningTeam.ToString());
        
        // 보상 지급 및 다음 라운드 준비 로직 (3초 후 재시작)
        Invoke(nameof(ResetRound), 3f);
    }

    private void ResetRound()
    {
        if (!IsServer) return;

        foreach (var p in PlayerState.AllPlayersList)
        {
            if (p != null)
            {
                p.currentTeam.Value = Team.Human;
                p.maxHealth.Value = 100; 
                p.currentHealth.Value = 100; // 인간 체력 복구

                // 스폰 포인트로 원대 복귀
                var movement = p.GetComponentInParent<PlayerMovement>();
                if (movement != null)
                {
                    var charCtrl = movement.GetComponent<UnityEngine.CharacterController>();
                    if (charCtrl != null) charCtrl.enabled = false;
                    movement.transform.position = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f)); // 임의 스폰 마커
                    if (charCtrl != null) charCtrl.enabled = true;
                }
            }
        }
        
        StartRound(); // 라운드 타이머(3분) 재시작
    }
}
