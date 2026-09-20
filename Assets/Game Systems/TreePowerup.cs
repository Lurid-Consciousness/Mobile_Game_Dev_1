using UnityEngine;

public class TreePowerup : MonoBehaviour
{
    [SerializeField] private bool lockPowerup;
    [SerializeField] private float lifetime = 25f;
    private bool collected;

    public bool IsLockPowerup => lockPowerup;

    void Update()
    {
        lifetime -= Time.deltaTime;
        transform.Rotate(0f, 60f * Time.deltaTime, 0f);
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (collected || player == null || !player.enabled || !player.IsAlive || player.IsGliding)
            return;
        PlayerPowerups powerups = player.GetComponent<PlayerPowerups>();
        if (powerups == null)
            return;
        collected = true;
        powerups.Collect(lockPowerup);
        Destroy(gameObject);
    }
}
