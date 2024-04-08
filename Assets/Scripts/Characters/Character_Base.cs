using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
public class Character_Base : NetworkBehaviour {

    // References
    protected Rigidbody RB;
    protected CapsuleCollider Collider;
    protected ConfigurableJoint groundJoint;

    protected Vector3 MoveVector = Vector3.zero;
    [HideInInspector] public Vector3 LookVector = Vector3.forward;
    [HideInInspector] public Vector3 Velocity = Vector3.zero;
    [HideInInspector] public Vector3 AngularVelocity = Vector3.zero;
    [HideInInspector] public Rigidbody FloorBody = null;
    [HideInInspector] public Collider FloorObject = null;
    [HideInInspector] public Collider PersistentFloorObject = null;
    [HideInInspector] public Vector3 InheritedVelocity = Vector3.zero;
    Quaternion floorLastRotation = Quaternion.identity;
    Vector3 lastPositionFloorSpace = Vector3.zero;
    Vector3 floorLastContactPoint = Vector3.zero;
    [SyncVar] Transform Parent;

    [SyncVar] public bool ClientAuthority = false;
    public float FloorObjectPersistenceTime = 1;
    float _FloorObjectChangedTick = -100;
    public bool IsLimp = false;
    public float FrictionBreakThreshold = 20;
    public float AngularFrictionBreakThreshold = 70;
    public float Friction = 4;
    public float WalkSpeed = 7.5f;
    public float Acceleration = 20;
    public bool AutoRejump = false;
    public float JumpPower = 5;
    public float JumpCooldown = .5f;
    float _TickJumped = -100;

    public bool Jump = false;
    bool _lastJumped = false;
    bool isGrounded = false;
    [SerializeField]
    protected bool InSlideZone = false;

    private void FakeStart() {
        // ALL CODE HERE MUST NOT BE HARMFUL TO RUN MORE THAN ONCE ON THE SAME SYSTEM!
        RB = GetComponent<Rigidbody>();
        Collider = GetComponent<CapsuleCollider>();
        groundJoint = GetComponent<ConfigurableJoint>();

        if (CheckAuthority()) {

            RB.isKinematic = false;

            RB.centerOfMass = Vector3.zero;
            RB.freezeRotation = true;
        } else {
            RB.detectCollisions = false;
        }
    }
    private void Start() {
        FakeStart();
    }
    public override void OnStartClient() {
        FakeStart();

        base.OnStartClient();
    }
    public override void OnStartServer() {
        FakeStart();

        base.OnStartServer();
    }

