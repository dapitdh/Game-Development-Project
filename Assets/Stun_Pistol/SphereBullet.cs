using UnityEngine;

public class SphereBullet : MonoBehaviour
{
    [Header("Stun Impact")]
    public float stunDuration = 2f;
    public float impactForce = 6f;
    public bool destroyOnHit = true;
    public GameObject impactVfx;

    void OnCollisionEnter(Collision collision)
    {
        var contact = collision.GetContact(0);

        var stunnable = collision.collider.GetComponentInParent<IStunnable>();
        if (stunnable != null)
        {
            stunnable.Stun(stunDuration, contact.point, contact.normal, gameObject, impactForce);
        }
        else
        {
            if (collision.rigidbody)
                collision.rigidbody.AddForceAtPosition(-contact.normal * impactForce, contact.point, ForceMode.Impulse);
        }

        if (impactVfx)
            Instantiate(impactVfx, contact.point, Quaternion.LookRotation(contact.normal));

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
