using UnityEngine;

public sealed class Stage1BossEncounter : MonoBehaviour
{
    private static readonly Vector2 BossSpawnPosition = new Vector2(0f, 13f);

    [SerializeField] private Stage1EncounterGate gate;
    [SerializeField] private Transform player;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Transform worldParent;
    [SerializeField] private Transform uiParent;
    [SerializeField] private bool started;

    public bool HasStarted => started;

    public void Initialize(
        Stage1EncounterGate newGate,
        Transform newPlayer,
        Sprite newBossSprite,
        Transform newWorldParent,
        Transform newUiParent)
    {
        gate = newGate;
        player = newPlayer;
        bossSprite = newBossSprite;
        worldParent = newWorldParent;
        uiParent = newUiParent;
        started = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStart(other);
    }

    public bool TryStart(Collider2D other)
    {
        if (started || gate == null || !gate.IsOpen || other == null ||
            other.GetComponentInParent<PlayerHealth>() == null)
        {
            return false;
        }

        started = true;
        gate.Close();
        SpawnBoss();
        return true;
    }

    private void SpawnBoss()
    {
        EnemyHealth health = BossFactory.Create(
            worldParent, uiParent, player, bossSprite, BossSpawnPosition);
        health.Died += OnBossDied;
    }

    private void OnBossDied(EnemyHealth health)
    {
        if (health != null)
        {
            health.Died -= OnBossDied;
        }

        gate.Open();
    }
}
