using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Destructable_Base : NetworkBehaviour {

    [Header("Health")]
    [SyncVar] public float Health = 100;
    [SyncVar] public float MaxHealth = 100;
    [SyncVar] public float TemporaryHealth = 0;
    public bool DestroyOnDeath = true;

    [Server] public void ChangeHealth(float HealthDelta) {
        if(Health <= 0) return;

        Health += HealthDelta;

        if (Health <= 0 && HealthDelta < 0) {
            OnDeath(-HealthDelta);
        }
    }
    protected virtual void OnDeath(float killingDamage) {
        IEnumerator cor() {
            yield return new WaitForSeconds(0);
            GameObject.Destroy(gameObject);
        }
        if(DestroyOnDeath)
            StartCoroutine(cor());
    }

    protected virtual void Start() {

    }
    protected virtual void Update() {

    }
}
