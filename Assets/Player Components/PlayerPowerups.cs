using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerups : MonoBehaviour
{
    [SerializeField] private float duration = 20f;
    [SerializeField] private float lockRange = 35f;
    [SerializeField] private float lockAngle = 45f;

    public float SprintRemaining { get; private set; }
    public float LockRemaining { get; private set; }
    public bool CanSprint => SprintRemaining > 0f;
    public List<Enemy> Targets { get; } = new List<Enemy>();

    private float nextScan;

    void Update()
    {
        SprintRemaining = Mathf.Max(0f, SprintRemaining - Time.deltaTime);
        LockRemaining = Mathf.Max(0f, LockRemaining - Time.deltaTime);
        if (LockRemaining <= 0f)
            Targets.Clear();
    }

    public void Collect(bool lockPowerup)
    {
        if (lockPowerup)
            LockRemaining = duration;
        else
            SprintRemaining = duration;
        GameAudio.PlayButton();
    }

    public void ClearTargets()
    {
        Targets.Clear();
        nextScan = 0f;
    }

    public void UpdateTargets(Vector3 aimPosition, Vector3 aimDirection, float charge)
    {
        if (LockRemaining <= 0f)
        {
            Targets.Clear();
            return;
        }
        if (Time.time < nextScan)
            return;
        nextScan = Time.time + 0.1f;

        Targets.RemoveAll(enemy => !CanLock(enemy, aimPosition, aimDirection));
        int count = 1 + Mathf.FloorToInt(Mathf.Clamp01(charge) * 3f);
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        while (Targets.Count < count)
        {
            Enemy best = null;
            float bestAngle = float.MaxValue;
            foreach (Enemy enemy in enemies)
            {
                if (Targets.Contains(enemy) || !CanLock(enemy, aimPosition, aimDirection))
                    continue;
                float angle = Vector3.Angle(aimDirection, enemy.transform.position - aimPosition);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = enemy;
                }
            }
            if (best == null)
                break;
            Targets.Add(best);
        }
    }

    private bool CanLock(Enemy enemy, Vector3 aimPosition, Vector3 aimDirection)
    {
        if (enemy == null || enemy.HealthPercent <= 0f || !enemy.gameObject.activeInHierarchy)
            return false;
        Vector3 offset = enemy.transform.position - aimPosition;
        if (offset.magnitude > lockRange || Vector3.Angle(aimDirection, offset) > lockAngle)
            return false;
        foreach (RaycastHit hit in Physics.RaycastAll(aimPosition, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PlayerController>() == GetComponent<PlayerController>())
                continue;
            if (hit.collider.GetComponentInParent<Enemy>() != enemy)
                return false;
        }
        return true;
    }
}
