using UnityEngine;

public class Sc_camera : MonoBehaviour
{
    GameObject hero;
    // float speed = 5;
    Vector3 offset;
    void Start()
    {
        hero = GameObject.Find("hero");
        // posCamWorld();

        posCamLocal();
        offset = new Vector3(hero.transform.position.x, hero.transform.position.y , hero.transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void posCamLocal()
    {
        this.transform.rotation = hero.transform.rotation;

        Vector3 pos = hero.transform.localPosition;
        pos -= this.transform.forward * 5f;
        pos.y += 2f;
        this.transform.position = pos;
    }
    void posCamWorld()
    {
        this.transform.LookAt(hero.transform);

        Vector3 pos = hero.transform.position;
        pos.y += 2f;
        pos.z -= 5f;
        this.transform.position = pos;
    }
    void LateUpdate()
    {
        // posCamLocal();
        // camByKeyboard();
        // camByMouse();
        camByMouseAngleAxis();
        raycast();
    }
    void camByKeyboard()
    {
        float speed = 0;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 50;
        }
        if (Input.GetKey(KeyCode.RightShift))
        {
            speed = -50;
        }
        this.transform.LookAt(hero.transform);
        this.transform.RotateAround(hero.transform.position, Vector3.up, speed * Time.deltaTime);
        if (Input.GetKey(KeyCode.Alpha1))
        {
            if (Vector3.Distance(this.transform.position, hero.transform.position) > 1f)
            {
                this.transform.Translate(Vector3.forward * 5 * Time.deltaTime, Space.Self);
            }
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {

            if (Vector3.Distance(this.transform.position, hero.transform.position) < 5f)
            {
                this.transform.Translate(Vector3.back * 5 * Time.deltaTime, Space.Self);
            }
        }
    }

    void camByMouse()
    {
        this.transform.RotateAround(hero.transform.position, Vector3.up, Input.GetAxis("Mouse X") * 5);
        this.transform.RotateAround(hero.transform.position, Vector3.left, Input.GetAxis("Mouse Y") * -5);
        this.transform.LookAt(hero.transform);
    }
    void camByMouseAngleAxis()
    {

        offset = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * 5, Vector3.up) * offset;
        offset = Quaternion.AngleAxis(Input.GetAxis("Mouse Y") * -5, this.transform.right) * offset;
        this.transform.position = hero.transform.position + offset;

        Vector3 arahCam = hero.transform.position - this.transform.position;
        this.transform.rotation = Quaternion.LookRotation(arahCam);
        // speed++;
        // Quaternion rot = Quaternion.AngleAxis(speed, Vector3.up);
        // this.transform.rotation = rot;
    }
    void raycast()
    {
        float panjangRay = 300f;
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Debug.DrawRay(ray.origin, ray.direction * panjangRay, Color.red);
        if (Physics.Raycast(ray, out hit, panjangRay))
        {
            Debug.Log(hit.transform.name);
            if (hit.transform.name == "tembok")
            {
                this.transform.position = hit.point;
            }
        }
    }
}
