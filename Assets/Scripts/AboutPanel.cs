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
    
    void Start()
    {
        // Пустой метод Start — анимация полностью передана 
        // нативному PanelManager и 3D-аниматору Panel.controller.
        // Это предотвратит конфликты масштабирования и зеркальное отображение.
    }
}
