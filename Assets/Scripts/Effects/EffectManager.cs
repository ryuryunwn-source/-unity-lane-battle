using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("エフェクト設定")]
    public GameObject attackLinePrefab;  // 攻撃ライン用
    public GameObject damageTextPrefab;  // ダメージ数字
    public Canvas effectCanvas;

    [Header("攻撃エフェクト色")]
    public Color attackLineColor = new Color(1f, 0.4f, 0f, 0.9f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 攻撃エフェクト: 攻撃者から対象へ線を引き、コールバックを呼ぶ
    public void PlayAttackEffect(Vector3 from, Vector3 to, Action onComplete)
    {
        StartCoroutine(AttackEffectCoroutine(from, to, onComplete));
    }

    private IEnumerator AttackEffectCoroutine(Vector3 from, Vector3 to, Action onComplete)
    {
        // シンプルな移動エフェクト（小さな球を飛ばす）
        if (attackLinePrefab != null && effectCanvas != null)
        {
            GameObject fx = Instantiate(attackLinePrefab, effectCanvas.transform);
            RectTransform rt = fx.GetComponent<RectTransform>();

            Vector2 fromScreen = RectTransformUtility.WorldToScreenPoint(null, from);
            Vector2 toScreen = RectTransformUtility.WorldToScreenPoint(null, to);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectCanvas.GetComponent<RectTransform>(), fromScreen, null, out Vector2 fromLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                effectCanvas.GetComponent<RectTransform>(), toScreen, null, out Vector2 toLocal);

            rt.anchoredPosition = fromLocal;

            float duration = 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = Vector2.Lerp(fromLocal, toLocal, t / duration);
                yield return null;
            }
            Destroy(fx);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        onComplete?.Invoke();
        ShowDamageText(to);
    }

    private void ShowDamageText(Vector3 pos)
    {
        if (damageTextPrefab == null || effectCanvas == null) return;

        GameObject dmgObj = Instantiate(damageTextPrefab, effectCanvas.transform);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, pos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectCanvas.GetComponent<RectTransform>(), screenPos, null, out Vector2 localPos);

        dmgObj.GetComponent<RectTransform>().anchoredPosition = localPos;
        StartCoroutine(FloatAndDestroy(dmgObj));
    }

    private IEnumerator FloatAndDestroy(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = startPos + Vector2.up * (50f * t);
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = 1f - (t / 0.8f);
            yield return null;
        }
        Destroy(obj);
    }
}
