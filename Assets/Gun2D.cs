using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Rigidbody2D bodyRb;
    [SerializeField] private Bullet2D bulletPrefab;

    [Header("Input (RT)")]
    public InputActionReference fireAction;
    [SerializeField] private bool allowMouseFallback = true;

    [Header("Fire")]
    [SerializeField] private float fireCooldown = 0.18f;

    [Header("Recoil")]
    [SerializeField] private float bodyKickSpeed = 2.5f;
    [SerializeField] private float armKickDeg = 14f;  // �� ���̒l�i������ɒ��ˏグ��g�����h�j

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shotClip;
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.9f;

    [SerializeField] private ArmAimer aimer;

    private float nextFireAt;
    private BodyLocomotion2D locomotion;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (bodyRb) locomotion = bodyRb.GetComponent<BodyLocomotion2D>();

        // ArmAimer ���������i�蓮�����ł�OK�j
        if (!aimer && armPivot) aimer = armPivot.GetComponent<ArmAimer>();
        if (!aimer && armPivot) aimer = armPivot.GetComponentInChildren<ArmAimer>(true);
        if (!aimer && armPivot) aimer = armPivot.GetComponentInParent<ArmAimer>();
        if (!aimer) aimer = GetComponentInParent<ArmAimer>();
        if (!aimer) aimer = GetComponentInChildren<ArmAimer>(true);
    }

    void OnEnable() { fireAction?.action.Enable(); }
    void OnDisable() { fireAction?.action.Disable(); }

    void Update()
    {
        bool pressed = false;
        if (fireAction != null) pressed = fireAction.action.WasPressedThisFrame();
        if (!pressed && allowMouseFallback && Mouse.current != null)
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
        if (pressed) TryFire();
    }

    void TryFire()
    {
        if (Time.time < nextFireAt) return;
        if (!muzzle || !bulletPrefab) return;

        nextFireAt = Time.time + fireCooldown;

        // 1) �e����
        Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

        // 2) �̃m�b�N�o�b�N
        if (locomotion != null)
        {
            Vector2 back = -muzzle.right;
            locomotion.AddRecoil(back, bodyKickSpeed);
        }
        else if (bodyRb != null)
        {
            Vector2 back = -muzzle.right * bodyKickSpeed;
            bodyRb.AddForce(back, ForceMode2D.Impulse);
        }

        // 3) �r���R�C���i��Ɂg��ʏ�����h�ցj
        if (aimer != null)
        {
            aimer.AddRecoilUpwards(armKickDeg);  // �� �����͎����Ō���
        }

        // 4) ���ˉ�
        if (shotClip)
        {
            if (audioSource) audioSource.PlayOneShot(shotClip, shotVolume);
            else
            {
                Vector3 pos = muzzle ? muzzle.position :
                               (Camera.main ? Camera.main.transform.position : Vector3.zero);
                AudioSource.PlayClipAtPoint(shotClip, pos, shotVolume);
            }
        }
    }

    void OnValidate()
    {
        if (!armPivot) Debug.LogWarning("[Gun2D] armPivot ���ݒ�", this);
        if (!muzzle) Debug.LogWarning("[Gun2D] muzzle ���ݒ�", this);
        if (!bulletPrefab) Debug.LogWarning("[Gun2D] bulletPrefab ���ݒ�", this);
        if (!bodyRb) Debug.LogWarning("[Gun2D] bodyRb ���ݒ�", this);
    }
}
