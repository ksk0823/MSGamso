using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndingSceneManager : MonoBehaviour
{
    public TextMeshPro scoreText;

    /*
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
        LoadAndDisplayScore();
    }

    void LoadAndDisplayScore()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            GameData data = JsonUtility.FromJson<GameData>(json);

            scoreText.text = $"Play Time\n{data.playTime:F2}sec\nScore\n{data.score}";
        }
        else
        {
            scoreText.text = "No Data";
        }
    }
    */

    public void Start()
    {
        int score = PlayerPrefs.GetInt("Score");
        scoreText.text = $"Score\n{score}";
    }
}
