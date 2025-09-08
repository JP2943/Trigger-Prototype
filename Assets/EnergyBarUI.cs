using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// ��ʍ���̃G�l���M�[�o�[���X�V����Ȉ�UI�B
/// Gun2DLeftEnergy �� Energy01 ���Q�Ƃ��� BarFill(Image) �� fillAmount ���X�V���܂��B
/// </summary>
public class EnergyBarUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Gun2DLeftEnergy source; // ���r�̔��˃X�N���v�g�����蓖��

    [Header("UI")]
    [SerializeField] private Image fill;             // BarFill(Image, Type=Filled, Horizontal)
    [SerializeField] private Image background;       // �C�ӁiBarBG�j

    [Header("Visuals")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.2f;
    [SerializeField] private bool smoothFill = true;
    [SerializeField, Range(0f, 20f)] private float smoothSpeed = 8f;
    [SerializeField] private Color normalColor = new Color(0.25f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.45f, 0.25f, 1f);
    [SerializeField] private Color overheatColor = new Color(0.95f, 0.2f, 0.2f, 1f);

    private float _displayAmount; // �X���[�Y�\���p
    // ����ꍇ�̂ݎg���iGun2DLeftEnergy �� public bool IsOverheated {get;} ����ő������Ƃ������Ή��j
    private PropertyInfo _propIsOverheated;

    void Awake()
    {
        if (!fill) Debug.LogWarning("[EnergyBarUI] fill(Image) �������ł��BBarFill �����蓖�ĂĂ��������B", this);
        if (!source)
        {
            // �Ō�̎�i�i�V�[���Ɉ�����Ȃ��z��j
            source = FindObjectOfType<Gun2DLeftEnergy>();
        }
        if (source != null)
        {
            _propIsOverheated = source.GetType().GetProperty("IsOverheated", BindingFlags.Instance | BindingFlags.Public);
            _displayAmount = source.Energy01; // �����l���킹
        }
    }

    void Update()
    {
        if (!source || !fill) return;

        float target = Mathf.Clamp01(source.Energy01);

        if (smoothFill)
        {
            // ���萔�^�X���[�W���O�i�t���[�����[�g��ˑ��j
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _displayAmount = Mathf.Lerp(_displayAmount, target, k);
        }
        else
        {
            _displayAmount = target;
        }

        fill.fillAmount = _displayAmount;

        // �F�ύX�i�I�[�o�[�q�[�g > �ቺ > �ʏ�̗D�揇�j
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

    // �G�f�B�^�Ŕz���R��̑������o
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
