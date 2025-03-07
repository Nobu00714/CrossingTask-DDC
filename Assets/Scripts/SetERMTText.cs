using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetERMTText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ER;
    [SerializeField] private TextMeshProUGUI MT;
    void Start()
    {
        
    }
    void Update()
    {
        ER.text = (VariableManager.ERSum).ToString() + " / 15";
        MT.text = (VariableManager.MTSum/15).ToString();
    }
}
