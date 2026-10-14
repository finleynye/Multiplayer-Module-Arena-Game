using System;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameStateManager gameStateManager;

    public void SpawnPlayer(PlayerMovement movement)
    {
        if(spawnPoints.Length == 0) return;

        int index = (int)(movement.OwnerClientId % (ulong) spawnPoints.Length);
        Transform spawn = spawnPoints[index];

        movement.rb.position = spawn.position;
        movement.rb.rotation = spawn.rotation;
    }
}
