using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; // SortingGroup

public class BossUnblockableAttackEvents : MonoBehaviour
{
    [Header("Refs")]
    public AttackHitbox2D handHitbox;
    public GameObject warnMarkPrefab;
    public Transform warnMarkAnchor;
    public AudioSource sfx;

    [Header("Warning")]
    public float warnYOffset = 0.6f;
    public float warnSeconds = 0.5f;

    [Header("Warning Sorting (�O�ʕ\��)")]
    public string warnSortingLayer = "Default";
    public int warnOrderInLayer = 9999;
    public bool forceRenderQueueTransparent = true;

    [Header("Warning Orientation (��]�ݒ�)")]
    [Tooltip("true: �e(��)�ɑ΂��郍�[�J����]��0�ɂ��� / false: �G��Ȃ�")]
    public bool zeroLocalRotation = true;
    [Tooltip("true: ���[���h��]��0(Quaternion.identity)�ɂ���B�e���X���Ă��Ă���ʏ�͐����Ɍ��������ꍇ")]
    public bool zeroWorldRotation = false;
    [Tooltip("true: �\���������ƃ��[���h��]��0�ɌŒ肷��i�肪�����Ă��X���Ȃ��j")]
    public bool lockWorldRotationWhileVisible = false;

    [Header("State Control")]
    public Animator bossAnimator;
    public string idleStateName = "Idle";
    public string readyStateName = "ReadyToPunch";
    public string goReadyTrigger = "GoReady";
    public string backIdleTrigger = "BackIdle";
    public bool autoGoReadyWhenPlayerIsLeft = true;
    public float holdAfterAttackSeconds = 2f;

    [Header("Player")]
    public Transform player;

    GameObject _warnInstance;
    bool _returningToIdle;
    Transform BossRoot => bossAnimator ? bossAnimator.transform : transform;

    void Reset() { warnMarkAnchor = transform; }
    void Awake() { if (!warnMarkAnchor) warnMarkAnchor = transform; }

    void Update()
    {
        if (!autoGoReadyWhenPlayerIsLeft || !bossAnimator || !player) return;

        if (IsInState(bossAnimator, idleStateName))
        {
            float dx = player.position.x - BossRoot.position.x;
            if (dx < 0f)
            {
                bossAnimator.ResetTrigger(backIdleTrigger);
                bossAnimator.SetTrigger(goReadyTrigger);
            }
        }
    }

    void LateUpdate()
    {
        if (lockWorldRotationWhileVisible && _warnInstance && _warnInstance.activeSelf)
        {
            _warnInstance.transform.rotation = Quaternion.identity;   // ��ɐ���
            var p = _warnInstance.transform.localPosition;
            _warnInstance.transform.localPosition = new Vector3(p.x, warnYOffset, p.z);
        }
    }

    // === Animation Events ===
    public void Ev_ShowWarn()
    {
        if (!_warnInstance && warnMarkPrefab)
        {
            _warnInstance = Instantiate(warnMarkPrefab, warnMarkAnchor ? warnMarkAnchor : transform);
            ApplySortingToAll(_warnInstance, warnSortingLayer, warnOrderInLayer, forceRenderQueueTransparent);
        }

        if (_warnInstance)
        {
            var t = _warnInstance.transform;

            // �ʒu
            t.localPosition = Vector3.up * warnYOffset;

            // �� ��]���Z�b�g�i�p�r�ɉ����Ăǂ��炩/�����j
            if (zeroWorldRotation) t.rotation = Quaternion.identity;           // ���[���h��]��0
            if (zeroLocalRotation) t.localRotation = Quaternion.identity;      // �e�ɑ΂����]��0

            if (!_warnInstance.activeSelf) _warnInstance.SetActive(true);
            if (warnSeconds > 0f && !lockWorldRotationWhileVisible) Invoke(nameof(HideWarn), warnSeconds);
        }

        if (sfx) sfx.Play();
    }

    public void Ev_HitboxOn_Unblockable()
    {
        HideWarn();
        if (handHitbox)
        {
            handHitbox.unblockable = true;
            handHitbox.justGuardable = true;
            handHitbox.SetActive(true);
        }
    }

    public void Ev_HitboxOff()
    {
        if (handHitbox) handHitbox.SetActive(false);

        if (!_returningToIdle && bossAnimator && holdAfterAttackSeconds > 0f)
        {
            StartCoroutine(ReturnToIdleAfterHold());
        }
        else
        {
            TryBackToIdle();
        }
    }

    void HideWarn()
    {
        if (_warnInstance && _warnInstance.activeSelf) _warnInstance.SetActive(false);
    }

    IEnumerator ReturnToIdleAfterHold()
    {
        _returningToIdle = true;
        yield return new WaitForSeconds(holdAfterAttackSeconds);
        TryBackToIdle();
        _returningToIdle = false;
    }

    void TryBackToIdle()
    {
        if (!bossAnimator) return;
        bossAnimator.ResetTrigger(goReadyTrigger);
        bossAnimator.SetTrigger(backIdleTrigger);
    }

    // utils
    static bool IsInState(Animator anim, string stateName)
    {
        if (!anim) return false;
        var st = anim.GetCurrentAnimatorStateInfo(0);
        return st.IsName(stateName);
    }

    static void ApplySortingToAll(GameObject root, string layerName, int order, bool forceTransparentQueue)
    {
        int layerID = SortingLayer.NameToID(layerName);
        if (layerID == 0 && layerName != "Default") layerID = SortingLayer.NameToID("Default");

        var sg = root.GetComponent<SortingGroup>();
        if (!sg) sg = root.AddComponent<SortingGroup>();
        sg.sortingLayerID = layerID;
        sg.sortingOrder = order;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.sortingLayerID = layerID;
            r.sortingOrder = order;

            if (forceTransparentQueue)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (!mat) continue;
                    if (mat.renderQueue < 3000) mat.renderQueue = 3000;
                }
            }
        }
    }
}
