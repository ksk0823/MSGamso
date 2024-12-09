using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;
using System;

public class PuzzleUI : MonoBehaviour
{
    [Serializable]
    public class ImageSetting
    {
        public Sprite Sprite;
        public Color Color;
        public float Rotation;
    }

    [Header("General")]
    public PuzzleMiniGame PuzzleMiniGame;

    [Header("Image")]
    public SerializedDictionary<PuzzleMiniGame.Sequence.InputType, ImageSetting> ImageSettings = new();

    [Header("UI")]
    public TextMeshProUGUI LevelUI;
    public RectTransform ImageUIGroup;

    private Image[] imageUIs;

    private void Awake()
    {
        imageUIs = ImageUIGroup.GetComponentsInChildren<Image>();

        PuzzleMiniGame.OnSequenceChanged += OnPuzzleSequenceChanged;
        PuzzleMiniGame.OnCleared         += OnPuzzleMiniGameCleared;
    }

    private void Update()
    {
        if (PuzzleMiniGame.CurrentState == PuzzleMiniGame.State.Done)
        {
            LevelUI.text = "Done!";
        }
        else
        {
            LevelUI.text = $"{ PuzzleMiniGame.CurrentLevel + 1 }";
        }
    }

    private void OnPuzzleSequenceChanged(PuzzleMiniGame.Sequence sequence)
    {
        void OnSequencePassed(PuzzleMiniGame.Sequence sequence, int index)
        {
            for (int i = 0; i < imageUIs.Length; i++)
            {   
                imageUIs[i].color   = i < sequence.Count && i >= sequence.Index ? ImageSettings[sequence.Generated[i]].Color : Color.clear;
            }
        }

        sequence.OnPassed += OnSequencePassed;

        for (int i = 0; i < imageUIs.Length; i++)
        {
            imageUIs[i].gameObject.SetActive(i < sequence.Count);
            imageUIs[i].sprite  = i < sequence.Count ? ImageSettings[sequence.Generated[i]].Sprite : null;
            imageUIs[i].color   = i < sequence.Count ? ImageSettings[sequence.Generated[i]].Color : Color.clear;
            imageUIs[i].transform.rotation = i < sequence.Count ? Quaternion.Euler(0, 0, ImageSettings[sequence.Generated[i]].Rotation) : Quaternion.identity;
        }
    }

    private void OnPuzzleMiniGameCleared()
    {
        Debug.Log("PuzzleMiniGame Cleared!");
    }
}