using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameCore
{
    public class CameraController : MonoBehaviour
    {
        public const float defaultProjectionSize = 12;



        public Tools tools => Tools.instance;
        public Transform lookAt;
        public Vector2 lookAtDelta;
        public Transform secondLookAt;
        private Camera cam;
        public Volume globalVolume { get; protected set; }
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
            var finalCameraSize = defaultProjectionSize / finalCameraScale;

            //相机跟随
            if (lookAt)
            {
                //相机摇晃
                var delta = lookAtDelta;
                if (shakeLevel != 0)
                {
                    var frequency = shakeLevel / 2;
                    float xNoise = Mathf.PerlinNoise1D(Time.time * frequency) - 0.5f; //[0,1] -> [-0.5f,0.5f]
                    float yNoise = Mathf.PerlinNoise1D(-Time.time * frequency) - 0.5f;
                    delta += new Vector2(xNoise * shakeLevel, yNoise * shakeLevel);
                }

                //相机第二跟随
                if (secondLookAt)
                {
                    var positionDelta = (Vector2)secondLookAt.position - (Vector2)lookAt.position;
                    delta += 0.5f * positionDelta;
                    finalCameraSize += positionDelta.magnitude / 2 - 4.5f;

                    //设置相机位置（有平滑效果）
                    cam.transform.position = Vector3.Lerp(cam.transform.position,
                                                           new(lookAt.position.x + delta.x, lookAt.position.y + delta.y, -10),
                                                           Tools.deltaTime * 5);
                }
                else
                {
                    //设置相机位置（无平滑效果）
                    cam.transform.position = new(lookAt.position.x + delta.x, lookAt.position.y + delta.y, -10);
                }
            }

            //设置摄像机大小（有平滑效果）
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, finalCameraSize, Tools.deltaTime * 5);
        }

        public void SetGlobalVolumeBloom(float threshold, float intensity)
        {
            if (globalVolume.profile.TryGet(out Bloom bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(threshold);
                bloom.intensity.Override(intensity);
            }
        }

        public void SetGlobalVolumeColorAdjustments(Color colorFilter, float colorFilterIntensity, float saturation)
        {
            if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(colorFilter * colorFilterIntensity);
                colorAdjustments.saturation.Override(saturation);
            }
        }

        public void SetGlobalVolumeVignette(float intensity)
        {
            if (globalVolume.profile.TryGet(out Vignette vignette))
            {
                vignette.intensity.Override(intensity);
            }
        }

        public void EnableGlobalVolumeVignette()
        {
            if (globalVolume.profile.TryGet(out Vignette vignette))
                vignette.active = true;
        }

        public void DisableGlobalVolumeVignette()
        {
            if (globalVolume.profile.TryGet(out Vignette vignette))
                vignette.active = false;
        }


        [RuntimeInitializeOnLoadMethod]
        static void BindMethod()
        {
            GScene.AfterChanged += scene =>
            {
                if (scene.name == SceneNames.GameScene)
                {
                    Debug.Log("GameScene Loaded!");
                    //获取全局后处理
                    GameObject gv = GameObject.Find("Global Volume");
                    if (gv)
                        Tools.instance.mainCameraController.globalVolume = gv.GetComponent<Volume>();
                    else
                        throw new Exception("Global Volume GameObject not found!");
                    if (!Tools.instance.mainCameraController.globalVolume)
                        throw new Exception("Global Volume component not found!");
                }
            };
        }
    }
}
