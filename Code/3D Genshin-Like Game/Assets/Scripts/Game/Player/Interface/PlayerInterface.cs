using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerInterface : MonoBehaviour
{
    private TextMeshProUGUI _staminaTMP;
    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        _staminaTMP = this.gameObject.GetComponent<TextMeshProUGUI>();
        PlayerStaminaController.onStaminaChange.AddListener(ChangeStamina);
    }

    public void ChangeStamina(float stamina)
    {
        _staminaTMP.text = Mathf.Floor(stamina).ToString();
    }
}
