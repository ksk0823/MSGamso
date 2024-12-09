using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatchGame : MiniGame
{
    public int spawnCount;
    public int catchCount;

    public int score { get; private set; } = 0;

    private List<Butterfly> butterflyList = new List<Butterfly>();

    ButterflyGenerator butterflyGenerator;
    // Start is called before the first frame update
    void Start()
    {
        butterflyGenerator = GetComponent<ButterflyGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (score >= catchCount)
        {
            SetCleared();
        }
    }

    public override void Reset()
    {
        base.Reset();

        score = 0;
    }

    public override void Play()
    {
        base.Play();

        for (int i = 0; i < spawnCount; i++)
        {
            Butterfly butterfly =  butterflyGenerator.GenerateButterfly();

            butterfly.OnCaught += () =>
            {
                score += 1;
            };

            butterflyList.Add(butterfly);
        }    
    }

    public override void Stop()
    {
        base.Stop();

        foreach (var butterfly in butterflyList)
        {
            Destroy( butterfly );
        }

        butterflyList.Clear();
    }
}
