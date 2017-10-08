using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstacleParent;
    public GameObject platformParent;
    public GameObject wallParent;

    public GameObject obstacle;
    public GameObject platform;
    public GameObject wall;

    public static float stepSize = 10f;

    private float spawnCooldown;
    private float currTime = 0;

    private float prevPos = 100f;

    private void Awake()
    {
        spawnCooldown = stepSize / PlayerController.speedY;
    }

    private void Update()
    {
        Spawn();
    }

    // Spawn the platform and the obstacles at some intervals
    private void Spawn()
    {
        if (currTime < spawnCooldown)
        {
            currTime += 1 * Time.deltaTime;
        }
        else
        {
            currTime = 0;
            prevPos += stepSize;

            float obsXPos = Random.Range(-3.0f, 3.0f);
            float obsYDistort = Random.Range(-3.5f, 3.5f);

            if (Mathf.Abs(obsXPos) > 2)
            {
                obsXPos = Mathf.Sign(obsXPos) * 3;
            }
            else if (Mathf.Abs(obsXPos) > 1 && Mathf.Abs(obsXPos) <= 2)
            {
                obsXPos = Mathf.Sign(obsXPos) * 1;
            }

            Instantiate(platform, new Vector3(0, 0, prevPos), Quaternion.identity, platformParent.transform);
            Instantiate(wall, new Vector3(0, 0, prevPos), Quaternion.identity, wallParent.transform);

            Instantiate(obstacle, new Vector3(obsXPos, 0.5f, prevPos + obsYDistort), Quaternion.identity, obstacleParent.transform);
        }
    }
}
