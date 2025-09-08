using UnityEngine;
using UnityEngine.InputSystem;

public class ArmAimer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform armPivot;            // 回転の中心（R_ArmPivot / L_ArmPivot）
    [SerializeField] private SpriteRenderer bodyRenderer;   // 体の向き参照（Body の SR）
    [SerializeField] private Transform bodyTransform;       // 未設定なら bodyRenderer.transform を使用
    [SerializeField] private Animator bodyAnimator;         // Body の Animator（Speed / Airborne を読む）

    [Header("Shoulder Offsets (Body local, right-facing)")]
    [SerializeField] private Vector2 shoulderOffsetIdle = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetWalk = new(0.20f, 0.85f);
    [SerializeField] private Vector2 shoulderOffsetJump = new(0.20f, 0.95f);
    [SerializeField] private float offsetLerpSpeed = 15f;   // オフセット切替の滑らかさ

    [Header("Animator Params (names)")]
    [SerializeField] private string speedParam = "Speed";       // float 0..1
    [SerializeField] private string airborneParam = "Airborne"; // bool

    [Header("Aim / Visual")]
    [SerializeField] private SpriteRenderer armRenderer;    // 腕のSR（flipY補正に使用）
    [SerializeField] private bool autoFlipYOnLeft = true;   // 左側を向いた時にflipYで見た目補正
    [Tooltip("腕スプライトの基準向き。右向きなら 0、左向き絵なら 180。")]
    [SerializeField] private float angleOffsetDeg = 0f;

    [Header("Lock-on Input")]
    public InputActionReference lockAction;                 // LB/RB などを割当（片腕ごとに）
    [SerializeField] private Key lockKeyFallback = Key.F;   // InputAction未設定時のフォールバックキー

    [Header("Lock-on Settings")]
    [SerializeField] private string targetTag = "Enemy";
    [SerializeField] private float lockRadius = 50f;

    [Header("Target Marker")]
    [SerializeField] private TargetMarker markerPrefab;
    [SerializeField] private Color markerTint = Color.white;

    [Header("Recoil (added on top of aim angle)")]
    [SerializeField] private float recoilRecoverDegPerSec = 600f; // 1秒あたり戻す角度

    // --------------- runtime state ---------------
    private Transform lockTarget;
    private TargetMarker marker;
    private Vector2 currentOffset;
    private float recoilAngle; // 一時的な“腕の跳ね上げ角”を加算

    private int speedHash;
    private int airborneHash;

    void Awake()
    {
        if (!bodyTransform && bodyRenderer) bodyTransform = bodyRenderer.transform;
        currentOffset = shoulderOffsetIdle;
        speedHash = Animator.StringToHash(speedParam);
        airborneHash = Animator.StringToHash(airborneParam);
    }

    void OnEnable() { lockAction?.action.Enable(); }
    void OnDisable() { lockAction?.action.Disable(); }

    void LateUpdate()
    {
        if (!armPivot || !bodyRenderer) return;

        // ---- 1) 肩位置：状態別オフセットを選んで、flipX に応じて左右ミラー ----
        Vector2 targetOffset = shoulderOffsetIdle;
        if (bodyAnimator)
        {
            bool airborne = bodyAnimator.GetBool(airborneHash);
            float speed = bodyAnimator.GetFloat(speedHash);
            if (airborne) targetOffset = shoulderOffsetJump;
            else if (speed > 0.01f) targetOffset = shoulderOffsetWalk;
        }
        // スムーズに切り替え
        float k = 1f - Mathf.Exp(-offsetLerpSpeed * Time.deltaTime);
        currentOffset = Vector2.Lerp(currentOffset, targetOffset, k);

        // 右向き基準オフセットを flipX に合わせてミラー → ワールド座標へ
        if (!bodyTransform) bodyTransform = bodyRenderer.transform;
        float ox = bodyRenderer.flipX ? -currentOffset.x : currentOffset.x;
        Vector3 local = new Vector3(ox, currentOffset.y, 0f);
        armPivot.position = bodyTransform.TransformPoint(local);

        // ---- 2) 入力：ロックオン切替（InputAction or Fallbackキー） ----
        bool toggle = false;
        if (lockAction) toggle = lockAction.action.WasPressedThisFrame();
        else if (Keyboard.current != null) toggle = Keyboard.current[lockKeyFallback].wasPressedThisFrame;
        if (toggle) ToggleLock();

        // ---- 3) 照準角（目標角 + リコイル角） & flipY補正 ----
        if (lockTarget)
        {
            Vector2 dir = (lockTarget.position - armPivot.position);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
            ang += recoilAngle; // ★リコイル加算
            armPivot.rotation = Quaternion.Euler(0, 0, ang);

            if (armRenderer && autoFlipYOnLeft)
            {
                bool facingLeft = (ang > 90f || ang < -90f);
                armRenderer.flipY = facingLeft;
                armRenderer.flipX = false;
            }

            // 範囲外/破棄で解除
            if (!lockTarget || dir.sqrMagnitude > lockRadius * lockRadius)
                ClearLock();
        }
        else
        {
            float baseAng = bodyRenderer.flipX ? 180f : 0f;
            float ang = baseAng + angleOffsetDeg + recoilAngle; // ★リコイル加算
            armPivot.rotation = Quaternion.Euler(0, 0, ang);

            if (armRenderer && autoFlipYOnLeft)
            {
                bool facingLeft = (ang > 90f || ang < -90f);
                armRenderer.flipY = facingLeft;
                armRenderer.flipX = false;
            }
        }

        // ---- 4) リコイル角を徐々に0へ戻す ----
        recoilAngle = Mathf.MoveTowards(recoilAngle, 0f, recoilRecoverDegPerSec * Time.deltaTime);
    }

    // ============== Public API (Gun2D から呼ぶ) ==============
    public void AddRecoilAngle(float deg)
    {
        recoilAngle += deg; // 例：大口径なら -10～-16 などを足す
    }

    // ============== Lock-on helpers ==============
    void ToggleLock()
    {
        if (lockTarget) { ClearLock(); return; }

        Transform nearest = null;
        float best = lockRadius * lockRadius;
        foreach (var e in GameObject.FindGameObjectsWithTag(targetTag))
        {
            float d2 = (e.transform.position - armPivot.position).sqrMagnitude;
            if (d2 < best) { best = d2; nearest = e.transform; }
        }
        lockTarget = nearest;

        if (lockTarget && markerPrefab)
        {
            if (!marker) marker = Instantiate(markerPrefab);
            marker.SetTarget(lockTarget);
            marker.SetTint(markerTint);
        }
    }

    void ClearLock()
    {
        lockTarget = null;
        if (marker) { Destroy(marker.gameObject); marker = null; }
    }

    // 視覚デバッグ（選択時に肩位置を表示）
    void OnDrawGizmosSelected()
    {
        if (!bodyRenderer) return;
        if (!bodyTransform) bodyTransform = bodyRenderer.transform;

        Vector2 use = Application.isPlaying ? currentOffset : shoulderOffsetIdle;
        float ox = (bodyRenderer.flipX ? -use.x : use.x);
        Vector3 p = bodyTransform.TransformPoint(new Vector3(ox, use.y, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, 0.045f);
    }
}
