using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Gun2DLeftEnergy : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;        // L_ArmPivot を割当
    [SerializeField] private Transform muzzle;          // 左腕の Muzzle（ローカル+Xが射出方向）
    [SerializeField] private Bullet2D bulletPrefab;     // Bullet_L（Variant）を割当
    [SerializeField] private ArmAimer aimer;            // 左腕の ArmAimer（任意。腕の跳ね上げを使うなら）

    [Header("Input (LT hold-to-fire)")]
    public InputActionReference fireAction;             // Gameplay/FireLeft（LT）
    [SerializeField] private bool allowMouseFallback = false; // テストで右クリック発射するなら true

    [Header("Fire")]
    [SerializeField, Tooltip("連射間隔(秒)。例: 0.08 なら 12.5発/秒")]
    private float fireInterval = 0.08f;
    [SerializeField, Tooltip("撃った時の腕の跳ね上げ角度。0で無効")]
    private float armKickDeg = 8f;

    [Header("Energy")]
    [SerializeField] private float energyMax = 100f;
    [SerializeField, Tooltip("開始時エネルギー(通常は最大と同じ)")]
    private float energy = 100f;
    [SerializeField, Tooltip("1発あたり消費量")]
    private float shotCost = 5f;
    [SerializeField, Tooltip("非発射時の回復量(毎秒)")]
    private float regenPerSecond = 15f;
    [SerializeField, Tooltip("発射後に回復が始まるまでの遅延(秒)")]
    private float regenDelayAfterShot = 0.15f;

    [Header("Overheat")]
    [SerializeField, Tooltip("エネルギーを撃ち切ったら overheat を発生させる")]
    private bool useOverheat = true;
    [SerializeField, Tooltip("オーバーヒートの固定クールダウン(秒)")]
    private float overheatCooldown = 1.5f;
    [SerializeField, Tooltip("オーバーヒート中も回復を許可するか")]
    private bool regenerateDuringOverheat = true;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;   // 任意
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip overheatClip;    // 任意（オーバーヒートになった瞬間）
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.9f;

    // 内部状態
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
        // 入力（押しっぱなし対応）
        bool pressed = false;
        if (fireAction != null) pressed = fireAction.action.IsPressed();
        if (!pressed && allowMouseFallback && Mouse.current != null)
            pressed = Mouse.current.rightButton.isPressed;

        // オーバーヒート解除
        if (_overheat && Time.time >= _overheatUntil)
            _overheat = false;

        // 発射
        if (pressed && Time.time >= _nextFireAt && !_overheat)
        {
            if (energy >= shotCost && bulletPrefab && muzzle)
            {
                FireOnce();
            }
            else
            {
                // 弾切れ → オーバーヒート開始
                if (useOverheat && !_overheat)
                {
                    StartOverheat();
                }
            }
        }

        // 回復（非発射時、一定遅延後／オーバーヒート中は設定次第）
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

        // 弾生成（左腕の Bullet_L Variant）
        Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);

        // 腕の跳ね上げ（任意。数値0で無効）
        if (aimer && armKickDeg != 0f)
        {
            // 右腕と同様に常に“上方向”へ
            float deg = Mathf.Abs(armKickDeg);
            // ArmAimerの拡張API（前回追加したもの）に合わせる：無ければ AddRecoilAngle(+/-) に置換
            var addUp = aimer.GetType().GetMethod("AddRecoilUpwards");
            if (addUp != null) addUp.Invoke(aimer, new object[] { deg });
            else aimer.SendMessage("AddRecoilAngle", deg, SendMessageOptions.DontRequireReceiver);
        }

        // エネルギー消費
        energy = Mathf.Max(0f, energy - shotCost);

        // SFX
        if (shotClip)
        {
            if (audioSource) audioSource.PlayOneShot(shotClip, shotVolume);
            else AudioSource.PlayClipAtPoint(shotClip, muzzle ? muzzle.position : Vector3.zero, shotVolume);
        }

        // 0到達でオーバーヒート
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

    // UI用に公開
    public float Energy01 => energy <= 0f ? 0f : energy / energyMax;

    void OnValidate()
    {
        if (shotCost < 0f) shotCost = 0f;
        if (regenPerSecond < 0f) regenPerSecond = 0f;
        if (fireInterval < 0.02f) fireInterval = 0.02f;
        if (energyMax < 1f) energyMax = 1f;
        energy = Mathf.Clamp(energy, 0f, energyMax);

        if (!muzzle) Debug.LogWarning("[Gun2DLeftEnergy] muzzle 未設定（左腕の銃口 Transform を割当）", this);
        if (!bulletPrefab) Debug.LogWarning("[Gun2DLeftEnergy] bulletPrefab 未設定（Bullet_L Variant を割当）", this);
        if (fireAction == null) Debug.LogWarning("[Gun2DLeftEnergy] fireAction 未設定（Gameplay/FireLeft を割当）", this);
    }
}
