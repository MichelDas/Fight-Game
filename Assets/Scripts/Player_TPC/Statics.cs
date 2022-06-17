using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPC
{
    public class Statics
    {
        #region hash
        public static string horizontal = "horizontal";
        public static string vertical = "vertical";
        public static string special = "special";
        public static string specialType = "specialType";
        public static string onLocomotion = "onLocomotion";
        public static string Horizontal = "Horizontal";
        public static string Vertical = "Vertical";
        public static string jumpType = "jumpType";
        public static string Jump = "Jump";
        public static string onAir = "onAir";
        public static string mirrorJump = "mirrorJump";
        public static string Fire3 = "Fire3";
        public static string incline = "incline";
        public static string inSpecial = "inSpecial";
        public static string walkVault = "vault_over_walk_1";
        public static string runVault = "vault_over_run";
        #endregion

        #region Vault Variables
        public static float vaultCheckDistance = 2;
        public static float vaultSpeedWalking = 2;
        public static float vaultSpeedRunning = 4;
        public static float vaultSpeedIdle = 1;
        #endregion

        #region Functions

        public static int GetAnimSpecialType(AnimSpecials i)
        {
            int retVal = 0;
            switch (i)
            {
                case AnimSpecials.runToStop:
                    retVal = 11;
                    break;
                case AnimSpecials.run:
                    retVal = 10;
                    break;
                case AnimSpecials.jump_idle:
                    retVal = 21;
                    break;
                case AnimSpecials.run_jump:
                    retVal = 22;
                    break;
                default:
                    break;
            }

            return retVal;
        }

        #endregion

        
    }

    public enum AnimSpecials
    {
        run, runToStop, jump_idle, run_jump
    }
}