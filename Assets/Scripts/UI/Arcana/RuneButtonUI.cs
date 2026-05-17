using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneButtonUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedFrame;

    private Action onLeftClick;
    private Action onRightClick;

    public void Set(string runeName, Sprite icon, Action leftClick, Action rightClick = null, bool selected = false)
    {
        onLeftClick = leftClick;
        onRightClick = rightClick;

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(runeName) ? "" : runeName;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }

    public void Clear()
    {
        Set("", null, null, null, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke();
        else
            onLeftClick?.Invoke();
    }
}