using UnityEngine;

public class ArrowProjectile : ProjectileBase
{
    private Vector2 moveDirection;

    private void Start()
    {
        // Richtung zum Spieler berechnen und speichern
        moveDirection = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    private void Update()
    {
        Move();
    }

    public override void Move()
    {
        // Bewegung in gespeicherter Richtung
        transform.position += (Vector3)moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Obstacle"))
        {
            OnHit(collision.gameObject);
        }
    }

    public override void OnHit(GameObject hitObject)
    {
        IDamageable damageable = hitObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(damage);
        }
        Destroy(gameObject);
    }
}
