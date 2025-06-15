using System.Collections;
using System.Collections.Generic;
using inonego;
using UnityCommunity.UnitySingleton;
using UnityEngine;

/// <summary>
/// 전체 게임의 상태와 진행을 관리합니다.
/// </summary>
public class GameManager : MonoSingleton<GameManager>
{

    public int Score { get; private set; } = 0;

    /// <summary>
    /// 실행 가능한 미니게임 목록입니다.
    /// </summary>
    public List<MiniGame> MiniGameList = new();

    /// <summary>
    /// 게임의 진행 상태를 나타냅니다.
    /// </summary>
    public enum State
    {
        Idle,    // 대기 중
        Running, // 실행 중
        Done     // 완료됨
    }

    public State CurrentState { get; private set; } = State.Idle;

    /// <summary>
    /// 게임의 상태가 변경될 때 알립니다.
    /// </summary>
    public delegate void OnStateChangedEvent(State state);
    /// <summary>
    /// 게임이 클리어되었을 때 알립니다.
    /// </summary>
    public delegate void OnGameClearedEvent();
    /// <summary>
    /// 게임이 실패했을 때 알립니다.
    /// </summary>
    public delegate void OnGameFailedEvent();

    public event OnStateChangedEvent OnStateChanged;
    public event OnGameClearedEvent OnGameCleared;
    public event OnGameFailedEvent OnGameFailed;

    /// <summary>
    /// 게임의 제한 시간입니다.
    /// </summary>
    public float GameTime;

    private LoadScene loadScene;

    protected override void Awake()
    {
        loadScene = GetComponent<LoadScene>();
    
        OnGameCleared += () =>
        {
            loadScene.Load();
        };
    }

    private void Update()
    {
        if(State.Running == CurrentState)
            GameTime += Time.deltaTime;

        Debug.Log($"Score: {CalculateScore()}");
    }

    private void SetState(State state)
    {
        CurrentState = state;
     
        OnStateChanged?.Invoke(state);
    }

    #region Game Control

    /// <summary>
    /// 게임을 초기 상태로 되돌립니다.
    /// </summary>
    public void Reset()
    {
        SetState(State.Idle);
        GameTime = 0;
    }

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    public void Play()
    {
        SetState(State.Running);
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public void Stop()
    {
        SetState(State.Done);
    }

    #endregion

    #region Event Processing
   

    /// <summary>
    /// 게임을 클리어한 것으로 설정합니다.
    /// </summary>
    public void SetGameCleared()
    {
        Score = CalculateScore();

        PlayerPrefs.SetInt("Score", Score);

        Stop();

        OnGameCleared?.Invoke();
    }

    /// <summary>
    /// 게임을 실패한 것으로 설정합니다.
    /// </summary>
    public void SetGameFailed()
    {
        Stop();

        OnGameFailed?.Invoke();
    }

    public int CalculateScore()
    {
        double l = 1000000, r = 550000;

        double a = 800, x = GameTime;

        if (0f <= x && x <= a)
        {
            double p = 1 / a * x;

            return (int)((l - r) * (p * p * (2 * p - 3) + 1) + r);
        }
        else
        {
            return x < 0 ? (int)l : (int)r;
        }
    }

    #endregion
}
