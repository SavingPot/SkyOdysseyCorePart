using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class CameraController : MonoBehaviour
    {
        public const float defaultProjectionSize = 12;



        public Tools tools => Tools.instance;
        public Transform lookAt;
        public Vector2 lookAtDelta;
        private Camera cam;
        public float shakeLevel = 0;
        public readonly List<Func<float>> cameraScale = new() { };



        private void Start()
        {
            cam = GetComponent<Camera>();
        }

        private void Update()
        {
            float finalCameraScale = 1;
            foreach (var item in cameraScale)
            {
                finalCameraScale *= item();
            }
            cam.orthographicSize = defaultProjectionSize / finalCameraScale;

            if (lookAt)
            {
                var delta = lookAtDelta;
                if (shakeLevel != 0)
                {
                    var frequency = shakeLevel / 2;
                    float xNoise = Mathf.PerlinNoise1D(Time.time * frequency) - 0.5f; //[0,1] -> [-0.5f,0.5f]
                    float yNoise = Mathf.PerlinNoise1D(-Time.time * frequency) - 0.5f;
                    delta += new Vector2(xNoise * shakeLevel, yNoise * shakeLevel);
                }
                cam.transform.position = new(lookAt.position.x + delta.x, lookAt.position.y + delta.y, -10);
            }
        }
    }
}
