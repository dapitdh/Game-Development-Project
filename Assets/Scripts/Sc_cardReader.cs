using UnityEngine;

public class Sc_cardReader : MonoBehaviour
{
    // Tetap: cara cari keycard & kerangkeng
    GameObject keyCard, kerangkeng, cardSlot, cardKey, hero;

    // Tambahan untuk smooth move
    bool moving;
    Vector3 targetPos, targetPosCard;
    [SerializeField] float moveSpeed = 0.5f; // atur kecepatan geser
    [SerializeField] float moveSpeedCard = 0.1f;

    void Start()
    {
        keyCard = GameObject.FindWithTag("KeyCard");
        kerangkeng = GameObject.Find("kerangkeng");
        hero = GameObject.FindWithTag("Player");

        // Hindari NullRef jika "cardSlot" tidak ditemukan
        Transform slot = this.transform.Find("cardSlot");
        if (slot != null) cardSlot = slot.gameObject;

        if (kerangkeng != null)
            targetPos = kerangkeng.transform.position; // init target ke posisi awal
    }

    void Update()
    {
        // Smooth move setiap frame
        if (moving && kerangkeng != null )
        {
            kerangkeng.transform.position = Vector3.MoveTowards(
                kerangkeng.transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            // --- GUARD: hanya gerakkan kartu kalau ada referensinya ---
            if (cardKey != null)
            {
                cardKey.transform.position = Vector3.MoveTowards(
                    cardKey.transform.position,
                    targetPosCard,
                    moveSpeedCard * Time.deltaTime
                );

                // Cek kalau sudah sampai -> destroy sekali, lalu null-kan referensi
                if (Vector3.Distance(cardKey.transform.position, targetPosCard) <= 0.001f)
                {
                    hero.GetComponent<Sc_pickupItem>().DropItem(); // biar hero lepas pegangan
                    Destroy(cardKey);
                    cardKey = null;
                }
            }

            if (kerangkeng.transform.position == targetPos)
                moving = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("KeyCard"))
        {
            Debug.Log("Kartu Dimasukkan");

            // Target dan pose kartu (gunakan cardSlot kalau ada)
            if (cardSlot != null)
            {
                targetPosCard = cardSlot.transform.position + new Vector3(0.5f, 0, 0); // sesuai logika awalmu
                other.transform.position = cardSlot.transform.position + new Vector3(0.1f, 0, 0);
                other.transform.rotation = cardSlot.transform.rotation;
            }
            else
            {
                // fallback sederhana bila "cardSlot" tidak ada
                targetPosCard = other.transform.position;
            }

            cardKey = other.gameObject;

            // Guard rigidbody biar tidak NullRef
            var rb = cardKey.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // cardkey punya >1 collider -> matikan semuanya (hanya di GameObject ini)
            Collider[] cols = cardKey.GetComponents<Collider>();
            foreach (var col in cols)
            {
                col.enabled = false;
            }

            if (cardSlot != null)
                cardKey.transform.SetParent(cardSlot.transform);

            // Target geser 2.3 unit ke "kanan" (sesuai kode kamu)
            targetPos = kerangkeng.transform.position - new Vector3(2.3f, 0, 0);
            moving = true;
        }
    }
}
