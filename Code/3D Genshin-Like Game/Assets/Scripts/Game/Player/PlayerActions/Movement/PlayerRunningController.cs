using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRunningController : MonoBehaviour, ICanMove, ICanSprint
{
    #region Run Sprint
    [Header("Run")]
    [SerializeField] private float speed = 2;

    [Header("Sprint")]
    [SerializeField] private float sprintSpeed = 5;

    [SerializeField] private int _sCostPerSprintStart = 10;
    [SerializeField] private int _sCostPerSprintSecond = 6;

    private Coroutine C_sprintStart;
    private Coroutine C_sprintStaminaSpending;

    private bool _isSprinting;

    private bool _canSprint;
    public bool CanSprint { get => _canSprint; private set => _canSprint = value; }
    #endregion

    #region General
    private Transform _playerTransfrom;
    private PlayerStaminaController _playerStaminaController;
    private PlayerClimbingController _playerClimbingController;
    private Rigidbody _rb;
    #endregion


    //Методы

    public void Run(Vector3 addToPosition)
    {
        if (C_sprintStart == null)
        {
            //_playerTransfrom.position = Vector3.MoveTowards(_playerTransfrom.position, _playerTransfrom.position + (addToPosition * _speed), _speed * Time.deltaTime);
            Move(addToPosition * speed * Time.fixedDeltaTime * 10);
        }
    }

    public void Sprint(Vector3 addToPosition)
    {
        if (_playerStaminaController.CurrStamina >= _sCostPerSprintSecond / 10f)
        {
            if (!IsSprintStarting())
            {
                _isSprinting = true;
                //_playerTransfrom.position = Vector3.MoveTowards(_playerTransfrom.position, _playerTransfrom.position + _playerTransfrom.TransformDirection(addToPosition), sprintSpeed * Time.deltaTime);
                Move(addToPosition * sprintSpeed * Time.fixedDeltaTime * 10);
                if (C_sprintStaminaSpending == null)
                    C_sprintStaminaSpending = StartCoroutine(StaminaSprintSpending());
            }
        }
    }

    public void StartSprint(Vector3 addToPosition)
    {
        _playerStaminaController.SpendStamina(_sCostPerSprintStart);
        if (IsSprintStarting())
            StopCoroutine(C_sprintStart);
        C_sprintStart = StartCoroutine(StartSprinting(addToPosition));
    }

    public void StopSprint()
    {
        if (C_sprintStaminaSpending != null)
        {
            StopCoroutine(C_sprintStaminaSpending);
            C_sprintStaminaSpending = null;
        }

        if (C_sprintStart != null)
        {
            StopCoroutine(C_sprintStart);
            C_sprintStart = null;
        }
    }

    private void Move(Vector3 toMove)
    {
        /*_playerTransfrom.position += _playerTransfrom.TransformDirection(toMove);*/
        //_playerTransfrom.position = Vector3.MoveTowards(_playerTransfrom.position, _playerTransfrom.position + _playerTransfrom.TransformDirection(toMove), 1f);
        _rb.MovePosition(_playerTransfrom.position + _playerTransfrom.TransformDirection(toMove));
    }

    //Переменные из методов

    public bool IsEnoughToStartSprint()
    {
        return _playerStaminaController.CurrStamina >= _sCostPerSprintStart ? true : false;
    }

    public bool IsSprintStarting()
    {
        return C_sprintStart == null ? false : true;
    }

    private bool IsMoving()
    {
        return C_sprintStart != null || _isSprinting || _playerClimbingController.IsClimbing ? true : false;
    }



    //Коротины
    private IEnumerator StartSprinting(Vector3 addToPosition)
    {
        Vector3 toMove = addToPosition;
        while (true)
        {
            //_playerTransfrom.position = Vector3.MoveTowards(_playerTransfrom.position, moveTo, sprintSpeed * Time.deltaTime);
            if (toMove.sqrMagnitude * sprintSpeed * Time.fixedDeltaTime > addToPosition.sqrMagnitude)
            {
                Move(addToPosition * sprintSpeed * Time.fixedDeltaTime);
                break;
            }
            else
                Move(toMove * sprintSpeed * Time.fixedDeltaTime);
            addToPosition -= toMove * sprintSpeed * Time.fixedDeltaTime;
            yield return new WaitForSeconds(0.0001f);
        }
        _canSprint = true;

        C_sprintStart = null;
    }

    private IEnumerator StaminaSprintSpending()
    {
        yield return new WaitForSeconds(0.1f);
        _playerStaminaController.SpendStamina(_sCostPerSprintSecond / 10f);
        _isSprinting = false;


        C_sprintStaminaSpending = null;
    }


    //Специальные методы
    private void FixedUpdate()
    {
        if (!IsMoving())//Если не бежим и не максимум стамины
        {
            _canSprint = false;
            if (_playerStaminaController.IsNeedStaminaRecovery())
                _playerStaminaController.TryStartStaminaRecovery();
        }
    }

    private void Awake()
    {
        Initialise();
    }

    private void Initialise()
    {
        _playerTransfrom = this.gameObject.GetComponent<Transform>();
        _playerStaminaController = this.gameObject.GetComponent<PlayerStaminaController>();
        _playerClimbingController = this.gameObject.GetComponent<PlayerClimbingController>();
        _rb = this.gameObject.GetComponent<Rigidbody>();
    }
    

}
