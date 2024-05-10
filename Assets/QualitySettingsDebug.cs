using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualitySettingsDebug : MonoBehaviour
{
void Start() {
    // Set the quality level to 'Default', which is at index 0
    QualitySettings.SetQualityLevel(0);
}

}
