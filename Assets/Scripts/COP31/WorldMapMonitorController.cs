using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// 7 Kıtalı Gerçek Zamanlı Dinamik Dünya Haritası Monitör Kontrolcüsü:
    /// - Kaynak görseli (world_map.jpg) okur, piksel temizleme + anti-aliased kenar yumuşatma uygular.
    /// - Beyaz okyanusları retro CRT laciverti (#0B131E) yapar.
    /// - Karaları 7 bağımsız coğrafi kıtaya böler (prosedürel Antarktika dahil).
    /// - Her kıtanın İklim Sağlığına (0-100) göre karaları dinamik boyar:
    ///   * %0 (En Kötü): Koyu Kırmızı (#6E0000 / RGB 110, 0, 0)
    ///   * %50 (Kritik): Kehribar (#B86B00 / RGB 184, 107, 0)
    ///   * %100 (Ütopya): Koyu Yeşil (#0A5C1A / RGB 10, 92, 26)
    /// - Kriz anında o kıta parlak alarm kırmızısı ile yanıp söner (pulse efekti).
    /// - Piksel-kusursuz kara maskelemesi ile okyanusa tıklamaları engeller.
    /// </summary>
    public class WorldMapMonitorController : MonoBehaviour
    {
        public static WorldMapMonitorController Instance { get; private set; }

        public const int MapWidth = 1000;
        public const int MapHeight = 667;

        [System.Serializable]
        public class ContinentState
        {
            public ContinentType type;
            public string name;
            public string representativeTitle;
            [Range(0f, 100f)] public float health;
            [Range(0f, 100f)] public float anger;

            // 6 Çevre Sorun Barı (0..100)
            [Range(0f, 100f)] public float tree;
            [Range(0f, 100f)] public float factory;
            [Range(0f, 100f)] public float pollution;
            [Range(0f, 100f)] public float renewable;
            [Range(0f, 100f)] public float food;
            [Range(0f, 100f)] public float zeroWaste;

            public bool inCrisis;
            public ContinentQuestion activeQuestion;
            public float crisisRemainingTime;
            public Vector2 primaryLandUV;
        }

        [Header("7 Kıta Durumları")]
        public ContinentState[] continents = new ContinentState[7];

        [Header("Ekran Referansları")]
        public RawImage monitorRawImage;

        // Dahili Dokular ve Maske Dizileri
        Texture2D dynamicMapTex;
        Color32[] displayPixels;
        sbyte[] continentMask;      // -1 = Okyanus, 0..6 = Kıta İndeksi
        float[] edgeAlpha;          // 0f = Saf Okyanus, 1f = Saf Kara, 0..1f = Anti-aliased Yumuşak Kenar
        readonly List<int>[] continentPixelIndices = new List<int>[7];
        readonly List<Vector2Int>[] continentLandCoords = new List<Vector2Int>[7];

        // Sabit Renkler
        static readonly Color32 OceanColor = new Color32(11, 19, 30, 255);       // #0B131E
        static readonly Color32 AlarmPulseRed = new Color32(255, 24, 24, 255);   // Parlak alarm kırmızısı

        bool isInitialized;
        bool hasAnyActiveCrisis;

        void Awake()
        {
            Instance = this;
            InitializeContinentData();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (dynamicMapTex != null)
            {
                Destroy(dynamicMapTex);
                dynamicMapTex = null;
            }
        }

        void Start()
        {
            if (!isInitialized)
            {
                InitializeMap();
            }
        }

        void Update()
        {
            if (!isInitialized) return;

            // Eğer aktif krizde olan bir kıta varsa, yanıp sönme (pulse) efekti için dokuyu güncelle
            if (hasAnyActiveCrisis)
            {
                UpdatePulseColors();
            }
        }

        public void BindRawImage(RawImage rawImage)
        {
            monitorRawImage = rawImage;
            if (!isInitialized)
            {
                InitializeMap();
            }
            else if (dynamicMapTex != null && monitorRawImage != null)
            {
                monitorRawImage.texture = dynamicMapTex;
            }
        }

        void InitializeContinentData()
        {
            // Kullanıcı şartnamesine göre 7 kıtanın başlangıç durumları:
            continents = new ContinentState[7];

            // 0: Kuzey Amerika (%35 İklim, Yüksek Fabrika & Sıfır Atık Sorunu)
            continents[0] = new ContinentState
            {
                type = ContinentType.NorthAmerica,
                name = "Kuzey Amerika",
                representativeTitle = "Vali Jack Harper",
                health = 35f,
                anger = 60f,
                tree = 50f,
                factory = 85f,
                pollution = 70f,
                renewable = 40f,
                food = 65f,
                zeroWaste = 25f,
                primaryLandUV = new Vector2(0.22f, 0.65f)
            };

            // 1: Güney Amerika (%45 İklim, Yüksek Ormansızlaşma/Ağaç Sorunu)
            continents[1] = new ContinentState
            {
                type = ContinentType.SouthAmerica,
                name = "Güney Amerika",
                representativeTitle = "Bakan Camila Silva",
                health = 45f,
                anger = 55f,
                tree = 20f,
                factory = 45f,
                pollution = 50f,
                renewable = 60f,
                food = 55f,
                zeroWaste = 40f,
                primaryLandUV = new Vector2(0.32f, 0.32f)
            };

            // 2: Avrupa (%65 İklim, Orta-İyi Yenilenebilir Enerji Geçişi)
            continents[2] = new ContinentState
            {
                type = ContinentType.Europe,
                name = "Avrupa",
                representativeTitle = "Komisyoner Dr. Elena Rostova",
                health = 65f,
                anger = 40f,
                tree = 65f,
                factory = 50f,
                pollution = 40f,
                renewable = 75f,
                food = 70f,
                zeroWaste = 65f,
                primaryLandUV = new Vector2(0.53f, 0.72f)
            };

            // 3: Afrika (%25 İklim, Ağır Gıda Güvenliği ve Kuraklık Krizi)
            continents[3] = new ContinentState
            {
                type = ContinentType.Africa,
                name = "Afrika",
                representativeTitle = "Elçi Kwame Mensah",
                health = 25f,
                anger = 80f,
                tree = 40f,
                factory = 30f,
                pollution = 60f,
                renewable = 30f,
                food = 15f,
                zeroWaste = 30f,
                primaryLandUV = new Vector2(0.52f, 0.45f)
            };

            // 4: Asya (%20 İklim, Çok Yüksek Fabrika, Çevre Kirliliği ve Fosil Yakıt)
            continents[4] = new ContinentState
            {
                type = ContinentType.Asia,
                name = "Asya",
                representativeTitle = "Bakan Li Wei",
                health = 20f,
                anger = 85f,
                tree = 30f,
                factory = 95f,
                pollution = 90f,
                renewable = 35f,
                food = 40f,
                zeroWaste = 20f,
                primaryLandUV = new Vector2(0.74f, 0.65f)
            };

            // 5: Okyanusya (%55 İklim, Orta Seviye İklim Durumu)
            continents[5] = new ContinentState
            {
                type = ContinentType.Oceania,
                name = "Okyanusya",
                representativeTitle = "Temsilci Liam O'Connor",
                health = 55f,
                anger = 50f,
                tree = 55f,
                factory = 40f,
                pollution = 45f,
                renewable = 60f,
                food = 60f,
                zeroWaste = 50f,
                primaryLandUV = new Vector2(0.85f, 0.30f)
            };

            // 6: Antarktika (%40 İklim, Küresel Isınmaya Bağlı Erime Riski)
            continents[6] = new ContinentState
            {
                type = ContinentType.Antarctica,
                name = "Antarktika",
                representativeTitle = "İstasyon Şefi Dr. Sven Lindholm",
                health = 40f,
                anger = 70f,
                tree = 10f,
                factory = 10f,
                pollution = 50f,
                renewable = 50f,
                food = 30f,
                zeroWaste = 70f,
                primaryLandUV = new Vector2(0.50f, 0.05f)
            };
        }

        public void InitializeMap()
        {
            if (isInitialized) return;

            for (int i = 0; i < 7; i++)
            {
                continentPixelIndices[i] = new List<int>(35000);
                continentLandCoords[i] = new List<Vector2Int>(35000);
            }

            int totalPixels = MapWidth * MapHeight;
            continentMask = new sbyte[totalPixels];
            edgeAlpha = new float[totalPixels];
            displayPixels = new Color32[totalPixels];

            // 1. Kaynak Görseli Yükle
            Texture2D src = LoadSourceMapTexture();

            // 2. Harita Piksellerini Çözümle ve 7 Kıtaya Böl
            ProcessSourceImageAndBuildMasks(src);

            if (src != null)
            {
                Destroy(src);
            }

            // 3. Çıkış Dokusu Oluştur (1000 x 667)
            dynamicMapTex = new Texture2D(MapWidth, MapHeight, TextureFormat.RGBA32, false);
            dynamicMapTex.name = "Dynamic_World_Map_Monitors";
            dynamicMapTex.filterMode = FilterMode.Bilinear;
            dynamicMapTex.wrapMode = TextureWrapMode.Clamp;

            // 4. Başlangıç Renklerini Bas
            RedrawEntireMap();

            if (monitorRawImage != null)
            {
                monitorRawImage.texture = dynamicMapTex;
                monitorRawImage.color = Color.white;
            }

            isInitialized = true;
            Debug.Log("[WorldMapMonitorController] 7 Kıta Haritası ve Piksel Maskeleri Başarıyla Yüklendi!");
        }

        Texture2D LoadSourceMapTexture()
        {
            // Önce Resources üzerinden dene
            var res = Resources.Load<Texture2D>("world_map");
            if (res != null && res.isReadable)
            {
                return res;
            }

            // Doğrudan diskten byte oku
            string path = Path.Combine(Application.dataPath, "Resources/world_map.jpg");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    return tex;
                }
            }

            Debug.LogWarning("[WorldMapMonitorController] world_map.jpg doğrudan bulunamadı, fallback oluşturuluyor.");
            return WorldMapTextureGenerator.GetOrCreateMapTexture();
        }

        void ProcessSourceImageAndBuildMasks(Texture2D src)
        {
            int srcW = src.width;
            int srcH = src.height;

            for (int y = 0; y < MapHeight; y++)
            {
                // Unity Texture2D koordinatlarında y=0 alttadır.
                // Standart resim koordinatında ise y_bitmap=0 üsttedir.
                int yBitmap = (MapHeight - 1) - y;
                float v = (float)y / (MapHeight - 1);

                for (int x = 0; x < MapWidth; x++)
                {
                    float u = (float)x / (MapWidth - 1);
                    int idx = y * MapWidth + x;

                    // A. Antarktika Prosedürel Şeridi (Görselin alt kısmına ekleme kuralı)
                    float antarcticaCoastBitmapY = 595f + Mathf.Sin(x * 0.035f) * 14f + Mathf.Cos(x * 0.08f) * 7f + Mathf.Sin(x * 0.15f) * 3f;
                    if (yBitmap >= antarcticaCoastBitmapY && x >= 30 && x <= 970)
                    {
                        continentMask[idx] = 6; // Antarktika
                        edgeAlpha[idx] = 1.0f;
                        continentPixelIndices[6].Add(idx);
                        continentLandCoords[6].Add(new Vector2Int(x, y));
                        continue;
                    }

                    // B. Kaynak Görselden Piksel Oku
                    Color srcPixel = src.GetPixelBilinear(u, v);
                    float lum = srcPixel.r * 0.299f + srcPixel.g * 0.587f + srcPixel.b * 0.114f;

                    // Okyanus ve Kara Ayrımı:
                    // Beyaz (#FFFFFF) = Okyanus (lum > 0.65f)
                    // Siyah (#000000) = Kara (lum <= 0.35f)
                    // 0.35f..0.65f = Kıyı Kenar Yumuşatma (Anti-Aliasing)
                    if (lum > 0.70f)
                    {
                        // Kesin Okyanus
                        continentMask[idx] = -1;
                        edgeAlpha[idx] = 0f;
                    }
                    else
                    {
                        // Kara veya Kıyı Sınırı
                        int continent = ClassifyContinent(x, yBitmap);
                        continentMask[idx] = (sbyte)continent;

                        // Anti-aliased kenar yumuşatma: 0.70'te 0, 0.30'da 1
                        float alpha = Mathf.Clamp01((0.70f - lum) / 0.40f);
                        edgeAlpha[idx] = alpha;

                        if (alpha >= 0.5f)
                        {
                            continentPixelIndices[continent].Add(idx);
                            continentLandCoords[continent].Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }

        static int ClassifyContinent(int x, int yBitmap)
        {
            // Amerika Kıtaları (x < 420)
            if (x < 420)
            {
                // Panama Kıstağı ve Kolombiya Ayrımı
                if (yBitmap >= 360) return 1; // Güney Amerika
                if (x >= 270 && yBitmap >= 345) return 1;
                return 0; // Kuzey Amerika
            }

            // Okyanusya (Avustralya & Yeni Zelanda)
            if (x >= 730 && yBitmap >= 415) return 5;

            // Afrika (x: 420..620, Akdeniz'in güneyi y >= 265)
            if (x >= 420 && x <= 620 && yBitmap >= 265)
            {
                // Sina Yarımadası / Süveyş Doğusu Asya'dır (Ortadoğu)
                if (x > 595 && yBitmap < 320) return 4;
                return 3; // Afrika
            }

            // Avrupa (x: 420..610, y < 265)
            if (x >= 420 && x < 610 && yBitmap < 265)
            {
                return 2; // Avrupa
            }

            // Asya (Geriye kalan tüm Avrasya sahası)
            return 4;
        }

        public void RedrawEntireMap()
        {
            if (dynamicMapTex == null || continentMask == null) return;

            // Her kıtanın şu anki rengini hesapla
            Color32[] continentColors = new Color32[7];
            for (int i = 0; i < 7; i++)
            {
                continentColors[i] = GetContinentCurrentColor(continents[i]);
            }

            int len = displayPixels.Length;
            for (int i = 0; i < len; i++)
            {
                sbyte mask = continentMask[i];
                if (mask < 0)
                {
                    displayPixels[i] = OceanColor;
                }
                else
                {
                    float a = edgeAlpha[i];
                    if (a >= 0.98f)
                    {
                        displayPixels[i] = continentColors[mask];
                    }
                    else if (a <= 0.02f)
                    {
                        displayPixels[i] = OceanColor;
                    }
                    else
                    {
                        // Kenar yumuşatma harmanlaması (Anti-Aliased Blend)
                        displayPixels[i] = BlendColor(OceanColor, continentColors[mask], a);
                    }
                }
            }

            dynamicMapTex.SetPixels32(displayPixels);
            dynamicMapTex.Apply(false);
        }

        void UpdatePulseColors()
        {
            if (dynamicMapTex == null) return;

            // Sadece krizde olan kıtaları güncelle
            float pulse = Mathf.PingPong(Time.time * 3.2f, 1f);

            bool anyActive = false;
            for (int c = 0; c < 7; c++)
            {
                if (!continents[c].inCrisis) continue;
                anyActive = true;

                Color32 baseCol = GetHealthColor(continents[c].health);
                Color32 pulseCol = BlendColor(baseCol, AlarmPulseRed, pulse);

                var list = continentPixelIndices[c];
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    int idx = list[i];
                    float a = edgeAlpha[idx];
                    if (a >= 0.95f)
                        displayPixels[idx] = pulseCol;
                    else
                        displayPixels[idx] = BlendColor(OceanColor, pulseCol, a);
                }
            }

            hasAnyActiveCrisis = anyActive;
            dynamicMapTex.SetPixels32(displayPixels);
            dynamicMapTex.Apply(false);
        }

        Color32 GetContinentCurrentColor(ContinentState cont)
        {
            Color32 baseCol = GetHealthColor(cont.health);
            if (cont.inCrisis)
            {
                float pulse = Mathf.PingPong(Time.time * 3.2f, 1f);
                return BlendColor(baseCol, AlarmPulseRed, pulse);
            }
            return baseCol;
        }

        /// <summary>
        /// İklim Sağlığına (0 - 100) göre dinamik renk fonksiyonu:
        /// %0: Koyu Kırmızı (#6E0000 / RGB 110, 0, 0)
        /// %50: Kehribar / Koyu Turuncu-Sarı (#B86B00 / RGB 184, 107, 0)
        /// %100: Koyu Yeşil (#0A5C1A / RGB 10, 92, 26)
        /// </summary>
        public static Color32 GetHealthColor(float health)
        {
            health = Mathf.Clamp(health, 0f, 100f);

            if (health <= 50f)
            {
                float t = health / 50f;
                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(110f, 184f, t));
                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(0f, 107f, t));
                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(0f, 0f, t));
                return new Color32(r, g, b, 255);
            }
            else
            {
                float t = (health - 50f) / 50f;
                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(184f, 10f, t));
                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(107f, 92f, t));
                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(0f, 26f, t));
                return new Color32(r, g, b, 255);
            }
        }

        static Color32 BlendColor(Color32 bg, Color32 fg, float t)
        {
            byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(bg.r, fg.r, t));
            byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(bg.g, fg.g, t));
            byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(bg.b, fg.b, t));
            return new Color32(r, g, b, 255);
        }

        // =========================================================================
        // PİKSEL-KUSURSUZ ETKİLEŞİM VE YARDIMCI METOTLAR
        // =========================================================================

        /// <summary>
        /// UV (0..1) noktasının hangi kıtaya ait olduğunu döner.
        /// Eğer nokta okyanustaysa -1 döner (kesin okyanus kuralı!).
        /// </summary>
        public int GetContinentAtUV(Vector2 uv)
        {
            if (continentMask == null) return -1;

            int px = Mathf.Clamp(Mathf.RoundToInt(uv.x * (MapWidth - 1)), 0, MapWidth - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(uv.y * (MapHeight - 1)), 0, MapHeight - 1);

            int idx = py * MapWidth + px;
            return continentMask[idx];
        }

        /// <summary>
        /// Verilen koordinatın aktif kara parçası olup olmadığını doğrular.
        /// </summary>
        public bool IsLandmass(int px, int py)
        {
            if (px < 0 || px >= MapWidth || py < 0 || py >= MapHeight) return false;
            int idx = py * MapWidth + px;
            return continentMask != null && continentMask[idx] >= 0;
        }

        /// <summary>
        /// İstenen kıtanın garantili bir kara parçası pikselini döner. Asla okyanus dönmez.
        /// </summary>
        public Vector2Int GetRandomLandPixel(ContinentType continent)
        {
            int c = (int)continent;
            if (c >= 0 && c < 7 && continentLandCoords[c] != null && continentLandCoords[c].Count > 0)
            {
                int r = UnityEngine.Random.Range(0, continentLandCoords[c].Count);
                return continentLandCoords[c][r];
            }
            return new Vector2Int(MapWidth / 2, MapHeight / 2);
        }

        /// <summary>
        /// İstenen kıtanın garantili kara UV koordinatını döner.
        /// </summary>
        public Vector2 GetLandUV(ContinentType continent)
        {
            var p = GetRandomLandPixel(continent);
            return new Vector2(p.x / (float)MapWidth, p.y / (float)MapHeight);
        }

        public ContinentState GetContinent(ContinentType type)
        {
            int idx = (int)type;
            if (idx >= 0 && idx < continents.Length) return continents[idx];
            return null;
        }

        public void TriggerContinentCrisis(ContinentType type, ContinentQuestion question, float duration)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= continents.Length) return;

            continents[idx].inCrisis = true;
            continents[idx].activeQuestion = question;
            continents[idx].crisisRemainingTime = duration;
            hasAnyActiveCrisis = true;

            Debug.Log($"[WorldMapMonitorController] {continents[idx].name} kıtasında kriz patlak verdi: {question.title}");
        }

        public void ResolveContinentCrisis(ContinentType type, BlindChoice chosenOption)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= continents.Length) return;

            var cont = continents[idx];
            cont.inCrisis = false;
            cont.activeQuestion = null;
            cont.crisisRemainingTime = 0f;

            if (chosenOption != null)
            {
                // Kıta değerlerini güncelle
                cont.health = Mathf.Clamp(cont.health + chosenOption.continentHealthDelta, 0f, 100f);
                cont.anger = Mathf.Clamp(cont.anger + chosenOption.angerDelta, 0f, 100f);

                cont.tree = Mathf.Clamp(cont.tree + chosenOption.treeDelta, 0f, 100f);
                cont.factory = Mathf.Clamp(cont.factory + chosenOption.factoryDelta, 0f, 100f);
                cont.pollution = Mathf.Clamp(cont.pollution + chosenOption.pollutionDelta, 0f, 100f);
                cont.renewable = Mathf.Clamp(cont.renewable + chosenOption.renewableDelta, 0f, 100f);
                cont.food = Mathf.Clamp(cont.food + chosenOption.foodDelta, 0f, 100f);
                cont.zeroWaste = Mathf.Clamp(cont.zeroWaste + chosenOption.zeroWasteDelta, 0f, 100f);
            }

            // Kriz durumunu yeniden kontrol et ve tüm haritayı yeni sağlık rengiyle güncelle
            hasAnyActiveCrisis = false;
            for (int i = 0; i < 7; i++)
            {
                if (continents[i].inCrisis) hasAnyActiveCrisis = true;
            }

            RedrawEntireMap();
        }
    }
}
