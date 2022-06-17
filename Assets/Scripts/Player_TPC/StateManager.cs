using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class StateManager : MonoBehaviour
    {
        [Header("Info")]
        public GameObject modelPrefab;
        public bool inGame;
        public bool isPlayer;

        [Header("Stats")]
        public float groundDistance = 0.6f; // how far do we shoot ray from the collider 
        public float groundOffset = 0; // how much up do we keep our model from the ground
        public float distanceToCheckForward = 1.3f; // length of the ray for obstacles forward
        public float runSpeed = 5;
        public float walkspeed = 2;
        public float jumpForce = 4;
        // if we on air for more then this time, we will play some animations like rolling on landing
        public float airTimeThreshold = 0.8f;  

        [Header("Inputs")]
        public float horizontal;
        public float vertical;
        public bool jumpInput;

        [Header("States")]
        public bool obstacleForward;
        public bool groundForward;
        public float groundAngle;
        public bool vaulting;

        #region StateRequests
        [Header("State Requests")]
        public CharStates currentState;
        public bool isOnGround;
        public bool run;
        public bool walk;
        public bool onLocomotion;
        public bool inAngle_MoveDir;  // if we are actually looking towards where we are moving
        public bool jumping;
        public bool canJump;
        public bool canVault = true;
        #endregion

        #region References
        [SerializeField] GameObject activeModel;
        [HideInInspector]
        public Animator anim;
        [HideInInspector]
        public Rigidbody rb;
        [HideInInspector]
        public Collider controllerCollider;
        #endregion

        #region Variables
        [HideInInspector]
        public Vector3 moveDirection;
        public float airTime;
        [HideInInspector]
        public bool prevGround;
        [HideInInspector]
        public Vector3 targetVaultPosition;
        #endregion

        LayerMask ignoreLayers;

        public enum VaultType
        {
            idle, walk, run
        }

        [HideInInspector]
        public VaultType curVaultType;
        private float vaultOverHeight;
        private float VaultFloorHeightDifference;

        public enum CharStates
        {
            idle, moving, onAir, hold, vaulting
        }

        #region Init Phase
        public void Init()
        {
            inGame = true;
            CreateModel();  // This Should handle model creation and initialization

            // This will take avatar of the child animator and
            //put that to the animator attached to this obj
            SetupAnimator();

            // This takes care of the Rigidbody initialization
            AddControllerReferences();

            canJump = true;
            gameObject.layer = 8;
            ignoreLayers = ~(1 << 3 | 1 << 8);

            controllerCollider = GetComponent<Collider>();
            if (controllerCollider == null)
            {
                Debug.Log("No Collider found for the controller!!");
            }
        }

        private void CreateModel()
        {
            //activeModel = Instantiate(modelPrefab) as GameObject;
           // activeModel.transform.parent = this.transform;
            activeModel.transform.localPosition = Vector3.zero;
            activeModel.transform.localEulerAngles = Vector3.zero;
            activeModel.transform.localScale = Vector3.one;
        }

        private void SetupAnimator()
        {
            anim = GetComponent<Animator>();

            Animator childAnim = activeModel.GetComponent<Animator>();
            anim.avatar = childAnim.avatar;
            Destroy(childAnim);
        }

        private void AddControllerReferences()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.angularDrag = 999;
            rb.drag = 4;
            rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
        }
        #endregion

        public void Tick()
        {
            isOnGround = OnGround();
        }

        public void FixedTick()
        {
            // we don't run fixed update in state manager
            if (currentState == CharStates.hold || currentState == CharStates.vaulting)
                return;


            obstacleForward = false;
            groundForward = false;
            isOnGround = OnGround();

            if (isOnGround)
            {
                Vector3 origin = transform.position;
                // clear forward?
                origin += Vector3.up * 0.75f;
                IsClear(origin, transform.forward, distanceToCheckForward, ref obstacleForward);

                if (!obstacleForward)
                {
                    // is ground forward?
                    origin += transform.forward * 0.6f;
                    //if there is no obstacle, we have to check furthur down to find if the ground is in obtuce angle
                    // basically we will find obtuce angle when going from flat to downslope
                    IsClear(origin, -transform.up, groundDistance * 3, ref groundForward);
                }
                else
                {
                    // I am looking to an obstacle but am moving other ways like back, left or right
                    if(Vector3.Angle(transform.forward, moveDirection) > 30)
                    {
                        // be able to move
                        obstacleForward = false;
                    }
                }
            }
            UpdateState();
            MonitorAirTime();
        }

       

        private void UpdateState()
        {
            // about to jump means on hold
            if (currentState == CharStates.hold)
                return;

            // we are vaulting
            if (vaulting)
            {
                currentState = CharStates.vaulting;
                return;
            }

            // we are moving
            if (horizontal != 0 || vertical != 0)
            {
                currentState = CharStates.moving;
            }
            else
            {
                // we are idle
                currentState = CharStates.idle;
            }

            // we are on air
            if (!isOnGround)
            {
                currentState = CharStates.onAir;
            }
        }


        private void MonitorAirTime()
        {
            if (!jumping)
                anim.SetBool(Statics.onAir, !isOnGround);

            if (isOnGround)
            {
                if (prevGround != isOnGround)
                {
                    anim.SetInteger(Statics.jumpType,
                        (airTime > airTimeThreshold) ?
                        (horizontal != 0 || vertical != 0) ? 2 : 1 // 2 is roll, 1 is hard landing
                        : 0);
                }
                airTime = 0;
            }
            else
            {
                airTime += Time.deltaTime;
            }

            prevGround = isOnGround;
        }

        
        private void IsClear(Vector3 origin, Vector3 direction, float distance, ref bool isHit)
        {
            RaycastHit hit = new RaycastHit();
            float targetDistance = distance;
            if (run)
                targetDistance += 0.5f;

            int numberOfHits = 0;

            for(int i=-1; i<2; i++)
            {
                Vector3 targetOrigin = origin;
                targetOrigin += transform.right * (i * 0.3f);
                Debug.DrawRay(targetOrigin, direction * distance, Color.green);
                if (Physics.Raycast(origin, direction, out hit, distance, ignoreLayers))
                {
                    isHit = true;
                    numberOfHits++;
                }
                else
                {
                    isHit = false;
                }
            }

            // before incline walk we will have a obstacle forward true as the ray will collide with upslope
            if (obstacleForward)
            {
                // ** here, we will calculate after hitting the obstacle, which way the ray goes
                // if the angle between the ray and the reflected ray is greater then 70 then it is a slope rather then obstacle

                // line of the ray
                Vector3 incomingVec = hit.point - origin;
                // line of the reflected ray
                Vector3 reflectVect = Vector3.Reflect(incomingVec, hit.normal);
                float angle = Vector3.Angle(incomingVec, reflectVect);


                if(angle < 70)
                {
                    // we can walk on it
                    obstacleForward = false;
                }
                else
                {
                    // let's see if we can vault on it
                    if(numberOfHits > 2) // we need to hit all three raycasts
                    {
                        bool willVault = false;
                        // if we can vault
                        CanVaultOver(hit, ref willVault);

                        if (willVault)
                        {
                            curVaultType = VaultType.walk;
                            if (run)
                                curVaultType = VaultType.run;
                            obstacleForward = false;
                            return;
                        }
                        else
                        {
                            obstacleForward = true;
                            return;
                        }
                    }
                }
            }

            // This is the ray a little in front then the origin
            if (groundForward)
            {
                // we are moving
                if(currentState == CharStates.moving)
                {
                    Vector3 p1 = transform.position;
                    Vector3 p2 = hit.point;
                    float diffY = p1.y - p2.y;
                    groundAngle = diffY;
                }

                float targetInCline = 0;

                if(Mathf.Abs(groundAngle) > 0.3f)
                {
                    if (groundAngle < 0)
                        targetInCline = 1;
                    else
                        targetInCline = -1;
                }

                if (groundAngle == 0)
                    targetInCline = 0;

                anim.SetFloat(Statics.incline, targetInCline, 0.3f, Time.deltaTime);
            }
        }

        internal void LegFront()
        {
            Vector3 ll = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            Vector3 rl = anim.GetBoneTransform(HumanBodyBones.RightFoot).position;
            Vector3 rel_ll = transform.InverseTransformPoint(ll);
            Vector3 rel_rr = transform.InverseTransformPoint(rl);

            bool left = rel_ll.z > rel_rr.z;
            anim.SetBool(Statics.mirrorJump, left);

        }

        #region Check Is on ground
        public bool OnGround()
        {
            bool retVal = false;

            if (currentState == CharStates.hold)
                return false;

            Vector3 origin = transform.position + (Vector3.up * 0.55f);

            RaycastHit hit = new RaycastHit();
            bool isHit = false;
            FindGround(origin, ref hit, ref isHit);

            // the central ray can miss sometimes when we are on air,
            // so we will cast four more rays downwards and
            // if none of them finds any ground  we can say we are on air
            if (!isHit)
            {
                for(int i=0; i<4; i++)
                {
                    Vector3 newOrigin = origin;

                    switch (i)
                    {
                        case 0: // forward
                            newOrigin += Vector3.forward / 3;
                            break;
                        case 1: // backwards
                            newOrigin -= Vector3.forward / 3;
                            break;
                        case 2: // left
                            newOrigin -= Vector3.right / 3;
                            break;
                        case 3: // right
                            newOrigin += Vector3.right / 3;
                            break;
                    }

                    FindGround(newOrigin, ref hit, ref isHit);

                    if (isHit == true)
                        break;
                }
            }

            retVal = isHit;


            // if ground is found stick the character to the ground
            // ** out collider wont touch the ground here
            if(retVal != false)
            {
                Vector3 targetPosition = transform.position;
                targetPosition.y = hit.point.y + groundOffset;
                transform.position = targetPosition;
            }

            return retVal;
        }

        private void FindGround(Vector3 origin, ref RaycastHit hit, ref bool isHit)
        {
            Debug.DrawRay(origin, -Vector3.up * 0.5f, Color.red);
            if(Physics.Raycast(origin, -Vector3.up, out hit, groundDistance, ignoreLayers))
            {
                isHit = true;
            }
        }

        #endregion

        #region Vaulting
        private void CanVaultOver(RaycastHit hit, ref bool willVault)
        {
            if (!onLocomotion || !inAngle_MoveDir)
                return;

            // We hit a wall around height of the knee
            // then we nee to see if we can vault over it
            Vector3 wallDirection = -hit.normal * 0.5f;
            // the opposite of the normal, is going to return us the direction
            // if the whole level is set with box colliders, then this will work like  charm
            RaycastHit vHit;
            Vector3 wallOrigin = transform.position + Vector3.up * vaultOverHeight;
            Debug.DrawRay(wallOrigin, wallDirection * Statics.vaultCheckDistance, Color.red);

            if(Physics.Raycast(wallOrigin, wallDirection, out vHit, Statics.vaultCheckDistance, ignoreLayers))
            {
                // it's a wall
                Debug.Log("wall found");
                willVault = false;
                return;
            }
            else
            {
                // it's not a wall
                // Now let's check if we can vault over
                if(canVault && !vaulting)
                {
                    Vector3 startOrigin = hit.point;
                    startOrigin.y = transform.position.y;
                    Vector3 vOrigin = startOrigin + Vector3.up * vaultOverHeight;

                    vOrigin += wallDirection * Statics.vaultCheckDistance;
                    Debug.DrawRay(vOrigin, -Vector3.up * Statics.vaultCheckDistance);

                    // we will do a raycast to check if there is ground on the other side of the obstacle
                    // if there is ground we will vault there
                    if(Physics.Raycast(vOrigin, -Vector3.up, out vHit, Statics.vaultCheckDistance, ignoreLayers))
                    {
                        float hitY = vHit.point.y;
                        float diff = hitY - transform.position.y;

                        if(Mathf.Abs(diff) < VaultFloorHeightDifference)
                        {
                            vaulting = true;
                            targetVaultPosition = vHit.point;
                            willVault = true;
                            return;
                        }
                    }
                }
            }
        }


        #endregion
    }
}