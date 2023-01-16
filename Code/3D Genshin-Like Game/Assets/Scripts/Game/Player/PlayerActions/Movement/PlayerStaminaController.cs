using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStaminaController : MonoBehaviour
{
    #region Variables
    [SerializeField] private int maxStamina = 100;
    private float _currStamina;
    public float CurrStamina { get => _currStamina; private set => _currStamina = value; }
    
    public bool IsNeedStaminaRecovery()
    {
        return _currStamina < maxStamina && C_staminaRecovery == null ? true : false;
    }

    [SerializeField] private int _sRecoveryPerSecond = 5;
    #endregion

    public static UnityEvent<float> onStaminaChange = new UnityEvent<float>();
    private Coroutine C_staminaRecovery;

    public void SpendStamina(float toSpend)
    {
        if(C_staminaRecovery != null)
            StopCoroutine(C_staminaRecovery);
        C_staminaRecovery = null;

        _currStamina -= toSpend;
        if (_currStamina < 0)
            _currStamina = 0;
        onStaminaChange.Invoke(_currStamina);
    }

    public void TryStartStaminaRecovery()
    {
        C_staminaRecovery = StartCoroutine(WaitForStaminaRecovery());
    }

    private IEnumerator WaitForStaminaRecovery()
    {
        yield return new WaitForSeconds(3f);
        C_staminaRecovery = StartCoroutine(StaminaRecovery());
    }

    private IEnumerator StaminaRecovery()
    {
        while (_currStamina < maxStamina)
        {
            yield return new WaitForSeconds(0.1f);
            _currStamina = Mathf.Clamp(_currStamina + _sRecoveryPerSecond / 10f, 0, maxStamina);
            onStaminaChange.Invoke(_currStamina);
        }
        C_staminaRecovery = null;
    }

    private void Start()
    {
        _currStamina = maxStamina;
        onStaminaChange.Invoke(_currStamina);
    }

}
