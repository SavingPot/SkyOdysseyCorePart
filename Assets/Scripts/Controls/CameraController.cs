using UnityEngine;

namespace GameCore
{
    public class CameraController : MonoBehaviour
    {
        public Tools tools => Tools.instance;
        public Transform lookAt;
        private Camera cam;

        private void Start()
        {
            cam = this.GetComponent<Camera>();
        }

        private void Update()
        {
            if (lookAt)
            {
                cam.transform.position = new(lookAt.position.x, lookAt.position.y, -10);
            }
        }
    }
}
