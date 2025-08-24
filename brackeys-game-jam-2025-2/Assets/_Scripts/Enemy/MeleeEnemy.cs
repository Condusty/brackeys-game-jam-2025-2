using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    [SerializeField] private float aggroLossTime = 5f; // Zeit bis Aggro verloren geht
    [SerializeField] private string wallTag = "Obstacle"; // Tag für Wände

    private bool isMoving = false;
    private bool isInAttackRange = false;
    private bool isAggro = false;
    private float attackCd = 1.5f;
    private float aggroTimer = 0f;
    private Vector2 lastKnownPlayerPosition;

    private void Update()
    {
        UpdateAggro();
        Move();
        CheckAttackCd();
    }

    private void UpdateAggro()
    {
        float distance = GetDistanceToPlayer();
        bool canSeePlayer = CanSeePlayer();

        // Aggro aktivieren wenn Spieler in Range und sichtbar
        if (distance <= aggroRange && canSeePlayer)
        {
            isAggro = true;
            aggroTimer = aggroLossTime;
            lastKnownPlayerPosition = player.transform.position;
        }

        // Aggro Timer verringern wenn Spieler nicht sichtbar
        if (isAggro && !canSeePlayer)
        {
            aggroTimer -= Time.deltaTime;
            if (aggroTimer <= 0)
            {
                isAggro = false;
            }
        }

        // Aggro Timer aktualisieren wenn Spieler sichtbar
        if (isAggro && canSeePlayer)
        {
            aggroTimer = aggroLossTime;
            lastKnownPlayerPosition = player.transform.position;
        }
    }

    private bool CanSeePlayer()
    {
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // Verwende RaycastAll um alle Collider zu prüfen
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToPlayer, distanceToPlayer);
        
        foreach (RaycastHit2D hit in hits)
        {
            // Ignoriere den Enemy selbst
            if (hit.collider.gameObject == gameObject)
                continue;
            
            // Ignoriere Trigger-Collider
            if (hit.collider.isTrigger)
                continue;
            
            // Wenn es ein Obstacle ist, blockiert es die Sicht
            if (hit.collider.CompareTag(wallTag))
            {
                Debug.Log($"Wall detected: {hit.collider.name} blocks vision");
                return false;
            }
            
            // Wenn wir den Spieler erreichen, ohne eine Wand zu treffen
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("Player reached without wall obstruction");
                return true;
            }
        }
        
        // Fallback: Wenn kein Player-Hit gefunden wurde
        Debug.Log("No player hit found in raycast");
        return false;
    }

    private void CheckAttackCd()
    {
        attackCd -= Time.deltaTime;
        if (attackCd <= 0 && isInAttackRange && CanSeePlayer())
        {
            Attack();
            attackCd = 1.5f;
        }
    }

    public override void Attack()
    {
        Debug.Log("Melee Enemy Attacks!");
        var playerDamageable = player.GetComponent<IDamageable>();
        if (playerDamageable != null)
        {
            playerDamageable.Damage(strength);
        }
    }

    public override void Die()
    {
        Debug.Log("Melee Enemy Dies!");
        Destroy(gameObject);
    }

    private void Move()
    {
        if (!isAggro)
        {
            isMoving = false;
            isInAttackRange = false;
            return;
        }

        float distance = GetDistanceToPlayer();
        Vector2 targetPosition = CanSeePlayer() ? player.transform.position : lastKnownPlayerPosition;

        if (distance > attackRange)
        {
            isMoving = true;
            isInAttackRange = false;
        }
        else
        {
            isMoving = false;
            isInAttackRange = true;
        }

        if (isMoving)
        {
            Vector2 directDirection = (targetPosition - (Vector2)transform.position).normalized;
            transform.position += (Vector3)directDirection * moveSpeed * Time.deltaTime;
        }
    }

    private float GetDistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Zeige Sichtlinie an
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }

        // Zeige Aggro-Status
        if (isAggro)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
    }
}
