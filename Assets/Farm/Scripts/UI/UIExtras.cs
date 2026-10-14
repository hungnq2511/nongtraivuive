using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Farm
{
    public partial class UIManager
    {
        private GameObject tasksBadge;
        private Text clockText;
        private Image clockIcon;

        /// <summary>Top-right: in-game clock + weather, daily tasks button (with red badge), settings.</summary>
        private void BuildTopRightExtras()
        {
            Button tasks = UIFactory.IconButton(hudRoot, "hud_tasks", null, 100, OpenTasks);
            ((RectTransform)tasks.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -16), new Vector2(100, 100));
            Image dot = UIFactory.Panel(tasks.transform, UIFactory.Red, "Badge");
            dot.sprite = ProcSprites.Circle;
            dot.type = Image.Type.Simple;
            dot.raycastTarget = false;
            dot.rectTransform.Place(new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(30, 30));
            tasksBadge = dot.gameObject;
            tasksBadge.SetActive(false);

            Image clock = UIFactory.Panel(hudRoot, new Color(0.2f, 0.12f, 0.05f, 0.75f), "Clock");
            clock.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-244, -30), new Vector2(210, 72));
            clockIcon = UIFactory.Icon(clock.transform, "hud_sun", 76);
            clockIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8, 0), new Vector2(76, 76));
            clockText = UIFactory.Label(clock.transform, "06:00", 32, Color.white, TextAnchor.MiddleCenter, true, true);
            clockText.rectTransform.Stretch();
            clockText.rectTransform.offsetMin = new Vector2(50, 0);
        }

        public void SetTasksBadge(bool on)
        {
            if (tasksBadge != null) tasksBadge.SetActive(on);
        }

        private string clockIconName;

        public void SetClock(string time, string icon, string label)
        {
            if (clockText == null) return;
            clockText.text = time;
            if (icon != clockIconName)
            {
                clockIconName = icon;
                clockIcon.sprite = SpriteDB.Get(icon);
            }
        }

        // ------------------------------------------------------------------ icons flying to the HUD
        public void FlyIcon(Vector3 world, string icon)
        {
            RectTransform target = (RectTransform)(icon == "hud_coin" ? coinText.transform.parent : storageText.transform.parent);
            StartCoroutine(FlyCo(world, icon, target));
        }

        private IEnumerator FlyCo(Vector3 world, string icon, RectTransform target)
        {
            Image img = UIFactory.Icon(canvasRt, icon, 84);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            float scale = canvasRt.lossyScale.x <= 0 ? 1f : canvasRt.lossyScale.x;
            Vector2 from = CameraController.I.WorldToScreen(world) / scale;
            float t = 0f;
            yield return new WaitForSeconds(0.25f);
            while (t < 1f && target != null)
            {
                t += Time.unscaledDeltaTime / 0.65f;
                Vector2 to = RectTransformUtility.WorldToScreenPoint(null, target.position) / scale;
                float e = t * t * (3f - 2f * t);
                Vector2 mid = (from + to) * 0.5f + new Vector2(0, 180f);
                Vector2 a = Vector2.Lerp(from, mid, e), b = Vector2.Lerp(mid, to, e);
                rt.anchoredPosition = Vector2.Lerp(a, b, e);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, e);
                yield return null;
            }
            Destroy(img.gameObject);
            if (target != null) StartCoroutine(PulseCo(target));
        }

        private IEnumerator PulseCo(RectTransform rt)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.25f;
                rt.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
