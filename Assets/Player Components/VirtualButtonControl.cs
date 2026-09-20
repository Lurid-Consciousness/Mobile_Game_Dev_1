using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class VirtualButtonControl : OnScreenControl
{
    [InputControl(layout = "Button")]
    [SerializeField]
    private string controlPathValue;

    private bool isPressed;

    protected override string controlPathInternal
    {
        get => controlPathValue;
        set => controlPathValue = value;
    }

    public void SetPressed(bool value)
    {
        if (isPressed == value)
            return;

        isPressed = value;
        SendValueToControl(value ? 1f : 0f);
    }

    public void Pulse()
    {
        StartCoroutine(PulseInput());
    }

    IEnumerator PulseInput()
    {
        SetPressed(true);
        yield return null;
        SetPressed(false);
    }

    protected override void OnDisable()
    {
        SetPressed(false);
        base.OnDisable();
    }
}
