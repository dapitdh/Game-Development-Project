using UnityEngine;

namespace DrawerScript
{
    public class Drawer : MonoBehaviour
    {
        public bool open = false;
        public float smooth = 5f;
        public float openOffset = -0.42f; 

        private Vector3 closedPos;
        private Vector3 openPos;

        void Start()
        {
            closedPos = transform.localPosition;
            openPos = closedPos + new Vector3(openOffset, 0f, 0f); 
        }

        void Update()
        {
            Vector3 targetPos = open ? openPos : closedPos;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
        }

		public void OpenDrawer()
		{
			open = !open;
			Debug.Log("OpenDrawer dipanggil, status open = " + open);
        }
    }
}
