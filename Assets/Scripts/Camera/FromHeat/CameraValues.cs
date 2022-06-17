using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Scriptables/Camera Values")]
public class CameraValues : ScriptableObject
{
    [SerializeField] private float turnSmooth = 0.1f;
    [SerializeField] private float moveSpeed = 9;
    [SerializeField] private float aimSpeed = 15;
    [SerializeField] private float y_rotate_speed = 8;
    [SerializeField] private float x_rotate_speed = 8;
    [SerializeField] private float minAngle = -35;
    [SerializeField] private float maxAngle = 35;
    
    [SerializeField] private float normalX = 0.23f;
    [SerializeField] private float normalY;
    [SerializeField] private float normalZ = -3f;

    [SerializeField] private float aimZ = -0.5f;
    [SerializeField] private float aimX = 0;
    
    [SerializeField] private float crouchY;
    [SerializeField] private float adaptSpeed = 9;

    public float TurnSmooth { get => turnSmooth; }
    public float MoveSpeed { get => moveSpeed;}
    public float Y_rotate_speed { get => y_rotate_speed; }
    public float X_rotate_speed { get => x_rotate_speed; }
    public float MinAngle { get => minAngle; }
    public float NormalZ { get => normalZ; }
    public float NormalX { get => normalX; }
    public float AimZ { get => aimZ; }
    public float AimX { get => aimX; }
    public float NormalY { get => normalY; }
    public float CrouchY { get => crouchY; }
    public float AdaptSpeed { get => adaptSpeed; }
    public float AimSpeed { get => aimSpeed; }
    public float MaxAngle { get => maxAngle; }
}
