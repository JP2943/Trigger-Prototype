using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// 画面左上のエネルギーバーを更新する簡易UI。
/// Gun2DLeftEnergy の Energy01 を参照して BarFill(Image) の fillAmount を更新します。
/// </summary>
public class EnergyBarUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Gun2DLeftEnergy source; // 左腕の発射スクリプトを割り当て

    [Header("UI")]
    [SerializeField] private Image fill;             // BarFill(Image, Type=Filled, Horizontal)
    [SerializeField] private Image background;       // 任意（BarBG）

    [Header("Visuals")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.2f;
    [SerializeField] private bool smoothFill = true;
    [SerializeField, Range(0f, 20f)] private float smoothSpeed = 8f;
    [SerializeField] private Color normalColor = new Color(0.25f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.45f, 0.25f, 1f);
    [SerializeField] private Color overheatColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    private float _displayAmount; // スムーズ表示用
    // ある場合のみ使う（Gun2DLeftEnergy に public bool IsOverheated {get;} を後で足したとき自動対応）
    private PropertyInfo _propIsOverheated;

    void Awake()
    {
        if (!fill) Debug.LogWarning("[EnergyBarUI] fill(Image) 未割当です。BarFill を割り当ててください。", this);
        if (!source)
        {
            // 最後の手段（シーンに一個しかない想定）
            source = FindObjectOfType<Gun2DLeftEnergy>();
        }
        if (source != null)
        {
            _propIsOverheated = source.GetType().GetProperty("IsOverheated", BindingFlags.Instance | BindingFlags.Public);
            _displayAmount = source.Energy01; // 初期値合わせ
        }
    }

    void Update()
    {
        if (!source || !fill) return;

        float target = Mathf.Clamp01(source.Energy01);

        if (smoothFill)
        {
            // 時定数型スムージング（フレームレート非依存）
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _displayAmount = Mathf.Lerp(_displayAmount, target, k);
        }
        else
        {
            _displayAmount = target;
        }

        fill.fillAmount = _displayAmount;

        // 色変更（オーバーヒート > 低下 > 通常の優先順）
        bool overheated = false;
        if (_propIsOverheated != null)
        {
            object v = _propIsOverheated.GetValue(source, null);
            if (v is bool b) overheated = b;
        }

        if (overheated)
            fill.color = overheatColor;
        else if (target <= lowThreshold)
            fill.color = lowColor;
        else
            fill.color = normalColor;
    }

    // エディタで配線漏れの早期検出
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
