using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Framerate : MonoBehaviour {

    public static Framerate Instance { get; private set; }


    private TextMeshProUGUI textMeshProUGUI;
    private int lastFrameIndex;
    private float[] frameDeltaTimeArray;

    private void Awake() {
        Instance = this;

        frameDeltaTimeArray = new float[50];

        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
    }

    private void Update() {
        frameDeltaTimeArray[lastFrameIndex] = Time.unscaledDeltaTime;
        lastFrameIndex = (lastFrameIndex + 1) % frameDeltaTimeArray.Length;

        textMeshProUGUI.text = Mathf.Round(CalculateFPS()).ToString();
    }

    private float CalculateFPS() {
        float total = 0f;
        foreach (float deltaTime in frameDeltaTimeArray) {
            total += deltaTime;
        }
        return frameDeltaTimeArray.Length / total;
    }

    public float GetFPS() {
        return CalculateFPS();
    }

}