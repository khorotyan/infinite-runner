using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyPlatform : MonoBehaviour
{
    private void Start()
    {
        float destroyTime = ObstacleSpawner.stepSize * 12 / PlayerController.speedY;

        Destroy(gameObject, destroyTime);
    }
}
