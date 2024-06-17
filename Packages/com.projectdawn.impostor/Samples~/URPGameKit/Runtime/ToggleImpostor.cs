using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class ToggleImpostor : MonoBehaviour
{
    public bool ForceLod;
    ProfilerRecorder m_MainThread;

    void OnEnable()
    {
        m_MainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 60);
    }

    void OnDisable()
    {
        m_MainThread.Dispose();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ForceLod = !ForceLod;
            ForceLodForAllScene(ForceLod);
        }
    }

    void ForceLodForAllScene(bool state)
    {
        var lodGroups = GameObject.FindObjectsOfType<LODGroup>();
        foreach (var lodGroup in lodGroups)
        {
            if (state)
            {
                lodGroup.ForceLOD(0);
            }
            else
            {
                lodGroup.ForceLOD(-1);
            }
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 64;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperCenter;

        string text = ForceLod ? $"Level Geometry (ESC to switch)" : "HLOD w/ Impostor (ESC to switch)";
        text += $" {GetAverage(m_MainThread) * (1e-6f):F1}ms";
        GUI.Label(new Rect(0, Screen.height - 100, Screen.width, 200), text, style);
    }

    double GetAverage(ProfilerRecorder recorder)
    {
        double sum = 0;
        for (int i = 0; i < recorder.Count; i++)
        {
            sum += recorder.GetSample(i).Value;
        }
        return sum / recorder.Count;
    }
}
