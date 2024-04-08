using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

[RequireComponent(typeof(Character_Base))]
public class Class_Base : Destructable_Base {

    public StateChangeEvent Ability1Event = new StateChangeEvent();
    public StateChangeEvent Fire1Event = new StateChangeEvent();

    public bool Fire1 = false;
    bool _LastFire1 = false;

    [Header("References")]
    public GameObject ViewmodelPrefab;
    GameObject CurrentViewmodel;
    Animator ViewmodelAnimator;
    Character_Base CharControl;

    protected virtual void OnAbility1Changed(bool IsDown) {
        if(IsDown) {
            ViewmodelAnimator.SetTrigger("Ability1Used");
        }
    }

    protected virtual void OnEnable() {
        Ability1Event.AddListener(OnAbility1Changed);
        
        CurrentViewmodel = Instantiate<GameObject>(ViewmodelPrefab, CameraController.main.camera, false);
        ViewmodelAnimator = CurrentViewmodel.GetComponentInChildren<Animator>();
    }
    protected virtual void OnDisable() {
        Ability1Event.RemoveListener(OnAbility1Changed);
        
        GameObject.Destroy(CurrentViewmodel);
        CurrentViewmodel = null;
    }
    protected override void Start() {
        base.Start();

        CharControl = GetComponent<Character_Base>();
    }
    protected override void Update() {
        base.Update();

        if (Fire1 != _LastFire1) {
            _LastFire1 = Fire1;
            if(Fire1Event != null) Fire1Event.Invoke(Fire1);
        }

        float Speed = Vector3.Distance(Vector3.ProjectOnPlane(CharControl.InheritedVelocity, Vector3.up), Vector3.ProjectOnPlane(CharControl.Velocity, Vector3.up));
        float MoveAlpha = Speed / CharControl.WalkSpeed;
        float MoveAlphaClamped = Mathf.Clamp01(MoveAlpha);
        float IdleAlpha = 1 - MoveAlpha;
        ViewmodelAnimator.SetFloat("MoveSpeed", MoveAlpha * MoveAlphaClamped);
        ViewmodelAnimator.SetFloat("InverseMoveSpeed", (1 - MoveAlpha) * MoveAlphaClamped);
        ViewmodelAnimator.SetFloat("IdleAlpha", IdleAlpha);
    }
}

public class StateChangeEvent : UnityEvent<bool> { }