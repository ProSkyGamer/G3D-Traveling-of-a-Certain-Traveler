using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStaminaController))]
[RequireComponent(typeof(PlayerRunningController))]
[RequireComponent(typeof(PlayerClimbingController))]
[RequireComponent(typeof(AttackController))]
public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    private MouseRotation _mouseRot;
    private PlayerRunningController _playerRunningController;
    private PlayerClimbingController _playerClimbingController;

    private string rotateCameraXInput = "Mouse X";
    private string rotateCameraYInput = "Mouse Y";

    public void LockPersonRotation()
    {
        _mouseRot.LockPersonRot = true;
    }

    public void UnlockPersonRotation()
    {
        _mouseRot.LockPersonRot = false;
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        _playerRunningController = this.gameObject.GetComponent<PlayerRunningController>();
        _mouseRot = _camera.gameObject.GetComponent<MouseRotation>();
        _playerClimbingController = this.gameObject.GetComponent<PlayerClimbingController>();

        _mouseRot.SetTarget(this.transform);
    }

    private void Update()
    {
        if (true)//Условия
        {
            if (Input.GetKeyDown(KeyCode.Space) && _playerClimbingController.IsGrounded)
                _playerClimbingController.Jump();
            WalkInput();
            CameraInput();
        }
    }

    protected virtual void CameraInput()
    {
        if (!_camera)
        {
            if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
            else
                _camera = Camera.main;
        }

        if (_camera == null)
            return;

        var Y = Input.GetAxis(rotateCameraYInput);
        var X = Input.GetAxis(rotateCameraXInput);

        var scroll =  -Input.GetAxis("Mouse ScrollWheel");

        _mouseRot.RotateCamera(X, Y);

        if (scroll != 0)
            _mouseRot.ZoomCamera(scroll);
    }

    protected virtual void WalkInput()
    {
        float xMove = Input.GetAxis("Horizontal");
        float zMove = Input.GetAxis("Vertical");

        if(_playerClimbingController.IsClimbing)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                _playerClimbingController.StopClimbing();
                return;
            }
            if(Input.GetKeyDown(KeyCode.Space))
            {
                zMove = zMove == 0 && xMove == 0 ? 1 : zMove;
                _playerClimbingController.JumpClimbing(new Vector3(xMove, zMove, 0));
                return;
            }
            if(xMove!=0 || zMove!=0)
            {
                _playerClimbingController.ClimbWall(new Vector3(xMove, zMove, 0));
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) && _playerRunningController.IsEnoughToStartSprint() && _playerClimbingController.IsGrounded)
            {
                xMove = xMove == 0 ? 0 : xMove / 7.5f;
                zMove = 2;
                _playerRunningController.StartSprint(new Vector3(xMove, 0f, zMove));
                return;
            }
            if (xMove != 0 || zMove != 0)
            {
                if (_playerRunningController.IsSprintStarting()) return;

                if (Input.GetKey(KeyCode.LeftControl) && _playerRunningController.CanSprint && _playerClimbingController.IsGrounded)
                    _playerRunningController.Sprint(new Vector3(xMove / 7.5f, 0f, zMove / 7.5f));
                else
                    _playerRunningController.Run(new Vector3(xMove / 10f, 0f, zMove / 10f));
            }
        }
    }
    
    public void StopRunning()
    {
        _playerRunningController.StopSprint();
    }

}
