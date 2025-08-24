using UnityEngine;

public class PlayerCtrl : MonoBehaviour
{
    [SerializeField] PlayerStatSO playerStat;
    Rigidbody2D rb;
    float speedX, speedY;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        speedX = Input.GetAxis("Horizontal") * playerStat.moveSpeed;
        speedY = Input.GetAxis("Vertical") * playerStat.moveSpeed;
        rb.linearVelocity= new Vector2(speedX, speedY);
    }
}
