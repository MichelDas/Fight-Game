using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class CameraManager : MonoBehaviour
    {
        public bool holdCamera;    
        public bool addDefaultAsNormal;   // if true, it we will take a default cameraState
        public Transform target;

        #region Variables
        [SerializeField]
        private string activeStateID;
        [SerializeField]
        float moveSpeed = 5;
        [SerializeField]
        float turnSpeed = 1.5f;
        [SerializeField]
        float turnSpeedController = 5.5f;
        [SerializeField]
        float turnSmoothing = .1f;
        [SerializeField]
        bool isController;
        public bool lockCursor;
        #endregion

        #region References
        [HideInInspector]
        public Transform pivot;
        [HideInInspector]
        public Transform camTrans;
        #endregion]

        static public CameraManager instance;

        Vector3 targetPosition;
        [HideInInspector]
        public Vector3 targetPositionOffset;

        #region Internal Variables
        float x;
        float y;
        float lookAngle;
        float tiltAngle;
        float offsetX;
        float offsetY;
        float smoothX = 0;
        float smoothY = 0;
        float smoothXvelocity = 0;
        float smoothYvelocity = 0;
        #endregion

        [SerializeField]
        List<CameraState> cameraStates = new List<CameraState>();
        CameraState activeState;
        CameraState defaultState;   // this is the state that the camera will be in when the game begins

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            if(Camera.main.transform == null)
            {
                Debug.Log("You haven't assigned a camera with the tag 'MainCamera");
            }

            camTrans = Camera.main.transform.parent;
            pivot = camTrans.parent;

            // Create Default State
            CameraState cameraState = new CameraState();

            cameraState.id = "default";
            cameraState.minAngle = 35;
            cameraState.maxAngle = 35;
            cameraState.cameraFOV = Camera.main.fieldOfView;
            cameraState.cameraZ = camTrans.localPosition.z;
            cameraState.pivotPosition = pivot.localPosition;

            defaultState = cameraState;

            if (addDefaultAsNormal)
            {
                cameraStates.Add(defaultState);
                defaultState.id = "normal";
            }

            activeState = defaultState;
            activeStateID = activeState.id;
            FixPositions();

            if(lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void FixedUpdate()
        {
            if (target)
            {
                targetPosition = target.position + targetPositionOffset;
            }

            CameraFollow();

            if (!holdCamera)
                HandleRotation();

            FixPositions();
        }

        private void CameraFollow()
        {
            Vector3 camPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            transform.position = camPosition;
        }

        private void HandleRotation()
        {
            HandleOffsets();

            x = Input.GetAxis("Mouse X") + offsetX;
            y = Input.GetAxis("Mouse Y") + offsetY;

            float targetTurnSpeed = turnSpeed;

            if (isController)
            {
                targetTurnSpeed = turnSpeedController;
            }

            if(turnSmoothing > 0)
            {
                smoothX = Mathf.SmoothDamp(smoothX, x, ref smoothXvelocity, turnSmoothing);
                smoothY = Mathf.SmoothDamp(smoothY, y, ref smoothYvelocity, turnSmoothing);
            }
            else
            {
                smoothX = x;
                smoothY = y;
            }

            lookAngle += smoothX * targetTurnSpeed;

            // reset the look angle when it does a full circle
            if (lookAngle > 360)
                lookAngle = 0;
            if (lookAngle < -360)
                lookAngle = 0;

            transform.rotation = Quaternion.Euler(0f, lookAngle, 0);

            tiltAngle -= smoothY * targetTurnSpeed;
            tiltAngle = Mathf.Clamp(tiltAngle, -activeState.minAngle, activeState.maxAngle);

            pivot.localRotation = Quaternion.Euler(tiltAngle, 0, 0);


        }

        private void HandleOffsets()
        {
            if(offsetX != 0)
            {
                offsetX = Mathf.MoveTowards(offsetX, 0, Time.deltaTime);
            }

            if(offsetY != 0)
            {
                offsetY = Mathf.MoveTowards(offsetY, 0, Time.deltaTime);
            }
        }

        private CameraState GetState(string id)
        {
            CameraState retVal = null;
            for(int i=0; i<cameraStates.Count; i++)
            {
                if(cameraStates[i].id == id)
                {
                    retVal = cameraStates[i];
                    break;
                }
            }
            return retVal;
        }

        public void ChangeState(string id)
        {
            if (activeState.id != id)
            {
                CameraState targetState = GetState(id);
                if (targetState == null)
                {
                    Debug.Log("Camera state ' " + id + " ' not found! using previous");
                    return;
                }
                activeState = targetState;
                activeStateID = activeState.id;
            }
        }

        private void FixPositions()
        {
            Vector3 targetPivotPosition = (activeState.useDefaultPosition) ? defaultState.pivotPosition : activeState.pivotPosition;
            pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPivotPosition, Time.deltaTime * 5);

            float targetZ = (activeState.useDefaultCameraZ) ? defaultState.cameraZ : activeState.cameraZ;
            Vector3 targetP = camTrans.localPosition;
            targetP.z = Mathf.Lerp(targetP.z, targetZ, Time.deltaTime * 5);
            camTrans.localPosition = targetP;

            float targetFov = (activeState.useDefaultFOV) ? defaultState.cameraFOV : activeState.cameraFOV;

            if (targetFov < 1)
            {
                targetFov = 2;
            }
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFov, Time.deltaTime);
        }

        IEnumerator LerpCameraFOV(float z)
        {
            float cur = Camera.main.fieldOfView;
            float targetFov = z;
            if(targetFov < 1)
            {
                targetFov = 2;
            }
            float t = 0;

            while(t < 1)
            {
                t += Time.deltaTime * 5;
                Camera.main.fieldOfView = Mathf.Lerp(cur, targetFov, t);
                yield return null;
            }
        }

        [System.Serializable]
        public class CameraState
        {
            [Header("Name of State")]
            public string id;
            [Header("Limits")]
            public float minAngle;
            public float maxAngle;
            [Header("Pivot Position")]
            public bool useDefaultPosition;
            public Vector3 pivotPosition;
            [Header("Camera Position")]
            public bool useDefaultCameraZ;
            public float cameraZ;
            [Header("Camera FOV")]
            public bool useDefaultFOV;
            public float cameraFOV;
        }
    }
}
