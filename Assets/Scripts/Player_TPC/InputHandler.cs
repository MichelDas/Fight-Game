using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class InputHandler : MonoBehaviour
    {
        StateManager stateManager;
        [HideInInspector]
        public CameraManager cameraManager;
        HandleMovement handleMovement;

        float horizontal;
        float vertical;
        internal AnimationCurve vaultCurve;

        void Awake()
        {
            handleMovement = gameObject.GetComponent<HandleMovement>();
            if (handleMovement == null)
                handleMovement = gameObject.AddComponent<HandleMovement>();

            stateManager = GetComponent<StateManager>();

            if (stateManager == null)
            {
                stateManager = gameObject.AddComponent<StateManager>();
            }
        }

        void Start()
        {
            if (CameraManager.instance)
                cameraManager = CameraManager.instance;
            if(cameraManager)
                cameraManager.target = this.transform;

            // Init in order
            stateManager.isPlayer = true;

            stateManager.Init();
            handleMovement.Init(stateManager, this);

            FixPlayerMeshes();
        }

        private void FixPlayerMeshes()
        {
            SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>();
            for(int i=0; i<skinned.Length; i++)
            {
                skinned[i].updateWhenOffscreen = true;
            }  
        }

        private void FixedUpdate()
        {
            stateManager.FixedTick();
            UpdateStatesFromInput();
            handleMovement.FixedTick();
        }

        private void Update()
        {
            // This only Checks if the player is onGround
            stateManager.Tick();
        }

        private void UpdateStatesFromInput()
        {
            vertical = Input.GetAxis(Statics.Vertical);
            horizontal = Input.GetAxis(Statics.Horizontal);

            Vector3 v = cameraManager.transform.forward * vertical;
            Vector3 h = cameraManager.transform.right * horizontal;

            v.y = 0;
            h.y = 0;

            stateManager.horizontal = horizontal;
            stateManager.vertical = vertical;

            Vector3 moveDir = (h + v).normalized;
            stateManager.moveDirection = moveDir;
            stateManager.inAngle_MoveDir = InAngle(stateManager.moveDirection, 25);

            // TODO confirm if  we neec to check for run as well
            if(stateManager.walk && horizontal != 0 || stateManager.walk && vertical != 0)
            {
                stateManager.inAngle_MoveDir = true;
            }


            stateManager.onLocomotion = stateManager.anim.GetBool(Statics.onLocomotion);
            HandleRun();

            stateManager.jumpInput = Input.GetButton(Statics.Jump);

        }

        private bool InAngle(Vector3 targetDir, float angleThreshold)
        {
            bool retVal = false;
            float angle = Vector3.Angle(transform.forward, targetDir);

            if(angle < angleThreshold)
            {
                retVal = true;
            }

            return retVal;
        }

        private void HandleRun()
        {
            bool runInput = Input.GetButton(Statics.Fire3);

            // If Left shift is being pressed,
            // WalkMode will be false and RunMode will be enabled
            if (runInput)
            {
                stateManager.walk = false;
                stateManager.run = true;
            }
            else
            {
                stateManager.walk = true;
                stateManager.run = false;
            }

            
            if(horizontal != 0 || vertical != 0)
            {
                stateManager.run = runInput;
                stateManager.anim.SetInteger(Statics.specialType, Statics.GetAnimSpecialType(AnimSpecials.run));
            }
            else
            {
                // if horizontal and vertical is 0
                // stop run
                if (stateManager.run)
                    stateManager.run = false;
            }

            // These are for some Angle Checks to stop run
            if (!stateManager.inAngle_MoveDir && handleMovement.doAngleCheck)
                stateManager.run = false;

            if (stateManager.obstacleForward)
                stateManager.run = false;

            if (stateManager.run == false)
                stateManager.anim.SetInteger(Statics.specialType, Statics.GetAnimSpecialType(AnimSpecials.runToStop));
        }
    }
}
