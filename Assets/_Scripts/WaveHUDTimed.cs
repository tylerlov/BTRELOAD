using System.Collections;
using Febucci.UI;
using TMPro; // Changed from UnityEngine.UI to TMPro
using UnityEngine;

namespace UltimateSpawner.Demo
{
    public class WaveHUDTimed : MonoBehaviour
    {
        // Private
        private WaveSpawnController waveController;
        private int currentWaveNumber = -1;

        // Public
        public TextMeshProUGUI waveText; // Changed from TextAnimatorPlayer to TextMeshProUGUI
        public TextMeshProUGUI nextWaveText; // Changed from Text to TextMeshProUGUI
        public float waveTextDisplayTime = 2.0f; // Set this value in Inspector

        private const float CONTROLLER_CHECK_INTERVAL = 0.5f; // Interval to check for the controller

        // Methods
        public void Start()
        {
            StartCoroutine(InitializeWaveController());
        }

        private IEnumerator InitializeWaveController()
        {
            while (waveController == null)
            {
                waveController = FindObjectOfType<WaveSpawnController>();
                if (waveController != null)
                {
                    waveController.OnWaveStarted.AddListener(OnWaveStarted);
                    break;
                }
                yield return new WaitForSeconds(CONTROLLER_CHECK_INTERVAL);
            }
        }

        public void Update()
        {
            if (waveController != null)
            {
                if (waveController.CurrentWave != currentWaveNumber)
                {
                    currentWaveNumber = waveController.CurrentWave;

                    if (currentWaveNumber < 1)
                    {
                        waveText.enabled = false;
                    }
                    else
                    {
                        waveText.enabled = true;
                        waveText.text = "<shake>ACTIVATE</shake>"; // Directly set text
                        StartCoroutine(DisableWaveTextAfterTime(waveTextDisplayTime));

                        OnWaveStarted();
                    }
                }
            }
        }

        private void OnWaveStarted()
        {
            if (currentWaveNumber > 1)
                StartCoroutine(ShowNextWaveHint());
        }

        private IEnumerator ShowNextWaveHint()
        {
            nextWaveText.color = Color.white;
            nextWaveText.enabled = true;

            yield return new WaitForSeconds(waveTextDisplayTime);

            WaitForSeconds wait = new WaitForSeconds(0.1f);

            Color temp = nextWaveText.color;

            while (temp.a > 0)
            {
                temp.a -= 0.05f;
                nextWaveText.color = temp;

                yield return wait;
            }
        }

        private IEnumerator DisableWaveTextAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            waveText.enabled = false;
        }
    }
}
