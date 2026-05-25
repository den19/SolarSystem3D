using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AboutPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject aboutPanel;
    public Button aboutButton;
    public Button backButton;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private CanvasGroup panelCanvasGroup;
    private RectTransform panelRectTransform;
    private Vector3 originalScale;
    private bool isAnimating = false;
    
    void Start()
    {
        // Get components
        panelCanvasGroup = aboutPanel.GetComponent<CanvasGroup>();
        panelRectTransform = aboutPanel.GetComponent<RectTransform>();
        
        // Store original scale for animation
        originalScale = panelRectTransform.localScale;
        
        // Setup button listeners
        aboutButton.onClick.AddListener(ShowAboutPanel);
        backButton.onClick.AddListener(HideAboutPanel);
        
        // Initially hide the panel
        aboutPanel.SetActive(false);
    }
    
    public void ShowAboutPanel()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateShow());
    }
    
    public void HideAboutPanel()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateHide());
    }
    
    private IEnumerator AnimateShow()
    {
        isAnimating = true;
        
        // Show the panel
        aboutPanel.SetActive(true);
        
        // Reset scale and alpha for animation
        panelRectTransform.localScale = Vector3.zero;
        panelCanvasGroup.alpha = 0f;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = showCurve.Evaluate(progress);
            
            panelRectTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, curveValue);
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
            
            yield return null;
        }
        
        // Ensure final values
        panelRectTransform.localScale = originalScale;
        panelCanvasGroup.alpha = 1f;
        
        isAnimating = false;
    }
    
    private IEnumerator AnimateHide()
    {
        isAnimating = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = hideCurve.Evaluate(progress);
            
            panelRectTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, curveValue);
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
            
            yield return null;
        }
        
        // Ensure final values
        panelRectTransform.localScale = Vector3.zero;
        panelCanvasGroup.alpha = 0f;
        
        // Hide the panel
        aboutPanel.SetActive(false);
        
        isAnimating = false;
    }
}
