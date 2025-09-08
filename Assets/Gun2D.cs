using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;          // ��: R_ArmPivot / L_ArmPivot�i���̃X�N���v�g��ArmPivot�ɕt����̂���{�j
    [SerializeField] private Transform muzzle;            // ��: R_Muzzle / L_Muzzle�i���[�J��+X���e�������j
    [SerializeField] private Rigidbody2D bodyRb;          // Player�{�̂�Rigidbody2D
    [SerializeField] private Bullet2D bulletPrefab;       // �e�v���n�u

    [Header("Input (RT)")]
    public InputActionReference fireAction;               // PlayerControls.Gameplay/Fire�i<Gamepad>/rightTrigger�j
    [SerializeField] private bool allowMouseFallback = true; // �e�X�g�p�F�}�E�X���ł����Ă�

    [Header("Fire")]
    [SerializeField] private float fireCooldown = 0.18f;  // �A�ˊԊu(�b)

    [Header("Recoil")]
    [SerializeField] private float bodyKickSpeed = 2.5f;  // �̂֗^����g�ǉ����x�h�ʁi�ړ��ɍ����j
    [SerializeField] private float armKickDeg = 12f;    // �r�̒��ˏグ�p�i-�����ɑ����̂����R�j

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;     // ArmPivot�ɕt����AudioSource�i2D�����j
    [SerializeField] private AudioClip shotClip;          // ���ˉ�
    [SerializeField] private float shotVolume = 0.9f;

    // ---- runtime ----
    private float nextFireTime;
    private BodyLocomotion2D locomotion;  // �̃m�b�N�o�b�N�p�i����Ύg�p�j
    private ArmAimer aimer;               // �r�L�b�N�p�p�i����Ύg�p�j

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (bodyRb)
        {
            locomotion = bodyRb.GetComponent<BodyLocomotion2D>();
        }
        if (armPivot)
        {
            // ArmAimer �� ArmPivot ���ɕt���Ă���z��B�ʃI�u�W�F�N�g�ɂ���ꍇ��Inspector�ō����ւ���
            aimer = armPivot.GetComponent<ArmAimer>();
            if (!aimer) aimer = GetComponent<ArmAimer>();
        }
    }

    void OnEnable() { fireAction?.action.Enable(); }
    void OnDisable() { fireAction?.action.Disable(); }

    void Update()
    {
        bool pressed = false;

        if (fireAction != null)
            pressed = fireAction.action.WasPressedThisFrame();

        if (!pressed && allowMouseFallback && Mouse.current != null)
            pressed = Mouse.current.leftButton.wasPressedThisFrame;

        if (pressed) TryFire();
    }

    public void TryFire()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        // 1) �e�̐����imuzzle�ʒu / armPivot�����j
        if (bulletPrefab && muzzle && armPivot)
            Instantiate(bulletPrefab, muzzle.position, armPivot.rotation);

        // 2) �̃m�b�N�o�b�N�i�ړ��ɍ����j or �����C���p���X
        if (armPivot)
        {
            Vector2 back = -(Vector2)armPivot.right;

            if (locomotion != null)
            {
                locomotion.AddRecoil(back, bodyKickSpeed); // �����i�ړ��ƍ����j
            }
            else if (bodyRb != null)
            {
                bodyRb.AddForce(back * bodyKickSpeed, ForceMode2D.Impulse); // �t�H�[���o�b�N
            }
        }

        // 3) �r�L�b�N�p�iAimer ���ōŏI�p�ɉ��Z���������J�o�[�j
        if (aimer != null)
            aimer.AddRecoilAngle(-Mathf.Abs(armKickDeg)); // �}�C�i�X�����֒��ˏグ

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

    // �֗�: �C���X�y�N�^�ŎQ�ƘR�ꂪ����Όx��
    void OnValidate()
    {
        if (armPivot == null) Debug.LogWarning("[Gun2D] armPivot �����ݒ�ł��BArmPivot�ɖ{�X�N���v�g��t���AarmPivot�Ɏ��������蓖�ĂĂ��������B", this);
        if (muzzle == null) Debug.LogWarning("[Gun2D] muzzle �����ݒ�ł��BArmPivot�̎q��Muzzle(�e��)���쐬���Ċ��蓖�ĂĂ��������B", this);
        if (bulletPrefab == null) Debug.LogWarning("[Gun2D] bulletPrefab �����ݒ�ł��B�e�̃v���n�u�����蓖�ĂĂ��������B", this);
        if (bodyRb == null) Debug.LogWarning("[Gun2D] bodyRb �����ݒ�ł��BPlayer��Rigidbody2D�����蓖�ĂĂ��������B", this);
    }
}
