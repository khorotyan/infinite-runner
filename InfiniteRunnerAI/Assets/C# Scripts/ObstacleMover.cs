using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private float speed = 0;
    private float minX = -3.0f;
    private float maxX = 3.0f;
    private bool dirRight = true;

    private void Start()
    {
        float destroyTime = ObstacleSpawner.stepSize * 12 / PlayerController.speedY;

        Destroy(gameObject, destroyTime);
    }

    private void Update()
    {
        MoveObstacle();
    }

    // Moves the obstacles back and forth
    private void MoveObstacle()
    {
        if (dirRight)
            transform.position += new Vector3(speed, 0, 0) * Time.deltaTime;
        else
            transform.position += new Vector3(-speed, 0, 0) * Time.deltaTime;

        if (transform.position.x > maxX)
            dirRight = false;
        else if (transform.position.x < minX)
            dirRight = true;
    }
}
