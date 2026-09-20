using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public Vector2 barSize = new Vector2(320f, 32f);

    private PlayerController playerController;
    private TreeObjective tree;
    private WaveManager waveManager;
    private AcornWallet wallet;
    private PlayerPowerups powerups;
    private float waveClearTimer;
    private int clearedWave;

    private GUIStyle pickupStyle;
    private GUIStyle hudStyle;
    private GUIStyle centeredStyle;
    private Font hudFont;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        wallet = GetComponent<AcornWallet>();
        powerups = GetComponent<PlayerPowerups>();
        tree = FindAnyObjectByType<TreeObjective>();
        waveManager = FindAnyObjectByType<WaveManager>();
        hudFont = Resources.Load<Font>("UI/Fonts/AtkinsonHyperlegible-Bold");

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
        EnsureStyles();
        DrawDot();

        if (playerController.CanPickup)
            DrawPickupPrompt();

        if (wallet != null)
            DrawLabel(new Rect(20f, 185f, 380f, 45f), $"Acorn points: {wallet.Acorns}", hudStyle);

        Defence barrier = playerController.TargetDefence;
        if (barrier != null)
        {
            string status = barrier.IsBroken ? "Broken barrier" : $"Barrier: {barrier.WallHealth * 100f:0}%";
            string action = barrier.NeedsRepair ? $"  {playerController.ButtonPrompt}: rebuild/repair ({barrier.RepairCost} points)" : "";
            DrawLabel(new Rect(Screen.width / 2f - 340f, CrosshairScreenY + 45f, 680f, 50f), status + action, centeredStyle);
        }

        DrawBar(new Vector2(20f, 20f), playerController.HealthPercent, Color.red, "Health");
        if (playerController.boomerang != null)
            DrawBar(new Vector2(20f, 50f), playerController.boomerang.ChargeAmount, Color.yellow, "Charge");

        if (powerups != null)
        {
            if (powerups.CanSprint)
                DrawLabel(new Rect(Screen.width - 440f, 65f, 420f, 45f), $"Sprint: {powerups.SprintRemaining:0.0}s", hudStyle);
            if (powerups.LockRemaining > 0f)
                DrawLabel(new Rect(Screen.width - 440f, 110f, 420f, 45f), $"Multi-lock: {powerups.LockRemaining:0.0}s  {powerups.Targets.Count}/4", hudStyle);
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
                    DrawLabel(new Rect(screen.x - 65f, Screen.height - screen.y - 25f, 130f, 45f), $"[LOCK {i + 1}]", centeredStyle);
                    GUI.color = Color.white;
                }
            }
        }

        if (tree != null)
            DrawBar(new Vector2(20f, 80f), tree.HealthPercent, Color.green, "Tree");

        if (waveManager != null)
            DrawLabel(new Rect(20f, 140f, 420f, 45f), $"Wave {waveManager.CurrentWave}  Enemies {waveManager.EnemiesRemaining}", hudStyle);

        if (waveClearTimer > 0f)
            DrawLabel(new Rect(Screen.width * 0.5f - 220f, 50f, 440f, 60f), $"WAVE {clearedWave} CLEARED", centeredStyle);

        if (!playerController.IsAlive || tree != null && !tree.IsAlive)
            DrawLabel(new Rect(Screen.width * 0.5f - 180f, 50f, 360f, 60f), "GAME OVER", centeredStyle);
    }

    void ShowWaveCleared(int wave)
    {
        clearedWave = wave;
        waveClearTimer = 2.5f;
    }

    void DrawDot()
    {
        Rect outline = new Rect(Screen.width * 0.5f - 7f, CrosshairScreenY - 7f, 14f, 14f);
        Rect dot = new Rect(Screen.width * 0.5f - 4f, CrosshairScreenY - 4f, 8f, 8f);

        GUI.color = Color.black;
        GUI.DrawTexture(outline, Texture2D.blackTexture);

        GUI.color = Color.white;
        GUI.DrawTexture(dot, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawPickupPrompt()
    {
        string prompt = $"Press {playerController.ButtonPrompt} to pick up";

        Rect promptPosition = new Rect(Screen.width * 0.5f - 250f, CrosshairScreenY + 22f, 500f, 55f);

        DrawLabel(promptPosition, prompt, pickupStyle);
    }

    void DrawBar(Vector2 position, float amount, Color color, string label)
    {
        Rect background = new Rect(position.x, position.y, barSize.x, barSize.y);
        Rect fill = new Rect(position.x + 2f, position.y + 2f, (barSize.x - 4f) * Mathf.Clamp01(amount), barSize.y - 4f);

        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = Color.white;
        DrawLabel(new Rect(position.x + 8f, position.y, barSize.x, barSize.y), label, hudStyle);
    }

    float CrosshairScreenY => Screen.height * (1f - playerController.aimViewportY);

    void EnsureStyles()
    {
        if (hudStyle != null)
            return;

        hudStyle = new GUIStyle(GUI.skin.label);
        hudStyle.font = hudFont;
        hudStyle.fontSize = 28;
        hudStyle.normal.textColor = Color.white;
        hudStyle.alignment = TextAnchor.MiddleLeft;

        centeredStyle = new GUIStyle(hudStyle);
        centeredStyle.fontSize = 32;
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        pickupStyle = new GUIStyle(centeredStyle);
        pickupStyle.fontSize = 30;
        pickupStyle.normal.textColor = new Color(1f, 0.82f, 0.18f);
    }

    void DrawLabel(Rect rect, string text, GUIStyle style)
    {
        Color textColor = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
    }
}
