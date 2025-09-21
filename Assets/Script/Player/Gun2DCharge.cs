using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2DCharge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Rigidbody2D bodyRb;
    [SerializeField] private Bullet2D normalBullet;
    [SerializeField] private Bullet2D chargedBullet;
    [SerializeField] private ArmAimer aimer;

    [Header("Input (RT)")]
    public InputActionReference fireAction;
    [SerializeField] private bool allowMouseFallback = true;

    [Header("Charge")]
    [SerializeField] private float chargeSeconds = 1.5f;   // チャージ成立まで

    [Header("Cooldowns")]
    [SerializeField] private float normalCooldown = 0.18f;
    [SerializeField] private float chargedCooldown = 0.35f;

    [Header("Recoil (Normal)")]
    [SerializeField] private float bodyKickSpeed_Normal = 2.5f;
    [SerializeField] private float armKickDeg_Normal = 14f;

    [Header("Recoil (Charged)")]
    [SerializeField] private float bodyKickSpeed_Charged = 5.5f; // ← 大きめに
    [SerializeField] private float armKickDeg_Charged = 28f;  // ← 大きめに

    [Header("SFX (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfxChargeStart;
    [SerializeField] private AudioClip sfxChargeLoop;
    [SerializeField] private AudioClip sfxFireNormal;
    [SerializeField] private AudioClip sfxFireCharged;
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.9f;

    [Header("Block")]
    [SerializeField] private PlayerHealthGuard guardRef;

    // ---- runtime ----
    float nextFireAt;
    bool charging;
    float chargeStart;
    BodyLocomotion2D locomotion;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (bodyRb) locomotion = bodyRb.GetComponent<BodyLocomotion2D>();

        // ArmAimer 自動解決（Gun2D と同じ流儀）
        if (!aimer && armPivot) aimer = armPivot.GetComponent<ArmAimer>();
        if (!aimer && armPivot) aimer = armPivot.GetComponentInChildren<ArmAimer>(true);
        if (!aimer && armPivot) aimer = armPivot.GetComponentInParent<ArmAimer>();
        if (!aimer) aimer = GetComponentInParent<ArmAimer>();
        if (!aimer) aimer = GetComponentInChildren<ArmAimer>(true);
    }

    void OnEnable() { fireAction?.action.Enable(); }
    void OnDisable() { fireAction?.action.Disable(); StopChargeLoop(); }

    void Update()
    {
        // 行動不可中は発射/チャージしない
        if (guardRef && (guardRef.IsGuarding || guardRef.IsHurt || guardRef.IsDashing || guardRef.IsDead))
        {
            charging = false;
            StopChargeLoop();
            return;
        }

        bool pressedDown = false, released = false, pressed = false;

        if (fireAction)
        {
            var act = fireAction.action;
            pressedDown = act.WasPressedThisFrame();
            released = act.WasReleasedThisFrame();
            pressed = act.IsPressed();
        }

        // マウス併用（任意）
        if (allowMouseFallback && Mouse.current != null)
        {
            pressedDown |= Mouse.current.leftButton.wasPressedThisFrame;
            released |= Mouse.current.leftButton.wasReleasedThisFrame;
            pressed |= Mouse.current.leftButton.isPressed;
        }

        if (pressedDown) BeginCharge();
        if (charging && !pressed) released = true; // フォーカス喪失等の保険
        if (charging && released) ReleaseToFire();
    }

    void BeginCharge()
    {
        charging = true;
        chargeStart = Time.time;

        if (audioSource && sfxChargeStart) audioSource.PlayOneShot(sfxChargeStart, 1f);
        if (audioSource && sfxChargeLoop)
        {
            audioSource.clip = sfxChargeLoop;
            audioSource.loop = true;
            audioSource.PlayDelayed(0.05f);
        }
    }

    void ReleaseToFire()
    {
        charging = false;
        StopChargeLoop();
        if (!muzzle) return;

        float held = Time.time - chargeStart;
        bool isCharged = held >= chargeSeconds;

        if (Time.time < nextFireAt) return;

        if (isCharged)
        {
            if (!chargedBullet) return;
            nextFireAt = Time.time + chargedCooldown;
            Instantiate(chargedBullet, muzzle.position, muzzle.rotation);
            ApplyRecoil(bodyKickSpeed_Charged, armKickDeg_Charged);
            if (audioSource && sfxFireCharged) audioSource.PlayOneShot(sfxFireCharged, shotVolume);
        }
        else
        {
            if (!normalBullet) return;
            nextFireAt = Time.time + normalCooldown;
            Instantiate(normalBullet, muzzle.position, muzzle.rotation);
            ApplyRecoil(bodyKickSpeed_Normal, armKickDeg_Normal);
            if (audioSource && sfxFireNormal) audioSource.PlayOneShot(sfxFireNormal, shotVolume);
        }
    }

    void ApplyRecoil(float bodyKickSpeed, float armKickDeg)
    {
        // 体ノックバック（Gun2D と同じ分岐）
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

        // 腕ノックバック（常に画面上方向へ）
        if (aimer != null)
        {
            aimer.AddRecoilUpwards(armKickDeg);
        }
    }

    void StopChargeLoop()
    {
        if (audioSource && audioSource.loop && audioSource.clip == sfxChargeLoop)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }
    }

    void OnValidate()
    {
        if (!armPivot) Debug.LogWarning("[Gun2DCharge] armPivot 未設定", this);
        if (!muzzle) Debug.LogWarning("[Gun2DCharge] muzzle 未設定", this);
        if (!normalBullet) Debug.LogWarning("[Gun2DCharge] normalBullet 未設定", this);
        if (!chargedBullet) Debug.LogWarning("[Gun2DCharge] chargedBullet 未設定", this);
        if (!bodyRb) Debug.LogWarning("[Gun2DCharge] bodyRb 未設定", this);
    }
}
