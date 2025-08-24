using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected float speed = 10f;
    [SerializeField] protected float damage = 1;

    protected GameObject player;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player");
    }

    public abstract void Move();

    public abstract void OnHit(GameObject hitObject);
}
