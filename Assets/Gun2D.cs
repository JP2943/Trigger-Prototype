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
    [SerializeField] private float armKickDeg = 14f;  // ← 正の値（上方向に跳ね上げる“強さ”）

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

        // ArmAimer 自動解決（手動割当でもOK）
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

        // 1) 弾生成
        Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

        // 2) 体ノックバック
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

        // 3) 腕リコイル（常に“画面上方向”へ）
        if (aimer != null)
        {
            aimer.AddRecoilUpwards(armKickDeg);  // ← 符号は自動で決定
        }

        // 4) 発射音
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
        if (!armPivot) Debug.LogWarning("[Gun2D] armPivot 未設定", this);
        if (!muzzle) Debug.LogWarning("[Gun2D] muzzle 未設定", this);
        if (!bulletPrefab) Debug.LogWarning("[Gun2D] bulletPrefab 未設定", this);
        if (!bodyRb) Debug.LogWarning("[Gun2D] bodyRb 未設定", this);
    }
}
