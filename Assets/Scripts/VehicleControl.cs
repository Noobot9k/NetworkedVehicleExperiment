using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class VehicleControl : NetworkBehaviour {

    SimpleCarController Vehicle;

    [SyncVar] bool IsBeingPiloted = false;

    NetworkConnection CurrentPilot = null;

    public bool HasControl = false;

    [Command(ignoreAuthority = true)] void CmdRequestControl(NetworkConnectionToClient sender = null) {
        print(sender.ToString() + " requests control");
        if(CurrentPilot != null)
            TargetControlStatus(sender, false);
        else {
            CurrentPilot = sender;
            TargetControlStatus(sender, true);
            Vehicle.SetConnectedAuthority(sender);
            //Vehicle.GetComponent<NetworkIdentity>().AssignClientAuthority(sender);
        }

        IsBeingPiloted = CurrentPilot != null;
    }
    [Command(ignoreAuthority = true)] void CmdReleaseControl(NetworkConnectionToClient sender = null) {
        if (CurrentPilot == sender) {
            IsBeingPiloted = false;
            CurrentPilot = null;
            Vehicle.SetConnectedAuthority(null);
            //Vehicle.GetComponent<NetworkIdentity>().RemoveClientAuthority();
        }
        
        // if a client is trying to send inputs but doesn't have control, they may be out of sync so this should be sent whether they had control or not.
        TargetControlStatus(sender, false);
    }
    [Command(ignoreAuthority = true)] void CmdSetInputs(float Throttle, float Steer, NetworkConnectionToClient sender = null) {
        if(sender != CurrentPilot) {
            Debug.LogWarning("Client " + sender.ToString() + " is trying to control " + gameObject.name + " without authority");
            // if a client is trying to send inputs but doesn't have control, they may be out of sync so this should be sent to remind them.
            TargetControlStatus(sender, false);
            return;
        }

        Vehicle.Throttle = Throttle;
        Vehicle.Steer = Steer;
    }
    [TargetRpc] void TargetControlStatus(NetworkConnection target, bool givenControl) {
        HasControl = givenControl;
        InputHandler.main.SetSendInputToCharacter(!givenControl);
    }


    void Start() {
        Vehicle = GetComponentInParent<SimpleCarController>();
    }
    void Update() {

        if(isClient) {
            if(Input.GetButtonDown("Interact")) {
                if(HasControl) {
                    HasControl = false;
                    InputHandler.main.SetSendInputToCharacter(true);
                    CmdReleaseControl();
                } else if(IsBeingPiloted == false) {
                    if(CameraController.main.MouseHit == transform) {
                        HasControl = true;
                        InputHandler.main.SetSendInputToCharacter(false);
                        CmdRequestControl();
                    }
                }
            }

            if(HasControl) {
                float Throttle = Input.GetAxis("Vertical");
                float Steer = Input.GetAxis("Horizontal");
                Vehicle.LocalThrottle = Throttle;
                Vehicle.LocalSteer = Steer;
                CmdSetInputs(Throttle, Steer);
            }
        }
        if(isServer) {

        }
    }
}
