using System;

using System.Collections.Generic;

using System.IO;

using UnityEngine;

using UnityEngine.UI;

namespace Pixamor.ClimateOS

{

    /// <summary>

    /// 7 Kıtalı Gerçek Zamanlı Dinamik Dünya Haritası Monitör Kontrolcüsü:

    /// - Kaynak görseli (world_map.jpg) okur, piksel temizleme + anti-aliased kenar yumuşatma uygular.

    /// - Okyanusları koyu Slate (#0B131E) yapar.

    /// - Karaları 7 bağımsız coğrafi kıtaya böler (prosedürel Antarktika dahil).

    /// - Her kıtanın İklim Sağlığına (0-100) göre karaları dinamik boyar.

    /// - Kriz anında o kıta parlak alarm kırmızısı ile yanıp söner (pulse efekti).

    /// - Piksel-kusursuz kara maskelemesi ile okyanusa tıklamaları engeller.

    /// </summary>

    [ExecuteAlways]

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

            public string representativeName => representativeTitle;

            [Range(0f, 100f)] public float health;

            [Range(0f, 100f)] public float anger;

            [Range(0f, 100f)] public float loyalty = 25f;

            public bool isPuppet;

            public bool IsPuppet => isPuppet || loyalty >= 75f;

            // 6 Çevre Sorun Barı (0..100)

            [Range(0f, 100f)] public float tree;

            [Range(0f, 100f)] public float factory;

            [Range(0f, 100f)] public float pollution;

            [Range(0f, 100f)] public float renewable;

            [Range(0f, 100f)] public float food;

            [Range(0f, 100f)] public float zeroWaste;

            public bool inCrisis;

            public bool isDeferredDoubled;

            public bool isVerified;

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

        // Sabit Renkler (V4: Koyu Gece Laciverti #0B1528 Okyanus + Zümrüt #10B981 / Kehribar #F59E0B / Kızıl #EF4444 Kıtalar)

        static readonly Color32 OceanColor = new Color32(11, 21, 40, 255);       // #0B1528 Koyu Gece Laciverti/Arduvaz Okyanus

        static readonly Color32 AlarmPulseRed = new Color32(239, 68, 68, 255);   // #EF4444 Parlak Kızıl Kriz Vurgusu

        bool isInitialized;

        bool hasAnyActiveCrisis;

        void Awake()

        {

            Instance = this;

            if (continents == null || continents.Length < 7 || continents[0] == null)

            {

                InitializeContinentData();

            }

            if (dynamicMapTex == null || continentMask == null)

            {

                InitializeMap();

            }

        }

        void OnEnable()

        {

            Instance = this;

            if (dynamicMapTex == null || continentMask == null)

            {

                InitializeMap();

            }

        }

        void OnDestroy()

        {

            if (Instance == this) Instance = null;

            if (dynamicMapTex != null)

            {

                Greybox.Kill(dynamicMapTex);

                dynamicMapTex = null;

            }

        }

        void Start()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (Instance == null) Instance = this;

            if (continents == null || continents.Length < 7 || continents[0] == null)
            {
                InitializeContinentData();
            }

            if (!isInitialized || dynamicMapTex == null || continentMask == null)
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

            if (Instance == null) Instance = this;

            monitorRawImage = rawImage;

            if (continents == null || continents.Length < 7 || continents[0] == null)

            {

                InitializeContinentData();

            }

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

            // 0: Kuzey Amerika

            continents[0] = new ContinentState

            {

                type = ContinentType.NorthAmerica,

                name = "Kuzey Amerika",

                representativeTitle = "Başkan Jack Harper",

                health = 62f,

                anger = 60f,

                loyalty = 25f,

                tree = 50f,

                factory = 85f,

                pollution = 70f,

                renewable = 40f,

                food = 65f,

                zeroWaste = 25f,

                primaryLandUV = new Vector2(0.22f, 0.65f)

            };

            // 1: Güney Amerika

            continents[1] = new ContinentState

            {

                type = ContinentType.SouthAmerica,

                name = "Güney Amerika",

                representativeTitle = "Başkan Camila Silva",

                health = 74f,

                anger = 55f,

                loyalty = 30f,

                tree = 20f,

                factory = 45f,

                pollution = 50f,

                renewable = 60f,

                food = 55f,

                zeroWaste = 40f,

                primaryLandUV = new Vector2(0.32f, 0.32f)

            };

            // 2: Avrupa

            continents[2] = new ContinentState

            {

                type = ContinentType.Europe,

                name = "Avrupa",

                representativeTitle = "Başkan Dr. Elena Rostova",

                health = 85f,

                anger = 40f,

                loyalty = 45f,

                tree = 65f,

                factory = 50f,

                pollution = 40f,

                renewable = 75f,

                food = 70f,

                zeroWaste = 65f,

                primaryLandUV = new Vector2(0.53f, 0.72f)

            };

