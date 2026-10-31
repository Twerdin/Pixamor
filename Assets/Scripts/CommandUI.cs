using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor
{
    // Ekranın ortak tasarım araçları. Oyun kuralları bu sınıfa bağlı değildir.
    public sealed class CommandUI : IDisposable
    {
        public static readonly Color Background = Hex(0x09131E);
        public static readonly Color Surface = Hex(0x101F2C);
        public static readonly Color Raised = Hex(0x192D3C);
        public static readonly Color Border = Hex(0x2B4353);
        public static readonly Color White = Hex(0xEDF3F5);
        public static readonly Color Muted = Hex(0x93AAB8);
        public static readonly Color Teal = Hex(0x60E1C5);
        public static readonly Color Amber = Hex(0xF3C277);
        public static readonly Color Red = Hex(0xFF7A79);
        public static readonly Color Blue = Hex(0x81BBEE);
        private readonly TMP_FontAsset regular, semibold, display, mono;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        public Sprite Rounded { get; }
        public Sprite Circle { get; }

        public CommandUI()
        {
            var fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            regular = SdfFont(Resources.Load<Font>("CommandUI/Fonts/Barlow-Regular") ?? fallback);
            semibold = SdfFont(Resources.Load<Font>("CommandUI/Fonts/Barlow-SemiBold") ?? fallback);
            display = SdfFont(Resources.Load<Font>("CommandUI/Fonts/BarlowCondensed-SemiBold") ?? fallback);
            mono = SdfFont(Resources.Load<Font>("CommandUI/Fonts/IBMPlexMono-Regular") ?? fallback);
            Rounded = Shape(false);
            Circle = Shape(true);
        }

        private TMP_FontAsset SdfFont(Font source)
        {
            // SDF yazılar büyütülünce küçük bir harf resmini büyütmek yerine kenarı yeniden hesaplar.
            var font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048);
            font.name=source.name+" SDF";
            font.atlasTextures[0].filterMode=FilterMode.Bilinear;
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÇĞİÖŞÜçğıöşü−→‹›/+:%.,!?() ");
            owned.Add(font);
            return font;
        }
        public static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public RectTransform Panel(Transform parent, string name, float x, float y, float w, float h,
            Color color, bool border = false)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            if (border)
            {
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = Border;
                outline.effectDistance = new Vector2(1, -1);
            }
            return rect;
        }

        public TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, int size = 28,
            Color? color = null, string role = "body")
        {
            var text = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<LocalizedUiText>();
            text.font = role == "display" ? display : role == "mono" ? mono : role == "strong" ? semibold : regular;
            text.text = value;
            text.fontSize = size;
            text.color = color ?? White;
            text.richText = false;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.overflowMode = TextOverflowModes.Truncate;
            if (role == "mono") text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public TMP_Text Caption(Transform parent, string value, float x, float y, float w, float h = 28,
            Color? color = null) => Text(parent, value, x, y, w, Mathf.Max(30,h), 22, color ?? Muted, "strong");

        public TMP_Text Number(Transform parent, string value, float x, float y, float w, float h, int size = 68,
            Color? color = null)
        {
            var number = Text(parent,value,x,y,w,h,size,color,"display");
            // Display fontun satır yüksekliği büyük rakamları kesmemeli.
            number.overflowMode = TextOverflowModes.Overflow;
            return number;
        }

        public Image Icon(Transform parent, string name, float x, float y, float size, Color color)
        {
            if (!icons.TryGetValue(name, out var sprite))
            {
                var texture = Resources.Load<Texture2D>("CommandUI/Art/" + name);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new UnityEngine.Rect(0,0,texture.width,texture.height),
                        new Vector2(.5f,.5f), 100);
                    owned.Add(sprite);
                }
                icons[name] = sprite;
            }
            var image = Rect(parent, name + "Icon", x, y, size, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        public Button Button(Transform parent, string name, float x, float y, float w, float h,
            Action action, Color? color = null)
        {
            var rect = Panel(parent, name, x, y, w, h, color ?? Raised, true);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f,1.25f,1.25f,1);
            colors.pressedColor = new Color(.8f,.95f,.95f,1);
            colors.disabledColor = new Color(.55f,.62f,.68f,.85f);
            colors.fadeDuration = .13f;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return button;
        }

        public void Badge(Transform parent, string value, float x, float y, float w, Color color)
        {
            var rect = Panel(parent, "Badge", x, y, w, 38, new Color(color.r*.13f,color.g*.13f,color.b*.13f));
            var text = Text(rect,value,8,5,w-16,28,22,color,"strong");
            text.alignment = TextAlignmentOptions.Center;
        }

        public Image Bar(Transform parent, float x, float y, float width, float height, Color color,
            bool showThreshold = false)
        {
            var track = Panel(parent,"Track",x,y,width,height,Border);
            var fill = Panel(track,"Fill",0,0,width,height,color);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = fill.anchoredPosition = Vector2.zero;
            if (showThreshold) Panel(track,"CriticalMark",width*.25f,-4,2,height+8,Muted);
            return fill.GetComponent<Image>();
        }

        public static void SetBar(Image image, float value, Color color)
        {
            image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value),1);
            image.color = color;
        }

        public static Color CrisisColor(CrisisType type)
        {
            switch(type)
            {
                case CrisisType.Flood: return Blue;
                case CrisisType.Drought: return Amber;
                case CrisisType.Wildfire: return Hex(0xF99B74);
                default: return Hex(0xE8A1B9);
            }
        }

        public static string TypeName(CrisisType type)
        {
            switch(type)
            {
                case CrisisType.Flood: return "SEL";
                case CrisisType.Drought: return "KURAKLIK";
                case CrisisType.Wildfire: return "ORMAN YANGINI";
                default: return "AŞIRI SICAKLIK";
            }
        }

        private Sprite Shape(bool circle)
        {
            const int size = 64;
            var pixels = new Color32[size*size];
            for (int y=0;y<size;y++)
            for (int x=0;x<size;x++)
            {
                float dx=Mathf.Abs(x-31.5f), dy=Mathf.Abs(y-31.5f);
                float d = circle ? Mathf.Sqrt(dx*dx+dy*dy)-31f :
                    Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx-22f,0),2)+Mathf.Pow(Mathf.Max(dy-22f,0),2))-9f;
                pixels[y*size+x] = new Color32(255,255,255,(byte)(Mathf.Clamp01(.5f-d)*255));
            }
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            owned.Add(texture);
            var sprite = Sprite.Create(texture,new UnityEngine.Rect(0,0,size,size),new Vector2(.5f,.5f),
                100,0,SpriteMeshType.FullRect,circle ? Vector4.zero : new Vector4(10,10,10,10));
            owned.Add(sprite);
            return sprite;
        }

        public void Dispose()
        {
            foreach(var obj in owned) if(obj != null) UnityEngine.Object.Destroy(obj);
            owned.Clear();
        }
    }
}
