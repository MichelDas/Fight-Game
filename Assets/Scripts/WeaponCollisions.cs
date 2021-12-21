using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollisions : MonoBehaviour
{
    PlayerController _PC;

    private void Start()
    {
        _PC = GetComponentInParent<PlayerController>();
    } 

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Weapon")
        {
            _PC.BlockedAttack = true;
        }
        else if (other.GetComponentInParent<PlayerController>())
        {
            PlayerController pC = other.GetComponentInParent<PlayerController>();

            if(pC != _PC)
            {
                // do some damage
            }
        }
        else
        {
            _PC.BlockedAttack = true;
        }
    }
}