            // 3: Afrika

            continents[3] = new ContinentState

            {

                type = ContinentType.Africa,

                name = "Afrika",

                representativeTitle = "Başkan Kwame Mensah",

                health = 58f,

                anger = 75f,

                loyalty = 20f,

                tree = 40f,

                factory = 30f,

                pollution = 60f,

                renewable = 30f,

                food = 15f,

                zeroWaste = 30f,

                primaryLandUV = new Vector2(0.52f, 0.45f)

            };

            // 4: Asya

            continents[4] = new ContinentState

            {

                type = ContinentType.Asia,

                name = "Asya",

                representativeTitle = "Başkan Li Wei",

                health = 54f,

                anger = 80f,

                loyalty = 20f,

                tree = 30f,

                factory = 95f,

                pollution = 90f,

                renewable = 35f,

                food = 40f,

                zeroWaste = 20f,

                primaryLandUV = new Vector2(0.74f, 0.65f)

            };

            // 5: Okyanusya

            continents[5] = new ContinentState

            {

                type = ContinentType.Oceania,

                name = "Okyanusya",

                representativeTitle = "Başkan Liam O'Connor",

                health = 78f,

                anger = 50f,

                loyalty = 35f,

                tree = 55f,

                factory = 40f,

                pollution = 45f,

                renewable = 60f,

                food = 60f,

                zeroWaste = 50f,

                primaryLandUV = new Vector2(0.85f, 0.30f)

            };

            // 6: Antarktika

            continents[6] = new ContinentState

            {

                type = ContinentType.Antarctica,

                name = "Antarktika",

                representativeTitle = "Konsey Başkanı Dr. Sven Lindholm",

                health = 70f,

                anger = 65f,

                loyalty = 30f,

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

            if (isInitialized && dynamicMapTex != null && continentMask != null && displayPixels != null)

            {

                if (monitorRawImage != null) monitorRawImage.texture = dynamicMapTex;

                return;

            }

            if (continents == null || continents.Length < 7 || continents[0] == null)

            {

                InitializeContinentData();

            }

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

            Texture2D src = LoadSourceMapTexture(out bool isTempSrc);

            // 2. Harita Piksellerini Çözümle ve 7 Kıtaya Böl

            ProcessSourceImageAndBuildMasks(src);

            if (src != null && isTempSrc)

            {

                Greybox.Kill(src);

            }

            // 3. Çıkış Dokusu Oluştur (1000 x 667)

            if (dynamicMapTex == null)

            {

                dynamicMapTex = new Texture2D(MapWidth, MapHeight, TextureFormat.RGBA32, false);

                dynamicMapTex.name = "Dynamic_World_Map_Monitors";

                dynamicMapTex.filterMode = FilterMode.Bilinear;

                dynamicMapTex.wrapMode = TextureWrapMode.Clamp;

            }

            // 4. Başlangıç Renklerini Bas

            RedrawEntireMap();

            if (monitorRawImage != null)

            {

                monitorRawImage.texture = dynamicMapTex;

                monitorRawImage.color = Color.white;

            }

            isInitialized = true;

        }

        Texture2D LoadSourceMapTexture(out bool isTemp)

