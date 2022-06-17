using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class AddVelocity_ASB : StateMachineBehaviour
    {
        public float life = 0.4f;
        public float force = 6;
        public Vector3 direction;
        [Space]
        [Header("This will override the direction")]
        public bool useTransformForward;
        public bool additive;
        public bool onEnter;
        public bool onExit;
        [Header("When Ending Applying velocity! Not anim state")]
        public bool onEndClampVelocity;

        [Header("Use this to tailor the force application")]
        public bool useForceCurve;
        public AnimationCurve forceCurve;

        StateManager stateManager;
        HandleMovement handleMovement;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onEnter)
            {
                if(useTransformForward && !additive)
                    direction = animator.transform.forward;

                if (useTransformForward && additive)
                    direction += animator.transform.forward;

                if (stateManager == null)
                    stateManager = animator.transform.GetComponent<StateManager>();

                if (!stateManager.isPlayer)
                    return;

                if (handleMovement == null)
                    handleMovement = animator.transform.GetComponent<HandleMovement>();
                Debug.Log("is this coming to on Enter");

                handleMovement.AddVelocity(direction, life, force, onEndClampVelocity, useForceCurve, forceCurve);
                
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onExit)
            {
                if (useTransformForward && !additive)
                    direction = animator.transform.forward;

                if (useTransformForward && additive)
                    direction += animator.transform.forward;

                if (stateManager == null)
                    stateManager = animator.transform.GetComponent<StateManager>();

                if (!stateManager.isPlayer)
                    return;

                if (handleMovement == null)
                    handleMovement = animator.transform.GetComponent<HandleMovement>();

                Debug.Log("is this coming to on Exit");
                handleMovement.AddVelocity(direction, life, force, onEndClampVelocity, useForceCurve, forceCurve);

            }
        }
    }

}