using System.Threading;
using Cysharp.Threading.Tasks;

    public interface IInteractable
    {
        string Label { get; }
        bool CanInteract { get; }
        UniTask InteractAction(CancellationToken token);
    }
