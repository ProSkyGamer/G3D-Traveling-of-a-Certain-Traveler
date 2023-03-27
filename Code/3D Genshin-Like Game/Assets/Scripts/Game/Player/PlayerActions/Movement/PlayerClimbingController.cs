using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingController : MonoBehaviour, ICanJump, ICanClimb
{
    #region Gravity
    [Header("Gravity")]
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _jumpHeight = 3.0f;

    private float _currVelocity;
    private bool _isGrounded;
    public bool IsGrounded { get => _isGrounded; private set => _isGrounded = value; }
    #endregion

    #region Climbing
    [Header("Climbing")]

    //General Climbing
    [SerializeField] private bool _isClimbing;
    [SerializeField] private LayerMask whatIsWall;
    private Vector3 _rayTransform;

    private float _detectionLength;

    //Climbing Settings
    [SerializeField] private int zMinMaxDecreeForClimbing = 45;
    [SerializeField] private float climbingSpeed = 1.5f;
    [SerializeField] private float sCostPerClimbMovement = 3 / 45f;

    //Climb Jump Settings
    [SerializeField] private float sCostPerClimbJump = 10f;
    [SerializeField] private float climbJumpForce = 10f;
    private bool _canJumpClimbing = true;
    private float _delayBetwenClimbJumps = 0.5f;

    public bool IsClimbing { get => _isClimbing; private set => _isClimbing = value; }

    #endregion

    #region General
    private Transform _playerTransfrom;
    private PlayerStaminaController _playerStaminaController;
    private PlayerInputController _playerInputController;
    private Rigidbody _rb;

    private float _characterHeight;
    private float _characterRadius;
    #endregion

    #region Debug
    private bool _drawGizmos;
    #endregion


    public void StartClimbing(RaycastHit wallHit)
    {
        _isClimbing = true;
        _playerTransfrom.forward = -wallHit.normal;
        _playerInputController.StopRunning();

        _playerStaminaController.SpendStamina(0);
        _playerInputController.LockPersonRotation();
        Move(new Vector3(0, 0, _detectionLength));
    }

    public void StopClimbing()
    {
        _isClimbing = false;
        _playerInputController.UnlockPersonRotation();
        _currVelocity = -2;
    }

    private void StateMachine()
    {
        RaycastHit frontWallHit;
        bool wallFrom = Physics.Raycast(_playerTransfrom.position +
            _playerTransfrom.TransformDirection(0, 0, _characterRadius),
            _playerTransfrom.forward, out frontWallHit, _detectionLength, whatIsWall);

        if (wallFrom && Input.GetKey(KeyCode.W) && _playerStaminaController.CurrStamina > 0)
        {
            if (!_isClimbing)
                StartClimbing(frontWallHit);
        }
        else if (IsClimbing && _playerStaminaController.CurrStamina <= 0)
            StopClimbing();
    }

    /*private void Update()
    {
        StateMachine();
    }*/

    private void TryStopClimbing()
    {
        RaycastHit hit;
        var isWall = Physics.Raycast(_playerTransfrom.position, _playerTransfrom.forward, out hit);
        if ((hit.normal.z >= zMinMaxDecreeForClimbing || hit.distance >= _characterRadius * 2) || !isWall)
        {
            Move(new Vector3(_characterRadius * 2, _characterHeight / 2, 0));
            StopClimbing();
        }
    }

    private void TryMoveToPosition(Vector3 addToPosition)
    {
        bool isClimbJump = addToPosition.x > 1 || addToPosition.y > 1;
        bool needChangeNormal = false;
        Vector3 newNormal = _playerTransfrom.forward;
        Vector3 addtionalMove = Vector3.zero;

        int directionX = addToPosition.x > 0 ? 1 : -1;
        int directionY = addToPosition.y > 0 ? 1 : -1;

        _rayTransform = _playerTransfrom.position + _playerTransfrom.TransformDirection(
            new Vector3(0, 0, _characterRadius * 99 / 100));

        if (addToPosition.x != 0 && addToPosition.y != 0)
        {
            SquareToCircle(addToPosition, isClimbJump);

            addToPosition = CheckXYDirectionAvailibility(addToPosition.y * Time.fixedDeltaTime * climbingSpeed,
                addToPosition.x * Time.fixedDeltaTime * climbingSpeed, out needChangeNormal, out newNormal, out addtionalMove);
        }
        else if (addToPosition.y != 0 && addToPosition.x == 0)
        {
            addToPosition = CheckYDirectionAvalibility(addToPosition.y * Time.fixedDeltaTime * climbingSpeed, out needChangeNormal, out newNormal, out addtionalMove);
        }
        else
        {
            addToPosition = CheckXDirectionAvalibility(addToPosition.x * Time.fixedDeltaTime * climbingSpeed,
                out needChangeNormal, out newNormal, out addtionalMove);
        }

        Debug.Log($"Add {addToPosition} Need {needChangeNormal} Normal {newNormal} Additional {addtionalMove}");


        if (!needChangeNormal && addToPosition == Vector3.zero)
            return;

        needChangeNormal = needChangeNormal && newNormal != Vector3.zero;

        if (addToPosition != Vector3.zero)
            Move(addToPosition);

        if (needChangeNormal)
            _playerTransfrom.forward = newNormal;

        if (addtionalMove != Vector3.zero)
            Move(addtionalMove);

        _playerStaminaController.SpendStamina(isClimbJump ? sCostPerClimbJump : sCostPerClimbMovement);
    }

    private Vector3 SquareToCircle(Vector3 toMove, bool isClimbJump)
    {
        return toMove.sqrMagnitude >= 1f && !isClimbJump ? toMove.normalized :
            isClimbJump && toMove.sqrMagnitude >= 1f * climbJumpForce ? toMove.normalized * climbJumpForce : toMove;
    }

    private Vector3 CheckYDirectionAvalibility(float toMoveY, out bool changeNormal, out Vector3 newNormal, out Vector3 additionalMove)
    {
        changeNormal = false;
        newNormal = _playerTransfrom.forward;
        additionalMove = Vector3.zero;

        int direction = toMoveY > 0 ? 1 : -1;

        RaycastHit directionHitBulging; //Выпуклая стена
        bool isDitrectionHitBulgin = Physics.Raycast(_rayTransform, _playerTransfrom.TransformDirection(
            Vector3.up * direction), out directionHitBulging, toMoveY + _characterHeight / 2, whatIsWall);


        if (!isDitrectionHitBulgin) //Если нет выпуклой стены
        {
            RaycastHit concaveWallHit; //Проверяем наличие вогнутой стены
            bool isConvanceWallHit = Physics.Raycast(_rayTransform +
              _playerTransfrom.TransformDirection(new Vector3(0, toMoveY, 0)),
              _playerTransfrom.forward, out concaveWallHit, toMoveY + _detectionLength, whatIsWall);

            if (isConvanceWallHit && !(concaveWallHit.normal.z >= zMinMaxDecreeForClimbing))
            //Если есть вогнутая стена
            //и на ней нельзя стоять
            {
                if (concaveWallHit.normal == -_playerTransfrom.forward)
                //Если нормаль вогнутой стены совпадает
                //с минус нормалью персонажа
                {
                    Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(
                        0, toMoveY, 0), false, _rayTransform + _playerTransfrom.
                        TransformDirection(new Vector3(0, toMoveY / 2, 0)),
                        out changeNormal, out newNormal);

                    return toMove;
                }
                else
                //Если нормаль вогнутой стены не совпдает с минус нормалью персонажа
                {
                    if (concaveWallHit.normal.z >= -zMinMaxDecreeForClimbing && concaveWallHit.normal.z < zMinMaxDecreeForClimbing)
                    //Если по стене можно карабкаться
                    {
                        Vector3 toMove = FindWall(new Vector3(
                        0, toMoveY, 0),
                        concaveWallHit.normal, out changeNormal, out newNormal);

                        if (changeNormal)
                            additionalMove = new Vector3(0, _characterHeight / 4 * direction, 0);

                        if (Mathf.Tan(concaveWallHit.normal.z) * (toMoveY - toMove.magnitude) + _detectionLength <= concaveWallHit.distance)
                        //Если между текущей и след стеной ЕСТЬ пробелы
                        {
                            changeNormal = false;
                            newNormal = _playerTransfrom.forward;
                            additionalMove = Vector3.zero;
                        }

                        return toMove;
                    }
                    else //Если нельзя карабкаться
                    {
                        Vector3 toMove = FindWall(new Vector3(
                        0, toMoveY, 0),
                        -_playerTransfrom.forward, out changeNormal, out newNormal);

                        if (concaveWallHit.normal.z >= zMinMaxDecreeForClimbing)//Если можно стоять
                        {
                            //Метод выхода из карабканья
                            TryStopClimbing();
                        }

                        return toMove;
                    }
                }
            }
            else //Если НЕТ стены или на ней МОЖНО стоять
            {
                for (int i = 1; i <= 15; i++)
                {
                    float distance = 0;
                    bool isWallFind = false;
                    RaycastHit checkHit;
                    bool isCheckHit;

                    if (i <= 5)
                    {
                        isCheckHit = Physics.Raycast(_rayTransform + (new Vector3(
                            0, toMoveY * i / 5, 0)) * direction, _playerTransfrom.forward,
                            out checkHit, _characterRadius * 2 + _detectionLength, whatIsWall);

                        if (isCheckHit)
                        {
                            isWallFind = true;
                        }
                        else
                        {
                            distance += toMoveY / 5;
                        }
                    }
                    else
                    {
                        isCheckHit = Physics.Raycast(_rayTransform + new Vector3(
                            0, toMoveY + (_characterHeight * i / 10), 0) * direction,
                            _playerTransfrom.forward, out checkHit, _characterRadius *
                            2 + _detectionLength, whatIsWall);

                        if (isCheckHit)
                        {
                            isWallFind = true;
                        }
                        else
                        {
                            distance += _characterHeight / 10;
                        }
                    }
                    if (distance >= _characterHeight)
                    {
                        //Метод выхода из карабканья
                        TryStopClimbing();
                    }
                    else if (isWallFind)
                    {
                        if (i > 5)
                            return Vector3.zero;
                        else
                        {
                            isWallFind = false;
                            distance = 0;
                        }
                    }
                }
                return Vector3.zero;
            }
        }
        else
        //Если есть выпуклая стена
        {
            if (directionHitBulging.normal.z >= -zMinMaxDecreeForClimbing &&
                directionHitBulging.normal.z < zMinMaxDecreeForClimbing)
            //Если можно карабкаться по стене
            {
                Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3
                    (0, directionHitBulging.distance * direction, 0), true,
                    _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                        0, directionHitBulging.distance / 2 * direction, 0)),
                    out changeNormal, out newNormal);

                if (toMove.magnitude == directionHitBulging.distance)
                {
                    additionalMove = direction > 0 ? new Vector3(0, _characterHeight / 3 * direction, _characterHeight / 2) :
                        new Vector3(0, -_characterHeight / 2, _characterRadius);
                }

                return toMove;
            }
            else //Если нельзя карабкаться (из-за угла)
            {
                Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(0, directionHitBulging.distance * direction, 0),
                    false, _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                        0, directionHitBulging.distance / 2 * direction, 0)), out changeNormal, out newNormal);

                return toMove;
            }
        }
    }

    private Vector3 CheckXDirectionAvalibility(float toMoveX, out bool changeNormal, out Vector3 newNormal, out Vector3 additionalMove)
    {
        changeNormal = false;
        newNormal = _playerTransfrom.forward;
        additionalMove = Vector3.zero;

        int direction = toMoveX > 0 ? 1 : -1;

        RaycastHit directionHitBulging; //Выпуклая стена
        bool isDirectionHitBulgin = Physics.Raycast(_rayTransform,
            _playerTransfrom.TransformDirection(Vector3.right * direction),
            out directionHitBulging, toMoveX * direction + _characterRadius, whatIsWall);

        if (!isDirectionHitBulgin) //Если нет выпуклой стены
        {
            RaycastHit concaveWallHit; //Вогнутая стена
            bool isConvaceWallHit = Physics.Raycast(_rayTransform + _playerTransfrom.
             TransformDirection(new Vector3(toMoveX, 0, 0)), _playerTransfrom.forward,
             out concaveWallHit, toMoveX + _detectionLength, whatIsWall);

            if (isConvaceWallHit && concaveWallHit.normal == -_playerTransfrom.forward &&
                concaveWallHit.distance <= toMoveX / 2 + _detectionLength)
            //Если есть вогнутая стена и нормаль совпадает
            {
                Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(toMoveX, 0, 0),
                    false, _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                        toMoveX / 2, 0, 0)), out changeNormal, out newNormal);

                return toMove;
            }
            else if (isConvaceWallHit && concaveWallHit.normal != -_playerTransfrom.forward)
            //Если есть вогнутая стена но нормаль не совпадает (изгиб)
            {
                Vector3 toMove = FindWall(new Vector3(toMoveX, 0, 0), concaveWallHit.normal, out changeNormal, out newNormal);

                additionalMove = new Vector3(_characterRadius / 2 * direction, 0, _characterRadius / 2);

                return toMove;
            }
            else
            {
                Vector3 toMove = FindWall(new Vector3(toMoveX, 0, 0), -_playerTransfrom.forward, out changeNormal, out newNormal);

                return toMove;
            }
        }
        else //Если есть выпуклая стена
        {
            Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3((directionHitBulging.distance > _characterRadius ? directionHitBulging.distance
                - _characterRadius : directionHitBulging.distance) * direction, 0, 0), true, _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                    (_characterRadius + (directionHitBulging.distance - _characterRadius) / 2) * direction, 0, 0)), out changeNormal, out newNormal);

            additionalMove = new Vector3(0, 0, _characterRadius);

            return toMove;
        }
    }

    private Vector3 CheckXYDirectionAvailibility(float toMoveY, float toMoveX, out bool changeNormal, out Vector3 newNormal, out Vector3 additionalMove)
    {
        int directionX;
        int directionY;
        changeNormal = false;
        newNormal = _playerTransfrom.forward;
        additionalMove = Vector3.zero;

        if (toMoveY > 0)
            directionY = 1;
        else
            directionY = -1;

        if (toMoveX > 0)
            directionX = 1;
        else
            directionX = -1;

        float angleXY = Mathf.Acos(toMoveX /
            Mathf.Sqrt(toMoveX * toMoveX + toMoveY * toMoveY));

        float lenToMoveXY = Mathf.Sqrt(
                toMoveX * toMoveX + toMoveY * toMoveY);

        float radHeight = Mathf.Sqrt(_characterHeight * _characterHeight / 4
                + _characterRadius * _characterRadius);

        RaycastHit directionHitBulging;
        bool isDirectionHitBulgin = Physics.Raycast(_rayTransform,
            _playerTransfrom.forward + new Vector3(angleXY * directionY,
            90 * directionX, 0), out directionHitBulging, lenToMoveXY + radHeight, whatIsWall);

        if (!isDirectionHitBulgin)
        {
            RaycastHit concaveWallHit; //Проверяем наличие вогнутой стены
            bool isConvanceWallHit = Physics.Raycast(_rayTransform +
                _playerTransfrom.TransformDirection(new Vector3(
                    toMoveX, toMoveY, 0)),
                    _playerTransfrom.forward, out concaveWallHit, lenToMoveXY + _detectionLength, whatIsWall);

            if (isConvanceWallHit && concaveWallHit.distance <= lenToMoveXY + _detectionLength
                && concaveWallHit.normal == -_playerTransfrom.forward)
            //Если есть вогнутая стена и нормаль совпадает
            {
                Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    toMoveX,
                    toMoveY,
                    0), false, _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                    toMoveX / 2,
                    toMoveY / 2,
                    0)), out changeNormal, out newNormal);
                return toMove;
            }

            else if (isConvanceWallHit && concaveWallHit.normal != _playerTransfrom.forward)
            //Если есть вогнутая стена но нормаль не совпадает
            {
                Vector3 toMove = FindWall(new Vector3(toMoveX, toMoveY, 0), concaveWallHit.normal, out changeNormal, out newNormal);
                return toMove;
            }
            else
            //Если нет вогнутой стены
            {
                Vector3 toMove = FindWall(new Vector3(toMoveX, toMoveY, 0), -_playerTransfrom.forward, out changeNormal, out newNormal);
                return toMove;
            }
        }
        else
        //Если есть выпуклая стена
        {
            if (directionHitBulging.normal.z < -zMinMaxDecreeForClimbing ||
             directionHitBulging.normal.z < zMinMaxDecreeForClimbing)
            //Если по ней можно карабкаться
            {
                if (directionHitBulging.distance > radHeight)
                //Если есть куда продвинуться
                {
                    Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                        0, directionHitBulging.distance * directionX, 0), true,
                        _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                            (directionHitBulging.distance * Mathf.Cos(angleXY)) / 2 * directionX,
                            (directionHitBulging.distance * Mathf.Sin(angleXY)) / 2 * directionY,
                            0)), out changeNormal, out newNormal);
                    return toMove;
                }
                else
                //Если некуда продвинуться
                {
                    changeNormal = true;
                    newNormal = -directionHitBulging.normal;
                    return Vector3.zero;
                }
            }
            else
            //Если по стене нельзя карабкаться
            {
                if (directionHitBulging.distance > radHeight)
                //Если есть куда провинуться
                {
                    Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    0, directionHitBulging.distance * directionX, 0), false,
                    _rayTransform + _playerTransfrom.TransformDirection(new Vector3(
                        (directionHitBulging.distance * Mathf.Cos(angleXY)) / 2 * directionX,
                        (directionHitBulging.distance * Mathf.Sin(angleXY)) / 2 * directionY,
                        0)), out changeNormal, out newNormal);
                    return toMove;
                }
                else //Если некуда продвинуться
                    return Vector3.zero;
            }
        }
    }

    //Переделать момент с передачей inputNormal (пеедеавать -_playerTransform.forward)
    private Vector3 MiddleCheck(RaycastHit inputNormal, Vector3 toMove,
        bool needChangeNormal, Vector3 rayMiddle, out bool changeNormal, out Vector3 newNormal)
    {
        changeNormal = false;
        newNormal = _playerTransfrom.forward;
        RaycastHit middleHit;
        bool isMiddleHit = Physics.Raycast(rayMiddle, _playerTransfrom.forward, out middleHit, toMove.magnitude + _detectionLength, whatIsWall);

        if (isMiddleHit && middleHit.normal == -_playerTransfrom.forward)
        {
            if (needChangeNormal)
            {
                changeNormal = true;
                newNormal = -inputNormal.normal;
            }
            return toMove;
        }
        else
        //Если нет стены в середине или нормлаль не совпадает
        {
            RaycastHit wallFindHit;
            bool isWallFindHit;

            for (int i = 1; i < 5; i++)
            {
                isWallFindHit = Physics.Raycast(_rayTransform + _playerTransfrom.TransformDirection(
                    toMove * i / (5 * 2)), _playerTransfrom.forward, out wallFindHit, toMove.magnitude + _detectionLength, whatIsWall);
                if (!isWallFindHit)
                    return toMove * (i - 1) / (5 * 2);
                else if (wallFindHit.normal != -_playerTransfrom.forward)
                {
                    changeNormal = true;
                    newNormal = -wallFindHit.normal;
                    return toMove * (i - 1) / (5 * 2);
                }
            }
            if (middleHit.normal != -_playerTransfrom.forward)
            {
                changeNormal = true;
                newNormal = -middleHit.normal;
            }
            return toMove * 4 / (5 * 2);
        }
    }

    private Vector3 FindWall(Vector3 toMove, Vector3 inputNormal, out bool changeNormal, out Vector3 newNormal)
    {
        int minI;
        int maxI;
        changeNormal = false;
        newNormal = _playerTransfrom.forward;

        RaycastHit middleHit;
        bool isMiddleHit = Physics.Raycast(_rayTransform + _playerTransfrom.TransformDirection(
            toMove / 2), _playerTransfrom.forward, out middleHit, toMove.magnitude + _detectionLength, whatIsWall);

        if (isMiddleHit && middleHit.normal == -_playerTransfrom.forward)
        {
            minI = 6;
            maxI = 10;
        }
        else
        {
            minI = 1;
            maxI = 5;
        }
        for (int i = minI; i < maxI; i++)
        {
            RaycastHit findHit;
            bool isFindHit = Physics.Raycast(_rayTransform + _playerTransfrom.TransformDirection(
                 toMove * i / 5 * 2), _playerTransfrom.forward, out findHit, toMove.magnitude + _detectionLength, whatIsWall);
            if (isFindHit && findHit.normal != -_playerTransfrom.forward)
            {
                changeNormal = true;
                newNormal = -findHit.normal;
                return toMove * (i - 1) / (5 * 2);
            }
            else if (!isFindHit)
            {
                return toMove * (i - 1) / (5 * 2);
            }
        }
        changeNormal = true;
        newNormal = -inputNormal;
        return toMove * (maxI - 1) / (5 * 2);
    }

    public void ClimbWall(Vector3 addToPosition)
    {
        /*//Debug.Log(addToPosition);
        RaycastHit hit;
        if (Physics.Raycast(_playerTransfrom.position, _playerTransfrom.forward, out hit))
        {
            transform.forward = -hit.normal;
            Debug.Log(hit.normal);
        }

        addToPosition = _playerTransfrom.TransformDirection(addToPosition);
        Move(addToPosition * climbingSpeed * Time.fixedDeltaTime);
        _playerStaminaController.SpendStamina(sCostPerClimbMovement / 45f);

        TryStopClimbing();*/
        TryMoveToPosition(addToPosition);
    }

    public void JumpClimbing(Vector3 addToPosition)
    {
        if (_canJumpClimbing)
        {
            /*addToPosition = _playerTransfrom.TransformDirection(addToPosition);
            Move(addToPosition * climbingSpeed * Time.fixedDeltaTime * 10);
            _playerStaminaController.SpendStamina(sCostPerClimbJump);*/
            TryMoveToPosition(addToPosition * climbJumpForce);

            _canJumpClimbing = false;
            StartCoroutine(WaitForNextClimbJump());

            /*TryStopClimbing();*/
        }
    }

    public void Jump()
    {
        _currVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
    }



    private bool IsOnTheGround()
    {
        RaycastHit groundHit;
        bool isGroundHit = Physics.Raycast(_playerTransfrom.position, Vector3.down, out groundHit, _characterHeight, _groundLayer);
        return isGroundHit && groundHit.distance <= _characterHeight / 2;

    }



    private IEnumerator WaitForNextClimbJump()
    {
        yield return new WaitForSeconds(_delayBetwenClimbJumps);
        _canJumpClimbing = true;
    }




    private void DoGravity()
    {
        _currVelocity += _gravity * Time.fixedDeltaTime;

        Move(Vector3.up * _currVelocity * Time.fixedDeltaTime);
    }

    private void Move(Vector3 toMove)
    {
        //_playerTransfrom.position = Vector3.MoveTowards(_playerTransfrom.position, _playerTransfrom.position + _playerTransfrom.TransformDirection(toMove), 1f);
        _rb.MovePosition(_playerTransfrom.position + _playerTransfrom.TransformDirection(toMove));
    }

    private void FixedUpdate()
    {
        if (!_isClimbing)
        {
            _isGrounded = IsOnTheGround();
            if (!_isGrounded || _currVelocity > 0)
                DoGravity();
        }
        _rb.velocity = Vector3.zero;
        StateMachine();
    }

    private void Awake()
    {
        Initialise();
    }

    private void Initialise()
    {
        _playerTransfrom = this.gameObject.GetComponent<Transform>();
        _playerStaminaController = this.gameObject.GetComponent<PlayerStaminaController>();
        _playerInputController = this.gameObject.GetComponent<PlayerInputController>();
        _rb = this.gameObject.GetComponent<Rigidbody>();

        var collider = this.gameObject.GetComponent<CapsuleCollider>();
        _characterHeight = collider.height;
        _characterRadius = collider.radius;

        _detectionLength = 0.1f + _characterRadius * 1 / 100;

        _drawGizmos = true;
    }

    private void OnDrawGizmos()
    {
        if (_drawGizmos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(_rayTransform, _playerTransfrom.TransformDirection(Vector3.right) * 1f);
            Gizmos.DrawRay(_rayTransform, _playerTransfrom.forward * 1f);
        }
    }
}
