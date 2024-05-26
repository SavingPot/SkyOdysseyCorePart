using SP.Tools.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IRectTransform
    {
        public float Horizontal => snapX ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x;
        public float Vertical => snapY ? SnapFloat(input.y, AxisOptions.Vertical) : input.y;
        public Vector2 Direction => new(Horizontal, Vertical);

        public float HandleRange
        {
            get => handleRange;
            set => handleRange = Mathf.Abs(value);
        }

        public float DeadZone
        {
            get => deadZone;
            set => deadZone = Mathf.Abs(value);
        }

        public AxisOptions AxisOptions { get => AxisOptions; set => axisOptions = value; }

        public float handleRange = 1;
        public float deadZone = 0;
        public AxisOptions axisOptions = AxisOptions.Both;
        public bool snapX = false;
        public bool snapY = false;

        public ImageIdentity background = null;
        public ImageIdentity handle = null;
        private RectTransform baseRect = null;

        private Canvas canvas;
        private Camera cam;

        public Image image;

        private Vector2 input = Vector2.zero;

        public RectTransform rectTransform => image.rectTransform;

        public static Joystick Create(string name, string bgId, string handleId, float size = 180, string bgTexture = "ori:player_joystick_background", string handleTexture = "ori:player_joystick_handle")
        {
            Joystick joystick = new GameObject(name).AddComponent<Joystick>();

            joystick.transform.SetParent(GameUI.canvas.transform);
            joystick.image = joystick.gameObject.AddComponent<Image>();
            joystick.image.rectTransform.sizeDelta = Vector2.zero;
            joystick.image.rectTransform.anchorMin = UIA.LowerLeft;
            joystick.image.rectTransform.anchorMax = UIA.LowerLeft;
            joystick.image.rectTransform.anchoredPosition = new(300, 200);

            //添加摇杆贴图
            joystick.background = GameUI.AddImage(UIA.Middle, bgId, bgTexture, joystick.transform);
            joystick.handle = GameUI.AddImage(UIA.Middle, handleId, handleTexture, joystick.background);

            //修改摇杆大小
            Vector2 sizeV = new(size, size);
            joystick.background.rectTransform.sizeDelta = sizeV;
            joystick.handle.rectTransform.sizeDelta = sizeV;
            joystick.image.rectTransform.localScale = Vector3.one;

            return joystick;
        }

        protected virtual void Start()
        {
            HandleRange = handleRange;
            DeadZone = deadZone;
            baseRect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                Debug.LogError("摇杆不在 Canvas 中");

            Vector2 center = new(0.5f, 0.5f);
            background.rt.pivot = center;
            handle.rt.anchorMin = center;
            handle.rt.anchorMax = center;
            handle.rt.pivot = center;
            handle.rt.anchoredPosition = Vector2.zero;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            cam = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                cam = canvas.worldCamera;

            Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, background.rt.position);
            Vector2 radius = background.rt.sizeDelta / 2;
            input = (eventData.position - position) / (radius * canvas.scaleFactor);
            FormatInput();
            HandleInput(input.magnitude, input.normalized, radius, cam);
            handle.rt.anchoredPosition = input * radius * handleRange;
        }

        protected virtual void HandleInput(float magnitude, Vector2 normalized, Vector2 radius, Camera cam)
        {
            if (magnitude > deadZone)
            {
                if (magnitude > 1)
                    input = normalized;
            }
            else
                input = Vector2.zero;
        }

        private void FormatInput()
        {
            if (axisOptions == AxisOptions.Horizontal)
                input = new Vector2(input.x, 0f);
            else if (axisOptions == AxisOptions.Vertical)
                input = new Vector2(0f, input.y);
        }

        private float SnapFloat(float value, AxisOptions snapAxis)
        {
            if (value == 0)
                return value;

            if (axisOptions == AxisOptions.Both)
            {
                float angle = Vector2.Angle(input, Vector2.up);
                if (snapAxis == AxisOptions.Horizontal)
                {
                    if (angle < 22.5f || angle > 157.5f)
                        return 0;
                    else
                        return (value > 0) ? 1 : -1;
                }
                else if (snapAxis == AxisOptions.Vertical)
                {
                    if (angle > 67.5f && angle < 112.5f)
                        return 0;
                    else
                        return (value > 0) ? 1 : -1;
                }
                return value;
            }
            else
            {
                if (value > 0)
                    return 1;
                if (value < 0)
                    return -1;
            }
            return 0;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            input = Vector2.zero;
            handle.rt.anchoredPosition = Vector2.zero;
        }

        protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out Vector2 localPoint))
            {
                Vector2 pivotOffset = baseRect.pivot * baseRect.sizeDelta;
                return localPoint - (background.rt.anchorMax * baseRect.sizeDelta) + pivotOffset;
            }
            return Vector2.zero;
        }
    }

    public enum AxisOptions { Both, Horizontal, Vertical }
}
