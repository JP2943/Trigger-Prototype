using UnityEngine;
using UnityEngine.UI;

/// プレイヤースタミナを Image(fillAmount) で表示
public class StaminaBarUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerHealthGuard source;

    [Header("UI")]
    [SerializeField] private Image fill;                // BarFill (Image, Type=Filled, Horizontal)
    [SerializeField] private Image background;          // 任意

    [Header("Visuals")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.2f;
    [SerializeField] private bool smoothFill = true;
    [SerializeField, Range(0f, 30f)] private float smoothSpeed = 10f;
    [SerializeField] private Color normalColor = new Color(0.3f, 0.85f, 1.0f, 1f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.9f, 0.35f, 1f);

    float _display;

    void Awake()
    {
        if (!source) source = FindObjectOfType<PlayerHealthGuard>();
        if (source) _display = source.Stamina01;
    }

    void Update()
    {
        if (!source || !fill) return;

        float target = Mathf.Clamp01(source.Stamina01);
        if (smoothFill)
        {
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _display = Mathf.Lerp(_display, target, k);
        }
        else _display = target;

        fill.fillAmount = _display;
        fill.color = (target <= lowThreshold) ? lowColor : normalColor;
    }

    void OnValidate()
    {
        if (fill && fill.type != Image.Type.Filled)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
