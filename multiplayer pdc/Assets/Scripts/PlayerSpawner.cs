using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;
    public GameObject playerOneShape;
    public GameObject playerTwoShape;

    void IPlayerJoined.PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            NetworkObject spawnedPlayer = null;
            if (player.RawEncoded - 1 == 1)
            {
                spawnedPlayer = Runner.Spawn(PlayerPrefab, new Vector3(0, 30, -65), Quaternion.identity);
                spawnedPlayer.GetComponent<PlayerControls>().localShape = playerOneShape;
            }
            else if (player.RawEncoded - 1 == 2)
            {
                spawnedPlayer = Runner.Spawn(PlayerPrefab, new Vector3(0, 30, 65), Quaternion.Euler(0, 180, 0));
                spawnedPlayer.GetComponent<PlayerControls>().localShape = playerTwoShape;
            }
        }
    }
}