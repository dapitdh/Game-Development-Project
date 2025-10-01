using UnityEngine;

namespace CameraDrawerScript
{
    public class CameraOpenDrawer : MonoBehaviour
    {
        public float DistanceOpen = 3f;

        void Update()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, DistanceOpen))
            {
                DrawerScript.Drawer drawer = hit.transform.GetComponent<DrawerScript.Drawer>();
                if (drawer != null && Input.GetKeyDown(KeyCode.Mouse0))
                {
                    drawer.OpenDrawer();
                }
            }
        }
    }
}
