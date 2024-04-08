using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DEPRICATED_Character_Base_CharControl : MonoBehaviour {

    // References
    CharacterController charControl;

    public Vector3 LookVector = Vector3.forward;
    Vector3 MoveVector = Vector3.zero;
    Vector3 Velocity = Vector3.zero;
    public Vector3 AngularVelocity = Vector3.zero;
    Quaternion floorLastRotation = Quaternion.identity;
    Vector3 lastFloorRelitivePosition = Vector3.zero;

    public float Friction = 2;
    public float StaticFrictionThreshold = 1;
    public float WalkSpeed = 5;
    public float JumpPower = 5;

    public bool Jump = false;
    bool _lastJumped = false;

    void Start() {
        charControl = GetComponent<CharacterController>();
        charControl.detectCollisions = false;
        charControl.enableOverlapRecovery = true;
    }
    void Update() {
        float DeltaTime = Time.deltaTime;

        transform.forward = Vector3.ProjectOnPlane(LookVector, Vector3.up).normalized;

        if (_lastJumped != Jump) {
            _lastJumped = Jump;
            if(Jump && charControl.isGrounded) {
                Velocity += Vector3.up * 5;
            }
        } else if(charControl.isGrounded) {

            bool hit = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, charControl.height / 2 + .1f);
            Vector3 floorVelocity = Vector3.zero;
            Vector3 floorAngularVelocity = Vector3.zero;
            if(hit) {
                Rigidbody hitBody = hitInfo.rigidbody; // hitInfo.collider.gameObject.GetComponentInParent<Rigidbody>();
                if(hitBody) {
                    floorVelocity = Vector3.ProjectOnPlane(hitBody.GetPointVelocity(transform.position + Vector3.down * charControl.height/2), Vector3.up);
                    //floorVelocity = hitBody.GetRelativePointVelocity(hitBody.transform.InverseTransformPoint(transform.position)) + hitBody.velocity;
                    //floorVelocity = hitBody.velocity;
                    //floorAngularVelocity = hitBody.angularVelocity;
                    if(Quaternion.Angle(hitBody.transform.rotation, floorLastRotation) > 5) floorLastRotation = hitBody.transform.rotation;
                    else floorAngularVelocity = (hitBody.transform.rotation * Quaternion.Inverse(floorLastRotation)).eulerAngles;
                    floorLastRotation = hitBody.transform.rotation;
                }
                Vector3 BodyProjectedVelocity = Vector3.ProjectOnPlane(floorVelocity, Vector3.up);
                Vector3 ProjectedVelocity = Vector3.ProjectOnPlane(Velocity , Vector3.up);
                if (Vector3.Distance(BodyProjectedVelocity, ProjectedVelocity) > StaticFrictionThreshold) {
                    Velocity = Vector3.MoveTowards(ProjectedVelocity, BodyProjectedVelocity, Friction * DeltaTime) + Vector3.up * Velocity.y;
                } else {
                    Velocity = BodyProjectedVelocity + Vector3.up * Velocity.y;
                }
                lastFloorRelitivePosition = hitInfo.transform.InverseTransformPoint(transform.position + Vector3.down * charControl.height / 2);
            }
            AngularVelocity = floorAngularVelocity;

            if (Velocity.y < 0)
                Velocity = Vector3.ProjectOnPlane(Velocity, Vector3.up) + Vector3.down * .5f;
        }

        charControl.Move(((MoveVector * WalkSpeed) + Velocity) * DeltaTime);

        Velocity += Physics.gravity * DeltaTime;
    }

    public void Move(Vector3 vector, bool CameraRelitive = false) {
        if (CameraRelitive == false) {
            MoveVector = Vector3.ClampMagnitude(vector, 1);
        } else {
            CameraController cam = CameraController.main;
            if(cam) {
                float magnitude = vector.magnitude;
                MoveVector = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(cam.transform.TransformDirection(vector), Vector3.up).normalized * magnitude, 1);
            }
        }
    }
}
