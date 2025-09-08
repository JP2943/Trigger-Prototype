using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2DLeftEnergy : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;        // L_ArmPivot ������
    [SerializeField] private Transform muzzle;          // ���r�� Muzzle�i���[�J��+X���ˏo�����j
    [SerializeField] private Bullet2D bulletPrefab;     // Bullet_L�iVariant�j������
    [SerializeField] private ArmAimer aimer;            // ���r�� ArmAimer�i�C�ӁB�r�̒��ˏグ���g���Ȃ�j

    [Header("Input (LT hold-to-fire)")]
    public InputActionReference fireAction;             // Gameplay/FireLeft�iLT�j
    [SerializeField] private bool allowMouseFallback = false; // �e�X�g�ŉE�N���b�N���˂���Ȃ� true

    [Header("Fire")]
    [SerializeField, Tooltip("�A�ˊԊu(�b)�B��: 0.08 �Ȃ� 12.5��/�b")]
    private float fireInterval = 0.08f;
    [SerializeField, Tooltip("���������̘r�̒��ˏグ�p�x�B0�Ŗ���")]
    private float armKickDeg = 8f;

    [Header("Energy")]
    [SerializeField] private float energyMax = 100f;
    [SerializeField, Tooltip("�J�n���G�l���M�[(�ʏ�͍ő�Ɠ���)")]
    private float energy = 100f;
    [SerializeField, Tooltip("1������������")]
    private float shotCost = 5f;
    [SerializeField, Tooltip("�񔭎ˎ��̉񕜗�(���b)")]
    private float regenPerSecond = 15f;
    [SerializeField, Tooltip("���ˌ�ɉ񕜂��n�܂�܂ł̒x��(�b)")]
    private float regenDelayAfterShot = 0.15f;

    [Header("Overheat")]
    [SerializeField, Tooltip("�G�l���M�[�������؂����� overheat �𔭐�������")]
    private bool useOverheat = true;
    [SerializeField, Tooltip("�I�[�o�[�q�[�g�̌Œ�N�[���_�E��(�b)")]
    private float overheatCooldown = 1.5f;
    [SerializeField, Tooltip("�I�[�o�[�q�[�g�����񕜂������邩")]
    private bool regenerateDuringOverheat = true;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;   // �C��
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip overheatClip;    // �C�Ӂi�I�[�o�[�q�[�g�ɂȂ����u�ԁj
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.9f;

    // �������
    private float _nextFireAt;
    private float _regenBlockedUntil;
    private bool _overheat;
    private float _overheatUntil;

    void Reset()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!armPivot) armPivot = transform;
        energy = Mathf.Clamp(energy, 0f, energyMax);
    }

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        energy = Mathf.Clamp(energy, 0f, energyMax);
    }

    void OnEnable() { fireAction?.action.Enable(); }
    void OnDisable() { fireAction?.action.Disable(); }

    void Update()
    {
        // ���́i�������ςȂ��Ή��j
        bool pressed = false;
        if (fireAction != null) pressed = fireAction.action.IsPressed();
        if (!pressed && allowMouseFallback && Mouse.current != null)
            pressed = Mouse.current.rightButton.isPressed;

        // �I�[�o�[�q�[�g����
        if (_overheat && Time.time >= _overheatUntil)
            _overheat = false;

        // ����
        if (pressed && Time.time >= _nextFireAt && !_overheat)
        {
            if (energy >= shotCost && bulletPrefab && muzzle)
            {
                FireOnce();
            }
            else
            {
                // �e�؂� �� �I�[�o�[�q�[�g�J�n
                if (useOverheat && !_overheat)
                {
                    StartOverheat();
                }
            }
        }

        // �񕜁i�񔭎ˎ��A���x����^�I�[�o�[�q�[�g���͐ݒ莟��j
        bool canRegen = !pressed && Time.time >= _regenBlockedUntil;
        if (_overheat && regenerateDuringOverheat) canRegen = true;

        if (canRegen && energy < energyMax)
        {
            energy = Mathf.Min(energyMax, energy + regenPerSecond * Time.deltaTime);
        }
    }

    private void FireOnce()
    {
        _nextFireAt = Time.time + fireInterval;
        _regenBlockedUntil = Time.time + regenDelayAfterShot;

        // �e�����i���r�� Bullet_L Variant�j
        Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

        // �r�̒��ˏグ�i�C�ӁB���l0�Ŗ����j
        if (aimer && armKickDeg != 0f)
        {
            // �E�r�Ɠ��l�ɏ�Ɂg������h��
            float deg = Mathf.Abs(armKickDeg);
            // ArmAimer�̊g��API�i�O��ǉ��������́j�ɍ��킹��F������� AddRecoilAngle(+/-) �ɒu��
            var addUp = aimer.GetType().GetMethod("AddRecoilUpwards");
            if (addUp != null) addUp.Invoke(aimer, new object[] { deg });
            else aimer.SendMessage("AddRecoilAngle", deg, SendMessageOptions.DontRequireReceiver);
        }

        // �G�l���M�[����
        energy = Mathf.Max(0f, energy - shotCost);

        // SFX
        if (shotClip)
        {
            if (audioSource) audioSource.PlayOneShot(shotClip, shotVolume);
            else AudioSource.PlayClipAtPoint(shotClip, muzzle ? muzzle.position : Vector3.zero, shotVolume);
        }

        // 0���B�ŃI�[�o�[�q�[�g
        if (useOverheat && energy <= 0f && !_overheat)
        {
            StartOverheat();
        }
    }

    private void StartOverheat()
    {
        _overheat = true;
        _overheatUntil = Time.time + overheatCooldown;

        if (overheatClip)
        {
            if (audioSource) audioSource.PlayOneShot(overheatClip, 1f);
            else AudioSource.PlayClipAtPoint(overheatClip, muzzle ? muzzle.position : Vector3.zero, 1f);
        }
    }

    // UI�p�Ɍ��J
    public float Energy01 => energy <= 0f ? 0f : energy / energyMax;

    void OnValidate()
    {
        if (shotCost < 0f) shotCost = 0f;
        if (regenPerSecond < 0f) regenPerSecond = 0f;
        if (fireInterval < 0.02f) fireInterval = 0.02f;
        if (energyMax < 1f) energyMax = 1f;
        energy = Mathf.Clamp(energy, 0f, energyMax);

        if (!muzzle) Debug.LogWarning("[Gun2DLeftEnergy] muzzle ���ݒ�i���r�̏e�� Transform �������j", this);
        if (!bulletPrefab) Debug.LogWarning("[Gun2DLeftEnergy] bulletPrefab ���ݒ�iBullet_L Variant �������j", this);
        if (fireAction == null) Debug.LogWarning("[Gun2DLeftEnergy] fireAction ���ݒ�iGameplay/FireLeft �������j", this);
    }
}
