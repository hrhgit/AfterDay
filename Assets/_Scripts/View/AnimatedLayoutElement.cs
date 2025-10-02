using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class AnimatedLayoutElement : MonoBehaviour
{
    private LayoutElement _layoutElement;
    private Coroutine _animationCoroutine;

    private void Awake()
    {
        _layoutElement = GetComponent<LayoutElement>();
    }

    // 公共接口，用於啟動寬度變化的動畫
    public void AnimateToWidth(float targetWidth, float duration = 0.2f)
    {
        // 如果有正在進行的動畫，先停止它
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        // 啟動一個新的動畫協程
        _animationCoroutine = StartCoroutine(AnimateWidthCoroutine(targetWidth, duration));
    }

    private IEnumerator AnimateWidthCoroutine(float targetWidth, float duration)
    {
        float startWidth = _layoutElement.preferredWidth;
        float timer = 0f;

        while (timer < duration)
        {
            // 使用 Mathf.Lerp 進行平滑插值
            float newWidth = Mathf.Lerp(startWidth, targetWidth, timer / duration);
            _layoutElement.preferredWidth = newWidth;
            
            timer += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 確保暫停時UI動畫也能播放
            yield return null; // 等待下一影格
        }

        // 確保動畫結束時寬度精確
        _layoutElement.preferredWidth = targetWidth;
        _animationCoroutine = null;
    }
}