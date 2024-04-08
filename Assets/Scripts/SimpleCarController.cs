using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class SimpleCarController : NetworkBehaviour {
    public List<AxleInfo> axleInfos; // the information about each individual axle
    public List<SimpleCarController> Trailers = new List<SimpleCarController>();
    public ConfigurableJoint LeaderCarConnector;
    public float maxMotorTorque; // maximum torque the motor can apply to wheel
    public float maxSteeringAngle; // maximum steer angle the wheel can have

    [SyncVar] public float Steer = .5f;
    [SyncVar] public float Throttle = .5f;
    public float LocalSteer = .5f;
    public float LocalThrottle = .5f;

    // references
    NetworkIdentity identity;

    void Start() {
        identity = GetComponent<NetworkIdentity>();
    }

    public override void OnStartServer() {
        base.OnStartServer();

        GetComponent<Rigidbody>().isKinematic = false;
    }

    public void FixedUpdate() {
        if(CheckAuthority() == false) {
            LocalSteer = Steer;
            LocalThrottle = Throttle;
        }

        float motor = maxMotorTorque * LocalThrottle;//.5f; //Input.GetAxis("Vertical");
        float steering = maxSteeringAngle * LocalSteer; //.5f; //Input.GetAxis("Horizontal");
        
        if(LeaderCarConnector) {
            Vector3 SwivelPoint = LeaderCarConnector.transform.TransformPoint(LeaderCarConnector.anchor);
            Vector3 localSwivel = transform.InverseTransformPoint(SwivelPoint);
            steering = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(localSwivel, Vector3.up).normalized, Vector3.up);
            print(steering);
        }

        foreach(SimpleCarController trailer in Trailers) {
            trailer.Throttle = Throttle;
            trailer.LocalThrottle = LocalThrottle;
        }

        foreach(AxleInfo axleInfo in axleInfos) {
            if(axleInfo.steering) {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if(axleInfo.motor) {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }

            axleInfo.leftWheel.GetWorldPose(out Vector3 Lpos, out Quaternion Lrot);
            Transform Lwheel = axleInfo.leftWheel.transform.GetChild(0);
            Lwheel.position = Lpos;
            Lwheel.rotation = Lrot;

            axleInfo.rightWheel.GetWorldPose(out Vector3 Rpos, out Quaternion Rrot);
            Transform Rwheel = axleInfo.rightWheel.transform.GetChild(0);
            Rwheel.position = Rpos;
            Rwheel.rotation = Rrot;

        }
    }
    bool CheckAuthority() {
        if(isServer && identity.connectionToClient == null) return true;
        if(isClient && hasAuthority) return true;
        return false;
    }
    [Server]
    public void SetAuthority(NetworkConnectionToClient owner) {
        if (owner == null) {
            identity.GetComponent<NetworkIdentity>().RemoveClientAuthority();
        } else {
            identity.GetComponent<NetworkIdentity>().AssignClientAuthority(owner);
        }
    }
    [Server]
    public void SetConnectedAuthority(NetworkConnectionToClient owner) {
        SetAuthority(owner);
        foreach(SimpleCarController connectedBody in Trailers) {
            connectedBody.SetAuthority(owner);
        }
    }
}

[System.Serializable]
public class AxleInfo {
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor; // is this wheel attached to motor?
    public bool steering; // does this wheel apply steer angle?
}