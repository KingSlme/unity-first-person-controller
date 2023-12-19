using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private Image _crosshairDot;
    [SerializeField] private TextMeshProUGUI _interactionText;
    [SerializeField] private Slider _staminaSlider;
    private float _fadeDuration = 0.3f;
    private bool _interactionUIFading = false;
    private bool _staminaSliderFading = false;

    private void Start()
    {   
        _crosshairDot.gameObject.SetActive(false);
        _interactionText.gameObject.SetActive(false);
        _staminaSlider.gameObject.SetActive(false);
    }

    public void SetStaminaSliderValue(float newValue) => _staminaSlider.value = newValue;
    public void SetStaminaSliderMaxValue(float newValue) => _staminaSlider.maxValue = newValue;

    public void ShowInteractionUI()
    {
        if (!_crosshairDot.gameObject.activeSelf)
            _crosshairDot.gameObject.SetActive(true);
        if (!_interactionText.gameObject.activeSelf && !_interactionUIFading)
            StartCoroutine(FadeText(true));
    }
    public void HideInteractionUI()
    {
        if (_crosshairDot.gameObject.activeSelf)
            _crosshairDot.gameObject.SetActive(false);
        if (_interactionText.gameObject.activeSelf && !_interactionUIFading)
            StartCoroutine(FadeText(false));
    }

    public void ShowStaminaSlider()
    {
        if (!_staminaSlider.gameObject.activeSelf && !_staminaSliderFading)
        {
            StartCoroutine(FadeSlider(true));
        }
    }

    public void HideStaminaSlider()
    {
        if (_staminaSlider.gameObject.activeSelf && !_staminaSliderFading)
        {
            StartCoroutine(FadeSlider(false));
        }
    } 

    private IEnumerator FadeText(bool fadeIn)
    {   
        if (fadeIn)
        {
            _interactionText.gameObject.SetActive(true);
        }

        _interactionUIFading = true;
        float elapsedTime = 0.0f;
        Color startColor = _interactionText.color;
        float targetAlpha = fadeIn ? 1.0f : 0.0f;
        while (elapsedTime < _fadeDuration)
        {
            float newAlpha = Mathf.Lerp(startColor.a, targetAlpha, elapsedTime / _fadeDuration);
            _interactionText.color =  new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        _interactionText.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        _interactionUIFading = false;

        if (!fadeIn)
        {
            _interactionText.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeSlider(bool fadeIn)
    {
        if (fadeIn)
        {
            _staminaSlider.gameObject.SetActive(true);
        }

        _staminaSliderFading = true;
        float elapsedTime = 0.0f;
        Image[] images = _staminaSlider.GetComponentsInChildren<Image>();
        Color[] startColors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
             startColors[i] = images[i].color;
        float targetAlpha = fadeIn ? 1.0f : 0.0f;

        while (elapsedTime < _fadeDuration)
        {
            for (int i = 0; i < images.Length; i++)
            {
                float newAlpha = Mathf.Lerp(startColors[i].a, targetAlpha, elapsedTime / _fadeDuration);
                images[i].color =  new Color(startColors[i].r, startColors[i].g, startColors[i].b, newAlpha);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        for (int i = 0; i < images.Length; i++)
            images[i].color = new Color(startColors[i].r, startColors[i].g, startColors[i].b, targetAlpha);
        _staminaSliderFading = false;

        if (!fadeIn)
        {
            _staminaSlider.gameObject.SetActive(false);
        }
    }
}