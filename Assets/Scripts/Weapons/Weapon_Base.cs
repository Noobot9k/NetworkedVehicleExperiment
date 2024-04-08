using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Class_Base)), RequireComponent(typeof(Character_Base))]
public class Weapon_Base : NetworkBehaviour {

    [Header("Weapon")]
    public bool FullAuto = true;
    public float Damage = 40;
    public Vector2 RandomSpread = new Vector2(2, 2);
    public float Firerate = 4;
    protected float _lastShot = -100;

    [Header("References")]
    public GameObject BulletholePrefab;
    Class_Base ConnectedClass;

    protected virtual void OnFire1Change(bool IsDown) {
        if(IsDown == true && CheckCanFire()) { // && FullAuto == false // this is uneeded
            FireBullet();
        }
    }

    protected virtual void FireBullet() {
        if(CheckCanFire() == false) return; // this shouldn't be relied on as override methods in inheriting classes might not have the same check.
        _lastShot = Time.time;

        Transform source = CameraController.main.camera.transform;
        Vector2 spreadValue = Random.insideUnitCircle * RandomSpread;
        Quaternion spreadQuat = Quaternion.Euler(spreadValue.y, spreadValue.x, 0);
        Quaternion shotRotation = Quaternion.LookRotation(source.forward) * spreadQuat;

        bool hit = Physics.Raycast(source.position, shotRotation * Vector3.forward, out RaycastHit hitInfo, 1000);
        Debug.DrawRay(source.position, shotRotation * Vector3.forward * 1000, Color.cyan, 2, true);
        if(hit) {
            Destructable_Base destructable = hitInfo.transform.GetComponent<Destructable_Base>();
            if(destructable) {
                //print(destructable);
                destructable.ChangeHealth(-Damage);
            } else {
                GameObject bullethole = Instantiate<GameObject>(BulletholePrefab, hitInfo.point, Quaternion.LookRotation(-hitInfo.normal), hitInfo.transform.parent);
            }
        }
    }
    protected bool CheckCanFire() {
        float cooldown = 1 / Firerate;
        return Time.time >= _lastShot + cooldown;
    }

    protected virtual void OnEnable() {
        ConnectedClass.Fire1Event.AddListener(OnFire1Change);
    }
    protected virtual void OnDisable() {
        ConnectedClass.Fire1Event.RemoveListener(OnFire1Change);
    }

    protected virtual void Awake() {
        ConnectedClass = GetComponent<Class_Base>();
    }
    protected virtual void Update() {
        if(FullAuto && ConnectedClass.Fire1 && CheckCanFire()) {
            FireBullet();
        }
    }
}
