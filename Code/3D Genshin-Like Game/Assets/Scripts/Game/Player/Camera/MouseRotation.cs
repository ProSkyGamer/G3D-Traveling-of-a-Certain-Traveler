using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Invector;

public class MouseRotation : MonoBehaviour
{
    [Header("Following Object")]
    [SerializeField] private Transform playerTranform;
    [SerializeField] private LayerMask cullingLayer = 1 << 0;
    [SerializeField] private float sensitivity = 4f;
    [SerializeField] private float smoothCameraRotation = 12f; //Сглаживание для вращения камеры
    [SerializeField] private float smoothCameraFollow = 10f; //Сглаживание для движения камеры

    [Header("Camera offset from object")]
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 3.5f;

    private float _defaultDistance;

    [Header("Camera height offset from object")]
    [SerializeField] private float minHeight = 0.8f;
    [SerializeField] private float maxHeight = 2;

    private float _height = 1.4f;
    [Header("Rotation Limit")]
    [SerializeField] private float yMinLimit = -40f;
    [SerializeField] private float yMaxLimit = 80f;

    [Header("Returns to default state && locks camera rotation")]
    [SerializeField] private bool lockPersonRot;
    public bool LockPersonRot { get => lockPersonRot; set => lockPersonRot = value; }
    [SerializeField] private bool lockCameraRot;
    [SerializeField] private bool allLockCamera;

    private Camera _camera;
    private Transform _targetLookAt;

    private Vector3 _currentTargetPosition;
    private Vector3 _currentCPos;
    private Vector3 _desiredCPos;
    private Vector2 _movementSpeed;

    private float _mouseX;
    private float _mouseY;
    private float _distance = 5f; //Текущее расстояние
    private float _currentHeight; //текущая высота
    private float _cullingDistance; //Distance For Raycast
    private float _checkHeightRadius = 0.4f;
    private float _cullingHeight = 0.2f;
    private float _cullingMinDist = 0.1f;
    private float _xMinLimit = -360f;
    private float _xMaxLimit = 360f;

    

    public void SetTarget(Transform transform)
    {
        playerTranform = transform;
        Init();
    }


    private void Update()
    {
        if (playerTranform == null || _targetLookAt == null || allLockCamera) return;

        CameraMovement();

    }


    private void Awake()
    {
        if(playerTranform != null)
            Init();
    }

    private void Init()
    {
        _camera = this.gameObject.GetComponent<Camera>();
        _currentTargetPosition = new Vector3(playerTranform.position.x,
            playerTranform.position.y, playerTranform.position.z);
        _targetLookAt = new GameObject("targetLookAt").transform;
        _targetLookAt.position = playerTranform.position;
        _targetLookAt.rotation = playerTranform.rotation;
        _targetLookAt.hideFlags = HideFlags.HideInHierarchy;

        _mouseY = playerTranform.eulerAngles.x;
        _mouseX = playerTranform.eulerAngles.y;

        _defaultDistance = Mathf.Lerp(minDistance, maxDistance, 0.5f);
        _height = Mathf.Lerp(minHeight, maxHeight, 0.5f);

        _distance = _defaultDistance;
        _currentHeight = _height;
    }

    public void ZoomCamera(float sign)
    {
        _defaultDistance = sign > 0 ? Mathf.Lerp(_defaultDistance, maxDistance, 0.1f) : Mathf.Lerp(minDistance, _defaultDistance, 0.9f);
        _height = sign > 0 ? Mathf.Lerp(_height, maxHeight, 0.1f) : Mathf.Lerp(minHeight, _height, 0.9f);
    }

    public void RotateCamera(float x, float y)
    {
        // free rotation 
        _mouseX += x * sensitivity;
        _mouseY -= y * sensitivity;

        _movementSpeed.x = x;
        _movementSpeed.y = -y;
        if (!allLockCamera || !lockCameraRot)
        {
            _mouseY = vExtensions.ClampAngle(_mouseY, yMinLimit, yMaxLimit);
            _mouseX = vExtensions.ClampAngle(_mouseX, _xMinLimit, _xMaxLimit);
        }
        else
        {
            _mouseY = playerTranform.root.localEulerAngles.x;
            _mouseX = playerTranform.root.localEulerAngles.y;
        }
    }


