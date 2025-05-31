using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class ScoreManager : MonoBehaviour
{
    [System.Serializable]
    public class GameData
    {
        public float playTime;
        public int score;
    }

    private string filePath;

    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "score.json");
    }

    public void SaveScore()
    {
        float time = GameManager.Instance.GameTime;
        int score = CalculateScore(time);

        GameData data = new GameData
        {
            playTime = time,
            score = score
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    private int CalculateScore(float time)
    {
        // 시간에 따라 점수를 계산
        float maxTime = 640f; // 기준 시간 (예: 8분)
        float baseScore = 1000f;
        float t = Mathf.Clamp(time, 1f, maxTime); // 최소 1초 보장
        return Mathf.RoundToInt(baseScore * (maxTime / t));
    }
}
