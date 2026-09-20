using UnityEngine;

public class GliderLaunchPad : MonoBehaviour
{
    [SerializeField] private GliderController glider;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private PlayerController player;
    [SerializeField] private float useDistance = 5f;
    [SerializeField] private int startingCost = 50;
    [SerializeField] private int extraCostPerWave = 15;

    private bool canLaunch;
    private WaveManager waves;

    public int Cost => startingCost + Mathf.Max(0, (waves != null ? waves.CurrentWave : 1) - 1) * extraCostPerWave;

    void Awake()
    {
        waves = FindAnyObjectByType<WaveManager>();
    }

    void Update()
    {
        canLaunch = player != null && glider != null && launchPoint != null
            && player.enabled && player.IsAlive && !glider.IsFlying && Time.timeScale > 0f
            && Vector3.Distance(player.transform.position, transform.position) <= useDistance;

    }

    public bool TryUse(PlayerController pilot)
    {
        if (pilot != player || !canLaunch || glider.IsFlying || pilot.IsGliding || !pilot.enabled)
            return false;

        AcornWallet wallet = player.GetComponent<AcornWallet>();
        if (wallet != null && wallet.TrySpend(Cost))
            glider.Launch(player, launchPoint);
        return true;
    }

    void OnGUI()
    {
        if (glider == null || player == null || !player.enabled || Time.timeScale <= 0f)
            return;

        if (glider.IsFlying)
        {
            string stall = glider.IsStalling ? "  STALL - LOWER THE NOSE" : "";
            GUI.Label(new Rect(20f, 175f, 800f, 30f),
                $"Glider: {glider.TimeRemaining:0}s  Speed: {glider.Speed:0.0} m/s  Alt: {glider.Altitude:0.0} m  V/S: {glider.VerticalSpeed:+0.0;-0.0;0.0}{stall}");
            GUI.Label(new Rect(20f, 200f, 700f, 30f), "W: nose down / gain speed   S: nose up / climb   A/D: bank and turn");
        }
        else if (canLaunch)
            GUI.Label(new Rect(Screen.width / 2f - 170f, Screen.height / 2f + 60f, 440f, 30f), $"{player.ButtonPrompt}: launch glider ({Cost} acorn points)");
    }
}
