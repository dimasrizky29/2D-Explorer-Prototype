/// <summary>
/// Kunci aksi bersama: attack dan dash tidak boleh tumpang tindih.
/// Di-register sebagai Singleton di GameLifetimeScope.
/// </summary>
public sealed class ActionGate
{
    public bool IsBusy { get; set; }
}
