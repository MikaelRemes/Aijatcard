using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Log : MonoBehaviour
{
    public CardDisplay cardDisplay;
    public CardDatabase database;
    public ScrollRect scroll;
    public float updateRate = 0.4f;

    private float updateTimer = 0f;
    private List<string> logBuffer = new List<string>();

    public void AddLogToBuffer(string text)
    {
        logBuffer.Add(text);
    }

    public void AddLog(string text)
    {
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, GetComponent<RectTransform>().sizeDelta.y + 62);
        GetComponent<TextMeshProUGUI>().text += "\n";
        GetComponent<TextMeshProUGUI>().text += text;
        scroll.verticalNormalizedPosition = 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && TMP_TextUtilities.IsIntersectingRectTransform(GetComponent<RectTransform>(), Input.mousePosition, null))
        {
            var wordIndex = TMP_TextUtilities.FindIntersectingWord(GetComponent<TextMeshProUGUI>(), Input.mousePosition, null);

            if (wordIndex != -1)
            {
                string LastClickedWord = GetComponent<TextMeshProUGUI>().textInfo.wordInfo[wordIndex].GetWord();

                //check if word is found in carddatabase
                if (LastClickedWord.Length == 3)
                {
                    if (database.getCardById(LastClickedWord) != null)
                    {
                        cardDisplay.SetCurrentCard(database.getCardById(LastClickedWord).GetComponent<Card>());
                    }

                }
            }
        }
        if (updateTimer > updateRate)
        {
            if (logBuffer.Count > 0)
            {
                string text = logBuffer[0];
                AddLog(text);
                logBuffer.Remove(text);
            }
            updateTimer = 0;
        }
        else updateTimer += Time.deltaTime;
    }
}
