using System.Linq;
using UnityEngine;

public class GuardVisualSwitcher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerHealthGuard playerGuard; // プレイヤーのガード状態を参照
    [SerializeField] private Transform rightArmRoot;        // 例: R_Shoulder
    [SerializeField] private Transform leftArmRoot;         // 例: L_Shoulder
    [SerializeField] private SpriteRenderer guardPoseSprite;// guard用の一枚絵(任意)。Animatorで切替済みなら空でOK

    // 内部
    SpriteRenderer[] _rightSprites, _leftSprites;
    bool _applied;

    void Reset()
    {
        if (!playerGuard) playerGuard = GetComponent<PlayerHealthGuard>();
        if (!rightArmRoot) rightArmRoot = transform.Find("R_Shoulder");
        if (!leftArmRoot) leftArmRoot = transform.Find("L_Shoulder");
    }

    void Awake()
    {
        if (!playerGuard) playerGuard = GetComponent<PlayerHealthGuard>();
        _rightSprites = rightArmRoot ? rightArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        _leftSprites = leftArmRoot ? leftArmRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        Apply(false); // 初期は非ガード想定
    }

    public void SetGuardVisual(bool guarding) => Apply(guarding);

    void Apply(bool guarding)
    {
        // 腕の見た目だけ非表示（スクリプトやコライダーは生かす）
        foreach (var sr in _rightSprites.Where(s => s)) sr.enabled = !guarding;
        foreach (var sr in _leftSprites.Where(s => s)) sr.enabled = !guarding;

        // Guard用一枚絵（使うならON/OFF）
        if (guardPoseSprite) guardPoseSprite.enabled = guarding;

        _applied = guarding;
    }
}