    private void CameraMovement()
    {
        _distance = Mathf.Lerp(_distance, _defaultDistance,
            smoothCameraFollow * Time.deltaTime);
        _cullingDistance = Mathf.Lerp(_cullingDistance, _distance, Time.deltaTime);
        var camDir = -1 * _targetLookAt.forward;

        camDir = camDir.normalized;

        var targetPos = new Vector3(playerTranform.position.x,
            playerTranform.position.y, playerTranform.position.z);
        _currentTargetPosition = targetPos;
        _desiredCPos = targetPos + new Vector3(0, _height, 0);
        _currentCPos = playerTranform.position + new Vector3(0, _currentHeight, 0);
        RaycastHit hitInfo;

        ClipPlanePoints planePoints = _camera.NearClipPlanePoints
            (_currentCPos + (camDir * _distance), 0);
        ClipPlanePoints oldPoints = _camera.NearClipPlanePoints
            (_desiredCPos + (camDir * _distance), 0);

        if (Physics.SphereCast(targetPos, _checkHeightRadius, Vector3.up,
            out hitInfo, _cullingHeight + 0.2f, cullingLayer))
        {
            var t = hitInfo.distance - 0.2f;
            t -= _height;
            t /= (_cullingHeight - _height);
            _cullingHeight = Mathf.Lerp(_height, _cullingHeight, Mathf.Clamp(t, 0.0f, 1.0f));
        }
        if (CullingRayCast(_desiredCPos, oldPoints, out hitInfo,
            _distance + 0.2f, cullingLayer, Color.blue))
        {
            _distance = hitInfo.distance - 0.2f;
            if (_distance < _defaultDistance)
            {
                var t = hitInfo.distance;
                t -= _cullingMinDist;
                t /= _cullingMinDist;
                _currentHeight = Mathf.Lerp(_cullingHeight,
                    _height, Mathf.Clamp(t, 0.0f, 1.0f));
                _currentCPos = playerTranform.position +
                    new Vector3(0, _currentHeight, 0);
            }
        }
        else
        {
            _currentHeight = _height;
        }
        if (CullingRayCast(_currentCPos, planePoints, out hitInfo,
            _distance, cullingLayer, Color.cyan))
            _distance = Mathf.Clamp(_cullingDistance, 0.0f, _defaultDistance);
        var lookPoint = _currentCPos + _targetLookAt.forward * 2f;
        lookPoint += (_targetLookAt.right * Vector3.Dot(camDir * _distance, _targetLookAt.right));
        _targetLookAt.position = _currentCPos;
        
        transform.position = _currentCPos + (camDir * _distance);

        if (!lockCameraRot)
        {
            Quaternion newRot = Quaternion.Euler(_mouseY, _mouseX, 0);
            _targetLookAt.rotation = Quaternion.Slerp(
                _targetLookAt.rotation, newRot, smoothCameraRotation * Time.deltaTime);

            var rotation = Quaternion.LookRotation((lookPoint) - transform.position);

            transform.rotation = rotation;
            if(!lockPersonRot)
                playerTranform.rotation = Quaternion.Euler(0, _mouseX, 0);
            _movementSpeed = Vector2.zero;
        }
    }

    private bool CullingRayCast(Vector3 from, ClipPlanePoints _to, out RaycastHit hitInfo
        , float distance, LayerMask cullingLayer, Color color)
    {
        bool value = false;

        if (Physics.Raycast(from, _to.LowerLeft - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            _cullingDistance = hitInfo.distance;
        }
        if (Physics.Raycast(from, _to.LowerRight - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (_cullingDistance > hitInfo.distance) _cullingDistance = hitInfo.distance;
        }
        if (Physics.Raycast(from, _to.UpperLeft - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (_cullingDistance > hitInfo.distance) _cullingDistance = hitInfo.distance;
        }
        if (Physics.Raycast(from, _to.UpperRight - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (_cullingDistance > hitInfo.distance) _cullingDistance = hitInfo.distance;
        }

        return hitInfo.collider && value;
    }
}
