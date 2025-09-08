using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;          // 例: R_ArmPivot / L_ArmPivot（このスクリプトはArmPivotに付けるのが基本）
    [SerializeField] private Transform muzzle;            // 例: R_Muzzle / L_Muzzle（ローカル+Xが銃口方向）
    [SerializeField] private Rigidbody2D bodyRb;          // Player本体のRigidbody2D
    [SerializeField] private Bullet2D bulletPrefab;       // 弾プレハブ

    [Header("Input (RT)")]
    public InputActionReference fireAction;               // PlayerControls.Gameplay/Fire（<Gamepad>/rightTrigger）
    [SerializeField] private bool allowMouseFallback = true; // テスト用：マウス左でも撃てる

    [Header("Fire")]
    [SerializeField] private float fireCooldown = 0.18f;  // 連射間隔(秒)

    [Header("Recoil")]
    [SerializeField] private float bodyKickSpeed = 2.5f;  // 体へ与える“追加速度”量（移動に合成）
    [SerializeField] private float armKickDeg = 12f;    // 腕の跳ね上げ角（-方向に足すのが自然）

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;     // ArmPivotに付けたAudioSource（2D推奨）
    [SerializeField] private AudioClip shotClip;          // 発射音
    [SerializeField] private float shotVolume = 0.9f;

    // ---- runtime ----
    private float nextFireTime;
    private BodyLocomotion2D locomotion;  // 体ノックバック用（あれば使用）
    private ArmAimer aimer;               // 腕キック角用（あれば使用）

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (bodyRb)
        {
            locomotion = bodyRb.GetComponent<BodyLocomotion2D>();
        }
        if (armPivot)
        {
            // ArmAimer が ArmPivot 側に付いている想定。別オブジェクトにある場合はInspectorで差し替えを
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

        // 1) 弾の生成（muzzle位置 / armPivot向き）
        if (bulletPrefab && muzzle && armPivot)
            Instantiate(bulletPrefab, muzzle.position, armPivot.rotation);

        // 2) 体ノックバック（移動に合成） or 物理インパルス
        if (armPivot)
        {
            Vector2 back = -(Vector2)armPivot.right;

            if (locomotion != null)
            {
                locomotion.AddRecoil(back, bodyKickSpeed); // 推奨（移動と合成）
            }
            else if (bodyRb != null)
            {
                bodyRb.AddForce(back * bodyKickSpeed, ForceMode2D.Impulse); // フォールバック
            }
        }

        // 3) 腕キック角（Aimer 側で最終角に加算＆自動リカバー）
        if (aimer != null)
            aimer.AddRecoilAngle(-Mathf.Abs(armKickDeg)); // マイナス方向へ跳ね上げ

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

    // 便利: インスペクタで参照漏れがあれば警告
    void OnValidate()
    {
        if (armPivot == null) Debug.LogWarning("[Gun2D] armPivot が未設定です。ArmPivotに本スクリプトを付け、armPivotに自分を割り当ててください。", this);
        if (muzzle == null) Debug.LogWarning("[Gun2D] muzzle が未設定です。ArmPivotの子にMuzzle(銃口)を作成して割り当ててください。", this);
        if (bulletPrefab == null) Debug.LogWarning("[Gun2D] bulletPrefab が未設定です。弾のプレハブを割り当ててください。", this);
        if (bodyRb == null) Debug.LogWarning("[Gun2D] bodyRb が未設定です。PlayerのRigidbody2Dを割り当ててください。", this);
    }
}
