using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public Vector2 barSize = new Vector2(220f, 20f);

    private PlayerController playerController;
    private TreeObjective tree;
    private WaveManager waveManager;
    private AcornWallet wallet;
    private PlayerPowerups powerups;
    private float waveClearTimer;
    private int clearedWave;

    private GUIStyle pickupStyle;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        wallet = GetComponent<AcornWallet>();
        powerups = GetComponent<PlayerPowerups>();
        tree = FindAnyObjectByType<TreeObjective>();
        waveManager = FindAnyObjectByType<WaveManager>();

        if (waveManager != null)
            waveManager.WaveCleared += ShowWaveCleared;
    }

    void OnDestroy()
    {
        if (waveManager != null)
            waveManager.WaveCleared -= ShowWaveCleared;
    }

    void Update()
    {
        if (waveClearTimer > 0f)
            waveClearTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        DrawDot();

        if (playerController.CanPickup)
            DrawPickupPrompt();

        if (wallet != null)
            GUI.Label(new Rect(20f, 140f, 280f, 30f), $"Acorn points: {wallet.Acorns}");

        Defence barrier = playerController.TargetDefence;
        if (barrier != null)
        {
            string status = barrier.IsBroken ? "Broken barrier" : $"Barrier: {barrier.WallHealth * 100f:0}%";
            string action = barrier.NeedsRepair ? $"  {playerController.ButtonPrompt}: rebuild/repair ({barrier.RepairCost} points)" : "";
            GUI.Label(new Rect(Screen.width / 2f - 220f, Screen.height / 2f + 35f, 550f, 30f), status + action);
        }

        DrawBar(new Vector2(20f, 20f), playerController.HealthPercent, Color.red, "Health");
        if (playerController.boomerang != null)
            DrawBar(new Vector2(20f, 50f), playerController.boomerang.ChargeAmount, Color.yellow, "Charge");

        if (powerups != null)
        {
            if (powerups.CanSprint)
                GUI.Label(new Rect(Screen.width - 300f, 50f, 290f, 30f), $"Sprint: {powerups.SprintRemaining:0.0}s - hold Shift + move");
            if (powerups.LockRemaining > 0f)
                GUI.Label(new Rect(Screen.width - 300f, 80f, 290f, 30f), $"Multi-lock: {powerups.LockRemaining:0.0}s  {powerups.Targets.Count}/4");
            Camera camera = playerController.cameraTransform.GetComponent<Camera>();
            for (int i = 0; i < powerups.Targets.Count; i++)
            {
                Enemy enemy = powerups.Targets[i];
                if (enemy == null)
                    continue;
                Vector3 screen = camera.WorldToScreenPoint(enemy.transform.position + Vector3.up);
                if (screen.z > 0f)
                {
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(screen.x - 40f, Screen.height - screen.y - 20f, 100f, 30f), $"[LOCK {i + 1}]");
                    GUI.color = Color.white;
                }
            }
        }

        if (tree != null)
            DrawBar(new Vector2(20f, 80f), tree.HealthPercent, Color.green, "Tree");

        if (waveManager != null)
            GUI.Label(new Rect(20f, 110f, 250f, 30f), $"Wave {waveManager.CurrentWave}  Enemies {waveManager.EnemiesRemaining}");

        if (waveClearTimer > 0f)
            GUI.Label(new Rect(Screen.width * 0.5f - 100f, 50f, 200f, 30f), $"WAVE {clearedWave} CLEARED");

        if (!playerController.IsAlive || tree != null && !tree.IsAlive)
            GUI.Label(new Rect(Screen.width * 0.5f - 50f, 50f, 100f, 30f), "GAME OVER");
    }

    void ShowWaveCleared(int wave)
    {
        clearedWave = wave;
        waveClearTimer = 2.5f;
    }

    void DrawDot()
    {
        Rect outline = new Rect(Screen.width * 0.5f - 3f, Screen.height * 0.5f - 3f, 6f, 6f);
        Rect dot = new Rect(Screen.width * 0.5f - 2f, Screen.height * 0.5f - 2f, 4f, 4f);

        GUI.color = Color.black;
        GUI.DrawTexture(outline, Texture2D.blackTexture);

        GUI.color = Color.white;
        GUI.DrawTexture(dot, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawPickupPrompt()
    {
        if (pickupStyle == null)
        {
            pickupStyle = new GUIStyle(GUI.skin.label);
            pickupStyle.fontSize = 18;
            pickupStyle.alignment = TextAnchor.MiddleCenter;
            pickupStyle.normal.textColor = Color.blue;
        }

        string prompt = $"Press {playerController.ButtonPrompt} to pick up";

        Rect promptPosition = new Rect(Screen.width * 0.5f - 100f, Screen.height * 0.5f + 20f, 200f, 30f);

        GUI.Label(promptPosition, prompt, pickupStyle);
    }

    void DrawBar(Vector2 position, float amount, Color color, string label)
    {
        Rect background = new Rect(position.x, position.y, barSize.x, barSize.y);
        Rect fill = new Rect(position.x + 2f, position.y + 2f, (barSize.x - 4f) * Mathf.Clamp01(amount), barSize.y - 4f);

        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + 5f, position.y, barSize.x, barSize.y), label);
    }
}
