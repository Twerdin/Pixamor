using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>
    /// Gerçek dünya haritası kıta sınırlarını ve taktiksel radar ızgarasını
    /// kodla sıfırdan yüksek çözünürlüklü (1024x512) Texture2D olarak üreten mimari jeneratör.
    /// Dışarıdan hiçbir görsel asset gerektirmez; scanline rasterization ile milisaniyeler içinde üretir.
    /// </summary>
    public static class WorldMapTextureGenerator
    {
        static Texture2D cachedTexture;

        public static Texture2D GetOrCreateMapTexture()
        {
            if (cachedTexture != null) return cachedTexture;

            const int width = 1024;
            const int height = 512;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.name = "Tactical_World_Map_Generated";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[width * height];

            // 1. Okyanus Arka Planı (Derin Taktik Lacivert)
            Color32 oceanColor = new Color32(8, 16, 28, 255);
            Color32 gridColor = new Color32(24, 45, 70, 255);
            Color32 equatorColor = new Color32(40, 75, 110, 255);

            for (int y = 0; y < height; y++)
            {
                // Enlem (Latitude) -90 .. +90
                float lat = (y / (float)height) * 180f - 90f;
                bool isEquator = Mathf.Abs(lat) < 1.0f;
                bool isLatGrid = Mathf.Abs(lat % 30f) < 0.8f;

                for (int x = 0; x < width; x++)
                {
                    // Boylam (Longitude) -180 .. +180
                    float lon = (x / (float)width) * 360f - 180f;
                    bool isLonGrid = Mathf.Abs(lon % 30f) < 0.8f;
                    bool isPrimeMeridian = Mathf.Abs(lon) < 0.9f;

                    if (isEquator || isPrimeMeridian)
                        pixels[y * width + x] = equatorColor;
                    else if (isLatGrid || isLonGrid)
                        pixels[y * width + x] = gridColor;
                    else
                        pixels[y * width + x] = oceanColor;
                }
            }

            // 2. Kıtaların Gerçek Coğrafi Poligonları (Yüksek Kontrastlı Taktik Renkler)
            Color32 landFill = new Color32(22, 52, 78, 255);      // Belirgin taktiksel kara dolgusu
            Color32 coastColor = new Color32(0, 240, 255, 255);   // Parlak neon siber kıyı şeridi
            Color32 coastGlow = new Color32(0, 140, 200, 180);    // Kıyı dış parlaması

            var polygons = GetWorldPolygons();

            // Scanline Fill ile karaları doldur
            foreach (var poly in polygons)
            {
                FillPolygon(pixels, width, height, poly, landFill);
            }

            // Kıyı hatlarını (Borders) çiz
            foreach (var poly in polygons)
            {
                DrawPolygonOutline(pixels, width, height, poly, coastColor, coastGlow);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true); // makeNoLongerReadable = true for performance
            cachedTexture = tex;
            return tex;
        }

        static void FillPolygon(Color32[] pixels, int width, int height, Vector2[] geoPoly, Color32 fill)
        {
            int n = geoPoly.Length;
            if (n < 3) return;

            // Piksel koordinatlarına dönüştür
            var pts = new Vector2[n];
            float minY = height;
            float maxY = 0;

            for (int i = 0; i < n; i++)
            {
                float px = (geoPoly[i].x + 180f) / 360f * (width - 1);
                float py = (geoPoly[i].y + 90f) / 180f * (height - 1);
                pts[i] = new Vector2(px, py);
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
            }

            int iMinY = Mathf.Max(0, Mathf.FloorToInt(minY));
            int iMaxY = Mathf.Min(height - 1, Mathf.CeilToInt(maxY));

            var nodeX = new List<float>(32);

            // Scanline döngüsü
            for (int y = iMinY; y <= iMaxY; y++)
            {
                nodeX.Clear();
                float fy = y + 0.5f;

                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    float y1 = pts[i].y;
                    float y2 = pts[j].y;

                    if ((y1 < fy && y2 >= fy) || (y2 < fy && y1 >= fy))
                    {
                        float x = pts[i].x + (fy - y1) / (y2 - y1) * (pts[j].x - pts[i].x);
                        nodeX.Add(x);
                    }
                }

                nodeX.Sort();

                for (int i = 0; i < nodeX.Count - 1; i += 2)
                {
                    int startX = Mathf.Max(0, Mathf.FloorToInt(nodeX[i]));
                    int endX = Mathf.Min(width - 1, Mathf.CeilToInt(nodeX[i + 1]));

                    for (int x = startX; x <= endX; x++)
                    {
                        pixels[y * width + x] = fill;
                    }
                }
            }
        }

        static void DrawPolygonOutline(Color32[] pixels, int width, int height, Vector2[] geoPoly, Color32 stroke, Color32 glow)
        {
            int n = geoPoly.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int x0 = Mathf.RoundToInt((geoPoly[i].x + 180f) / 360f * (width - 1));
                int y0 = Mathf.RoundToInt((geoPoly[i].y + 90f) / 180f * (height - 1));
                int x1 = Mathf.RoundToInt((geoPoly[j].x + 180f) / 360f * (width - 1));
                int y1 = Mathf.RoundToInt((geoPoly[j].y + 90f) / 180f * (height - 1));

                DrawLine(pixels, width, height, x0, y0, x1, y1, stroke, glow);
            }
        }

        static void DrawLine(Color32[] pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 stroke, Color32 glow)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < w && y0 >= 0 && y0 < h)
                {
                    pixels[y0 * w + x0] = stroke;

                    // 1 piksel kalınlık / hafif parlama
                    if (x0 + 1 < w) pixels[y0 * w + (x0 + 1)] = glow;
                    if (x0 - 1 >= 0) pixels[y0 * w + (x0 - 1)] = glow;
                    if (y0 + 1 < h) pixels[(y0 + 1) * w + x0] = glow;
                    if (y0 - 1 >= 0) pixels[(y0 - 1) * w + x0] = glow;
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        static List<Vector2[]> GetWorldPolygons()
        {
            return new List<Vector2[]>
            {
                // 1. KUZEY AMERİKA (Alaska, Kanada, ABD, Meksika, Orta Amerika)
                new Vector2[]
                {
                    new Vector2(-168, 66), new Vector2(-163, 71), new Vector2(-141, 70), new Vector2(-130, 69),
                    new Vector2(-115, 68), new Vector2(-95, 69), new Vector2(-82, 66), new Vector2(-80, 52),
                    new Vector2(-75, 58), new Vector2(-64, 60), new Vector2(-56, 53), new Vector2(-60, 46),
                    new Vector2(-66, 44), new Vector2(-71, 42), new Vector2(-75, 37), new Vector2(-76, 34),
                    new Vector2(-81, 31), new Vector2(-80, 25), new Vector2(-82, 28), new Vector2(-84, 30),
                    new Vector2(-90, 29), new Vector2(-97, 26), new Vector2(-97, 21), new Vector2(-91, 19),
                    new Vector2(-87, 18), new Vector2(-87, 21), new Vector2(-88, 16), new Vector2(-83, 10),
                    new Vector2(-79, 8),  new Vector2(-82, 8),  new Vector2(-85, 13), new Vector2(-93, 16),
                    new Vector2(-100, 17), new Vector2(-105, 20), new Vector2(-110, 23), new Vector2(-115, 32),
                    new Vector2(-110, 28), new Vector2(-112, 25), new Vector2(-115, 28), new Vector2(-118, 34),
                    new Vector2(-124, 40), new Vector2(-125, 48), new Vector2(-130, 54), new Vector2(-135, 58),
                    new Vector2(-146, 60), new Vector2(-156, 58), new Vector2(-165, 55), new Vector2(-168, 60),
                    new Vector2(-165, 64)
                },

                // 2. GÜNEY AMERİKA (Brezilya, Arjantin, Şili, Peru, Kolombiya)
                new Vector2[]
                {
                    new Vector2(-77, 8),  new Vector2(-72, 12), new Vector2(-63, 11), new Vector2(-53, 5),
                    new Vector2(-48, 0),  new Vector2(-35, -5), new Vector2(-38, -13), new Vector2(-41, -21),
                    new Vector2(-48, -26), new Vector2(-53, -33), new Vector2(-58, -35), new Vector2(-65, -42),
                    new Vector2(-66, -47), new Vector2(-68, -52), new Vector2(-66, -55), new Vector2(-74, -53),
                    new Vector2(-74, -45), new Vector2(-72, -37), new Vector2(-71, -30), new Vector2(-70, -20),
                    new Vector2(-76, -14), new Vector2(-80, -5), new Vector2(-80, -1), new Vector2(-78, 2)
                },

                // 3. GRÖNLAND
                new Vector2[]
                {
                    new Vector2(-44, 60), new Vector2(-35, 65), new Vector2(-22, 70), new Vector2(-18, 77),
                    new Vector2(-12, 82), new Vector2(-30, 83), new Vector2(-45, 83), new Vector2(-60, 82),
                    new Vector2(-68, 76), new Vector2(-55, 71), new Vector2(-52, 64)
                },

                // 4. AFRİKA
                new Vector2[]
                {
                    new Vector2(-6, 36),  new Vector2(10, 37),  new Vector2(15, 32),  new Vector2(25, 32),
                    new Vector2(30, 31),  new Vector2(32, 31),  new Vector2(34, 28),  new Vector2(38, 22),
                    new Vector2(43, 13),  new Vector2(51, 11),  new Vector2(44, 4),   new Vector2(40, -4),
                    new Vector2(40, -11), new Vector2(36, -20), new Vector2(33, -27), new Vector2(31, -30),
                    new Vector2(26, -34), new Vector2(19, -34), new Vector2(18, -30), new Vector2(14, -23),
                    new Vector2(12, -16), new Vector2(9, 1),    new Vector2(8, 4),    new Vector2(3, 6),
                    new Vector2(-1, 5),   new Vector2(-8, 4),   new Vector2(-13, 9),  new Vector2(-17, 15),
                    new Vector2(-16, 21), new Vector2(-13, 28), new Vector2(-10, 30)
                },

                // 5. AVRASYA (Avrupa, Rusya, Asya, Çin, Hindistan, Ortadoğu)
                new Vector2[]
                {
                    new Vector2(-9, 39),  new Vector2(-9, 43),  new Vector2(-2, 43),  new Vector2(-4, 48),
                    new Vector2(2, 51),   new Vector2(7, 54),   new Vector2(10, 57),  new Vector2(12, 56),
                    new Vector2(18, 59),  new Vector2(24, 65),  new Vector2(15, 68),  new Vector2(26, 71),
                    new Vector2(32, 69),  new Vector2(40, 68),  new Vector2(50, 68),  new Vector2(68, 73),
                    new Vector2(80, 73),  new Vector2(105, 77), new Vector2(115, 73), new Vector2(140, 72),
                    new Vector2(160, 70), new Vector2(170, 66), new Vector2(180, 65), new Vector2(170, 60),
                    new Vector2(162, 56), new Vector2(156, 51), new Vector2(143, 53), new Vector2(136, 45),
                    new Vector2(130, 42), new Vector2(126, 38), new Vector2(129, 35), new Vector2(125, 38),
                    new Vector2(122, 40), new Vector2(118, 39), new Vector2(122, 32), new Vector2(120, 27),
                    new Vector2(114, 22), new Vector2(109, 21), new Vector2(106, 10), new Vector2(101, 7),
                    new Vector2(103, 1),  new Vector2(99, 8),   new Vector2(96, 17),  new Vector2(90, 22),
                    new Vector2(86, 20),  new Vector2(80, 13),  new Vector2(77, 8),   new Vector2(73, 15),
                    new Vector2(73, 19),  new Vector2(69, 23),  new Vector2(67, 25),  new Vector2(60, 25),
                    new Vector2(56, 26),  new Vector2(50, 26),  new Vector2(48, 30),  new Vector2(51, 25),
                    new Vector2(55, 24),  new Vector2(59, 22),  new Vector2(54, 17),  new Vector2(44, 13),
                    new Vector2(39, 21),  new Vector2(35, 28),  new Vector2(35, 32),  new Vector2(36, 36),
                    new Vector2(31, 36),  new Vector2(27, 37),  new Vector2(26, 40),  new Vector2(29, 41),
                    new Vector2(35, 42),  new Vector2(41, 41),  new Vector2(37, 45),  new Vector2(30, 46),
                    new Vector2(28, 42),  new Vector2(24, 38),  new Vector2(21, 38),  new Vector2(20, 40),
                    new Vector2(18, 42),  new Vector2(13, 45),  new Vector2(16, 41),  new Vector2(15, 38),
                    new Vector2(14, 41),  new Vector2(12, 42),  new Vector2(9, 44),   new Vector2(5, 43),
                    new Vector2(3, 42),   new Vector2(0, 39),   new Vector2(-2, 37),  new Vector2(-5, 36),
                    new Vector2(-6, 36),  new Vector2(-9, 37)
                },

                // 6. AVUSTRALYA
                new Vector2[]
                {
                    new Vector2(142, -11), new Vector2(145, -15), new Vector2(150, -23), new Vector2(153, -28),
                    new Vector2(151, -34), new Vector2(147, -38), new Vector2(141, -38), new Vector2(138, -35),
                    new Vector2(134, -33), new Vector2(124, -33), new Vector2(118, -35), new Vector2(115, -34),
                    new Vector2(115, -32), new Vector2(114, -26), new Vector2(114, -22), new Vector2(119, -20),
                    new Vector2(122, -17), new Vector2(128, -15), new Vector2(131, -12), new Vector2(136, -12),
                    new Vector2(136, -16), new Vector2(141, -17)
                },

                // 7. BÜYÜK BRİTANYA VE İRLANDA
                new Vector2[]
                {
                    new Vector2(-5, 50), new Vector2(-1, 51), new Vector2(1, 51), new Vector2(1, 53),
                    new Vector2(0, 54),  new Vector2(-2, 57), new Vector2(-3, 58), new Vector2(-5, 58),
                    new Vector2(-6, 56), new Vector2(-4, 55), new Vector2(-3, 53), new Vector2(-5, 52)
                },

                // 8. JAPONYA
                new Vector2[]
                {
                    new Vector2(130, 31), new Vector2(133, 33), new Vector2(135, 34), new Vector2(140, 35),
                    new Vector2(142, 40), new Vector2(141, 43), new Vector2(145, 44), new Vector2(142, 45),
                    new Vector2(140, 42), new Vector2(137, 37), new Vector2(133, 36)
                },

                // 9. MADAGASKAR
                new Vector2[]
                {
                    new Vector2(49, -12), new Vector2(50, -16), new Vector2(48, -25),
                    new Vector2(44, -25), new Vector2(44, -18), new Vector2(47, -13)
                },

                // 10. ENDONEZYA (Sumatra - Java)
                new Vector2[]
                {
                    new Vector2(95, 5),   new Vector2(105, -6), new Vector2(114, -8),
                    new Vector2(106, -6), new Vector2(98, 3)
                },

                // 11. BORNEO
                new Vector2[]
                {
                    new Vector2(109, 1),  new Vector2(117, 4),  new Vector2(119, 2),
                    new Vector2(116, -4), new Vector2(110, -3)
                },

                // 12. YENİ ZELANDA
                new Vector2[]
                {
                    new Vector2(173, -35), new Vector2(178, -38), new Vector2(175, -41),
                    new Vector2(172, -44), new Vector2(167, -46), new Vector2(170, -43)
                },

                // 13. İZLANDA
                new Vector2[]
                {
                    new Vector2(-24, 64), new Vector2(-14, 65), new Vector2(-14, 66),
                    new Vector2(-23, 66)
                }
            };
        }
    }
}
