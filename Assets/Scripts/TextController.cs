using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextController : MonoBehaviour
{
    [SerializeField] private TaskController taskController;
    [SerializeField] private TextMeshProUGUI biasText;
    [SerializeField] private TextMeshProUGUI numText;
    void Start()
    {
    }
    void Update()
    {
        if(taskController.bias == TaskController.Bias.Fast)
        {
            biasText.text = "速さ重視：できるだけ速く";
        }
        if(taskController.bias == TaskController.Bias.Neutral)
        {
            biasText.text = "ニュートラル：速く正確に";
        }
        if(taskController.bias == TaskController.Bias.Accurate)
        {
            biasText.text = "正確さ重視：できるだけエラーしないように";
        }
        numText.text = "現在の試行数："+(taskController.taskNum+1)+" / "+taskController.AmplitudeSum*taskController.WidthSum*taskController.set;
    }
}
