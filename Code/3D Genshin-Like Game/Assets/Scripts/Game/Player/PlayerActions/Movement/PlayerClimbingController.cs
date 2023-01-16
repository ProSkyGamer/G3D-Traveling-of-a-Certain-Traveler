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

    private float _gVelocity;
    private bool _isGrounded;
    public bool IsGrounded { get => _isGrounded; private set => _isGrounded = value; }
    #endregion

    #region Climbing
    [Header("Climbing")]
    [SerializeField] private float climbingSpeed = 2;
    [SerializeField] private int sCostPerClimbMovement = 3;
    [SerializeField] private int sCostPerClimbJump = 10;
    [SerializeField] private float climbJumpForce = 10;

    [SerializeField] private int zMinMaxDecreeForClimbing = 45;
    private bool _canJumpClimbing = true;
    private float _delayBetwenClimbJumps = 0.5f;

    [SerializeField] private bool _isClimbing;
    public bool IsClimbing { get => _isClimbing; private set => _isClimbing = value; }

    #endregion

    #region From Video
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask whatIsWall;


    private float _detectionLength;


    private RaycastHit frontWallHit;
    private bool wallFrom;
    private Vector3 _rayTransfrom;

    private Transform lastWall;
    private Vector3 lastWallNormal;


    #endregion

    #region General
    private Transform _playerTransfrom;
    private CharacterController _characterController;
    private PlayerStaminaController _playerStaminaController;
    private PlayerInputController _playerInputController;
    #endregion

    private void WallCheck()
    {
        wallFrom = Physics.Raycast(_playerTransfrom.position + _playerTransfrom.TransformDirection(0, 0, _characterController.radius), _playerTransfrom.forward, out frontWallHit, _detectionLength, whatIsWall);
    }

    public void StartClimbing()
    {
        _isClimbing = true;
        lastWall = frontWallHit.transform;
        _playerTransfrom.forward = -frontWallHit.normal;
        lastWallNormal = frontWallHit.normal; //? для вращения стены?
        _playerInputController.StopRunning();

        _playerStaminaController.SpendStamina(0);
        _playerInputController.LockPersonRotation();
    }

    public void StopClimbing()
    {
        _isClimbing = false;
        _playerInputController.UnlockPersonRotation();
        _gVelocity = -2;
        _characterController.Move(Vector3.zero);
    }

    private void StateMachine()
    {
        if (wallFrom && Input.GetKey(KeyCode.W))
        {
            if (!_isClimbing) StartClimbing();

            if (_playerStaminaController.CurrStamina > 0)
                _playerStaminaController.SpendStamina(sCostPerClimbMovement / 45);
        }
    }

    private void Update()
    {
        WallCheck();
        StateMachine();
    }

    private void TryStopClimbing()
    {
        RaycastHit hit;
        var isWall = Physics.Raycast(_playerTransfrom.position, _playerTransfrom.forward, out hit);
        if ((hit.normal.z >= zMinMaxDecreeForClimbing || hit.distance >= _characterController.radius * 2) || !isWall)
        {
            _characterController.Move(new Vector3(_characterController.radius * 2, _characterController.height / 2, 0));
            StopClimbing();
        }

        if (_isClimbing && _playerStaminaController.CurrStamina <= 0)
            StopClimbing();
    }

    private void TryMoveToPosition(Vector3 addToPosition)
    {
        bool isClimbJump = addToPosition.x > 1 || addToPosition.y > 1;
        bool needChangeNormal = false;
        Vector3 newNormal = _playerTransfrom.forward;

        if (addToPosition.x != 0 && addToPosition.y != 0)
        {
            _rayTransfrom = _playerTransfrom.position + _playerTransfrom.TransformDirection(
            new Vector3(_characterController.radius + 0.06f, 0, _characterController.radius + 0.06f));
            SquareToCircle(addToPosition, isClimbJump);
            addToPosition = CheckXYDirectionAvailibility(addToPosition.y * Time.fixedDeltaTime * climbingSpeed,
                addToPosition.x * Time.fixedDeltaTime * climbingSpeed, out needChangeNormal, out newNormal);
        }
        else if (addToPosition.y != 0 && addToPosition.x == 0)
        {
            _rayTransfrom = _playerTransfrom.position + _playerTransfrom.TransformDirection(
            new Vector3(_characterController.radius + 0.06f, 0, _characterController.radius + 0.06f));
            addToPosition = CheckYDirectionAvalibility(addToPosition.y * Time.fixedDeltaTime * climbingSpeed,
                out needChangeNormal, out newNormal);
        }
        else
        {
            _rayTransfrom = _playerTransfrom.position + _playerTransfrom.TransformDirection(
            new Vector3(_characterController.radius + 0.06f, 0, 0));
            addToPosition = CheckXDirectionAvalibility(addToPosition.x * Time.fixedDeltaTime * climbingSpeed,
                out needChangeNormal, out newNormal);
        }

        Debug.Log($"Add {addToPosition} Need {needChangeNormal} Normal {newNormal}");


        if (!needChangeNormal && addToPosition == Vector3.zero)
            return;
        if (newNormal == Vector3.zero)
            needChangeNormal = false;

        //Возвращать значение из методов CheckDirectionAvailibility с TransfronDirection
        if (addToPosition != Vector3.zero)
        {
            _characterController.Move(_playerTransfrom.TransformDirection(addToPosition));
        }
        if (needChangeNormal)
        {
            _playerTransfrom.forward = newNormal;
        }

        _playerStaminaController.SpendStamina(isClimbJump ? sCostPerClimbJump : sCostPerClimbMovement / 45f);
    }

    private Vector3 SquareToCircle(Vector3 toMove, bool isClimbJump)
    {
        return toMove.sqrMagnitude >= 1f && !isClimbJump ? toMove.normalized :
            isClimbJump && toMove.sqrMagnitude >= 1f * climbJumpForce ? toMove.normalized * climbJumpForce : toMove;
    }

    private Vector3 CheckYDirectionAvalibility(float toMoveY, out bool changeNormal, out Vector3 newNormal)
    {
        int direction;
        changeNormal = false;
        newNormal = _playerTransfrom.forward;

        if (toMoveY > 0)
            direction = 1;
        else
            direction = -1;

        RaycastHit directionHitBulging; //Выпуклая стена
        bool isDitrectionHitBulgin = Physics.Raycast(_rayTransfrom, _playerTransfrom.TransformDirection(
            Vector3.up * direction), out directionHitBulging, toMoveY + _detectionLength);
        if (!isDitrectionHitBulgin) //Если нет выпуклой стены
        {
            RaycastHit concaveWallHit; //Проверяем наличие вогнутой стены
            bool isConvanceWallHit = Physics.Raycast(_rayTransfrom +
                _playerTransfrom.TransformDirection(new Vector3(0, toMoveY, 0)),
                _playerTransfrom.forward, out concaveWallHit, toMoveY + _detectionLength);
            if (isConvanceWallHit && !(concaveWallHit.normal.z >= zMinMaxDecreeForClimbing) &&
                concaveWallHit.distance <= toMoveY / 2 + _detectionLength)//Если есть вогнутая стена
                                                       //и на ней нельзя стоять
            {
                if (concaveWallHit.normal == -_playerTransfrom.forward)//Если нормаль вогнутой стены совпадает
                                                                       //с минус нормалью персонажа
                {
                    Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(
                    0, toMoveY, 0),
                    false, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        0, toMoveY / 2, 0)), out changeNormal, out newNormal);
                    return toMove;
                }
                else //Если нормаль вогнутой стены не совпдает с минус нормалью персонажа
                {
                    if (concaveWallHit.normal.z >= -zMinMaxDecreeForClimbing && concaveWallHit.normal.z < zMinMaxDecreeForClimbing)//Если по стене можно карабкаться
                    {
                        Vector3 toMove = FindWall(new Vector3(
                        0, toMoveY, 0),
                        concaveWallHit.normal, out changeNormal, out newNormal);
                        if (Mathf.Tan(concaveWallHit.normal.z) * (toMoveY - toMove.sqrMagnitude) + _detectionLength <= concaveWallHit.distance)
                        //Если между текущей и след стеной ЕСТЬ пробелы
                        {
                            changeNormal = false;
                            newNormal = _playerTransfrom.forward;
                        }
                        return toMove;
                        /*Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(
                        0, toMoveY, 0),
                        true, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        0, toMoveY / 2, 0)), out changeNormal, out newNormal);
                        return toMove;*/
                    }
                    else //Если нельзя карабкаться
                    {
                        /*Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(
                        0, toMoveY, 0),
                        false, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        0, toMoveY / 2, 0)), out changeNormal, out newNormal);*/
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
                        isCheckHit = Physics.Raycast(_rayTransfrom + (new Vector3(
                            0, toMoveY * i / 5, 0)) * direction, _playerTransfrom.forward,
                            out checkHit, _characterController.radius * 2 + _detectionLength);
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
                        isCheckHit = Physics.Raycast(_rayTransfrom + new Vector3(
                            0, toMoveY + (_characterController.height * i / 10), 0) * direction,
                            _playerTransfrom.forward, out checkHit, _characterController.radius *
                            2 + _detectionLength);
                        if (isCheckHit)
                        {
                            isWallFind = true;
                        }
                        else
                        {
                            distance += _characterController.height / 10;
                        }
                    }
                    if (distance >= _characterController.height)
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
        else//Если есть выпуклая стена
        {
            if (directionHitBulging.distance > _characterController.height / 2) //Если есть куда продвинуться
            {
                if (directionHitBulging.normal.z >= -zMinMaxDecreeForClimbing &&
                    directionHitBulging.normal.z < zMinMaxDecreeForClimbing) //Если можно карабкаться по стене
                {
                    Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    0, directionHitBulging.distance * direction, 0),
                    true, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        0, directionHitBulging.distance / 2 *
                        direction, 0)), out changeNormal, out newNormal);
                    return toMove;
                }
                else //Если нельзя карабкаться (из-за угла)
                {
                    Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    0, directionHitBulging.distance * direction, 0),
                    false, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        0, directionHitBulging.distance / 2 *
                        direction, 0)), out changeNormal, out newNormal);
                    return toMove;
                }
            }
            else //Если некуда продвинуться вверх
                return Vector3.zero;
        }
    }

    private Vector3 CheckXDirectionAvalibility(float toMoveX, out bool changeNormal, out Vector3 newNormal)
    {
        int direction;
        changeNormal = false;
        newNormal = _playerTransfrom.forward;
        if (toMoveX > 0)
            direction = 1;
        else
            direction = -1;

        RaycastHit directionHitBulging; //Выпуклая стена
        bool isDirectionHitBulgin = Physics.Raycast(_rayTransfrom,
            _playerTransfrom.forward + new Vector3(0, 90 * direction, 0),
            out directionHitBulging, toMoveX + _detectionLength);
        if (!isDirectionHitBulgin) //Если нет выпуклой стены
        {
            RaycastHit concaveWallHit; //Вогнутая стена
            bool isConvaceWallHit = Physics.Raycast(_rayTransfrom +
                _playerTransfrom.TransformDirection(new Vector3(
                    toMoveX, 0, 0)), _playerTransfrom.forward,
                    out concaveWallHit, toMoveX + _detectionLength);
            if (isConvaceWallHit && concaveWallHit.normal == -_playerTransfrom.forward &&
                concaveWallHit.distance <= toMoveX / 2 + _detectionLength)
            //Если есть вогнутая стена и нормаль совпадает
            {
                Vector3 toMove = MiddleCheck(concaveWallHit, new Vector3(
                    toMoveX, 0, 0),
                    false, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        toMoveX / 2, 0, 0)), out changeNormal, out newNormal);
                return toMove;
            }
            else if (isConvaceWallHit && concaveWallHit.normal != _playerTransfrom.forward)
            //Если есть вогнутая стена но нормаль не совпадает (изгиб)
            {
                Vector3 toMove = FindWall(new Vector3(toMoveX, 0, 0), concaveWallHit.normal, out changeNormal, out newNormal);
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
            Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    directionHitBulging.distance * direction, 0, 0),
                    true, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
                        directionHitBulging.distance / 2 *
                        direction, 0, 0)), out changeNormal, out newNormal);
            return toMove;
        }
    }

    private Vector3 CheckXYDirectionAvailibility(float toMoveY, float toMoveX, out bool changeNormal, out Vector3 newNormal)
    {
        int directionX;
        int directionY;
        changeNormal = false;
        newNormal = _playerTransfrom.forward;

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

        RaycastHit directionHitBulging;
        bool isDirectionHitBulgin = Physics.Raycast(_rayTransfrom,
            _playerTransfrom.forward + new Vector3(angleXY * directionY,
            90 * directionX, 0), out directionHitBulging, lenToMoveXY + _detectionLength);

        float radHeight = Mathf.Sqrt(_characterController.height * _characterController.height / 4
                + _characterController.radius * _characterController.radius);

        if (!isDirectionHitBulgin)
        {
            RaycastHit concaveWallHit; //Проверяем наличие вогнутой стены
            bool isConvanceWallHit = Physics.Raycast(_rayTransfrom +
                _playerTransfrom.TransformDirection(new Vector3(
                    toMoveX, toMoveY, 0)),
                    _playerTransfrom.forward, out concaveWallHit, lenToMoveXY + _detectionLength);

            if (isConvanceWallHit && concaveWallHit.distance <= lenToMoveXY + _detectionLength
                && concaveWallHit.normal == -_playerTransfrom.forward)
            //Если есть вогнутая стена и нормаль совпадает
            {
                Vector3 toMove = MiddleCheck(directionHitBulging, new Vector3(
                    toMoveX,
                    toMoveY,
                    0), false, _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
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
                        _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
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
                    _rayTransfrom + _playerTransfrom.TransformDirection(new Vector3(
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
        bool isMiddleHit = Physics.Raycast(rayMiddle, _playerTransfrom.forward, out middleHit, toMove.sqrMagnitude + _detectionLength);

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
                isWallFindHit = Physics.Raycast(_rayTransfrom + _playerTransfrom.TransformDirection(
                    toMove * i / (5 * 2)), _playerTransfrom.forward, out wallFindHit, toMove.sqrMagnitude + _detectionLength);
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
        bool isMiddleHit = Physics.Raycast(_rayTransfrom + _playerTransfrom.TransformDirection(
            toMove / 2), _playerTransfrom.forward, out middleHit, toMove.sqrMagnitude + _detectionLength);

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
            bool isFindHit = Physics.Raycast(_rayTransfrom + _playerTransfrom.TransformDirection(
                 toMove * i / 5 * 2), _playerTransfrom.forward, out findHit, toMove.sqrMagnitude + _detectionLength);
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
        _characterController.Move(addToPosition * climbingSpeed * Time.fixedDeltaTime);
        _playerStaminaController.SpendStamina(sCostPerClimbMovement / 45f);

        TryStopClimbing();*/
        TryMoveToPosition(addToPosition);
    }

    public void JumpClimbing(Vector3 addToPosition)
    {
        if (_canJumpClimbing)
        {
            /*addToPosition = _playerTransfrom.TransformDirection(addToPosition);
            _characterController.Move(addToPosition * climbingSpeed * Time.fixedDeltaTime * 10);
            _playerStaminaController.SpendStamina(sCostPerClimbJump);*/
            TryMoveToPosition(addToPosition * climbJumpForce);

            _canJumpClimbing = false;
            StartCoroutine(WaitForNextClimbJump());

            /*TryStopClimbing();*/
        }
    }

    public void Jump()
    {
        _gVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
    }



    private bool IsOnTheGround()
    {
        return Physics.CheckSphere(_playerTransfrom.position - new Vector3(0, _characterController.height / 2, 0), 0.3f, _groundLayer);
    }



    private IEnumerator WaitForNextClimbJump()
    {
        yield return new WaitForSeconds(_delayBetwenClimbJumps);
        _canJumpClimbing = true;
    }




    private void DoGravity()
    {
        _gVelocity += _gravity * Time.fixedDeltaTime;

        _characterController.Move(Vector3.up * _gVelocity * Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        if (!_isClimbing)
        {
            _isGrounded = IsOnTheGround();
            if (_isGrounded && _gVelocity < 0)
                _gVelocity = -2;
            DoGravity();
        }
        else if (_playerStaminaController.CurrStamina <= 0)
            _isClimbing = false;
    }

    private void Awake()
    {
        Initialise();
    }

    private void Initialise()
    {
        _characterController = this.gameObject.GetComponent<CharacterController>();
        _playerTransfrom = this.gameObject.GetComponent<Transform>();
        _playerStaminaController = this.gameObject.GetComponent<PlayerStaminaController>();
        _playerInputController = this.gameObject.GetComponent<PlayerInputController>();

        _detectionLength = 0.15f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + transform.TransformDirection(0, 0, 0.5f), transform.forward * 1f);
    }
}
