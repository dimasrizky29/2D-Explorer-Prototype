using System;
using System.Threading;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

public class PlayerInteractor : MonoBehaviour
{
    public InteractionDetector _detector;
    public ActionButton _interactButton;
    private CancellationTokenSource _cts;

    public event Action<IInteractable> OnInteract;

    private void Start()
    {
        _cts = new CancellationTokenSource();

        if (_detector != null)
        {
            _detector.OnNearestChanged += HandleNearestChanged;
            HandleNearestChanged(_detector.Nearest);
        }

        if (_interactButton != null)
        {
            _interactButton.OnClicked += HandleInteractClicked;
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        if (_detector != null)
        {
            _detector.OnNearestChanged -= HandleNearestChanged;
        }

        if (_interactButton != null)
        {
            _interactButton.OnClicked -= HandleInteractClicked;
        }
    }

    private void HandleNearestChanged(IInteractable nearest)
    {
        if (_interactButton == null) return;

        if (nearest != null && nearest.CanInteract)
        {
            _interactButton.SetDynamicTarget(nearest);
            _interactButton.SetVisible(true);
            _interactButton.SetInteractable(true);
        }
        else
        {
            _interactButton.SetDynamicTarget(null);
            _interactButton.SetVisible(false);
        }
    }

    private void HandleInteractClicked()
    {
        if (_detector == null || _detector.Nearest == null) return;
            
        TryInteractAsync(_detector.Nearest).Forget();
    }

    private async UniTaskVoid TryInteractAsync(IInteractable interactable)
    {
        if (interactable == null || !interactable.CanInteract) return;

        try
        {
            OnInteract?.Invoke(interactable);

            _interactButton.SetInteractable(false);
            await interactable.InteractAction(_cts.Token);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            Debug.LogError($"{ex}, [PlayerInteractor] Error during interaction");
        }
        finally
        {
            if (this != null && _detector != null)
            {
                HandleNearestChanged(_detector.Nearest);
            }
        }
    }
}
