using UnityEngine;
using UnityEngine.InputSystem; // New Input System 用

/// <summary>
/// Playモード中に P キーで BossGorilla の攻撃を再生するだけのデバッグ用トリガー。
/// シーン内の Empty に付けて使います。
/// </summary>
public class AttackTestTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BossGorillaAttack target; // BossGorilla ルートに付いているコンポーネントを割当

    [Header("Hotkey")]
    [SerializeField] private bool editorOnly = false;   // エディタのみ有効にしたい場合
    [SerializeField] private bool logFire = true;       // 押した時にログを出す

    void Awake()
    {
        // 未割当なら自動でシーン中から探す（1体だけ想定）
        if (!target)
        {
#if UNITY_2023_1_OR_NEWER
            // 推奨API（Unity 2023.1+）
            target = Object.FindFirstObjectByType<BossGorillaAttack>(FindObjectsInactive.Exclude);
#else
            // 旧API（フォールバック）
#pragma warning disable CS0618
            target = Object.FindObjectOfType<BossGorillaAttack>();
#pragma warning restore CS0618
#endif
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (editorOnly && !Application.isEditor) return;
#endif
        // New Input System（推奨）
        var kbd = Keyboard.current;
        if (kbd != null && kbd.pKey.wasPressedThisFrame)
        {
            if (target)
            {
                target.DoAttack();
                if (logFire) Debug.Log("[AttackTestTrigger] P pressed -> DoAttack()");
            }
            else
            {
                Debug.LogWarning("[AttackTestTrigger] target が未割当です。BossGorillaAttack をアサインしてください。", this);
            }
        }

        // 予備の旧Input（プロジェクトが Both 設定の場合のみ反応）
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (target) target.DoAttack();
        }
    }

    void OnValidate()
    {
        if (!target)
        {
#if UNITY_2023_1_OR_NEWER
            target = Object.FindFirstObjectByType<BossGorillaAttack>(FindObjectsInactive.Exclude);
#else
#pragma warning disable CS0618
            target = Object.FindObjectOfType<BossGorillaAttack>();
#pragma warning restore CS0618
#endif
        }

        if (!target)
            Debug.LogWarning("[AttackTestTrigger] target 未割当", this);
    }
}
