using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Primitive + World-Space UI üretim yardımcıları. Dışarıdan hiçbir asset gerektirmez.
    /// </summary>
    public static class Greybox
    {
        static Font font;
        static Shader litShader;
        static Shader unlitShader;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ---------------------------------------------------------------- Materials

        public static Material Mat(Color color, bool unlit = false)
        {
            var m = new Material(unlit ? UnlitShader() : LitShader());
            SetColor(m, color);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static Shader LitShader()
        {
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = DefaultPrimitiveShader();
            return litShader;
        }

        static Shader UnlitShader()
        {
            if (unlitShader == null) unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null) unlitShader = LitShader();
            return unlitShader;
        }

        static Shader DefaultPrimitiveShader()
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var s = tmp.GetComponent<Renderer>().sharedMaterial.shader;
            Kill(tmp);
            return s;
        }

        // ---------------------------------------------------------------- 3D

        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static Transform Empty(string name, Transform parent, Vector3 localPos, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go.transform;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 localScale, Color color, bool unlit = false, bool collider = true, bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat(color, unlit);
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;

            var col = go.GetComponent<Collider>();
            if (!collider)
            {
                Kill(col);
            }
            else if (type == PrimitiveType.Cylinder)
            {
                // Basık silindirlerde kapsül collider bozuk olur; kutu ile değiştir.
                Kill(col);
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Color color, bool unlit = false, bool collider = true, bool castShadows = true) =>
            Prim(PrimitiveType.Cube, name, parent, localPos, localScale, color, unlit, collider, castShadows);

        /// <summary>Görsel renderer içermeyen, yalnızca raycast/tıklama algılayan görünmez 3D kutu.</summary>
        public static GameObject TriggerBox(string name, Transform parent, Vector3 localPos, Vector3 localSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            var col = go.AddComponent<BoxCollider>();
            col.size = localSize;
            return go;
        }

        public static Light PointLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        // ---------------------------------------------------------------- UI

        /// <summary>World-space canvas. Boyut metre cinsinden, içerik piksel cinsinden (ppm = piksel/metre).</summary>
        public static RectTransform WorldCanvas(string name, Transform parent, Vector3 localPos, Vector3 euler,
            Vector2 sizeMeters, float ppm)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMeters * ppm;
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.Euler(euler);
            rt.localScale = Vector3.one / ppm;
            return rt;
        }

        /// <summary>Sol-üst köşeye göre piksel yerleşimi (y aşağı doğru artar).</summary>
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void PlaceCenter(RectTransform rt, float offsetY, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, offsetY);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor,
            float x, float y, float w, float h, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            Place((RectTransform)go.transform, x, y, w, h);
            return t;
        }

        public static Image Panel(Transform parent, Color color, float x, float y, float w, float h)
        {
            var img = Child(parent, "Panel", color).GetComponent<Image>();
            Place((RectTransform)img.transform, x, y, w, h);
            return img;
        }

        /// <summary>Image içeren boş bir RectTransform (anchor'ları çağıran ayarlar).</summary>
        public static RectTransform Child(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        public static UIBar Bar(Transform parent, float x, float y, float w, float h, Color bg, Color fill,
            bool vertical = false)
        {
            var bgImg = Panel(parent, bg, x, y, w, h);
            var rt = Child(bgImg.transform, "Fill", fill);
            rt.pivot = Vector2.zero;
            Stretch(rt);
            var bar = new UIBar(rt, rt.GetComponent<Image>(), vertical);
            bar.Set(0f);
            return bar;
        }
    }

    /// <summary>Sprite gerektirmeyen, anchor tabanlı basit dolum barı.</summary>
    public sealed class UIBar
    {
        readonly RectTransform fill;
        readonly Image image;
        readonly bool vertical;

        public UIBar(RectTransform fill, Image image, bool vertical)
        {
            this.fill = fill;
            this.image = image;
            this.vertical = vertical;
        }

        public void Set(float value01)
        {
            if (fill == null) return;
            float v = Mathf.Clamp01(value01);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = vertical ? new Vector2(1f, v) : new Vector2(v, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        public void Set(float value01, Color color)
        {
            Set(value01);
            if (image != null) image.color = color;
        }
    }
}
