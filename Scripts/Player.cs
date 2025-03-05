using UnityEngine;
using System.Collections;
using UnityEditor;

[RequireComponent (typeof (PlayerController))]
[RequireComponent (typeof (GunController))]
public class Player : LivingEntity
{

    public Crosshairs crosshairs;
    public float moveSpeed = 5;

    Camera _viewCamera;
    PlayerController _controller;
    GunController _gunController;
	
    protected override void Start ()
    {
        base.Start ();
    }

    void Awake()
    {
        _controller = GetComponent<PlayerController> ();
        _gunController = GetComponent<GunController> ();
        _viewCamera = Camera.main;
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }
    
    void OnNewWave(int waveNumber)
    {
        Health = startingHealth;
        _gunController.EquipGun(waveNumber - 1);
    }
    
    void Update () 
    {
        // Movement input
        Vector3 moveInput = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0, Input.GetAxisRaw ("Vertical"));
        Vector3 moveVelocity = moveInput.normalized * moveSpeed;
        _controller.Move (moveVelocity);

        // Look input
        Ray ray = _viewCamera.ScreenPointToRay (Input.mousePosition);
        Plane groundPlane = new Plane (Vector3.up, Vector3.up * _gunController.GunHeight);
        float rayDistance;

        if (groundPlane.Raycast(ray,out rayDistance)) 
        {
            Vector3 point = ray.GetPoint(rayDistance);
            //Debug.DrawLine(ray.origin,point,Color.red);
            _controller.LookAt(point);
            crosshairs.transform.position = point;
            crosshairs.DetectTargets(ray);

            if ((new Vector2(point.x, point.z) - new Vector2(transform.position.x, transform.position.z)).sqrMagnitude > 1)
            {
                _gunController.Aim(point);
            }
            
            
        }

        // Weapon input
        if (Input.GetMouseButton(0))
        {
            _gunController.OnTriggerHold();
        }
        if (Input.GetMouseButtonUp(0))
        {
            _gunController.OnTriggerRelease();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _gunController.Reload();
        }

        if (transform.position.y < -10)
        {
            TakeDamage(Health);
        }
    }

    public override void Die()
    {
        AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
    }
}