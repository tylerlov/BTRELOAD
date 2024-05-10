using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateSpawner.Demo
{
    public class WaveHUDTimed : MonoBehaviour
    {
        // Private
        private WaveSpawnController waveController;
        private int currentWaveNumber = -1;

        // Public
        public Text waveText;
        public Text nextWaveText;
        public float waveTextDisplayTime = 2.0f; // Set this value in Inspector

        // Methods
        public void Start()
        {
            waveController = Component.FindObjectOfType<WaveSpawnController>();

            if (waveController != null)
            {
                waveController.OnWaveStarted.AddListener(OnWaveStarted);
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
                        waveText.text = string.Format("Wave {0}", currentWaveNumber);
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
