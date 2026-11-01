using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// V3: Aqua-Modern (Su Yeşili / Seafoam - 60-30-10 Kuralı) İklim Bakanlığı OS UI ve 3D Sahne Üretim Yardımcıları.
    /// KURAL: Hiçbir panel, kart, buton veya bar sprite = null olan düz renkli kutu OLAMAZ.
    /// Tüm UI elemanları Assets/UI/EcoModern_Sprites altındaki 25+ gerçek RGBA PNG Sprite dosyasını kullanır ve Image.color = Color.white korunur.
    /// </summary>
    public static class Greybox
    {
        static Font font;
        static Shader litShader;
        static Shader unlitShader;

        public static Vector4 GetBorderForSprite(string spriteName)
        {
            if (spriteName == "bar_bg_pill" ||
                spriteName == "bar_fill_aqua" ||
                spriteName == "bar_fill_climate" ||
                spriteName == "bar_fill_public" ||
                spriteName == "bar_timer_urgent" ||
                spriteName == "ring_budget_15s" ||
                spriteName == "minigame_target_zone" ||
                spriteName == "bar_bg_eco" ||
                spriteName == "bar_fill_eco")
            {
                return new Vector4(8, 4, 8, 4);
            }

            if (spriteName == "minigame_cursor_needle")
            {
                return new Vector4(6, 8, 6, 8);
            }

            if (spriteName == "panel_main_aqua" ||
                spriteName == "panel_telemetry_glass" ||
                spriteName == "panel_minigame_frame" ||
                spriteName == "card_stat_top_aqua" ||
                spriteName == "card_stat_climate" ||
                spriteName == "card_stat_public" ||
                spriteName == "card_stat_budget" ||
                spriteName == "card_dialogue_citizen" ||
                spriteName == "card_dialogue_president" ||
                spriteName == "badge_speaker_tag" ||
                spriteName == "btn_choice_aqua" ||
                spriteName == "btn_choice_hover" ||
                spriteName == "btn_sanction_strike" ||
                spriteName == "btn_bribe_lobby" ||
                spriteName == "btn_header_utility" ||
                spriteName == "panel_main_modal" ||
                spriteName == "card_stat_top" ||
                spriteName == "btn_action_green" ||
                spriteName == "btn_secondary")
            {
                return new Vector4(16, 16, 16, 16);
            }

            return Vector4.zero;
        }

        public static Sprite LoadSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            Vector4 border = GetBorderForSprite(spriteName);

#if UNITY_EDITOR
            string assetPath = $"Assets/UI/EcoModern_Sprites/{spriteName}.png";
            if (!File.Exists(Path.Combine(Application.dataPath, $"UI/EcoModern_Sprites/{spriteName}.png")))
            {
                assetPath = $"Assets/UI/Sprites/{spriteName}.png";
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (border != Vector4.zero && importer.spriteBorder != border)
                {
                    importer.spriteBorder = border;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            var editorSp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSp != null) return editorSp;
#endif
            var sp = Resources.Load<Sprite>($"UI/EcoModern_Sprites/{spriteName}");
            if (sp != null) return sp;

            var tex = Resources.Load<Texture2D>($"UI/EcoModern_Sprites/{spriteName}");
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            }

            string diskPath = Path.Combine(Application.dataPath, $"UI/EcoModern_Sprites/{spriteName}.png");
            if (File.Exists(diskPath))
            {
                byte[] bytes = File.ReadAllBytes(diskPath);
                var dTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (dTex.LoadImage(bytes))
                {
                    return Sprite.Create(dTex, new Rect(0, 0, dTex.width, dTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
            }
            return null;
        }

        public static void ApplySprite(Image img, string spriteName, bool sliced = true)
        {
            if (img == null) return;
            var sp = LoadSprite(spriteName);
            if (sp != null)
            {
                img.sprite = sp;
                img.color = Color.white;
                img.type = (sliced && sp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

        public static Image Icon(Transform parent, string spriteName, float x, float y, float w, float h, Color tint)
        {
            var go = new GameObject($"Icon_{spriteName}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Place(rt, x, y, w, h);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var sp = LoadSprite(spriteName);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            img.color = Color.white;
            return img;
        }

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
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            return go.transform;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 localScale, Color color, bool unlit = false, bool collider = true, bool castShadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
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
                Kill(col);
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Color color, bool unlit = false, bool collider = true, bool castShadows = true) =>
            Prim(PrimitiveType.Cube, name, parent, localPos, localScale, color, unlit, collider, castShadows);

        public static GameObject TriggerBox(string name, Transform parent, Vector3 localPos, Vector3 localSize)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            var col = go.AddComponent<BoxCollider>();
            col.size = localSize;
            return go;
        }

        public static Light PointLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        // ---------------------------------------------------------------- UI (100% PNG SPRITE TABANLI)

        public static RectTransform WorldCanvas(string name, Transform parent, Vector3 localPos, Vector3 euler,
            Vector2 sizeMeters, float ppm)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            if (Camera.main != null) canvas.worldCamera = Camera.main;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            go.AddComponent<GraphicRaycaster>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMeters * ppm;
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.Euler(euler);
            rt.localScale = Vector3.one / ppm;
            return rt;
        }

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
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = true;
            t.raycastTarget = false;
            Place((RectTransform)go.transform, x, y, w, h);
            return t;
        }

        /// <summary>
        /// 9-Slice PNG Sprite kullanan modern UI kartı / paneli / butonu oluşturur.
        /// Image.color her zaman Color.white tutulur ki PNG'nin kendi gradyanı kararmasın.
        /// </summary>
        public static RectTransform SpriteBox(Transform parent, string name, string spriteName, float x, float y, float w, float h, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = raycast;
            ApplySprite(img, spriteName, sliced: true);
            var rt = (RectTransform)go.transform;
            Place(rt, x, y, w, h);
            return rt;
        }

        public static RectTransform Child(Transform parent, string name, Color color, string spriteName = "card_stat_top_aqua")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            ApplySprite(img, spriteName, sliced: true);
            img.color = color.a <= 0.01f ? Color.clear : Color.white;
            return (RectTransform)go.transform;
        }

        public static UIBar Bar(Transform parent, float x, float y, float w, float h, Color bg, Color fill,
            bool vertical = false, string bgSprite = "bar_bg_pill", string fillSprite = "bar_fill_aqua")
        {
            var bgRt = SpriteBox(parent, "Bar_Track", bgSprite, x, y, w, h, raycast: false);
            var fillRt = SpriteBox(bgRt, "Bar_Fill", fillSprite, 0f, 0f, w, h, raycast: false);
            fillRt.pivot = Vector2.zero;
            Stretch(fillRt);

            var fillImg = fillRt.GetComponent<Image>();
            fillImg.color = Color.white;

            var bar = new UIBar(fillRt, fillImg, vertical);
            bar.Set(0.65f, Color.white);
            return bar;
        }
    }

    /// <summary>Gerçek PNG Sprite (bar_bg_pill.png ve bar_fill_aqua.png / bar_timer_urgent.png) kullanan modern oval ilerleme çubuğu.</summary>
    [System.Serializable]
    public sealed class UIBar
    {
        [SerializeField] public RectTransform fill;
        [SerializeField] public Image image;
        [SerializeField] public bool vertical;

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

            if (image != null && image.sprite == null)
            {
                Greybox.ApplySprite(image, "bar_fill_aqua", sliced: true);
            }
        }

        public void Set(float value01, Color color)
        {
            Set(value01);
            if (image != null)
            {
                if (image.sprite == null)
                    Greybox.ApplySprite(image, "bar_fill_aqua", sliced: true);
                image.color = color;
            }
        }
    }
}
