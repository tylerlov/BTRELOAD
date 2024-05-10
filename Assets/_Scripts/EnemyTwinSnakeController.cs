using UnityEngine;
using Chronos; // Chronos namespace for time control
using System.Collections;

public class EnemyTwinSnakeController : MonoBehaviour
{
    public GameObject snake1;
    public GameObject snake2;

    private Clock globalClock1;
    private Clock globalClock2;
    private Clock testClock; // Clock to monitor for changes

    public bool Snake1Time = true;
    public bool Snake2Time = true;

    private bool previousSnake1Time;
    private bool previousSnake2Time;

    private bool isShootingSnake1 = true; // Toggle to decide which snake shoots next

    void Start()
    {
        globalClock1 = Timekeeper.instance.Clock("Boss Time 1");
        globalClock2 = Timekeeper.instance.Clock("Boss Time 2");
        testClock = Timekeeper.instance.Clock("Test"); // Initialize the test clock

        previousSnake1Time = Snake1Time;
        previousSnake2Time = Snake2Time;

        SetClockDirection(globalClock1, Snake1Time);
        SetClockDirection(globalClock2, Snake2Time);

        StartCoroutine(AlternateShooting());
        StartCoroutine(MonitorTestClock()); // Start monitoring the test clock
    }

    IEnumerator MonitorTestClock()
    {
        float previousTestTimeScale = testClock.localTimeScale;
        while (true)
        {
            if (testClock.localTimeScale != previousTestTimeScale && testClock.localTimeScale >= 0)
            {
                SetClockTimeScale(globalClock1, testClock.localTimeScale);
                SetClockTimeScale(globalClock2, testClock.localTimeScale);
                previousTestTimeScale = testClock.localTimeScale;
            }
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }

    IEnumerator AlternateShooting()
    {
        while (true)
        {
            // Determine which snake is currently set to shoot
            EnemyTwinSnakeBoss shootingSnake = isShootingSnake1 ? snake1.GetComponent<EnemyTwinSnakeBoss>() : snake2.GetComponent<EnemyTwinSnakeBoss>();
            EnemyTwinSnakeBoss otherSnake = isShootingSnake1 ? snake2.GetComponent<EnemyTwinSnakeBoss>() : snake1.GetComponent<EnemyTwinSnakeBoss>();

            // Check if the current shooting snake has a Koreographer event ID assigned
            if (!shootingSnake.HasShootEventID())
            {
                // If no Koreographer event ID is assigned, proceed with manual shooting
                shootingSnake.ShootProjectile();

                // Wait for the shootInterval of the current snake before allowing the next snake to shoot
                yield return new WaitForSeconds(shootingSnake.ShootInterval);

                // Toggle the flag to alternate shooting only if the other snake does not have a Koreographer event ID assigned
                if (!otherSnake.HasShootEventID())
                {
                    isShootingSnake1 = !isShootingSnake1;
                }
            }
            else
            {
                // If a Koreographer event ID is assigned, do not alternate based on time.
                // Just wait a short period before checking again to keep the coroutine alive without doing anything.
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void Update()
    {
        if (Snake1Time != previousSnake1Time)
        {
            SetClockDirection(globalClock1, Snake1Time);
            previousSnake1Time = Snake1Time;
        }
        if (Snake2Time != previousSnake2Time)
        {
            SetClockDirection(globalClock2, Snake2Time);
            previousSnake2Time = Snake2Time;
        }

    }

    private void OnSnakeDamage(int snakeNumber)
    {
        if (snakeNumber == 1)
        {
            Snake1Time = !Snake1Time;
            SetClockDirection(globalClock1, Snake1Time);
        }
        else if (snakeNumber == 2)
        {
            Snake2Time = !Snake2Time;
            SetClockDirection(globalClock2, Snake2Time);
        }
    }

    private void SetClockTimeScale(Clock clock, float timeScale)
    {
        clock.localTimeScale = timeScale;
    }

     private void SetClockDirection(Clock clock, bool forward)
    {
        clock.localTimeScale = forward ? 1 : -1;
    }
}