    //[ClientCallback]
    void FixedUpdate() {
        if(CheckAuthority() == false){
            transform.parent = Parent;
            return;
        }

        float DeltaTime = Time.fixedDeltaTime; // Time.fixedDeltaTime;
        //bool isGrounded = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Collider.height / 2 + .1f);
        //FloorBody = null;
        //if(isGrounded) {
        //    FloorBody = hitInfo.rigidbody;
        //    FloorObject = hitInfo.collider;
        //}
        Vector3 floorVelocity = Vector3.zero;
        Vector3 floorAngularVelocity = Vector3.zero;

        transform.forward = Vector3.ProjectOnPlane(LookVector, Vector3.up).normalized;

        Velocity = RB.velocity;

        if (MoveVector.magnitude < .1f) {
            Collider.material.staticFriction = 1f;
            Collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
        } else {
            Collider.material.staticFriction = 0;
            Collider.material.frictionCombine = PhysicMaterialCombine.Minimum;
        }
        if(_lastJumped != Jump && Time.time > _TickJumped + JumpCooldown) {
            _TickJumped = Time.time;
            _lastJumped = Jump;
            if(Jump && isGrounded) {
                Velocity += Vector3.up * 5;
            }
        } else if(isGrounded) {

            if(FloorBody) {
                if(FloorBody.GetComponent<NetworkIdentity>()) {
                    if(transform.parent != FloorBody.transform) {
                        transform.parent = FloorBody.transform;
                        SetParent(FloorBody.transform);
                    }
                } else {
                    if(transform.parent != null) {
                        transform.parent = null;
                        SetParent(null);
                    }
                }

                Vector3 floorContactPoint = transform.position + Vector3.down * Collider.height / 2;
                Vector3 positionFloorSpace = FloorObject.transform.InverseTransformPoint(floorContactPoint);
                Vector3 floorPositionAtLastPoint = FloorObject.transform.TransformPoint(lastPositionFloorSpace);
                Vector3 difference = floorPositionAtLastPoint - floorLastContactPoint;

                if(difference.magnitude > FrictionBreakThreshold * DeltaTime) difference = Vector3.zero;
                floorVelocity = Vector3.ProjectOnPlane(difference, Vector3.up) / DeltaTime;
                InheritedVelocity = floorVelocity;

                floorLastContactPoint = floorContactPoint;
                lastPositionFloorSpace = positionFloorSpace;
                //floorVelocity = Vector3.ProjectOnPlane(hitBody.GetPointVelocity(transform.position + Vector3.down * Collider.height/2), Vector3.up);


                if(Quaternion.Angle(FloorBody.transform.rotation, floorLastRotation) > AngularFrictionBreakThreshold * DeltaTime) floorLastRotation = FloorBody.transform.rotation;
                else floorAngularVelocity = (FloorBody.transform.rotation * Quaternion.Inverse(floorLastRotation)).eulerAngles; //this is being overwritten.
                floorLastRotation = FloorBody.transform.rotation;

                floorAngularVelocity = FloorBody.angularVelocity;
            } else {
                InheritedVelocity = Vector3.zero;

                if(transform.parent != null) {
                    transform.parent = null;
                    SetParent(null);
                }
            }

            AngularVelocity = floorAngularVelocity / DeltaTime;

            if(Velocity.y < 0)
                Velocity = Vector3.ProjectOnPlane(Velocity, Vector3.up) + Vector3.down * .5f;
        } else {
            if(transform.parent != null) {
                transform.parent = null;
                SetParent(null);
            }
        }

        Velocity += Physics.gravity * DeltaTime;

        if((Velocity - floorVelocity).magnitude <= WalkSpeed * 1.05 && InSlideZone == false) {
            Vector3 localVelocity = Velocity - InheritedVelocity;
            if(localVelocity.magnitude > 1 && MoveVector.magnitude > .1f && Vector3.Dot(localVelocity.normalized, MoveVector.normalized) < 0) // snap movement
                Velocity = InheritedVelocity + Vector3.ProjectOnPlane(localVelocity, MoveVector.normalized);
            Velocity = Vector3.MoveTowards(Vector3.ProjectOnPlane(Velocity, Vector3.up), InheritedVelocity + MoveVector * WalkSpeed, Acceleration * DeltaTime) + Vector3.up * Velocity.y;
        } else
            Velocity = Vector3.MoveTowards(Vector3.ProjectOnPlane(Velocity, Vector3.up), InheritedVelocity + MoveVector * WalkSpeed, Friction * DeltaTime) + Vector3.up * Velocity.y;

        if (IsLimp == false)
            RB.velocity = Velocity;

        if(isGrounded == false) {
            if(AutoRejump == true) {
                //Jump = false;
                _lastJumped = false; // this may need to be removed depending on how this variable ends up being used in the future.
            }
        }
        if (FloorObject == null && Time.time > _FloorObjectChangedTick + FloorObjectPersistenceTime) {
            PersistentFloorObject = null;
        }
        InSlideZone = false;
        isGrounded = false;
        FloorBody = null;
        FloorObject = null;
    }
    private void OnCollisionStay(Collision collision) {
        if(CheckAuthority() == false) return;

        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);

        foreach(ContactPoint contact in contacts) {
            if (Vector3.Angle(contact.normal, Vector3.up) < 45) {
                isGrounded = true;
                FloorObject = contact.otherCollider;
                PersistentFloorObject = FloorObject;
                _FloorObjectChangedTick = Time.time;
                Rigidbody body = FloorObject.GetComponent<Rigidbody>();
                if(body) {
                    FloorBody = FloorObject.GetComponent<Rigidbody>();
                }
            }
        }
    }

    [Command]
    protected void CmdSetParent(Transform parent) {
        Parent = parent;
    }
    protected void SetParent(Transform parent) {
        if(isServer)
            Parent = parent;
        else
            CmdSetParent(parent);
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

    private void OnTriggerStay(Collider other) {
        if(other.CompareTag("PhysicsSlide")) {
            InSlideZone = true;
        }
    }

    public bool CheckAuthority() {
        //print(gameObject.name + ": \n   ClientAuth = " + ClientAuthority.ToString() + " IsServer = " + isServer.ToString());
        return ((ClientAuthority && isLocalPlayer) || (ClientAuthority == false && isServer));
    }
}
