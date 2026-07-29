using System.Collections.Generic;
using UnityEngine;

public sealed class Stage1EncounterGate : MonoBehaviour
{
    private readonly HashSet<EnemyHealth> remainingEnemies = new HashSet<EnemyHealth>();
    [SerializeField] private EnemyHealth[] configuredEnemies = new EnemyHealth[0];
    [SerializeField] private Collider2D barrier;
    [SerializeField] private Renderer[] visuals = new Renderer[0];

    public int RemainingEnemies => remainingEnemies.Count;
    public bool IsOpen { get; private set; }

    public void Close()
    {
        SetOpenState(false);
    }

    public void Open()
    {
        SetOpenState(true);
    }

    public void Initialize(
        IReadOnlyList<EnemyHealth> enemies,
        Collider2D newBarrier,
        Renderer[] newVisuals)
    {
        configuredEnemies = enemies == null
            ? new EnemyHealth[0]
            : CopyEnemies(enemies);
        barrier = newBarrier;
        visuals = newVisuals ?? new Renderer[0];
        RebuildTracking();
    }

    private void OnEnable()
    {
        RebuildTracking();
    }

    private void RebuildTracking()
    {
        Unsubscribe();
        remainingEnemies.Clear();
        IsOpen = false;

        foreach (EnemyHealth enemy in configuredEnemies)
        {
            if (enemy == null || !remainingEnemies.Add(enemy))
            {
                continue;
            }

            enemy.Died += OnEnemyDied;
        }

        bool shouldBeClosed = remainingEnemies.Count > 0;
        if (barrier != null)
        {
            barrier.enabled = shouldBeClosed;
        }

        foreach (Renderer visual in visuals)
        {
            if (visual != null)
            {
                visual.enabled = shouldBeClosed;
            }
        }

        TryOpen();
    }

    private void Update()
    {
        if (IsOpen || remainingEnemies.Count == 0)
        {
            return;
        }

        remainingEnemies.RemoveWhere(enemy => enemy == null);
        TryOpen();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        if (enemy != null)
        {
            enemy.Died -= OnEnemyDied;
        }

        remainingEnemies.Remove(enemy);
        TryOpen();
    }

    private void TryOpen()
    {
        if (IsOpen || remainingEnemies.Count > 0)
        {
            return;
        }

        Open();
    }

    private void Unsubscribe()
    {
        foreach (EnemyHealth enemy in remainingEnemies)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }
        }
    }

    private static EnemyHealth[] CopyEnemies(IReadOnlyList<EnemyHealth> enemies)
    {
        var copy = new EnemyHealth[enemies.Count];
        for (int index = 0; index < enemies.Count; index++)
        {
            copy[index] = enemies[index];
        }

        return copy;
    }

    private void SetOpenState(bool open)
    {
        IsOpen = open;
        if (barrier != null)
        {
            barrier.enabled = !open;
        }

        foreach (Renderer visual in visuals)
        {
            if (visual != null)
            {
                visual.enabled = !open;
            }
        }
    }
}
