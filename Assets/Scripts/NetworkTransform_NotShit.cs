using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkTransform_NotShit : NetworkBehaviour {

    [SyncVar] Vector3 Position = Vector3.zero;
    [SyncVar] Quaternion Rotation = new Quaternion();
    [SyncVar] Vector3 Velocity = Vector3.zero;
    [SyncVar] Vector3 AngularVelocity = Vector3.zero;

    Vector3 posLastFrame = new Vector3();
    Quaternion rotLastFrame = new Quaternion();
    Vector3 lastPos = Vector3.zero;
    Quaternion lastRot = new Quaternion();
    float AverageLinearSpeed = 0;
    float AverageAngularSpeed = 0;
    float lastPosUpdate = -100;
    float lastRotUpdate = -100;
    float lastPosUpdateLength = 0;
    float lastRotUpdateLength = 0;

    [Header("Dead Reckoning")]
    public float DR_Linear_Threshold = 2;
    public float DR_Angular_Threshold = 7.5f;
    public float DR_VelocityMagnitudal_Threshold = .5f;
    public float DR_VelocityAngular_Threshold = 35f;
    public float DR_AngularVelocity_Threshold = 5;

    //references
    Rigidbody rb;
    NetworkIdentity identity;
    public List<NetworkTransform_NotShit> SyncedBodies = new List<NetworkTransform_NotShit>();

    private void Start() {
        rb = GetComponent<Rigidbody>();
        identity = GetComponent<NetworkIdentity>();
    }


    void FixedUpdate() {
        if(CheckAuthority() == false) return;
        
        Position = transform.localPosition;
        Rotation = transform.localRotation;
        if(rb) {
            Velocity = rb.velocity;
            AngularVelocity = rb.angularVelocity;
        }

        if (!isServer)
        CmdSyncState(Position, Rotation, Velocity, AngularVelocity);
    }
    void Update() {
        if(CheckAuthority() == true) return;

        float DeltaTime = Time.deltaTime;

        if (posLastFrame != Position) {
            lastPos = posLastFrame;
            posLastFrame = Position;
            lastPosUpdateLength = Time.time - lastPosUpdate;
            lastPosUpdate = Time.time;
            float distance = (lastPos - Position).magnitude;
            AverageLinearSpeed = distance / lastPosUpdateLength;
        }
        if (rotLastFrame != Rotation) {
            lastRot = rotLastFrame;
            rotLastFrame = Rotation;
            lastRotUpdateLength = Time.time - lastRotUpdate;
            lastRotUpdate = Time.time;
            float angle = Quaternion.Angle(lastRot, Rotation);
            AverageAngularSpeed = angle / lastRotUpdateLength;
        }


        if(CheckIfOutOfRange(true)) {

            UpdateConnectedStates();
        }

        float TimeSinceLastLinearUpdate = Time.time - lastPosUpdate;
        Vector3 posWorldSpace = Position;
        Quaternion rotWorldSpace = Rotation;
        if(transform.parent) {
            posWorldSpace = transform.parent.TransformPoint(Position);
            rotWorldSpace = transform.parent.rotation * Rotation;
        }
        Debug.DrawRay(posWorldSpace, rotWorldSpace * Vector3.forward * (Velocity.magnitude * TimeSinceLastLinearUpdate), Color.red, 0, false);
        Debug.DrawRay(posWorldSpace + Vector3.left * .25f, Vector3.right     * .5f, Color.green, 0, false);
        Debug.DrawRay(posWorldSpace + Vector3.down * .25f, Vector3.up        * .5f, Color.green, 0, false);
        Debug.DrawRay(posWorldSpace + Vector3.back * .25f, Vector3.forward   * .5f, Color.green, 0, false);
    }

    void UpdateConnectedStates() {
        UpdateState();
        foreach(NetworkTransform_NotShit connectedBody in SyncedBodies) {
            connectedBody.UpdateState();
        }
    }
    public void UpdateState() {
        transform.localPosition = Position;
        transform.localRotation = Rotation;
        if(rb) {
            rb.velocity = Velocity;
            rb.angularVelocity = AngularVelocity;
        }
    }

    bool CheckIfOutOfRange(bool Verbose = false) {
        float TimeSinceLastLinearUpdate = Time.time - lastPosUpdate;
        float LinearInaccuracy = Velocity.magnitude * TimeSinceLastLinearUpdate;
        float TimeSinceLastAngularUpdate = Time.time - lastRotUpdate;
        float AngularInaccuracy = AngularVelocity.magnitude * TimeSinceLastAngularUpdate;

        if(Vector3.Distance(transform.localPosition, Position) > DR_Linear_Threshold + LinearInaccuracy)                                    { Debug.Log("Position out of range. Correcting..."); return true; }
        if(Quaternion.Angle(transform.localRotation, Rotation) > DR_Angular_Threshold + AngularInaccuracy)                                  { Debug.Log("Rotation out of range. Correcting..."); return true; }
        if(rb) {
            if(Mathf.Abs(rb.velocity.magnitude - Velocity.magnitude) > DR_VelocityMagnitudal_Threshold)                                     { Debug.Log("Speed out of range. Correcting..."); return true; }
            if(Vector3.Angle(rb.velocity.normalized, Velocity.normalized) > DR_VelocityAngular_Threshold)                                   { Debug.Log("Direction of movement out of range. Correcting..."); return true; }
            if(Quaternion.Angle(Quaternion.Euler(rb.angularVelocity), Quaternion.Euler(AngularVelocity)) > DR_AngularVelocity_Threshold)    { Debug.Log("AngularVelocity out of range. Correcting..."); return true; }
        }
        return false;
    }

    [Command(ignoreAuthority = true)] void CmdSyncState(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angleVel, NetworkConnectionToClient sender = null) {
        if(identity.connectionToClient != sender) { Debug.LogWarning("Client " + sender.ToString() + " is trying to control " + gameObject.name + " without authority!"); return; }

        Position = pos;
        Rotation = rot;
        Velocity = vel;
        AngularVelocity = angleVel;
    }
    bool CheckAuthority() {
        if(isServer && identity.connectionToClient == null) return true;
        if(isClient && hasAuthority) return true;
        return false;
    }
}