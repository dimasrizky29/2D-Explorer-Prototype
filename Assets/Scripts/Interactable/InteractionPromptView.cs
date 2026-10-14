using UnityEngine;
using TMPro;
using VContainer;

public class InteractionPromptView : MonoBehaviour
{
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    private InteractionDetector _detector;
    private Camera _mainCamera;

    [Inject]
    public void Construct(InteractionDetector detector)
    {
        _detector = detector;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (visualContainer != null)
        {
            visualContainer.SetActive(false);
        }

        if (_detector != null)
        {
            _detector.OnNearestChanged += HandleNearestChanged;
            HandleNearestChanged(_detector.Nearest);
        }
    }

    private void OnDestroy()
    {
        if (_detector != null)
        {
            _detector.OnNearestChanged -= HandleNearestChanged;
        }
    }

    private void LateUpdate()
    {
        if (_detector == null || _detector.Nearest == null || visualContainer == null || !visualContainer.activeSelf)
        {
            return;
        }

        if (_detector.Nearest is MonoBehaviour mb)
        {
            Vector3 targetWorldPos = mb.transform.position + offset;
                
            // If it is World Space UI
            if (transform.parent == null || transform.parent.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace)
            {
                transform.position = targetWorldPos;
            }
            else
            {
                // Screen Space UI projection
                Vector2 screenPos = _mainCamera.WorldToScreenPoint(targetWorldPos);
                transform.position = screenPos;
            }
        }
    }

    private void HandleNearestChanged(IInteractable nearest)
    {
        if (visualContainer == null) return;

        if (nearest != null && nearest.CanInteract)
        {
            if (promptText != null)
            {
                promptText.text = nearest.Label;
            }
            visualContainer.SetActive(true);
        }
        else
        {
            visualContainer.SetActive(false);
        }
    }
}
