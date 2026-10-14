using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;

    public event Action OnClicked;

    private IInteractable _dynamicTarget;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void Update()
    {
        if (_dynamicTarget != null && labelText != null)
        {
            labelText.text = _dynamicTarget.Label;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void SetDynamicTarget(IInteractable target)
    {
        _dynamicTarget = target;
        if (target != null && labelText != null)
        {
            labelText.text = target.Label;
        }
    }

    public void SetLabel(string label)
    {
        _dynamicTarget = null;
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandleClick()
    {
        OnClicked?.Invoke();
    }
}
