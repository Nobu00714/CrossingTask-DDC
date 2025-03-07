using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using uWintab;
using UnityEngine.SceneManagement;

public class TaskController : MonoBehaviour
{
    [SerializeField] public int Participant;
    [SerializeField] public Bias bias;
    [SerializeField] private Team team;
    [SerializeField] public Device device;
    [SerializeField] private bool practice;
    [SerializeField] public int set;
    [SerializeField] public int taskNum;
    [SerializeField] public int biasNum;
    [SerializeField] public int AmplitudeSum;
    [SerializeField] public int WidthSum;
    [SerializeField] private int nowTaskAmplitude;
    [SerializeField] private int nowTaskWidth;
    [SerializeField] GameObject start;
    [SerializeField] GameObject goal;
    [SerializeField] GameObject wall;
    [SerializeField] GameObject button;
    [SerializeField] private AudioClip correctAudio;
    [SerializeField] private AudioClip wrongAudio;
    [SerializeField] private GameObject cursor;
    private Tablet tablet;
    private bool taskReady;
    private bool taskStarted;
    private bool startCrossed;
    private bool goalCrossed;
    private bool nextButtonPressed;
    private bool Touching = false;
    private int[] taskAmplitude;
    private int[] taskWidth;
    private List<int> taskList;
    private StreamWriter swPos;
    private StreamWriter swMT;
    private float taskStartTime;
    private float taskFinishTime;
    private float mouseX;
    private float mouseY;   
    private bool falseStart = false;
    private bool touchWall = false;
    private SpriteRenderer startRenderer;
    private SpriteRenderer goalRenderer;
    private SpriteRenderer wallRenderer;
    private AudioSource audioSource;
    private float Ae;
    private float We;
    private float mouseXprev;
    private float mouseYprev;
    private int taskClear;
    private int sameTaskNum;
    RectTransform rectTransform;
    private bool buttonPressable = true;
    GameObject trajestory;
    GameObject trajestoryLine;
    [SerializeField] GameObject cursorTrajestoryParent;
    [SerializeField] LineRenderer lineRenderer;
    GameObject LineInstance;
    private bool first = true;
    private bool CSVUpdated = false;
    private bool firstPenTouch =true;
    private bool firstPenRelease =true;
    private bool firstStartCross = true;
    private bool firstGoalCross = true;
    private List<Vector3> trajestoryList = new List<Vector3>();
    public enum Bias
    {
        Fast,
        Neutral,
        Accurate
    }
    public enum Team
    {
        FastToAccurate,
        AccurateToFast
    }
    public enum Device
    {
        Mouse,
        Pen
    }
    void Start()
    {
        startRenderer = start.GetComponent<SpriteRenderer>();
        goalRenderer = goal.GetComponent<SpriteRenderer>();
        wallRenderer = wall.GetComponent<SpriteRenderer>();
        audioSource = this.GetComponent<AudioSource>();
        rectTransform = button.GetComponent<RectTransform>();
        tablet = this.GetComponent<Tablet>();
        taskList = new List<int>();
        for(int i=0; i<AmplitudeSum*WidthSum; i++)
        {
            taskList.Add(i);
        }
        ShuffleList(taskList);

        taskAmplitude = new int[3];
        taskAmplitude[0] = 200;
        taskAmplitude[1] = 550;
        taskAmplitude[2] = 880;

        taskWidth = new int[5];
        taskWidth[0] = 8;
        taskWidth[1] = 15;
        taskWidth[2] = 28;
        taskWidth[3] = 50;
        taskWidth[4] = 100;

        taskUpdate();
        
        Cursor.visible = false;
        trajestory = (GameObject)Resources.Load("CursorTrajestory");
        trajestoryLine = (GameObject)Resources.Load("Line");

        bias = VariableManager.bias;
        biasNum = VariableManager.biasNum;
        Participant = VariableManager.Participant;
        taskNum = VariableManager.taskNum;
        VariableManager.MTSum = 0;
        swMT = VariableManager.swMT;
        VariableManager.ERSum = 0;
    }
    void OnApplicationQuit()
    {
        swMT.Close();
    }
    void Update()
    {
        DecideCursorPos();
        DrawCursorTrajestory();
        if(taskNum == 0)
        {
            if(first)
            {
                makeMTCSV();
                first = false;
                VariableManager.swMT = swMT;
            }
        }

        float startX = nowTaskAmplitude/2;
        float startYUp = nowTaskWidth/2;
        float startYBottom = -nowTaskWidth/2;
        float startXRight = nowTaskAmplitude/2 + nowTaskWidth/2;
        float startXLeft = nowTaskAmplitude/2 - nowTaskWidth/2;
        float goalX = -nowTaskAmplitude/2;
        float goalXRight = - nowTaskAmplitude/2 + nowTaskWidth/2;
        float goalXLeft = - nowTaskAmplitude/2 - nowTaskWidth/2;
        float goalYUp = nowTaskWidth/2;
        float goalYBottom = -nowTaskWidth/2;

        //ネクストボタンが押され，マウスがスタートより下に戻ったら準備完了
        if(nextButtonPressed)
        {
            if(mouseX>startX)
            {
                nextButtonPressed = false;
                taskReady = true;
                CSVUpdated = false;
                firstStartCross = true;
                firstGoalCross = true;
                falseStart = false;
                touchWall = false;
                wallRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
            }
        }
        //準備完了かつ（ペンが触れているかマウスクリックされている）
        if(taskReady && Touching)
        {
            //スタート地点を超えたら
            if(mouseX<=startX && mouseXprev>startX)
            {
                //スタートを通過したフラグをtrueにする
                startCrossed = true;
                //このifいる？
                if(!falseStart)
                {
                    //スタートターゲットを通過したら
                    if(mouseY<=startYUp && mouseY>=startYBottom)
                    {
                        //一回目にスタートを通過したら
                        if(firstStartCross)
                        {
                            firstStartCross = false;
                            //座標保存用CSVを作成
                            makePosCSV();
                        }
                        Ae = 0f;
                        startRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                        taskStartTime = Time.time;
                        //タスクを開始し，タスクの準備が完了していない状態にする
                        taskStarted = true;
                        taskReady = false;
                    }
                    //ターゲット外を通過したら
                    else
                    {
                        startRenderer.color = new Color(1f,0f,0f,1f);   //赤色にする
                        //FalseStartのフラグをtrueにする
                        falseStart = true;
                        taskReady = false;
                    }
                }
            }
            //スタート地点より下に戻ったら
            if(mouseX>startX)
            {
                startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                //FalseStartを解除する
                
            }
        }

        //タスクが開始している場合
        if(taskStarted)
        {
            Ae += Mathf.Sqrt(Mathf.Pow(mouseX-mouseXprev,2)+Mathf.Pow(mouseY-mouseYprev,2));
            if(mouseX<0 && mouseXprev>=0 && Touching)
            {
                HitWall();
            }
            //ゴール地点を超えたら
            if(mouseX<goalX && Touching && !falseStart && !touchWall)
            {
                //ゴール時の時間を記録
                taskFinishTime = Time.time;
                //タスクが開始しているフラグをFalseにし，ゴールを通過したフラグをTrueにする
                taskStarted = false;
                goalCrossed = true;
                //ゴールの中心地点とマウスまたはペンのx座標のずれを記録
                We = mouseY;

                if(firstGoalCross)
                {
                    firstGoalCross = false;
                    //ゴールターゲットを通過したら
                    if(mouseY>=goalYBottom && mouseY<=goalYUp)
                    {
                        goalRenderer.color = new Color(0f,1f,0f,1f);   //緑色にする
                        //クリアを記録
                        taskClear = 1;
                        audioSource.PlayOneShot(correctAudio);
                    }
                    //ターゲット外を通過したら
                    else
                    {
                        goalRenderer.color = new Color(1f,0f,0f,1f);   //赤色にする
                        //エラーを記録
                        taskClear = 0;
                        audioSource.PlayOneShot(wrongAudio);
                        VariableManager.ERSum++;
                    }

                    //クリアかエラーを記録
                    updateMTCSV(taskClear);
                    //ボタンをランダム位置に出現
                    button.SetActive(true);
                    buttonPressable = true;
                    //マウスポジションの記録を終了
                    swPos.Close();
                    sameTaskNum = 0;
                }

                //Nextボタンの位置を変更
                rectTransform.anchoredPosition = new Vector3(-1000,UnityEngine.Random.Range(-300,300),0);
            }
        }

        if(device == Device.Pen)
        {
            //ペンを付けた時
            if(tablet.pressure>0.001)
            {
                //ペンをまだ離していないフラグをTrueにする（初めて離した時の処理のため）
                firstPenRelease = true;

                //ペンを付けた最初のフレームの処理
                if(firstPenTouch)
                {
                    //ペンが触れているフラグをTrueにする
                    Touching = true;
                    //ペンを最初に触れたフラグをFalseにする
                    firstPenTouch = false;
                    //ペンの軌跡を表示するためのオブジェクトを生成
                    LineInstance = (GameObject)Instantiate(trajestoryLine, Vector3.zero, Quaternion.identity);
                    LineInstance.transform.parent = cursorTrajestoryParent.transform;

                    //Nextボタンを押したら
                    if(buttonPressable && mouseX>=rectTransform.anchoredPosition.x-250 && mouseX<=rectTransform.anchoredPosition.x+250 && mouseY>=rectTransform.anchoredPosition.y-250 && mouseY<=rectTransform.anchoredPosition.y+250)
                    {
                        startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        //ゴール通過後だったらタスクを更新
                        if(goalCrossed)
                        {
                            taskNum++;
                            taskUpdate();
                        }
                        //フラグ管理（ボタンを押した，ボタン消す，ボタン押せる状態ではなくす，ゴール通過していない）
                        nextButtonPressed = true;
                        button.SetActive(false);
                        buttonPressable = false;
                        goalCrossed = false;
                    }
                }
            }

            //ペンを離したとき
            if(tablet.pressure<0.001)
            { 
                //ペンをまだつけていないフラグをTrueにする（初めてつけた時の処理のため）
                firstPenTouch = true;

                //ペンを離した最初のフレームでの処理
                if(firstPenRelease)
                {
                    firstPenRelease =false;
                    //カーソル軌跡を消す
                    trajestoryList.Clear();
                    foreach ( Transform child in cursorTrajestoryParent.transform )
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                    //スタートターゲットでミスしてから離した場合
                    if(falseStart)
                    {
                        //もう一度同じタスクを繰り返させる
                        sameTaskNum++;
                        nextButtonPressed = true;
                        taskStarted = false;
                        //ビープ音を鳴らす
                        audioSource.PlayOneShot(wrongAudio);
                        //座標の記録を終了
                        swPos.Close();
                    }
                    Touching = false;
                    startCrossed = false;
                   /* //ゴールしてから離したら
                    if(startCrossed && goalCrossed)
                    {
                        if(!CSVUpdated)
                        {
                            
                            
                            
                            sameTaskNum = 0;
                            CSVUpdated = true;
                        
                        }
                    }

                    //ゴールせずに離したら（スタートは成功）
                    if(startCrossed && !goalCrossed && !falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //ゴール前に離したというデータを記録
                            updateMTCSV(2);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            
                        }
                    }
                    //ゴールせずに離したら（スタートも失敗）
                    if(startCrossed && !goalCrossed && falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(3);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }
                    //スタートせずに離したら
                    if(!startCrossed && taskReady)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            //updateMTCSV(4);
                            //audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }*/
                }
            }
        }

        if(device == Device.Mouse)
        {
            //ペンを付けた時
            if(Input.GetMouseButtonDown(0))
            {
                //ペンをまだ離していないフラグをTrueにする（初めて離した時の処理のため）
                firstPenRelease = true;

                //ペンを付けた最初のフレームの処理
                if(firstPenTouch)
                {
                    //ペンが触れているフラグをTrueにする
                    Touching = true;
                    Debug.Log("Touch");
                    //ペンを最初に触れたフラグをFalseにする
                    firstPenTouch = false;
                    //ペンの軌跡を表示するためのオブジェクトを生成
                    LineInstance = (GameObject)Instantiate(trajestoryLine, Vector3.zero, Quaternion.identity);
                    LineInstance.transform.parent = cursorTrajestoryParent.transform;

                    //Nextボタンを押したら
                    if(buttonPressable && mouseX>=rectTransform.anchoredPosition.x-250 && mouseX<=rectTransform.anchoredPosition.x+250 && mouseY>=rectTransform.anchoredPosition.y-250 && mouseY<=rectTransform.anchoredPosition.y+250)
                    {
                        startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                        //ゴール通過後だったらタスクを更新
                        if(goalCrossed)
                        {
                            taskNum++;
                            taskUpdate();
                        }
                        //フラグ管理（ボタンを押した，ボタン消す，ボタン押せる状態ではなくす，ゴール通過していない）
                        nextButtonPressed = true;
                        button.SetActive(false);
                        buttonPressable = false;
                        goalCrossed = false;
                    }
                }
            }

            //ペンを離したとき
            if(Input.GetMouseButtonUp(0))
            { 
                //ペンをまだつけていないフラグをTrueにする（初めてつけた時の処理のため）
                firstPenTouch = true;

                foreach ( Transform child in cursorTrajestoryParent.transform )
                {
                    GameObject.Destroy(child.gameObject);
                }
                trajestoryList.Clear();

                //ペンを離した最初のフレームでの処理
                if(firstPenRelease)
                {
                    //ペンが触れているフラグをFalseにする
                    Touching = false;
                    Debug.Log("Release");

                    firstPenRelease =false;
                    
                    //スタートターゲットでミスしてから，または，壁に触れてから離した場合
                    if(falseStart || touchWall)
                    {                        
                        //もう一度同じタスクを繰り返させる
                        sameTaskNum++;
                        nextButtonPressed = true;
                        taskStarted = false;
                        //ビープ音を鳴らす
                        audioSource.PlayOneShot(wrongAudio);
                        //座標の記録を終了
                        swPos.Close();
                    }
                    
                    startCrossed = false;

                    //カーソル軌跡を消す
                    
                    

                   /* //ゴールしてから離したら
                    if(startCrossed && goalCrossed)
                    {
                        if(!CSVUpdated)
                        {
                            
                            
                            
                            sameTaskNum = 0;
                            CSVUpdated = true;
                        
                        }
                    }

                    //ゴールせずに離したら（スタートは成功）
                    if(startCrossed && !goalCrossed && !falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //ゴール前に離したというデータを記録
                            updateMTCSV(2);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                            
                        }
                    }
                    //ゴールせずに離したら（スタートも失敗）
                    if(startCrossed && !goalCrossed && falseStart)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            updateMTCSV(3);
                            audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }
                    //スタートせずに離したら
                    if(!startCrossed && taskReady)
                    {
                        if(!CSVUpdated)
                        {
                            startRenderer.color = new Color(1f,1f,1f,1f);
                            //フォールススタートしたというデータを記録
                            //updateMTCSV(4);
                            //audioSource.PlayOneShot(wrongAudio);
                            //もう一度同じタスクを実行
                            sameTaskNum++;
                            nextButtonPressed = true;
                            //マウスポジションの記録を終了
                            swPos.Close();
                            taskStarted = false;
                            CSVUpdated = true;
                        }
                    }*/
                }
            }
        }

        if(Touching && taskStarted)
        {
            updatePosCSV();
        }
        ChangeBias();
        ShowSetResult();
        FinishExperiment();
        mouseXprev = mouseX;
        mouseYprev = mouseY;

/*
        if(device == Device.Mouse)
        {
            if(Input.GetMouseButtonDown(0))
            {
                LineInstance = (GameObject)Instantiate(trajestoryLine, Vector3.zero, Quaternion.identity);
                LineInstance.transform.parent = cursorTrajestoryParent.transform;
                if(taskReady)
                {
                    mousePress = true;
                    makePosCSV();
                }
                if(buttonPressable && mouseX>=rectTransform.anchoredPosition.x-250 && mouseX<=rectTransform.anchoredPosition.x+250 && mouseY>=rectTransform.anchoredPosition.y-250 && mouseY<=rectTransform.anchoredPosition.y+250)
                {
                    startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                    goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
                    if(goalCrossed)
                    {
                        taskNum++;
                        taskUpdate();
                    }
                    nextButtonPressed = true;
                    button.SetActive(false);
                    buttonPressable = false;
                    goalCrossed = false;
                }
            }
            if(Input.GetMouseButtonUp(0))
            {  
                trajestoryList.Clear();
                //ゴールしてから離したら
                if(goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        //クリアかエラーを記録
                        updateMTCSV(taskClear);
                        //ボタンをランダム位置に出現
                        button.SetActive(true);
                        buttonPressable = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        sameTaskNum = 0;
                        CSVUpdated = true;
                    }
                }

                //ゴールせずに離したら
                if(startCrossed && !goalCrossed)
                {
                    if(!CSVUpdated)
                    {
                        startRenderer.color = new Color(1f,1f,1f,1f);
                        //ゴール前に離したというデータを記録
                        updateMTCSV(2);
                        audioSource.PlayOneShot(wrongAudio);
                        //もう一度同じタスクを実行
                        sameTaskNum++;
                        nextButtonPressed = true;
                        //マウスポジションの記録を終了
                        swPos.Close();
                        taskStarted = false;
                        CSVUpdated = true;
                    }
                }
                foreach ( Transform child in cursorTrajestoryParent.transform )
                {
                    GameObject.Destroy(child.gameObject);
                }
                mousePress = false;
                startCrossed = false;
            }
        }
        */
    }
    //中央の壁にぶつかったら
    public void HitWall()
    {
        Debug.Log("Hit Wall");
        touchWall = true;
        wallRenderer.color = new Color(1f,0f,0f,1f);   //赤色にする
    }
    //ボタンが押されたら
    public void OnClick()
    {
        startRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
        goalRenderer.color = new Color(1f,1f,1f,1f);   //白色にする
        if(goalCrossed)
        {
            taskNum++;
            taskUpdate();
        }
        nextButtonPressed = true;
        button.SetActive(false);
        goalCrossed = false;
    }
    //カーソルの位置を決定
    private void DecideCursorPos()
    {
        if(device == Device.Pen)
        {
            mouseX = tablet.x * 2560 -2560/2;
            mouseY = tablet.y * 1440 - 1440/2;
        }
        if(device == Device.Mouse)
        {
            mouseX = Input.mousePosition.x - 2560/2;
            mouseY = Input.mousePosition.y - 1440/2;
        }
        cursor.GetComponent<RectTransform>().anchoredPosition = new Vector3(mouseX, mouseY, 1);
        //Debug.Log("X:"+mouseX+"Y:"+mouseY);
    }
    //カーソルの軌跡を描画
    private void DrawCursorTrajestory()
    {
        if(Touching)
        {
            //GameObject instance = (GameObject)Instantiate(trajestory, cursor.GetComponent<RectTransform>().anchoredPosition, Quaternion.identity);
            //instance.transform.parent = cursorTrajestoryParent.transform;
            LineRenderer lineRenderer = LineInstance.GetComponent<LineRenderer>();
            trajestoryList.Add(cursor.GetComponent<RectTransform>().anchoredPosition);
            var positions = new Vector3[trajestoryList.Count];
            for(int i=0; i<trajestoryList.Count; i++)
            {
                positions[i] = trajestoryList[i];
            }
            lineRenderer.positionCount = trajestoryList.Count;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0,0,1,1);
            lineRenderer.endColor = new Color(0,0,1,1);
            lineRenderer.SetPositions(positions);
        }
    }
    private void ShowSetResult()
    {
        if(taskNum%15 == 0 && taskNum != 0 && !VariableManager.resultCheck)
        {
            VariableManager.taskNum = taskNum;
            SceneManager.LoadScene("SetResultScene");
        }
        if(taskNum%15 == 1)
        {
            VariableManager.resultCheck = false;
        }
    }
    //バイアスを変更
    private void ChangeBias()
    {
        if(taskNum>=set*WidthSum*AmplitudeSum)
        {
            taskNum = 0;
            first = true;
            biasNum++;
            VariableManager.biasNum = biasNum;
            swMT.Close();
            if(team == Team.FastToAccurate)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
            }
            if(team == Team.AccurateToFast)
            {
                if(biasNum == 1)
                {
                    bias = Bias.Accurate;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToControlScene");
                }
                if(biasNum == 2)
                {
                    bias = Bias.Fast;
                    VariableManager.bias = bias;
                    SceneManager.LoadScene("ToFastScene");
                }
            }
        }
    }
    //タスクがすべて終わったら終了
    private void FinishExperiment()
    {
        if(biasNum>2)
        {   
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
            #else
                Application.Quit();//ゲームプレイ終了
            #endif
        }
    }
    //タスクの順番をシャッフル
    private void ShuffleList(List<int> list)
    {
        int tmp;
        int rndNum;
        for(int i=list.Count-1; i>1; i--)
        {
            rndNum = UnityEngine.Random.Range(0,i);
            tmp = list[rndNum];
            list[rndNum] = list[i];
            list[i] = tmp;
        }
    }
    //タスクの更新
    private void taskUpdate()
    {
        nowTaskWidth = taskWidth[taskList[taskNum%(AmplitudeSum*WidthSum)]%WidthSum];
        nowTaskAmplitude = taskAmplitude[taskList[taskNum%(AmplitudeSum*WidthSum)]/WidthSum];
        start.transform.localScale = new Vector3(2,nowTaskWidth,1);
        goal.transform.localScale = new Vector3(2,nowTaskWidth,1);
        start.transform.position = new Vector3((nowTaskAmplitude/2)-1,nowTaskWidth/2,0);
        goal.transform.position = new Vector3(-(nowTaskAmplitude/2)-1,nowTaskWidth/2,0);
    }
    //マウス座標を保存するCSVを作成
    private void makePosCSV()
    {
        if(practice)
        {
            swPos = new StreamWriter(@"PracticePos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swPos = new StreamWriter(@"Pos"+Participant.ToString()+bias.ToString()+"No."+taskNum+"A"+nowTaskAmplitude+"W"+nowTaskWidth+"Num"+sameTaskNum+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "長さ", "幅", "時間", "x座標", "y座標"};
        string s2 = string.Join(",", s1);
        swPos.WriteLine(s2);
    }
    //フレームごとにマウス座標を保存
    private void updatePosCSV()
    {
        string[] s1 = {Participant.ToString(),nowTaskAmplitude.ToString(),nowTaskWidth.ToString(),(Time.time-taskStartTime).ToString(),mouseX.ToString(),mouseY.ToString()};
        string s2 = string.Join(",",s1);
        if(swPos!=null)
        {
            swPos.WriteLine(s2);
        }
    }
    //操作時間を保存するCSVを作成
    private void makeMTCSV()
    {
        if(practice)
        {
            swMT = new StreamWriter(@"PracticeMT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        else
        {
            swMT = new StreamWriter(@"MT"+Participant.ToString()+bias.ToString()+".csv", true, Encoding.GetEncoding("UTF-8"));
        }
        string[] s1 = { "参加者", "セット", "試行", "長さ", "幅","バイアス", "操作時間", "有効経路幅","有効経路長","クリア" };
        string s2 = string.Join(",", s1);
        swMT.WriteLine(s2);
    }
    //タスクのクリアごとに操作時間を保存
    private void updateMTCSV(int clear)
    {
        string[] s1 = {Participant.ToString(), (taskNum/(AmplitudeSum*WidthSum)).ToString(), (taskNum%(AmplitudeSum*WidthSum)).ToString(), nowTaskAmplitude.ToString(), nowTaskWidth.ToString(), bias.ToString(), (taskFinishTime-taskStartTime).ToString(), We.ToString(), Ae.ToString(), clear.ToString()};
        string s2 = string.Join(",", s1);
        if(swMT!=null)
        {
            swMT.WriteLine(s2);
        }
        if(clear==1 || clear == 0)
        {
            VariableManager.MTSum += taskFinishTime-taskStartTime;
            if(bias == Bias.Neutral && taskNum/(AmplitudeSum*WidthSum)>=3)
            {
                VariableManager.AllMTSumNeutral += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Fast)
            {
                VariableManager.AllMTSumFast += taskFinishTime-taskStartTime;
            }
            if(bias == Bias.Accurate)
            {
                VariableManager.AllMTSumAccurate += taskFinishTime-taskStartTime;
            }
        }
        
    }
}