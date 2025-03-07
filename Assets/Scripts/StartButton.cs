using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void OnClickStartButton()
    {
        SceneManager.LoadScene("TaskScene");
        VariableManager.resultCheck = true;
    }
}
