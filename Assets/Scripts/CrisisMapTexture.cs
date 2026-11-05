using UnityEngine;

namespace Pixamor
{
    // Dış görsel gerektirmeyen şematik dünya haritası. Siyasi sınır içermez.
    // Olay verilerindeki konumlar gibi burada da (0,0) sol üst köşedir.
    public static class CrisisMapTexture
    {
        private static readonly Vector2[][] Continents =
        {
            new[] { P(.10f,.16f), P(.20f,.09f), P(.32f,.14f), P(.34f,.25f),
                P(.28f,.35f), P(.26f,.43f), P(.31f,.50f), P(.28f,.53f),
                P(.22f,.43f), P(.17f,.37f), P(.13f,.29f) },
            new[] { P(.29f,.52f), P(.36f,.54f), P(.40f,.63f), P(.36f,.76f),
                P(.32f,.90f), P(.29f,.80f), P(.27f,.65f) },
            new[] { P(.34f,.07f), P(.41f,.06f), P(.42f,.18f), P(.37f,.24f) },
            new[] { P(.46f,.22f), P(.50f,.16f), P(.54f,.22f), P(.58f,.19f),
                P(.62f,.12f), P(.76f,.11f), P(.89f,.18f), P(.94f,.28f),
                P(.87f,.35f), P(.83f,.45f), P(.80f,.47f), P(.80f,.57f),
                P(.76f,.54f), P(.72f,.43f), P(.69f,.53f), P(.65f,.42f),
                P(.59f,.38f), P(.56f,.32f), P(.52f,.36f), P(.49f,.32f), P(.45f,.32f) },
            new[] { P(.46f,.39f), P(.53f,.37f), P(.60f,.43f), P(.61f,.54f),
                P(.57f,.65f), P(.53f,.76f), P(.49f,.66f), P(.47f,.53f), P(.43f,.47f) },
            new[] { P(.76f,.60f), P(.81f,.59f), P(.86f,.63f), P(.81f,.65f) },
            new[] { P(.81f,.72f), P(.87f,.68f), P(.92f,.73f), P(.93f,.83f),
                P(.86f,.86f), P(.80f,.81f) },
            new[] { P(.58f,.72f), P(.60f,.69f), P(.60f,.78f), P(.58f,.81f) },
            new[] { P(.94f,.85f), P(.96f,.81f), P(.97f,.84f), P(.95f,.89f) }
        };

        public static Texture2D Create()
        {
            const int width = 680;
            const int height = 360;
            var pixels = new Color32[width * height];
            Color32 water = new Color32(12, 31, 47, 255);
            Color32 grid = new Color32(23, 50, 65, 255);
            Color32 land = new Color32(48, 96, 91, 255);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool isLand = false;
                var point = new Vector2(x / (float)(width - 1), y / (float)(height - 1));
                foreach (var polygon in Continents)
                    if (Contains(point, polygon)) { isLand = true; break; }

                // Texture satırları alttan başlar; GUI koordinatlarını ters çeviriyoruz.
                pixels[(height - 1 - y) * width + x] = isLand ? land :
                    (x % 68 == 0 || y % 45 == 0 ? grid : water);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "PrototypeWorldMap";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Vector2 P(float x, float y) => new Vector2(x, y);

        private static bool Contains(Vector2 point, Vector2[] polygon)
        {
            // Yatay ışının kenarları tek sayıda kesmesi, noktanın içeride olduğunu gösterir.
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
