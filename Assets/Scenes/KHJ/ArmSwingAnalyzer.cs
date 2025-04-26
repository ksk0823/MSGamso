using UnityEngine;

using System;
using System.Linq;

[Serializable]
public struct ArmSwingAnalysisResult
{
    public bool IsSuccess;

    public float DominantFrequencyL;
    public float DominantFrequencyR;
    public float SwingPeriodL;
    public float SwingPeriodR;
}

[Serializable]
public class ArmSwingAnalyzer
{
    
#region 주기 분석

    public int SubDivideCount = 4;
    public float MaxFrequencyForAnalysis = 4.0f;   // 분석할 최대 주파수 (Hz)
    public float SignificanceThreshold = 0.1f;     // 의미있는 주파수로 간주할 임계값

    // ------------------------------------------------------------
    /// <summary>
    /// 암스윙을 분석합니다.
    /// </summary>
    /// <returns>분석 성공 여부</returns>
    // ------------------------------------------------------------
    public ArmSwingAnalysisResult Analyze(PoseSnapshot[] array, int sampleCount)
    {        
        // ------------------------------------------------------------
        // 시간 및 위치 데이터 추출
        // ------------------------------------------------------------
        float[] times = new float[sampleCount];
        
        Vector3[] lControllerPositions = new Vector3[sampleCount];
        Vector3[] rControllerPositions = new Vector3[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            times[i] = array[i].TimeStamp;

            lControllerPositions[i] = array[i].Pose.LController.Position;
            rControllerPositions[i] = array[i].Pose.RController.Position;
        }
        
        // Y축 속도만 추출 (상하 움직임)
        float[] lY = new float[lControllerPositions.Length];
        float[] rY = new float[rControllerPositions.Length];
        
        for (int i = 0; i < times.Length; i++)
        {
            lY[i] = lControllerPositions[i].y;
            rY[i] = rControllerPositions[i].y;
        }
        
        // 평균 샘플링 간격 계산
        float dt = (times[times.Length - 1] - times[0]) / (times.Length - 1);
        
        // 왼쪽 컨트롤러 분석
        var (DominantFrequencyL, SwingPeriodL) = CalculateFFT(lY, dt);
        
        // 오른쪽 컨트롤러 분석
        var (DominantFrequencyR, SwingPeriodR) = CalculateFFT(rY, dt);
        
        return new ArmSwingAnalysisResult
        {
            IsSuccess = true,

            DominantFrequencyL = DominantFrequencyL,
            DominantFrequencyR = DominantFrequencyR,
            SwingPeriodL = SwingPeriodL,
            SwingPeriodR = SwingPeriodR,
        };
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 주파수 분석을 수행합니다.
    /// </summary>
    /// <param name="data">분석할 데이터</param>
    /// <param name="dt">샘플링 간격</param>
    /// <returns>(주파수, 주기) 튜플</returns>
    // ------------------------------------------------------------
    private (float frequency, float period) CalculateFFT(float[] data, float dt)
    {
        // 데이터 전처리
        float[] preProcessedData = PreprocessData(data);
        
        int n = preProcessedData.Length;

        float samplingRate = 1.0f / dt;

        int maxFrequencyIndex = Mathf.Min(n / 2, Mathf.FloorToInt(MaxFrequencyForAnalysis * n / samplingRate));
        
        // ------------------------------------------------------------
        // DFT 계산
        // ------------------------------------------------------------
        float[] magnitudes = new float[maxFrequencyIndex * SubDivideCount];

        // 0Hz(DC) 제외
        for (int k = 1; k < maxFrequencyIndex * SubDivideCount; k++)
        {
            float sumReal = 0;
            float sumImag = 0;
            
            float a = 2 * Mathf.PI * k / SubDivideCount / n;

            for (int t = 0; t < n; t++)
            {
                float angle = a * t;

                sumReal += preProcessedData[t] * Mathf.Cos(angle);
                sumImag -= preProcessedData[t] * Mathf.Sin(angle);
            }
            
            magnitudes[k] = Mathf.Sqrt(sumReal * sumReal + sumImag * sumImag) / n;
        }
        
        // 주요 주파수 찾기
        int dominantIndex = 0;
        float maxMagnitude = 0;
        
        for (int i = 1; i < maxFrequencyIndex * SubDivideCount; i++)
        {
            if (magnitudes[i] > maxMagnitude)
            {
                maxMagnitude = magnitudes[i];

                dominantIndex = i;
            }
        }
        
        // 유의미한 주파수인지 확인
        if (maxMagnitude < SignificanceThreshold)
        {
            return (0, 0); // 유의미한 주기성 없음
        }
        
        // 주파수 및 주기 계산
        float frequency = samplingRate / n * dominantIndex / SubDivideCount;
        float period = frequency > 0 ? 1.0f / frequency : 0;
        
        return (frequency, period);
    }
    
    // ------------------------------------------------------------
    /// <summary>
    /// 데이터 전처리 (추세 제거 및 창 함수 적용)
    /// </summary>
    /// <param name="data">원본 데이터</param>
    /// <returns>전처리된 데이터</returns>
    // ------------------------------------------------------------
    private float[] PreprocessData(float[] data)
    {
        int n = data.Length;

        float[] result = new float[n];
        
        // 평균 제거 (DC 성분 제거)
        float mean = data.Average();

        for (int i = 0; i < n; i++)
        {
            result[i] = data[i] - mean;
        }
        
        // 해밍 창 함수 적용
        for (int i = 0; i < n; i++)
        {
            float window = 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * i / (n - 1));
     
            result[i] *= window;
        }
        
        return result;
    }

#endregion

}