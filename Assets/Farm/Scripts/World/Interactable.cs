using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    /// <summary>Anything in the world the player can tap.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        /// <summary>The visible sprite (usually a scaled child of this transform).</summary>
        public SpriteRenderer Sr;
        /// <summary>True while a farmer task targeting this object is queued.</summary>
        [System.NonSerialized] public bool Pending;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public virtual bool HitTest(Vector2 p)
        {
            if (Sr == null || Sr.sprite == null) return false;
            Bounds b = Sr.bounds;
            float hx = b.extents.x * 0.75f, hy = b.extents.y * 0.85f;
            return Mathf.Abs(p.x - b.center.x) <= hx && Mathf.Abs(p.y - b.center.y) <= hy;
        }

        public virtual int HitOrder => Sr != null ? Sr.sortingOrder : 0;
        public virtual bool Movable => false;

        public abstract void OnTap();

        /// <summary>Action key used for drag-to-repeat (Hay Day style). Null if not draggable.</summary>
        public virtual string DragKey() => null;
        public virtual void OnDragApply(string key) { }

        private float punch;

        public void Punch() => punch = 1f;

        protected virtual void LateUpdate()
        {
            if (punch > 0f && Sr != null)
            {
                punch = Mathf.Max(0f, punch - Time.deltaTime * 4f);
                float s = 1f + Mathf.Sin(punch * Mathf.PI) * 0.08f;
                Sr.transform.localScale = BaseScale * s;
            }
        }

        [System.NonSerialized] public Vector3 BaseScale = Vector3.one;

        protected SpriteRenderer MakeSprite(string spriteName, float scale, string childName = "Sprite")
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteDB.Get(spriteName);
            BaseScale = Vector3.one * scale;
            go.transform.localScale = BaseScale;
            return sr;
        }
    }

    /// <summary>Small speech-bubble with an icon floating above a world object.</summary>
    public class WorldBubble : MonoBehaviour
    {
        private SpriteRenderer bg, icon;
        private float t;
        private Vector3 basePos;
        private string current;

        public static WorldBubble Create(Transform parent, float height)
        {
            var go = new GameObject("Bubble");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, height, 0);
            var b = go.AddComponent<WorldBubble>();
            b.basePos = go.transform.localPosition;
            b.bg = go.AddComponent<SpriteRenderer>();
            b.bg.sprite = ProcSprites.Circle;
            b.bg.color = new Color(1f, 0.98f, 0.92f, 0.95f);
            go.transform.localScale = Vector3.one * 0.75f;
            var ig = new GameObject("Icon");
            ig.transform.SetParent(go.transform, false);
            b.icon = ig.AddComponent<SpriteRenderer>();
            ig.transform.localScale = Vector3.one * 0.3f;
            b.t = Random.value * 6f;
            go.SetActive(false);
            return b;
        }

        public void Show(string iconName, float iconScale = 0.3f)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (current != iconName)
            {
                current = iconName;
                icon.sprite = SpriteDB.Get(iconName);
                icon.transform.localScale = Vector3.one * iconScale;
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            t += Time.deltaTime;
            transform.localPosition = basePos + new Vector3(0, Mathf.Sin(t * 3f) * 0.06f, 0);
            int order = IsoGrid.OverlayOrder + IsoGrid.SortOrder(transform.position.y);
            bg.sortingOrder = order;
            icon.sortingOrder = order + 1;
        }
    }
}
