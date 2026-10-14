using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public enum RouteId
{
    Restart,
}

public class BuildingInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string label = "Portal";
    [SerializeField] private RouteId route = RouteId.Restart;

    public string Label => label;

    public bool CanInteract => true;

    public async UniTask InteractAction(CancellationToken token)
    {
        var routeId = GetRouteString(route);
        await SceneManager.LoadSceneAsync(routeId, LoadSceneMode.Single).ToUniTask(cancellationToken: token);
    }

    private static string GetRouteString(RouteId id)
    {
        switch (id)
        {
            case RouteId.Restart:
                return SceneManager.GetActiveScene().name;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(id), id, null);
        }
    }
}
