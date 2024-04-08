using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public Character_Base target;
    public Vector3 Offset = Vector3.up * .5f;
    new public Transform camera;
    [HideInInspector] public Transform MouseHit = null;

    public static CameraController main;

    public float Sensitivity = 4;

    void Start() {
        main = this;
        Cursor.lockState = CursorLockMode.None;
    }
    void LateUpdate() {
        if(Input.GetButtonUp("Menu")) {
            if(Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }


        if(!target) return;

        float xDelta = Input.GetAxisRaw("Mouse X") + Input.GetAxisRaw("LookHorizontal");
        float yDelta = Input.GetAxisRaw("Mouse Y") + Input.GetAxisRaw("LookVertical");

        transform.position = target.transform.TransformPoint(Offset);
        transform.rotation *= Quaternion.Euler(0, Sensitivity * xDelta, 0) * Quaternion.Euler(Vector3.Project(target.AngularVelocity * Time.deltaTime, Vector3.up) );
        camera.transform.localRotation *= Quaternion.Euler(Sensitivity * -yDelta, 0, 0);
        camera.transform.localRotation.ToAngleAxis(out float angle, out Vector3 axis);
        camera.transform.localRotation = Quaternion.AngleAxis(Mathf.Clamp(angle, -90, 90), axis.x >= 0 ? Vector3.right : Vector3.left);

        bool hit = Physics.Raycast(camera.position, camera.forward, out RaycastHit MouseHitInfo, 2, ~0, QueryTriggerInteraction.Ignore);
        if(hit) MouseHit = MouseHitInfo.collider.transform;
        else MouseHit = null;
        if (hit)
            Debug.DrawRay(camera.position, camera.forward * 2, Color.yellow);
        else
            Debug.DrawRay(camera.position, camera.forward * 2, Color.red);
    }
}
