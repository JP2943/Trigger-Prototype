using System.Linq;
using UnityEngine;

/// ガード/ダッシュ中は両腕のSpriteRendererだけを非表示にする
/// （ロジックは StateMachineBehaviour から SetHiddenByGuard/SetHiddenByDash を呼ぶ）
public class GuardVisualSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform rightArmRoot;   // 例: R_Shoulder
    [SerializeField] private Transform leftArmRoot;    // 例: L_Shoulder
    [SerializeField] private SpriteRenderer guardPoseSprite; // 任意（使っていなければ空でOK）

    SpriteRenderer[] _rightSprites, _leftSprites;
    bool _hiddenByGuard;
    bool _hiddenByDash;
    bool _appliedHidden;

    void Reset()
    {
        if (!rightArmRoot) rightArmRoot = transform.Find("R_Shoulder");
        if (!leftArmRoot) leftArmRoot = transform.Find("L_Shoulder");
    }

    void Awake()
    {
        _rightSprites = rightArmRoot ? rightArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        _leftSprites = leftArmRoot ? leftArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        Apply(false); // 初期は表示
    }

    /// Guard ステートの Enter/Exit から呼ぶ
    public void SetHiddenByGuard(bool on)
    {
        _hiddenByGuard = on;
        Apply(_hiddenByGuard || _hiddenByDash);
        // Guard用の一枚絵を使っている場合だけON/OFF（使っていなければ未設定でOK）
        if (guardPoseSprite) guardPoseSprite.enabled = on;
    }

    /// Dash ステートの Enter/Exit から呼ぶ
    public void SetHiddenByDash(bool on)
    {
        _hiddenByDash = on;
        Apply(_hiddenByGuard || _hiddenByDash);
    }

    void Apply(bool hide)
    {
        if (hide == _appliedHidden) return;
        foreach (var sr in _rightSprites.Where(s => s)) sr.enabled = !hide;
        foreach (var sr in _leftSprites.Where(s => s)) sr.enabled = !hide;
        _appliedHidden = hide;
    }
}
