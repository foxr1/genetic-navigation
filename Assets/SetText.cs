using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SetText : MonoBehaviour
{
    public void SetInfoText(string generation, string genome, string fitness, double timeElapsed)
    {
        // https://stackoverflow.com/questions/463642/how-can-i-convert-seconds-into-hourminutessecondsmilliseconds-time
        TimeSpan time = TimeSpan.FromSeconds(timeElapsed);
        string str = time.ToString(@"hh\:mm\:ss\:fff");

        gameObject.GetComponent<TextMeshProUGUI>().text = $"" +
            $"<b><size=150%>Genetic Algorithm Demonstration</size></b>\r\n\r\n" +
            $"<b>Current Generation:</b> {generation}\r\n" +
            $"<b>Current Genome:</b> {genome}\r\n" +
            $"<b>Current Fitness:</b> {fitness}\r\n" +
            $"<b>Time Elapsed:</b> {str}\r\n";
    }
}
