using UnityEngine;

public class AcornCollectible : MonoBehaviour
{
    [SerializeField] private int value = 10;
    private Rigidbody rb;
    private bool collected;
    private float readyTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        readyTime = Time.time + 1f;
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (collected || Time.time < readyTime || rb == null || rb.isKinematic
            || player == null || !player.enabled || !player.IsAlive || player.IsGliding)
            return;

        AcornWallet wallet = player.GetComponent<AcornWallet>();
        if (wallet == null)
            return;

        collected = true;
        wallet.Collect(value);
        GameAudio.PlayButton();
        Destroy(gameObject);
    }
}
