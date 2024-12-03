using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AYellowpaper.SerializedCollections;

public class PuzzleUI : MonoBehaviour
{
    [Header("General")]
    public PuzzleMiniGame PuzzleMiniGame;

    [Header("Image")]
    public SerializedDictionary<PuzzleMiniGame.Sequence.InputType, Sprite> Images = new();

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
        LevelUI.text    = $"{ PuzzleMiniGame.CurrentLevel + 1 }";
    }

    private void OnPuzzleSequenceChanged(PuzzleMiniGame.Sequence sequence)
    {
        void OnSequencePassed(PuzzleMiniGame.Sequence sequence, int index)
        {
            for (int i = 0; i < imageUIs.Length; i++)
            {
                imageUIs[i].color   = i < sequence.Count && i >= sequence.Index ? Color.white : Color.clear;
            }
        }

        sequence.OnPassed += OnSequencePassed;

        for (int i = 0; i < imageUIs.Length; i++)
        {
            imageUIs[i].gameObject.SetActive(i < sequence.Count);
            imageUIs[i].sprite  = i < sequence.Count ? Images[sequence.Generated[i]] : null;
            imageUIs[i].color   = i < sequence.Count ? Color.white : Color.clear;
        }
    }

    private void OnPuzzleMiniGameCleared()
    {
        Debug.Log("PuzzleMiniGame Cleared!");
    }
}