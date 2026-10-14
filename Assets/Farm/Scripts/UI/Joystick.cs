using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Farm
{
    /// <summary>On-screen joystick (bottom-left). Also maps WASD / arrow keys for editor testing.</summary>
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static Vector2 Value { get; private set; }

        private RectTransform baseRt, knob;
        private Vector2 touchValue;
        private bool held;
        private float radius;

        public static Joystick Create(Transform parent)
        {
            RectTransform area = UIFactory.Rect("Joystick", parent);
            area.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(330, 330));
            var hit = area.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0); // invisible touch area

            Image b = UIFactory.Panel(area, new Color(1f, 1f, 1f, 0.22f), "Base");
            b.sprite = ProcSprites.Circle;
            b.type = Image.Type.Simple;
            b.raycastTarget = false;
            b.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240));

            Image k = UIFactory.Panel(b.transform, new Color(1f, 0.97f, 0.88f, 0.9f), "Knob");
            k.sprite = ProcSprites.Circle;
            k.type = Image.Type.Simple;
            k.raycastTarget = false;
            k.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));

            var j = area.gameObject.AddComponent<Joystick>();
            j.baseRt = b.rectTransform;
            j.knob = k.rectTransform;
            j.radius = 95f;
            return j;
        }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, e.pressEventCamera, out Vector2 local))
                return;
            Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
            touchValue = v.magnitude < 0.12f ? Vector2.zero : v;
            knob.anchoredPosition = v * radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            held = false;
            touchValue = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            Vector2 v = held ? touchValue : Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (!held && kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
                v = Vector2.ClampMagnitude(v, 1f);
            }
            if (UIManager.I != null && UIManager.I.ModalOpen) v = Vector2.zero;
            Value = v;
        }

        private void OnDisable() => Value = Vector2.zero;
    }
}