        {

            isTemp = false;

            var res = Resources.Load<Texture2D>("world_map");

            if (res != null && res.isReadable)

            {

                return res;

            }

            string path = Path.Combine(Application.dataPath, "Resources/world_map.jpg");

            if (File.Exists(path))

            {

                byte[] bytes = File.ReadAllBytes(path);

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (tex.LoadImage(bytes))

                {

                    isTemp = true;

                    return tex;

                }

            }

            isTemp = true;

            var fallback = new Texture2D(MapWidth, MapHeight, TextureFormat.RGBA32, false);

            return fallback;

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

                    // A. Gerçekçi Antarktika Kıyı Şeridi Silüeti (Yapay sinüs dalgası kaldırıldı)

                    float antarcticaCoastBitmapY = GetAntarcticaCoastY(x);

                    if (yBitmap >= antarcticaCoastBitmapY && x >= 15 && x <= 985)

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

        static float GetAntarcticaCoastY(int x)

        {

            // Gerçek Coğrafi Antarktika Kıyı Şeridi Silüeti (1000x667 Projeksiyon):

            // - Antarktik Yarımadası (Graham Land): x: 260..310 (Kuzeye Güney Amerika'ya uzanır)

            // - Weddell Denizi Körfezi: x: 310..380

            // - Queen Maud Land: x: 380..540

            // - Wilkes Land: x: 540..740

            // - Ross Denizi / Ross Buz Sahanlığı Körfezi: x: 740..840

            // - Marie Byrd Land: x: 840..980

            float baseY;

            if (x < 260)

            {

                baseY = Mathf.Lerp(595f, 580f, x / 260f);

            }

            else if (x <= 288) // Yarımada batı kenarı (Kuzeye çıkış)

            {

                baseY = Mathf.Lerp(580f, 542f, (x - 260f) / 28f);

            }

            else if (x <= 310) // Yarımada ucu ve doğu kıyısı

            {

                baseY = Mathf.Lerp(542f, 565f, (x - 288f) / 22f);

            }

            else if (x <= 360) // Weddell Denizi körfezi

            {

                baseY = Mathf.Lerp(565f, 618f, (x - 310f) / 50f);

            }

            else if (x <= 420) // Filchner-Ronne

            {

                baseY = Mathf.Lerp(618f, 600f, (x - 360f) / 60f);

            }

            else if (x <= 540) // Queen Maud Land

            {

                baseY = Mathf.Lerp(600f, 584f, (x - 420f) / 120f);

            }

            else if (x <= 740) // Enderby & Wilkes Land

            {

                baseY = Mathf.Lerp(584f, 576f, (x - 540f) / 200f);

            }

            else if (x <= 790) // Ross Denizi körfezi

            {

                baseY = Mathf.Lerp(576f, 628f, (x - 740f) / 50f);

            }

            else if (x <= 840) // Ross Buz Sahanlığı

            {

                baseY = Mathf.Lerp(628f, 592f, (x - 790f) / 50f);

            }

            else // Marie Byrd Land & Ellsworth Land

            {

                baseY = Mathf.Lerp(592f, 595f, (x - 840f) / 160f);

            }

            // Doğal kıyı girinti-çıkıntıları (harmonik kıvrımlar)

            float detail = Mathf.Sin(x * 0.12f) * 2.8f + Mathf.Cos(x * 0.28f) * 1.6f + Mathf.Sin(x * 0.55f) * 0.9f;

            return baseY + detail;

        }

        static int ClassifyContinent(int x, int yBitmap)

        {

            // 6. Antarktika Kontrolü

            if (yBitmap >= GetAntarcticaCoastY(x))

            {

                return 6;

            }

            // 0. Grönland & Kuzey Amerika:

            // Grönland'ın doğusu (x < 442, y < 210) tamamen Kuzey Amerika'dadır; cetvelle bölünmez!

            if (yBitmap < 210 && x < 442)

            {

                return 0; // Kuzey Amerika (Grönland dahil)

            }

            // Amerika Kıtaları (x < 420):

            if (x < 420)

            {

                // Panama Kıstağı ve Darién Boğazı doğal sınırı

                if (yBitmap >= 365 || (x >= 268 && yBitmap >= 352))

                {

                    return 1; // Güney Amerika

                }

                return 0; // Kuzey Amerika

            }

            // 5. Okyanusya (Avustralya, Yeni Zelanda, Papua Yeni Gine):

            // Endonezya ve Filipinler adaları Asya'da kalır (x < 790 && y < 448)

            if (x >= 730 && yBitmap >= 405)

            {

                if (x < 790 && yBitmap < 448)

                    return 4; // Asya (Endonezya)

                return 5; // Okyanusya

            }

            // 3. Afrika vs Asya / Avrupa (Süveyş ve Kızıldeniz doğal kıvrımlı sınırı):

            if (yBitmap >= 270)

            {

                // Akdeniz'den başlayıp (522, 295) Süveyş Kanalı ve Kızıldeniz boyunca (582, 420) Bab-el-Mandeb'e uzanan hat

                if (yBitmap >= 295 && yBitmap <= 420)

                {

                    float t = (yBitmap - 295f) / 125f;

                    float xRedSea = 522f + 60f * t + Mathf.Sin(t * Mathf.PI) * 8f;

                    if (x <= xRedSea) return 3; // Afrika

                    return 4; // Asya (Ortadoğu / Arap Yarımadası / İran)

                }

                else if (yBitmap > 420)

                {

                    if (x <= 620) return 3; // Sahra-altı Afrika

                    return 4; // Asya / Hint Okyanusu

                }

                else // yBitmap: 270..295 arası Akdeniz kıyısı

                {

                    if (yBitmap >= 275 && x <= 520) return 3; // Kuzey Afrika

                }

            }

            // 2. Avrupa vs 4. Asya Doğal Sınırı:

            // Ural Dağları -> Hazar Denizi -> Kafkaslar -> Karadeniz -> İstanbul Boğazı

            float xDiv;

            if (yBitmap < 220)

            {

                // Ural Dağları hattı

                float t = yBitmap / 220f;

                xDiv = 635f + (605f - 635f) * t + Mathf.Sin(t * 6f) * 5f;

            }

            else if (yBitmap < 250)

            {

                // Ural Nehri - Hazar Denizi kuzeyi

                float t = (yBitmap - 220f) / 30f;

                xDiv = 605f + (575f - 605f) * t;

            }

            else if (yBitmap < 270)

            {

                // Kafkas Dağları (Hazar Denizi ile Karadeniz arası)

                float t = (yBitmap - 250f) / 20f;

                xDiv = 575f + (540f - 575f) * t;

            }

            else if (yBitmap < 280)

            {

                // Karadeniz - İstanbul Boğazı / Marmara

                float t = (yBitmap - 270f) / 10f;

                xDiv = 540f + (488f - 540f) * t;

            }

            else

            {

                // Ege Denizi / Akdeniz

                xDiv = 488f - (yBitmap - 280f) * 1.2f;

            }

            if (x < xDiv)

            {

                return 2; // Avrupa

            }

            return 4; // Asya

        }

        public void RedrawEntireMap()

        {

            if (dynamicMapTex == null || continentMask == null) return;

            if (continents == null || continents.Length < 7 || continents[0] == null)

            {

                InitializeContinentData();

            }

            // Her kıtanın şu anki rengini hesapla

            Color32[] continentColors = new Color32[7];

            for (int i = 0; i < 7; i++)

            {

                continentColors[i] = GetContinentCurrentColor(continents != null && i < continents.Length ? continents[i] : null);

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

            if (dynamicMapTex == null || continents == null || continentPixelIndices == null) return;

            // Sadece krizde olan kıtaları güncelle

            float pulse = Application.isPlaying ? Mathf.PingPong(Time.time * 3.2f, 1f) : 0f;

            bool anyActive = false;

            for (int c = 0; c < 7; c++)

            {

                if (c >= continents.Length || continents[c] == null || !continents[c].inCrisis) continue;

                anyActive = true;

                Color32 baseCol = GetHealthColor(continents[c].health);

                Color32 pulseCol = BlendColor(baseCol, AlarmPulseRed, pulse);

                var list = continentPixelIndices[c];

                if (list == null) continue;

                int count = list.Count;

                for (int i = 0; i < count; i++)

                {

                    int idx = list[i];

                    if (idx < 0 || idx >= displayPixels.Length) continue;

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

            if (cont == null) return new Color32(16, 185, 129, 255);

            Color32 baseCol = GetHealthColor(cont.health);

            if (cont.inCrisis)

            {

                float pulse = Application.isPlaying ? Mathf.PingPong(Time.time * 3.2f, 1f) : 0.7f;

                return BlendColor(baseCol, AlarmPulseRed, pulse);

            }

            return baseCol;

        }

        /// <summary>

        /// V4 Semantik Harita Renkleri:

        /// - Dengeli Kıtalar (%65..100): Zümrüt Yeşili (#10B981)

        /// - Orta Riskli Kıtalar (%35..65): Kehribar/Altın (#F59E0B)

        /// - Kritik Kıtalar (%0..35): Parlak Kızıl/Mercan (#EF4444)

        /// </summary>

        public static Color32 GetHealthColor(float health)

        {

            health = Mathf.Clamp(health, 0f, 100f);

            if (health < 35f)

            {

                float t = health / 35f;

                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(239f, 245f, t));

                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(68f, 158f, t));

                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(68f, 11f, t));

                return new Color32(r, g, b, 255);

            }

            else if (health < 65f)

            {

                float t = (health - 35f) / 30f;

                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(245f, 16f, t));

                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(158f, 185f, t));

                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(11f, 129f, t));

                return new Color32(r, g, b, 255);

            }

            else

            {

                float t = (health - 65f) / 35f;

                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(16f, 52f, t));

                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(185f, 211f, t));

                byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(129f, 153f, t));

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

        public void SetContinentCrisisState(ContinentType type, bool inCrisis, ContinentQuestion question, float duration)

        {

            int idx = (int)type;

            if (idx < 0 || idx >= continents.Length || continents[idx] == null) return;

            continents[idx].inCrisis = inCrisis;

            continents[idx].activeQuestion = question;

            continents[idx].crisisRemainingTime = duration;

            hasAnyActiveCrisis = false;

            for (int i = 0; i < 7; i++)

            {

                if (continents[i] != null && continents[i].inCrisis)

                    hasAnyActiveCrisis = true;

            }

            RedrawEntireMap();

        }

        public void RefreshContinentVisuals(ContinentType type)

        {

            hasAnyActiveCrisis = false;

            for (int i = 0; i < 7; i++)

            {

                if (continents[i] != null && continents[i].inCrisis)

                    hasAnyActiveCrisis = true;

            }

            RedrawEntireMap();

        }

    }

}

