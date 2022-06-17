using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    Rigidbody rigidbody;
    Animator anim;
    
    [SerializeField] private float speed = 4;
    [SerializeField] private float turnSpeed = 5;
    [SerializeField] float horizontal;
    [SerializeField] float vertical;
    [SerializeField] float lerpRate = 5;
    [SerializeField] float attackTimer = 1;
    [SerializeField] bool blockedAttack;

    [SerializeField] PhysicMaterial zfriction; // zero friction
    [SerializeField] PhysicMaterial mfriction; // maximum friction
    [SerializeField] CameraHandler cameraHandler;
    public LayerMask layerMask;
    Vector3 directionPos;
    Vector3 lookPos;

    Transform cam;
    
    CapsuleCollider capCol;

    //Attack Variables
    float targetValue;
    float curValue;
    bool holdAttack;
    bool attack;
    float aTimer;
    float decTimer;

    // Block Variables
    bool blocking;
    float bTimer;

    // Mouse Variables
    float MouseX;
    float MouseY;

    public bool BlockedAttack { get => blockedAttack; set => blockedAttack = value; }


    // Start is called before the first frame update
    void Start()
    {
        if (cameraHandler)
            cameraHandler.Init(transform);

        cam = Camera.main.transform;

        rigidbody = GetComponent<Rigidbody>();
        capCol = GetComponent<CapsuleCollider>();
        SetupAnimator();
        
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        lookPos = ray.GetPoint(100);

        HandleFriction();
        //ControlAttackAnimations();
        //ControlBlockAnimations();
    }
    public bool onGround;
    private void FixedUpdate()
    {
        if(cameraHandler)
            cameraHandler.FixedTick(Time.deltaTime);

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 ver = cam.forward * vertical;
        Vector3 hor = cam.right * horizontal;

        ver.y = 0;
        hor.y = 0;

        onGround = IsOnGround();

        HandleMovement(hor, ver, onGround);
        HandleRotation(hor, ver, onGround);

        anim.SetFloat("Forward", vertical, 0.1f, Time.deltaTime);
        anim.SetFloat("Sideways", horizontal, 0.1f, Time.deltaTime);

        return;


        rigidbody.AddForce(((cam.right * horizontal) + (cam.forward * vertical)) * speed / Time.deltaTime);

        directionPos = transform.position + cam.forward * 100;

        Vector3 dir = directionPos - transform.position;
        dir.y = 0;

        Quaternion lookdir = Quaternion.LookRotation(dir);

        rigidbody.rotation = Quaternion.Slerp(transform.rotation, lookdir , turnSpeed * Time.deltaTime);

        Debug.Log(lookdir + "  " + transform.rotation);
        
    }

    bool IsOnGround()
    {
        return true;
        Vector3 origin = transform.position + new Vector3(0, 0.5f, 0);
        float dis = 10f;
        RaycastHit hit;
        //Debug.DrawLine(origin, -Vector3.up, Color.red, dis);
        Debug.DrawRay(origin, -Vector3.up, Color.red, dis);
        if (Physics.Raycast(origin, -Vector3.up, out hit, dis, layerMask))
        {
            return true;
        }
        return false;
    }

    private void HandleMovement(Vector3 hor, Vector3 ver, bool onGround)
    {
        if (onGround)
        {
            //Debug.Log((ver + hor).normalized);
            Debug.Log(Speed());

            rigidbody.AddForce((ver + hor) * Speed() / Time.deltaTime); ;
        }
    }

    Vector3 storeDirection;
    public Vector3 dir;

    private void HandleRotation(Vector3 hor, Vector3 ver, bool onGround)
    {
        storeDirection = transform.position + hor + ver;

        dir = storeDirection - transform.position;
        dir.y = 0;

        if (horizontal != 0 || vertical != 0)
        {
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle != 0)
            {
                float newAngle = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(dir));
                if (newAngle != 0)
                {
                    rigidbody.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                }
            }
        }
    }

    private float Speed()
    {
        float retVal = 0;

        //if (statesManager.isAiming)
        //{
        //    retVal = aimSpeed;
        //}
        //else
        //{
        //    if (statesManager.isWalking || statesManager.isReloading)
        //    {
        //        retVal = walkSpeed;
        //    }
        //    else
        //    {
        //        retVal = runSpeed;
        //    }
        //}

        //retVal *= speedMultiplier;
        retVal = speed;

        return retVal;
    }

    void HandleFriction()
    {
        capCol.material = (horizontal == 0 && vertical == 0) ? mfriction : zfriction;
    }

    //void ControlAttackAnimations()
    //{
    //    MouseX = Input.GetAxis("Mouse X");
    //    MouseY = Input.GetAxis("Mouse Y");

    //    float mouseLB = Input.GetAxis("Fire1");
    //    #region Decide attack type

    //    if (mouseLB > 0.1f)
    //    {
    //        decTimer += Time.deltaTime;

    //        int attackType;
    //        if(Mathf.Abs(MouseX) > Mathf.Abs(MouseY))
    //        {
    //            if (MouseX < 0) // looking to right
    //            {
    //                attackType = 2;
    //            }
    //            else // looking to left
    //            {
    //                attackType = 1;
    //            }
    //        }
    //        else
    //        {
    //            attackType = 0;
    //        }

    //        anim.SetInteger("AttackType", attackType);

    //        if(decTimer > 0.5f)
    //        {
    //            holdAttack = true;
    //            anim.SetBool("Attacking", true);
    //            decTimer = 0;
    //        }
    //    }

    //    #endregion

    //    if(mouseLB < 0.1f)
    //    {
    //        if (holdAttack)
    //            attack = true;
    //    }
    //    if (holdAttack)
    //    {
    //        if (attack)
    //        {
    //            aTimer += Time.deltaTime;

    //            targetValue = 1;

    //            if(aTimer > attackTimer)
    //            {
    //                holdAttack = false;
    //                attack = false;
    //                anim.SetBool("Attacking", false);
    //                aTimer = 0;
    //                targetValue = 0;
    //            }
    //        }
    //        else
    //        {
    //            targetValue = 0;
    //        }
    //    }
    //    else
    //    {
    //        targetValue = 0;
    //    }

    //    if (blockedAttack)
    //        targetValue = 0;
    //    curValue = Mathf.MoveTowards(curValue, targetValue, Time.deltaTime * lerpRate);
    //    anim.SetFloat("Attack", curValue);
    //}

    void ControlBlockAnimations()
    {

        float mouseRB = Input.GetAxis("Fire2");

        if (mouseRB > 0.1f)
        {
            decTimer += Time.deltaTime;
            if (!blocking)
            {
                float blockType = 0;
                if (Mathf.Abs(MouseX) > Mathf.Abs(MouseY))
                {
                    if (MouseX < 0) // looking to right
                    {
                        blockType = 1;
                    }
                    else // looking to left
                    {
                        blockType = 2;
                    }
                }
                else
                {
                    blockType = 0;
                }

                anim.SetFloat("BlockSide", blockType);
            }

            if (decTimer > 0.5f)
            {
                anim.SetBool("Block", true);
                blocking = true;
                decTimer = 0;
            }
        }
        else
        {
            anim.SetBool("Block", false);
            blocking = false;
        }

        if (blockedAttack)
        {
            bTimer += Time.deltaTime;
            if(bTimer > 1)
            {
                blockedAttack = false;
                bTimer = 0;
            }
        }

    }

    void SetupAnimator()
    {
        anim = GetComponent<Animator>();


        // I use avatar from a child animator component if present
        // this is to enable easy swapping of the character model as a child node
        foreach (var childAnimator in GetComponentsInChildren<Animator>())
        {
            if(childAnimator != anim)
            {
                anim.avatar = childAnimator.avatar;
                Destroy(childAnimator);
                //Debug.Log("found animator");
                break; // stop searching after getting the first animator
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        anim.SetLookAtWeight(1, 0.5f, 1, 1, 1);
        anim.SetLookAtPosition(lookPos);
    }
}
