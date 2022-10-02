using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class AnalysisScreen : MonoBehaviour
{
    [SerializeField] private Text analysisText;
    [SerializeField] private Text messageText;
    private bool shouldAnimText = false;
    private bool destroyed = false;

    private int maxMessageLines = 18;
    private List<string> messageLines = new List<string>();

    private void Start()
    {
        CheckActiveSelf().Forget();
    }

    private async UniTask CheckActiveSelf()
    {
        while (true)
        {
            if (destroyed) return;

            bool activeSelf = gameObject.activeSelf;
            if (activeSelf && !shouldAnimText)
            {
                shouldAnimText = true;
                StartCoroutine(AnimAnalysisText());
            }
            else if (!activeSelf && shouldAnimText)
            {
                shouldAnimText = false;
            }
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    
    private IEnumerator AnimAnalysisText()
    {
        float animSpeed = 0.4f;
        while (shouldAnimText)
        {
            analysisText.text = "Analyzing SUS";
            for(int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(animSpeed);
                analysisText.text += '.';
            }
            yield return new WaitForSeconds(animSpeed);
        }
    }

    public void AddAnalyzingMessage(string msg, bool overrideLine)
    {
        if (overrideLine)
        {
            messageLines[messageLines.Count - 1] = msg;
        }
        else
        {
            if (messageLines.Count < maxMessageLines) messageLines.Add(msg);
            else
            {
                messageLines.RemoveAt(0);
                messageLines.Add(msg);
            }
        }

        if (messageText == null) return;
            messageText.text = "";
        foreach(string line in messageLines) messageText.text += $"\n{line}";
    }



    private void OnDestroy()
    {
        destroyed = true;
    }
}
