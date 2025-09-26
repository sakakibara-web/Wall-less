using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }
    public List<Transform> spawnPoints;

    private Dictionary<int, Transform> assignedSpawnPoints = new Dictionary<int, Transform>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Transform GetSpawnPointForPlayer(Player player)
    {
        if (!assignedSpawnPoints.ContainsKey(player.ActorNumber))
        {
            if (spawnPoints.Count > 0)
            {
                // プレイヤーのActorNumberをインデックスとして、スポーンポイントを割り当てる
                int spawnIndex = player.ActorNumber % spawnPoints.Count;
                assignedSpawnPoints[player.ActorNumber] = spawnPoints[spawnIndex];
                return assignedSpawnPoints[player.ActorNumber];
            }
            else
            {
                Debug.LogError("No spawn points assigned in PlayerSpawnManager.");
                return null;
            }
        }
        return assignedSpawnPoints[player.ActorNumber];
    }
}