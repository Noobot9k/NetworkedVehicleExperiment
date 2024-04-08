using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Character_Base))]
public class InputHandler : NetworkBehaviour {

    public static InputHandler main;

    Character_Base charHandler;
    Class_Base Class;

    bool SendInputToCharacter = true;
    public int fpslimit = 0;

    void Start() {
        if(isLocalPlayer == false) this.enabled = false;

        charHandler = GetComponent<Character_Base>();
        Class = GetComponent<Class_Base>();
    }
    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();

        main = this;
    }
    void Update() {
        Vector3 moveVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if(SendInputToCharacter) {
            charHandler.Move(moveVector, true);
            charHandler.Jump = Input.GetButton("Jump");

            if(Input.GetButtonDown("Ability1"))
                if (Class.Ability1Event != null) Class.Ability1Event.Invoke(true);
            else if (Input.GetButtonUp("Ability1"))
                if (Class.Ability1Event != null) Class.Ability1Event.Invoke(false);

            Class.Fire1 = Input.GetButton("Fire1") || Input.GetAxisRaw("Fire1Axis") > 0f;
        }

        Camera cam = Camera.main;
        if(cam) {
            if(cam.transform.up.y > .5f) charHandler.LookVector = cam.transform.forward;
            else if (cam.transform.forward.y < 0) charHandler.LookVector = cam.transform.up;
            else if (cam.transform.forward.y > 0) charHandler.LookVector = -cam.transform.up;
        }

        Application.targetFrameRate = fpslimit;
    }

    public void SetSendInputToCharacter(bool sendInput) {
        SendInputToCharacter = sendInput;
        if (sendInput == false) {
            charHandler.Move(Vector3.zero);
            charHandler.Jump = false;
        }
    }
}
