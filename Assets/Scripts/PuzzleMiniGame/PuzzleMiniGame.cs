using System;
using System.Collections;
using System.Collections.Generic;
using inonego;
using UnityEngine;

public partial class PuzzleMiniGame : MiniGame
{
    public delegate void OnSequenceChangedEvent(Sequence sequence);
    
    public event OnSequenceChangedEvent OnSequenceChanged;

    public List<int> SequenceLengthOnEachLevel = new(); 

    public int TotalLevel => SequenceLengthOnEachLevel.Count;
    public int CurrentLevel         { get; private set; } = 0;

    public Sequence CurrentSequence { get; private set; } = null;

    private Timer playTimer = new Timer();

    public float ElapsedTime    => playTimer.ElapsedTime;
    public float LeftTime       => playTimer.LeftTime;

    public override void Play()
    {
        base.Play();

        // 레벨 0부터 시작함
        if (0 < TotalLevel) SetLevel(0);
    }

    public override void Stop()
    {
        base.Stop();

        CurrentLevel = 0;
        CurrentSequence = null;
    }

    private void SetLevel(int level)
    {
        CurrentLevel = level;
        CurrentSequence = new Sequence(SequenceLengthOnEachLevel[CurrentLevel]);

        CurrentSequence.OnCleared    += OnSequenceCleared;
        CurrentSequence.OnFailed     += OnSequenceFailed;

        OnSequenceChanged?.Invoke(CurrentSequence);
    }

    private void OnSequenceCleared()
    {
        CurrentLevel += 1;

        if (CurrentLevel == TotalLevel)
        {
            SetCleared();
        }
        else
        {
            SetLevel(CurrentLevel);
        }
    }

    private void OnSequenceFailed()
    {
        // 실패 시에 해당 레벨 재시작
        SetLevel(CurrentLevel);
    }

    public void ProcessInput(Sequence.InputType inputType)
    {
        CurrentSequence?.ProcessInput(inputType);
    }

    public void ProcessInput(int inputType) => ProcessInput((Sequence.InputType)inputType);
}