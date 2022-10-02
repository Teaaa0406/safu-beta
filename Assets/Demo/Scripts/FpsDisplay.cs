using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FpsDisplay : MonoBehaviour
{
    [SerializeField] private Text fpsText;
    private int frameCount;
    private float prevTime;
    private float fps;

    void Start()
    {
        frameCount = 0;
        prevTime = 0;
    }
    // XVˆ—
    void Update()
    {
        frameCount++;
        float time = Time.realtimeSinceStartup - prevTime;

        if (time >= 0.4f)
        {
            fps = frameCount / time;
            fpsText.text = $"FPS: {fps.ToString("F1")}";

            frameCount = 0;
            prevTime = Time.realtimeSinceStartup;
        }
    }
}