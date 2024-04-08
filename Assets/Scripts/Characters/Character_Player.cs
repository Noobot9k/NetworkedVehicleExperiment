using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Player : Character_Base {

    public override void OnStartClient() {
        base.OnStartClient();

        if(!isLocalPlayer) {
            RB.detectCollisions = false;
        } else {
            CameraController.main.target = this;
            RB.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

}
