using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class Navigation_Base : NetworkBehaviour {

    [Tooltip("Measured in seconds per meter of distance between chaser and target.")]
    public float TargetLead = .333f;
    public float MaxLeadMultiplier = 3;

    // references
    NavMeshAgent Agent;
    Character_Base CharControl;
    Character_Base TargetPlayer = null;

    void FakeStart() {

        CharControl = GetComponent<Character_Base>();

        Agent = GetComponent<NavMeshAgent>();
        Agent.updatePosition = false;
        Agent.updateRotation = false; // true;
    }
    public override void OnStartClient() {
        base.OnStartClient();
        FakeStart();
    }
    public override void OnStartServer() {
        base.OnStartServer();
        FakeStart();
    }
    void Update() {
        if(CharControl.CheckAuthority() == false) return;

        GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");

        TargetPlayer = GetClosestPlayer(Players, out float TargetPlayerDistance);

        if(TargetPlayer != null) {
            Vector3 leadVector = Quaternion.Euler(0, TargetPlayer.AngularVelocity.y * Time.deltaTime * Mathf.Clamp(TargetLead * TargetPlayerDistance, 0, MaxLeadMultiplier), 0)
                * TargetPlayer.Velocity * Mathf.Clamp(TargetLead * TargetPlayerDistance, 0, MaxLeadMultiplier);
            if(IsOnVehicleWithPlayer(TargetPlayer)) leadVector = Vector3.zero;
            Agent.SetDestination(TargetPlayer.transform.position + leadVector);

            Debug.DrawLine(transform.position, TargetPlayer.transform.position + leadVector, Color.magenta);
        }

        Agent.nextPosition = transform.position;
        CharControl.Move(Vector3.ClampMagnitude(Agent.desiredVelocity, 1));
        if (Agent.desiredVelocity.magnitude > 1) CharControl.LookVector = Agent.desiredVelocity.normalized;
    }

    Character_Base GetClosestPlayer(GameObject[] Players, out float Distance) {
        Character_Base ClosestPlayer = null;
        float ClosestPlayerDistance = Mathf.Infinity;
        foreach(GameObject player in Players) {
            float distance = Vector3.Distance(player.transform.position, transform.position);
            if(distance < ClosestPlayerDistance) {
                Character_Base character = player.GetComponent<Character_Base>();
                if(character == null) continue;
                ClosestPlayer = character;
                ClosestPlayerDistance = distance;
            }
        }
        Distance = ClosestPlayerDistance;
        return ClosestPlayer;
    }
    Character_Base GetClosestPlayer(GameObject[] Players) {
        return GetClosestPlayer(Players, out float _);
    }
    bool IsOnVehicleWithPlayer(Character_Base player) {
        if(CharControl.PersistentFloorObject != null && player.PersistentFloorObject != null) {

            Rigidbody PlayerFloorBody = GetRigidBodyOfCollider(player.PersistentFloorObject);
            Rigidbody CharFloorBody = GetRigidBodyOfCollider(CharControl.PersistentFloorObject);

            if(PlayerFloorBody != null && CharFloorBody != null) {
                if(CharFloorBody == PlayerFloorBody) {
                    return true;
                } else if(CharFloorBody.transform.IsChildOf(PlayerFloorBody.transform)) {
                    return true;
                } else if(PlayerFloorBody.transform.IsChildOf(CharFloorBody.transform)) {
                    return true;
                }
            }
        }
        return false;
    }
    Rigidbody GetRigidBodyOfCollider(Collider collider) {
        return collider.GetComponentInParent<Rigidbody>();
    }
    private void OnCollisionStay(Collision collision) {
        if(CharControl.CheckAuthority() == false) return;

        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("VehicleLayer")) {
            if(TargetPlayer != null && IsOnVehicleWithPlayer(TargetPlayer) == false) {
                CharControl.Jump = true;
            } else {
                CharControl.Jump = false;
            }
        }
    }
    private void FixedUpdate() {
        if(CharControl.CheckAuthority() == false) return;

        CharControl.Jump = false;
    }
}
