using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class to make projectiles move
/// </summary>
public class Projectile : MonoBehaviour
{
    
    [Tooltip("The distance this projectile will move each second in meters.")]
    public float projectileSpeed = 3.0f;

    [Tooltip("How far away from the main camera before destroying the projectile gameobject in meters.")]
    public float destroyDistance = 2000.0f;

    private void Update()
    {
        MoveProjectile();
    }

    private void MoveProjectile()
    {
        // move the transform
        transform.position = transform.position + transform.forward * projectileSpeed * Time.deltaTime;

        if (Camera.main != null)
        {
            // calculate the distance from the main camera
            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);

            // if the distance is greater than the destroyDistance
            if (dist > destroyDistance)
            {
                gameObject.SetActive(false); // deactivate instead of destroy
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}