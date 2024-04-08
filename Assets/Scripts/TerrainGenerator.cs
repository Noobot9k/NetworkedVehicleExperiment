using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TerrainGenerator : NetworkBehaviour {

    public Transform ObsticlePrefab;

    public float ObsticleCount = 500;
    public float ObsticleRange = 150;
    public Vector2 ScaleRange = new Vector2(1, 5);

    public override void OnStartServer() {
        base.OnStartServer();
        for(int i = 0; i < ObsticleCount; i++) {
            Vector3 size = new Vector3(Random.Range(ScaleRange.x, ScaleRange.y), Random.Range(ScaleRange.x, ScaleRange.y), Random.Range(ScaleRange.x, ScaleRange.y));
            
            Transform newObj = Instantiate<Transform>(ObsticlePrefab, Vector3.up * size.y / 2, Quaternion.Euler(0,Random.Range(-180, 180),0), transform);
            newObj.localScale = size;

            Vector3 randomPos = RandomizePosition();
            while(Vector3.Distance(randomPos, Vector3.zero) < 10) {
                randomPos = RandomizePosition();
            }
            newObj.position += randomPos;

            NetworkServer.Spawn(newObj.gameObject);
        }
    }
    Vector3 RandomizePosition() {
        Vector2 randomPos = Random.insideUnitCircle;
        return new Vector3(randomPos.x, 0, randomPos.y) * ObsticleRange;
    }
}
