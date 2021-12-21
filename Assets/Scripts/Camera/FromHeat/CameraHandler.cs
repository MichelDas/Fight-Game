using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    public Transform camTrans;
    public Transform target;
    public Transform pivot;
    public Transform mTransform;
    public bool leftPivot;
    float delta;

    [SerializeField] float mouseX;
    [SerializeField] float mouseY;
    float smoothX;
    float smoothY;
    float smoothXvelocity;
    float smoothYvelocity;
    float lookAngle;
    float tiltAngle;

    public CameraValues values;

    private void Start()
    {
        
    }

    public void Init(Transform tr)
    {
        mTransform = this.transform;
        //states = inp.states;
        target = tr;
    }

    public void FixedTick(float d)
    {
        delta = d;
        if(target == null)
        {
            Debug.Log("No Target");
            return;
        }
        HandlePositions();
        HandleRotation();

        float speed = values.MoveSpeed;
        //if (states.states.isAiming)
        //{
        //    speed = values.AimSpeed;
        //}

        Vector3 targetPosition = Vector3.Lerp(mTransform.position, target.position, delta * speed);
        mTransform.position = targetPosition;
    }

    void HandlePositions()
    {
        float targetX = values.NormalX;
        float targetZ = values.NormalZ;
        float targetY = values.NormalY;

        //if(states.states.isCrouching)
        //    targetY = values.CrouchY

        //if (states.states.isAiming)
        //{
        //    targetX = values.AimX;
        //    targetZ = values.AimZ;
        //}

        if (leftPivot)
        {
            targetX = -targetX;
        }

        Vector3 newPivotPosition = pivot.localPosition;
        newPivotPosition.x = targetX;
        newPivotPosition.y = targetY;

        Vector3 newCamPosition = camTrans.localPosition;
        newCamPosition.z = targetZ;
        //newCamPosition.y = targetY;

        float t = delta * values.AdaptSpeed;
        pivot.localPosition = Vector3.Lerp(pivot.localPosition, newPivotPosition, t);
        camTrans.localPosition = Vector3.Lerp(camTrans.localPosition, newCamPosition, t);
    }

    void HandleRotation()
    {
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if(values.TurnSmooth > 0)
        {
            smoothX = Mathf.SmoothDamp(smoothX, mouseX, ref smoothXvelocity, values.TurnSmooth);
            smoothY = Mathf.SmoothDamp(smoothY, mouseY, ref smoothYvelocity, values.TurnSmooth);
        }
        else
        {
            smoothX = mouseX;
            smoothY = mouseY;
        }

        lookAngle += smoothX * values.Y_rotate_speed;
        Quaternion targetRot = Quaternion.Euler(0, lookAngle, 0);
        mTransform.rotation = targetRot;

        tiltAngle -= smoothY * values.X_rotate_speed;
        tiltAngle = Mathf.Clamp(tiltAngle, values.MinAngle, values.MaxAngle);
        pivot.localRotation = Quaternion.Euler(tiltAngle, 0, 0);
    }
}
