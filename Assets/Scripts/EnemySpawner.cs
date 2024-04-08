using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour {

    public GameObject EnemyPrefab;

    public float EnemyCount = 100;
    public float EnemySpawnRange = 50;

    public override void OnStartServer() {
        base.OnStartServer();
        for(int i = 0; i < EnemyCount; i++) {
            //NetworkManager.singleton.pref
            GameObject Enemy = Instantiate<GameObject>(EnemyPrefab);
            Enemy.transform.parent = gameObject.transform;
            Vector2 randomPos = Random.insideUnitCircle;
            Enemy.transform.position = transform.position + new Vector3(randomPos.x, 0, randomPos.y) * EnemySpawnRange + Vector3.up * 8;
            NetworkServer.Spawn(Enemy);
        }
    }
    private void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(transform.position, EnemySpawnRange);
    }

    void Start() {
        
    }
    void Update() {

    }
}
