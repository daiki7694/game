using UnityEngine;

public class PlayerInputProvider : MonoBehaviour, ICarInput
{
    public string steerAxis = "Horizontal";
    public string throttleAxis = "Vertical";
    public KeyCode brakeKey = KeyCode.Space;

    public float Steering => Input.GetAxis(steerAxis);
    public float Throttle => Input.GetAxis(throttleAxis);
    public bool Brake => Input.GetKey(brakeKey);
}
