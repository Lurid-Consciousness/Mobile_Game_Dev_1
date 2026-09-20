using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public class MobileTouchStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField]
    private string controlPathValue;

    public float movementRange = 110f;
    public float centerThreshold = 0.3f;
    public float centerHoldTime = 2f;
    public float jumpTapTime = 0.35f;
    public float sprintActivationDistance = 210f;

    private RectTransform zoneRect;
    private RectTransform baseVisual;
    private RectTransform handleVisual;
    private RectTransform sprintTarget;
    private Image sprintTargetImage;
    private Text statusText;
    private VirtualButtonControl centerAction;
    private VirtualButtonControl tapAction;
    private VirtualButtonControl sprintAction;
    private Vector2 touchOrigin;
    private Vector2 stickValue;
    private int pointerId = -1;
    private float centerTimer;
    private float touchTimer;
    private float largestDistance;
    private bool pointerHeld;
    private bool centerPressed;
    private bool autoRun;
    private bool cancelledAutoRun;
    private bool movementStick;

    protected override string controlPathInternal
    {
        get => controlPathValue;
        set => controlPathValue = value;
    }

    public void Configure(bool controlsMovement, VirtualButtonControl holdAction, VirtualButtonControl quickTapAction, VirtualButtonControl runAction)
    {
        movementStick = controlsMovement;
        centerAction = holdAction;
        tapAction = quickTapAction;
        sprintAction = runAction;
        BuildVisuals();
    }

    void Update()
    {
        if (!pointerHeld || autoRun)
            return;

        touchTimer += Time.unscaledDeltaTime;

        if (stickValue.magnitude <= centerThreshold)
        {
            centerTimer += Time.unscaledDeltaTime;

            if (!centerPressed && centerTimer >= centerHoldTime && centerAction != null)
            {
                centerPressed = true;
                centerAction.SetPressed(true);
                statusText.text = movementStick ? "CROUCH" : "CHARGING";
            }
        }
        else if (!centerPressed)
        {
            centerTimer = 0f;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointerHeld)
            return;

        cancelledAutoRun = autoRun;

        if (autoRun)
            CancelAutoRun();

        pointerHeld = true;
        pointerId = eventData.pointerId;
        centerTimer = 0f;
        touchTimer = 0f;
        largestDistance = 0f;
        centerPressed = false;
        stickValue = Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        touchOrigin = ClampOrigin(localPoint);

        baseVisual.anchoredPosition = touchOrigin;
        baseVisual.gameObject.SetActive(true);
        handleVisual.anchoredPosition = Vector2.zero;

        if (sprintTarget != null)
        {
            sprintTarget.anchoredPosition = touchOrigin + Vector2.up * sprintActivationDistance;
            sprintTarget.gameObject.SetActive(true);
            sprintTargetImage.color = new Color(0.96f, 0.72f, 0.2f, 0.72f);
        }

        statusText.text = movementStick ? "TAP: JUMP   POWERUP: SPRINT" : "HOLD: CHARGE";
        SendValueToControl(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pointerHeld || eventData.pointerId != pointerId || autoRun)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        Vector2 movement = localPoint - touchOrigin;
        largestDistance = Mathf.Max(largestDistance, movement.magnitude / movementRange);

        if (movementStick && sprintAction != null && movement.y >= sprintActivationDistance && Mathf.Abs(movement.x) <= movementRange)
        {
            LockAutoRun();
            return;
        }

        Vector2 limitedMovement = Vector2.ClampMagnitude(movement, movementRange);
        stickValue = limitedMovement / movementRange;
        handleVisual.anchoredPosition = limitedMovement;
        SendValueToControl(stickValue);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerHeld || eventData.pointerId != pointerId)
            return;

        bool shouldJump = movementStick && !cancelledAutoRun && !autoRun && !centerPressed && touchTimer <= jumpTapTime && largestDistance <= centerThreshold;

        pointerHeld = false;
        pointerId = -1;

        if (centerAction != null)
            centerAction.SetPressed(false);

        centerPressed = false;

        if (shouldJump && tapAction != null)
            tapAction.Pulse();

        if (!autoRun)
        {
            stickValue = Vector2.zero;
            SendValueToControl(Vector2.zero);
            baseVisual.gameObject.SetActive(false);

            if (sprintTarget != null)
                sprintTarget.gameObject.SetActive(false);
        }
    }

    void LockAutoRun()
    {
        autoRun = true;
        stickValue = Vector2.up;
        SendValueToControl(stickValue);
        sprintAction.SetPressed(true);

        if (centerAction != null)
            centerAction.SetPressed(false);

        centerPressed = false;
        handleVisual.anchoredPosition = Vector2.up * movementRange;
        sprintTargetImage.color = new Color(0.25f, 0.9f, 0.3f, 0.9f);
        statusText.text = "AUTO RUN\nTOUCH LEFT SIDE TO CANCEL";
    }

    void CancelAutoRun()
    {
        autoRun = false;
        stickValue = Vector2.zero;
        SendValueToControl(Vector2.zero);

        if (sprintAction != null)
            sprintAction.SetPressed(false);
    }

    Vector2 ClampOrigin(Vector2 point)
    {
        float horizontalPadding = movementRange + 20f;
        float bottomPadding = movementRange + 20f;
        float topPadding = movementRange + (movementStick ? sprintActivationDistance + 80f : 20f);

        point.x = Mathf.Clamp(point.x, zoneRect.rect.xMin + horizontalPadding, zoneRect.rect.xMax - horizontalPadding);
        point.y = Mathf.Clamp(point.y, zoneRect.rect.yMin + bottomPadding, zoneRect.rect.yMax - topPadding);
        return point;
    }

    void BuildVisuals()
    {
        zoneRect = GetComponent<RectTransform>();
        Image zoneImage = GetComponent<Image>();
        zoneImage.color = Color.clear;
        zoneImage.raycastTarget = true;

        GameObject baseObject = new GameObject("Stick Base", typeof(RectTransform), typeof(Image));
        baseObject.transform.SetParent(transform, false);
        baseVisual = baseObject.GetComponent<RectTransform>();
        baseVisual.anchorMin = new Vector2(0.5f, 0.5f);
        baseVisual.anchorMax = new Vector2(0.5f, 0.5f);
        baseVisual.pivot = new Vector2(0.5f, 0.5f);
        baseVisual.sizeDelta = new Vector2(250f, 250f);
        baseObject.GetComponent<Image>().color = movementStick ? new Color(0.2f, 0.65f, 0.25f, 0.32f) : new Color(0.2f, 0.45f, 0.85f, 0.32f);

        GameObject centerObject = new GameObject("Hold Zone", typeof(RectTransform), typeof(Image));
        centerObject.transform.SetParent(baseObject.transform, false);
        RectTransform centerRect = centerObject.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(75f, 75f);
        centerObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
        CreateGlyph(centerObject.transform, "UI/Glyphs/Kenney/Touch/touch_tap_hold");

        GameObject handleObject = new GameObject("Stick Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(baseObject.transform, false);
        handleVisual = handleObject.GetComponent<RectTransform>();
        handleVisual.anchorMin = new Vector2(0.5f, 0.5f);
        handleVisual.anchorMax = new Vector2(0.5f, 0.5f);
        handleVisual.pivot = new Vector2(0.5f, 0.5f);
        handleVisual.sizeDelta = new Vector2(90f, 90f);
        handleObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.68f);

        GameObject statusObject = new GameObject("Status", typeof(RectTransform), typeof(Text));
        statusObject.transform.SetParent(baseObject.transform, false);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0f, -165f);
        statusRect.sizeDelta = new Vector2(420f, 70f);
        statusText = statusObject.GetComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 19;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.text = movementStick ? "TAP: JUMP   HOLD: CROUCH" : "HOLD: CHARGE";

        if (movementStick)
        {
            GameObject sprintObject = new GameObject("Auto Run Target", typeof(RectTransform), typeof(Image));
            sprintObject.transform.SetParent(transform, false);
            sprintTarget = sprintObject.GetComponent<RectTransform>();
            sprintTarget.anchorMin = new Vector2(0.5f, 0.5f);
            sprintTarget.anchorMax = new Vector2(0.5f, 0.5f);
            sprintTarget.pivot = new Vector2(0.5f, 0.5f);
            sprintTarget.sizeDelta = new Vector2(190f, 85f);
            sprintTargetImage = sprintObject.GetComponent<Image>();
            sprintTargetImage.color = new Color(0.96f, 0.72f, 0.2f, 0.72f);

            GameObject sprintTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            sprintTextObject.transform.SetParent(sprintObject.transform, false);
            RectTransform sprintTextRect = sprintTextObject.GetComponent<RectTransform>();
            sprintTextRect.anchorMin = Vector2.zero;
            sprintTextRect.anchorMax = Vector2.one;
            sprintTextRect.offsetMin = Vector2.zero;
            sprintTextRect.offsetMax = Vector2.zero;
            Text sprintText = sprintTextObject.GetComponent<Text>();
            sprintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sprintText.fontSize = 22;
            sprintText.alignment = TextAnchor.MiddleCenter;
            sprintText.color = Color.black;
            sprintText.text = "AUTO RUN";
        }

        baseVisual.gameObject.SetActive(false);

        if (sprintTarget != null)
            sprintTarget.gameObject.SetActive(false);
    }

    void CreateGlyph(Transform parent, string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);

        if (texture == null)
            return;

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
        glyphObject.transform.SetParent(parent, false);

        RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
        glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
        glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
        glyphRect.pivot = new Vector2(0.5f, 0.5f);
        glyphRect.sizeDelta = new Vector2(58f, 58f);

        Image glyph = glyphObject.GetComponent<Image>();
        glyph.sprite = sprite;
        glyph.preserveAspect = true;
        glyph.raycastTarget = false;
    }

    protected override void OnDisable()
    {
        pointerHeld = false;
        pointerId = -1;
        autoRun = false;
        stickValue = Vector2.zero;

        if (centerAction != null)
            centerAction.SetPressed(false);

        if (sprintAction != null)
            sprintAction.SetPressed(false);

        SendValueToControl(Vector2.zero);
        base.OnDisable();
    }
}
