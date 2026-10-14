using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Satu-satunya penulis Time.timeScale untuk efek sementara: hit stop (0) dan slow motion (0..1).
/// Beberapa permintaan digabung lewat <see cref="TimeScaleStack"/>, jadi tidak saling menimpa.
/// </summary>
public static class TimeControl
{
    private static readonly TimeScaleStack Stack = new TimeScaleStack();
    private static bool _running;

    // Domain reload bisa dimatikan: state static tidak boleh bocor antar sesi Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Stack.Clear();
        _running = false;
        Time.timeScale = 1f;
    }

    public static void Request(float scale, float seconds)
    {
        if (seconds <= 0f || scale >= 1f) return;

        Stack.Add(scale, Time.unscaledTime + seconds);
        if (_running) return;
        RunAsync().Forget();
    }

    // Tanpa token milik objek: entity mati/hancur di tengah efek tidak boleh bikin timeScale nyangkut.
    private static async UniTaskVoid RunAsync()
    {
        _running = true;
        try
        {
            while (true)
            {
                float scale = Stack.Evaluate(Time.unscaledTime);
                if (!Stack.HasEntries) break;

                Time.timeScale = scale;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: default);
            }
        }
        finally
        {
            Time.timeScale = 1f;
            _running = false;
        }
    }
}
