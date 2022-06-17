using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class HandleMovement : MonoBehaviour
    {
        StateManager stateManager;
        Rigidbody rb;

        // if we move the camera too fast, then the character will stop and run that direction
        public bool doAngleCheck = true;  
        [SerializeField]
        float degreesRunThreshold = 8;
        [SerializeField]
        bool useDot = true;

        bool overrideForce;
        bool inAngle;

        float rotateTimer_;
        float velocityChange = 4;
        bool applyJumpForce;

        Vector3 storeDirection;
        InputHandler inputHandler;

        [SerializeField] Vector3 curVelocity;
        [SerializeField] Vector3 targetVelocity;
        float prevAngle;
        Vector3 prevDir;

        Vector3 overrideDirection;
        float overrideSpeed;
        float forceOverrideTimer;
        float forceOverLife;
        bool stopVelocity;
        bool useForceCurve;
        AnimationCurve forceCurve;
        float fc_t;
        private float speed;
        private bool initVault;
        Vector3 startPosition;

        bool forceOverHasRan;
        delegate void ForceOverrideStart();
        ForceOverrideStart forceOverstart;
        delegate void ForceOverrideWrap();
        ForceOverrideWrap forceOverWrap;

        // this will be called from inputHandler
        public void Init(StateManager st, InputHandler i)
        {
            // as we are putting values from stateManager,
            // stateManager (st) needs to be initialized before this Init is called (inside InputHandler )
            inputHandler = i;
            stateManager = st;
            rb = st.rb;
           // stateManager.anim.applyRootMotion = false;
        }

        // This will be called from inputHandler
        public void FixedTick()
        {
            if(stateManager.currentState == StateManager.CharStates.vaulting)
            {
                if (!initVault)
                {
                    VaultLogicInit();
                    initVault = true;
                }
                else
                {
                    HandleVaulting();
                }
                return;
            }

            if (!overrideForce && !initVault)
            {
                HandleDrag();
                if (stateManager.onLocomotion)
                    MovementNormal();
                HandleJump();
            }
            else
            {
                stateManager.horizontal = 0;
                stateManager.vertical = 0;
                OverrideLogic();
            }
        }

        #region Vault

        bool canVault; // stateManager.vault er data ta just store korbo
        Vector3 targetVaultPosition;
        private Vector3 targetPos;

        private void VaultLogicInit()
        {
            //forceOverWrap = StopVaulting;
            canVault = stateManager.canVault;
            stateManager.canVault = false;
            VaultPhaseInit(stateManager.targetVaultPosition);
        }

        private void VaultPhaseInit(Vector3 targetVaultPosition)
        {
            // make collider of the controller is a trigger
            stateManager.controllerCollider.isTrigger = true;
            if (!stateManager.run)
                stateManager.anim.CrossFade(Statics.walkVault, 0.1f);
            else
                stateManager.anim.CrossFade(Statics.runVault, 0.05f);

            int mirror = UnityEngine.Random.Range(0, 2);
            stateManager.anim.SetBool(Statics.mirrorJump, (mirror > 0));

            forceOverrideTimer = 0;
            forceOverLife = Vector3.Distance(transform.position, targetPos);
            fc_t = 0; // animation curve

            stateManager.rb.isKinematic = true;
            startPosition = transform.position;
            overrideDirection = targetPos - startPosition;
            targetVaultPosition = targetPos;

            // How fast do we vault
            switch (stateManager.curVaultType)
            {
                case StateManager.VaultType.idle:
                    overrideSpeed = Statics.vaultSpeedIdle;
                    break;
                case StateManager.VaultType.walk:
                    overrideSpeed = Statics.vaultSpeedWalking;
                    break;
                case StateManager.VaultType.run:
                    overrideSpeed = Statics.vaultSpeedRunning;
                    break;

            }
        }

        private void HandleVaulting()
        {
            // this will ensure the curve is sampled on the actual length of the lerp
            fc_t += Time.deltaTime;

            float targetSpeed = overrideSpeed * inputHandler.vaultCurve.Evaluate(fc_t);

            forceOverrideTimer += Time.deltaTime * targetSpeed / forceOverLife;

            if(forceOverrideTimer > 1)
            {
                forceOverrideTimer = 1;
                StopVaulting();
            }

            Vector3 targetPosition = Vector3.Lerp(startPosition, targetVaultPosition, forceOverrideTimer);
            transform.position = targetPosition;

            // handleRotation
            Quaternion targetRot = Quaternion.LookRotation(overrideDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5);
        }

        private void StopVaulting()
        {
            stateManager.currentState = StateManager.CharStates.moving;
            stateManager.vaulting = false;
            stateManager.controllerCollider.isTrigger = false;
            stateManager.rb.isKinematic = false;
            initVault = false;
            StartCoroutine("OpenCanVaultIfApplicable");
        }

        IEnumerator OpenCanVaultIfApplicable()
        {
            yield return new WaitForSeconds(0.4f);
            stateManager.canVault = canVault; // enable it if the user has enabled it
        }

        // this is called from animation state behavior
        public void AddVelocity(Vector3 direction, float t, float force, bool clamp, bool useFCurve, AnimationCurve fcurve)
        {
            forceOverLife = t;
            overrideSpeed = force;
            overrideForce = true;
            forceOverrideTimer = 0;
            overrideDirection = direction;
            rb.velocity = Vector3.zero;
            stopVelocity = clamp;
            forceCurve = fcurve;
            useForceCurve = useFCurve;
        }

        #endregion

        #region Movement

        // This function will calculate the direction to move and call the function for applying velocity
        private void MovementNormal()
        {
            Debug.Log("is movement called");
            inAngle = stateManager.inAngle_MoveDir;

            // TODO this part is same as InputHandler line 69, we can get the value of v and h from there and assign here directly
            Vector3 v = inputHandler.cameraManager.transform.forward * stateManager.vertical;
            Vector3 h = inputHandler.cameraManager.transform.right * stateManager.horizontal;

            v.y = 0;
            h.y = 0;


            if (stateManager.isOnGround)
            {
                if (stateManager.onLocomotion)
                    HandleRotation_Normal(h, v);

                // initialize targetSpeed with walkspeed
                float targSpeed = stateManager.walkspeed;


                // if it is not incline or decline, i.e. flat surface
                // ground angle will be added in future
                //&& stateManager.groundAngle == 0
                if (stateManager.run )
                {
                    targSpeed = stateManager.runSpeed;
                }

                Debug.Log(h + "    " + v);
                // if our character is facing the same way the camera is facing
                if (inAngle)
                {

                    HandleVelocity_Normal(h, v, targSpeed);

                }
                else
                    rb.velocity = Vector3.zero;
            }

            HandleAnimations_Normal();        
        }

       
        // This is called from MovementNormal to Apply velocity to the character
        private void HandleVelocity_Normal(Vector3 h, Vector3 v, float spd)
        {
            curVelocity = rb.velocity;
            //Debug.Log(h + "   " + v);
            if(stateManager.horizontal != 0 || stateManager.vertical != 0)
            {
                targetVelocity = (h + v).normalized * spd;
                velocityChange = 3;
            }
            else
            {
                velocityChange = 2;
                targetVelocity = Vector3.zero;
            }

            Vector3 vel = Vector3.Lerp(curVelocity, targetVelocity, velocityChange);
            rb.velocity = vel;

            if (stateManager.obstacleForward)
                rb.velocity = Vector3.zero;
        }

        private void HandleRotation_Normal(Vector3 h, Vector3 v)
        {
            if(Mathf.Abs(stateManager.vertical) > 0 || Mathf.Abs(stateManager.horizontal) > 0)
            {
                storeDirection = (v + h).normalized;
                float targetAngle = Mathf.Atan2(storeDirection.x, storeDirection.z) * Mathf.Rad2Deg;

                if(stateManager.run && doAngleCheck)
                {
                    if (!useDot)
                    {
                        if((Mathf.Abs(prevAngle - targetAngle)) > degreesRunThreshold)
                        {
                            prevAngle = targetAngle;
                            PlayAnimSpecial(AnimSpecials.runToStop,false);
                            return;
                        }

                    }
                    else
                    {
                        float dot = Vector3.Dot(prevDir, stateManager.moveDirection);
                        if(dot < 0)
                        {
                            prevDir = stateManager.moveDirection;
                            PlayAnimSpecial(AnimSpecials.runToStop,false);
                            return;
                        }
                    }
                }
                prevDir = stateManager.moveDirection;
                prevAngle = targetAngle;

                storeDirection += transform.position;
                Vector3 targetDir = (storeDirection - transform.position).normalized;
                targetDir.y = 0;
                if (targetDir == Vector3.zero)
                    targetDir = transform.forward;
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, velocityChange * Time.deltaTime);

            }
        }

        #endregion

        #region JumpFunctions

        private void HandleJump()
        {
            if(stateManager.isOnGround && stateManager.canJump)
            {
                if(stateManager.jumpInput && !stateManager.jumping && stateManager.onLocomotion
                    && stateManager.currentState != StateManager.CharStates.hold && stateManager.currentState != StateManager.CharStates.onAir)
                {
                    // if we jump from idle state
                    if(stateManager.currentState == StateManager.CharStates.idle || stateManager.obstacleForward)
                    {
                        stateManager.anim.SetBool(Statics.special, true);
                        stateManager.anim.SetInteger(Statics.specialType, Statics.GetAnimSpecialType(AnimSpecials.jump_idle));
                    }

                    // if we are jumping from while moving
                    if(stateManager.currentState == StateManager.CharStates.moving && !stateManager.obstacleForward)
                    {
                        stateManager.LegFront();
                        stateManager.jumping = true;
                        stateManager.anim.SetBool(Statics.special, true);
                        stateManager.anim.SetInteger(Statics.specialType, Statics.GetAnimSpecialType(AnimSpecials.run_jump));
                        stateManager.currentState = StateManager.CharStates.hold;
                        stateManager.anim.SetBool(Statics.onAir, true);
                        stateManager.canJump = false;
                    }
                }
            }

            if (stateManager.jumping)
            {
                if (stateManager.isOnGround)
                {
                    if (!applyJumpForce)
                    {
                        StartCoroutine(AddJumpForce(0));
                        applyJumpForce = true;
                    }
                }
                else
                {
                    stateManager.jumping = false;
                }
            }
            else
            {

            }
        }

        IEnumerator AddJumpForce(float delay)
        {
            yield return new WaitForSeconds(delay);
            rb.drag = 0;
            Vector3 vel = rb.velocity;
            Vector3 forward = transform.forward;
            vel = forward * 3;
            vel.y = stateManager.jumpForce;
            rb.velocity = vel;
            StartCoroutine(CloseJump());
        }

        IEnumerator CloseJump()
        {
            // go up for 0.3 seconds
            yield return new WaitForSeconds(0.3f);
            stateManager.currentState = StateManager.CharStates.onAir;
            stateManager.jumping = false;
            applyJumpForce = false;
            stateManager.canJump = false;
            StartCoroutine(EnableJump());
        }

        IEnumerator EnableJump()
        {
            // will be able to jump after 1.3 seconds
            yield return new WaitForSeconds(1.3f);
            stateManager.canJump = true;
        }

        #endregion

        #region Utilities

        private void HandleDrag()
        {
            if(stateManager.horizontal != 0 || stateManager.vertical != 0 || stateManager.isOnGround == false)
            {
                rb.drag = 0;
            }
            else
            {
                rb.drag = 4;
            }
        }

        private void HandleAnimations_Normal()
        {
            // transform global position to local position
            Vector3 relativeDirection = transform.InverseTransformDirection(stateManager.moveDirection);

            float h = relativeDirection.x;
            float v = relativeDirection.z;

            // if there is obstacle, no walking animation
            if (stateManager.obstacleForward)
                v = 0;

            stateManager.anim.SetFloat(Statics.vertical, v, 0.2f, Time.deltaTime);
            stateManager.anim.SetFloat(Statics.horizontal, h, 0.2f, Time.deltaTime);
        }

        public void PlayAnimSpecial(AnimSpecials t, bool sptrue = true)
        {
            int n = Statics.GetAnimSpecialType(t);
            stateManager.anim.SetBool(Statics.special, sptrue);
            stateManager.anim.SetInteger(Statics.specialType, n);
            StartCoroutine(CloseSpecialOnAnim(0.4f));
        }

        IEnumerator CloseSpecialOnAnim(float v)
        {
            yield return new WaitForSeconds(v);
            stateManager.anim.SetBool(Statics.special, false);
        }

        //internal void AddVelocity(Vector3 direction, float t, float force, bool clamp)
        //{
        //    forceOverLife = t;
        //    overrideSpeed = force;
        //    overrideForce = true;
        //    forceOverrideTimer = 0;
        //    overrideDirection = direction;
        //    rb.velocity = Vector3.zero;  // as we are add forces to it, we firstly make this zero so we don't have extra velocity
        //    stopVelocity = clamp;

        //}

        private void OverrideLogic()
        {
            rb.drag = 0;

            if(!forceOverHasRan)  // Run any delegate we have assigned in the start
            {
                if (forceOverstart != null)
                    forceOverstart();

                forceOverHasRan = true;
            }

            float targetSpeed = overrideSpeed;

            if (useForceCurve)
            {
                fc_t += Time.deltaTime / forceOverLife;
                targetSpeed *= forceCurve.Evaluate(fc_t);
            }

            rb.velocity = overrideDirection * targetSpeed;

            forceOverrideTimer += Time.deltaTime;
            if(forceOverrideTimer > forceOverLife)
            {
                if (stopVelocity)
                    rb.velocity = Vector3.zero;

                stopVelocity = false;
                overrideForce = false;forceOverHasRan = false;

                if (forceOverWrap != null) // run any delegates we have assigned on end
                    forceOverWrap();

                forceOverWrap = null;
                forceOverstart = null;
            }



        }

        #endregion

    }
}
