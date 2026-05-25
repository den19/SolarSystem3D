using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Tooltip("The Prefab to be spawned into the scene.")]
    public GameObject spawnPrefab = null;

    [Tooltip("The time between spawns")]
    public float spawnTime = 0.1f;

    // Пул объектов для переиспользования снарядов/астероидов
    private List<GameObject> pooledProjectiles = new List<GameObject>();
    private float nextSpawn = 0f;

    void Start()
    {
        nextSpawn = 0f;
        pooledProjectiles.Clear();
    }

    void Update()
    {
        nextSpawn += Time.deltaTime;

        if (nextSpawn > spawnTime)
        {
            SpawnProjectile();
            nextSpawn = 0f;
        }
    }

    private void SpawnProjectile()
    {
        if (spawnPrefab == null) return;

        GameObject projectile = GetPooledObject();
        if (projectile != null)
        {
            projectile.transform.position = transform.position;
            projectile.transform.rotation = transform.rotation;
            projectile.SetActive(true);
        }
    }

    private GameObject GetPooledObject()
    {
        // Ищем неактивный снаряд в пуле
        for (int i = 0; i < pooledProjectiles.Count; i++)
        {
            if (pooledProjectiles[i] != null && !pooledProjectiles[i].activeInHierarchy)
            {
                return pooledProjectiles[i];
            }
        }

        // Если все снаряды заняты или пул пуст — создаем новый
        GameObject newObj = Instantiate(spawnPrefab, transform.position, transform.rotation, null);
        pooledProjectiles.Add(newObj);
        return newObj;
    }
}
