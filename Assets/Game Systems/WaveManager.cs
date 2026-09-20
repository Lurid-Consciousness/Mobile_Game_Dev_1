using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveManager : MonoBehaviour
{
    public event System.Action<int> WaveCleared;

    public Enemy raccoonTemplate;
    public Enemy bearTemplate;
    public Enemy flyingTemplate;
    public TreeObjective tree;
    public PlayerController player;
    public int startingRaccoons = 3;
    public int raccoonsAddedPerWave = 1;
    public int bearUnlockWave = 3;
    public int bearGrowthRate = 3;
    public int flyingUnlockWave = 5;
    public int flyingGrowthRate = 4;
    public float spawnRadius = 12f;
    public float spawnDelay = 0.75f;
    public float timeBetweenWaves = 5f;

    public int CurrentWave { get; private set; }
    public int EnemiesRemaining => activeEnemies.Count;

    private List<Enemy> activeEnemies = new List<Enemy>();

    void Awake()
    {
        if (raccoonTemplate != null)
            raccoonTemplate.gameObject.SetActive(false);

        if (bearTemplate != null)
            bearTemplate.gameObject.SetActive(false);

        if (flyingTemplate != null)
            flyingTemplate.gameObject.SetActive(false);
    }

    IEnumerator Start()
    {
        if (raccoonTemplate == null || bearTemplate == null || flyingTemplate == null || tree == null || player == null)
        {
            Debug.LogError("Wave Manager is missing a reference.");
            yield break;
        }

        yield return new WaitForSeconds(1f);

        while (tree.IsAlive && player.IsAlive)
        {
            CurrentWave++;
            List<Enemy> wave = CreateWave();

            for (int i = 0; i < wave.Count; i++)
            {
                if (!tree.IsAlive || !player.IsAlive)
                    break;

                SpawnEnemy(wave[i]);
                yield return new WaitForSeconds(spawnDelay);
            }

            while (activeEnemies.Count > 0 && tree.IsAlive && player.IsAlive)
            {
                activeEnemies.RemoveAll(enemy => enemy == null);
                yield return null;
            }

            if (tree.IsAlive && player.IsAlive)
                WaveCleared?.Invoke(CurrentWave);

            if (tree.IsAlive && player.IsAlive)
                yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    List<Enemy> CreateWave()
    {
        List<Enemy> wave = new List<Enemy>();
        int raccoonCount = startingRaccoons + (CurrentWave - 1) * raccoonsAddedPerWave;
        int bearCount = CurrentWave >= bearUnlockWave ? Random.Range(1, 2 + (CurrentWave - bearUnlockWave) / Mathf.Max(1, bearGrowthRate)) : 0;
        int flyingCount = CurrentWave >= flyingUnlockWave ? Random.Range(1, 2 + (CurrentWave - flyingUnlockWave) / Mathf.Max(1, flyingGrowthRate)) : 0;

        AddEnemies(wave, raccoonTemplate, raccoonCount);
        AddEnemies(wave, bearTemplate, bearCount);
        AddEnemies(wave, flyingTemplate, flyingCount);

        for (int i = 0; i < wave.Count; i++)
        {
            int randomIndex = Random.Range(i, wave.Count);
            Enemy savedEnemy = wave[i];
            wave[i] = wave[randomIndex];
            wave[randomIndex] = savedEnemy;
        }

        return wave;
    }

    void AddEnemies(List<Enemy> wave, Enemy enemy, int amount)
    {
        for (int i = 0; i < amount; i++)
            wave.Add(enemy);
    }

    void SpawnEnemy(Enemy template)
    {
        Vector2 direction = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 position = tree.transform.position + new Vector3(direction.x, 0f, direction.y);

        if (template.GetComponent<FlyingEnemy>() != null)
            position.y = 6f;
        else
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                position = hit.position;

            position.y = template.GetComponent<BearEnemy>() != null ? 1.5f : 0.75f;
        }

        Enemy enemy = Instantiate(template, position, Quaternion.identity);
        enemy.gameObject.SetActive(true);
        activeEnemies.Add(enemy);
    }
}
