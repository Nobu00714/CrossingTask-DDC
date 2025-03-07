using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetAverageMTTextFast : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI MTText;
    void Start()
    {
        
    }
    void Update()
    {
        MTText.text = "ニュートラルでの平均操作時間："+(VariableManager.AllMTSumNeutral/(VariableManager.setNum*3*5))+"  "+
                        "速さ重視での目標操作時間："+((VariableManager.AllMTSumNeutral/(VariableManager.setNum*3*5))*0.9f);
        
    }
}
