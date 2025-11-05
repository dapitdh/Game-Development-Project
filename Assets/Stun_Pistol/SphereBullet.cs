using UnityEngine;

public class SphereBullet : MonoBehaviour
{
    [Header("Stun Impact")]
    public float stunDuration = 2f;
    public float impactForce = 6f;
    public bool destroyOnHit = true;
    public GameObject impactVfx; // opsional

    void OnCollisionEnter(Collision collision)
    {
        var contact = collision.GetContact(0);

        // Cari target yang bisa distun (di collider yang kena atau parent-nya)
        var stunnable = collision.collider.GetComponentInParent<IStunnable>();
        if (stunnable != null)
        {
            stunnable.Stun(stunDuration, contact.point, contact.normal, gameObject, impactForce);
        }
        else
        {
            // Kalau tidak ada sistem stun, tetap kasih dorongan fisika kalau punya RB
            if (collision.rigidbody)
                collision.rigidbody.AddForceAtPosition(-contact.normal * impactForce, contact.point, ForceMode.Impulse);
        }

        if (impactVfx)
            Instantiate(impactVfx, contact.point, Quaternion.LookRotation(contact.normal));

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
