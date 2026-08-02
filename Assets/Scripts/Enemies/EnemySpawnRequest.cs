using UnityEngine;

public sealed class EnemySpawnRequest
{
    public Transform Parent { get; }
    public Transform Target { get; }
    public Vector2 Position { get; }
    public string InstanceName { get; }

    public EnemySpawnRequest(
        Transform parent,
        Transform target,
        Vector2 position,
        string instanceName = null)
    {
        Parent = parent;
        Target = target;
        Position = position;
        InstanceName = instanceName;
    }
}
