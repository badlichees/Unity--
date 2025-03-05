using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController : MonoBehaviour 
{
    Vector3 _velocity;
    Rigidbody _myRigidbody;

    void Start () 
    {
        _myRigidbody = GetComponent<Rigidbody> ();
    }

    public void Move(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void LookAt(Vector3 lookPoint) 
    {
        Vector3 heightCorrectedPoint = new Vector3 (lookPoint.x, transform.position.y, lookPoint.z);
        transform.LookAt (heightCorrectedPoint);
    }

    void FixedUpdate()
    {
        _myRigidbody.MovePosition (_myRigidbody.position + _velocity * Time.fixedDeltaTime);
    }
}