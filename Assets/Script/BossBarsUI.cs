using UnityEngine;
using UnityEngine.UI;

public class BossBarsUI : MonoBehaviour
{
    [Header("Refs")]
    public BossStats boss;
    public Image hpFill;
    public Image staminaFill;

    [Header("Colors")]
    public Color hpColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    public Color staminaColor = new Color(0.2f, 0.9f, 0.9f, 1f);

    void Reset() { if (!boss) boss = FindObjectOfType<BossStats>(); }

    void Update()
    {
        if (!boss) return;
        if (hpFill) { hpFill.fillAmount = boss.Hp01; hpFill.color = hpColor; }
        if (staminaFill) { staminaFill.fillAmount = boss.Stamina01; staminaFill.color = staminaColor; }
    }
}
