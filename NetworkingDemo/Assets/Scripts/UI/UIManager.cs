using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Linq;
using System;

public class UIManager : MonoBehaviour
{
    [SerializeField]
TMP_Text localFrame;
    [SerializeField]
 TMP_Text remoteFrame;
    [SerializeField]
 TMP_Text frameDifference;

    [SerializeField]
 TMP_Text debugText;

    [SerializeField]
    TMP_Text ftp;

    [SerializeField]
    TMP_Text localInput;

    [SerializeField]
    TMP_Text state;


    readonly float messageTime = 1.5f;
    float t = 0;

    private void Update()
    {
        ushort l =  GameSimulation.localFrame;
        ushort r =  GameSimulation.LastRemoteFrame;
      

        localFrame.text = "Local Frame: " + l.ToString();
        remoteFrame.text = "Remote Frame: " + r.ToString();
        frameDifference.text = "Frame Advantage: " + (l - r).ToString();
        ftp.text = "Frames To Process: " + GameSimulation.framesToProcess;
        GameState g;
        if((g = Transport.current) != null)
        {
            SimPlayer local = g.players[0];
            state.text = "State: " + local.state;
            
        }



       if(debugText.text.Length != 0 && (t+=Time.deltaTime) >= messageTime)
        {
            t = 0;
            RemoveLine(1);
        }     
    }

    private void RemoveLine(int i) =>  debugText.text = string.Join(Environment.NewLine,Regex.Split(debugText.text, "\n").Skip(i).ToArray());

    private void PrintLog(string log, string stack, LogType type)
    {
        debugText.text+= $"{type}: {log}\n";

    }

    private void OnEnable()
    {
        Application.logMessageReceived += PrintLog;
    }


}
