namespace Iridium.DB
{
    public enum IsolationLevel
    {
        None,
        Chaos,
        ReadUncommitted,
        ReadCommitted,
        RepeatableRead,
        Serializable,
        Snapshot,
    }
}