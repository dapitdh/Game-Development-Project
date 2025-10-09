using UnityEngine;

public class Sc_holdItem : MonoBehaviour
{
    GameObject itemHold;
    bool isHeroNear = false;
    bool isHolding = false;
    Rigidbody rb;
    Collider col;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        GameObject hero = GameObject.Find("hero");
        if (hero != null)
        {
            Debug.Log("Hero found");
            itemHold = hero.transform.Find("itemHolder").gameObject;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isHeroNear && itemHold != null)
        {
            if (!isHolding)
            {
                // === PICK UP ===
                transform.SetParent(itemHold.transform);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;

                rb.isKinematic = true;
                col.enabled = false;

                isHolding = true;
                Debug.Log("Item picked up");
            }
            else
            {
                // === DROP ===
                transform.SetParent(null);
                rb.isKinematic = false;
                col.enabled = true;

                // lempar sedikit ke depan hero (opsional)
                rb.AddForce(itemHold.transform.forward * 2f, ForceMode.Impulse);

                isHolding = false;
                Debug.Log("Item dropped");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hero"))
            isHeroNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hero"))
            isHeroNear = false;
    }
}
