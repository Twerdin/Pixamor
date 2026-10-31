using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.COP31
{
    public enum ContinentType
    {
        NorthAmerica = 0,
        SouthAmerica = 1,
        Europe = 2,
        Africa = 3,
        Asia = 4,
        Oceania = 5,
        Antarctica = 6
    }

    [Serializable]
    public class BlindChoice
    {
        [TextArea(2, 4)]
        public string decisionText;         // SADECE ve SADECE diplomatik metin (KESİNLİKLE RAKAM, İKON VEYA ETKİ GÖSTERİLMEYECEK)
        public int moneyDelta;              // Küresel Hazine ($)
        public int halkDelta;               // Küresel Halk Memnuniyeti
        public int iklimDelta;              // Küresel İklim Dengesi
        public float continentHealthDelta;  // Kıta Sağlığı (-30..+30)
        public float angerDelta;            // Kıta Temsilcisi Siniri (-30..+30)

        // 6 Çevre Sorun Barı Değişimleri (-30..+30)
        public float treeDelta;
        public float factoryDelta;
        public float pollutionDelta;
        public float renewableDelta;
        public float foodDelta;
        public float zeroWasteDelta;

        public string outcomeSummary;       // Karar verildikten sonra alt bilgi bandında gösterilecek diplomatik sonuç
    }

    [Serializable]
    public class ContinentQuestion
    {
        public int id;
        public ContinentType continent;
        public string title;
        [TextArea(3, 6)]
        public string description;
        public BlindChoice[] choices;
    }

    public static class ContinentQuestionDatabase
    {
        static readonly List<ContinentQuestion> questions = new List<ContinentQuestion>();

        static ContinentQuestionDatabase()
        {
            BuildDatabase();
        }

        public static List<ContinentQuestion> GetAllQuestions()
        {
            if (questions.Count == 0) BuildDatabase();
            return questions;
        }

        public static List<ContinentQuestion> GetQuestionsForContinent(ContinentType continent)
        {
            if (questions.Count == 0) BuildDatabase();
            var list = new List<ContinentQuestion>();
            for (int i = 0; i < questions.Count; i++)
            {
                if (questions[i].continent == continent)
                    list.Add(questions[i]);
            }
            return list;
        }

        static void BuildDatabase()
        {
            questions.Clear();

            // =========================================================================
            // 1. KUZEY AMERİKA (Sanayi, Yüksek Emisyon, Sıfır Atık Sorunu)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 101,
                continent = ContinentType.NorthAmerica,
                title = "Sanayi Bölgesi Baca Filtresi ve Emisyon Krizi",
                description = "Kıta genelindeki ağır sanayi havzalarında kükürt ve azot oksit partikül salınımı acil sınırları aştı. Sanayi konsorsiyumu acil denetimlerin üretimi durduracağını iddia ediyor.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Bölgedeki kömür ve çelik tesislerine derhal zorunlu elektrostatik filtre şartı getir ve denetimleri başlat.",
                        moneyDelta = -25, halkDelta = 12, iklimDelta = 15,
                        continentHealthDelta = 18f, angerDelta = 15f,
                        factoryDelta = -20f, pollutionDelta = -22f, treeDelta = 5f, renewableDelta = 5f, foodDelta = 0f, zeroWasteDelta = 10f,
                        outcomeSummary = "Kuzey Amerika: Ağır sanayi tesislerine filtre zorunluluğu getirildi; hava kalitesi toparlanıyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Karasal elektrik şebekesine federal bütçeden acil güneş ve rüzgar enerjisi takviye paketi aktar.",
                        moneyDelta = -40, halkDelta = 15, iklimDelta = 18,
                        continentHealthDelta = 22f, angerDelta = -10f,
                        factoryDelta = -12f, pollutionDelta = -15f, treeDelta = 0f, renewableDelta = 25f, foodDelta = 0f, zeroWasteDelta = 5f,
                        outcomeSummary = "Kuzey Amerika: Güneş ve rüzgar takviyesi devreye alındı; yenilenebilir enerji payı yükseldi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Sanayi lobisinin talebini onayla; ceza kesmek yerine filtre yenileme takvimini gelecek yıla ertele.",
                        moneyDelta = 20, halkDelta = -18, iklimDelta = -15,
                        continentHealthDelta = -16f, angerDelta = -20f,
                        factoryDelta = 20f, pollutionDelta = 25f, treeDelta = -5f, renewableDelta = -5f, foodDelta = 0f, zeroWasteDelta = -15f,
                        outcomeSummary = "Kuzey Amerika: Sanayi denetimleri ertelendi; hava kirliliği ve zehirli duman tehlikeli seviyede."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 102,
                continent = ContinentType.NorthAmerica,
                title = "Mega Metropol Katı Atık ve Ambalaj Kirliliği",
                description = "Doğu yakasındaki mega kentlerde katı atık depolama alanları doldu; nehir havzalarına tonlarca mikroplastik ve kimyasal sızıntı tespit edildi.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Tek kullanımlık plastik ambalajları tamamen yasakla ve üretici sorumluluğu kanununu yürürlüğe koy.",
                        moneyDelta = -15, halkDelta = -5, iklimDelta = 12,
                        continentHealthDelta = 15f, angerDelta = 20f,
                        factoryDelta = -5f, pollutionDelta = -18f, treeDelta = 0f, renewableDelta = 0f, foodDelta = 5f, zeroWasteDelta = 30f,
                        outcomeSummary = "Kuzey Amerika: Tek kullanımlık plastikler yasaklandı; sıfır atık göstergeleri hızla iyileşiyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Belediye bütçeleriyle yüksek teknolojili plazma yakma ve enerji geri kazanım tesisleri kur.",
                        moneyDelta = -30, halkDelta = 10, iklimDelta = 8,
                        continentHealthDelta = 12f, angerDelta = -5f,
                        factoryDelta = 5f, pollutionDelta = -10f, treeDelta = 0f, renewableDelta = 10f, foodDelta = 0f, zeroWasteDelta = 20f,
                        outcomeSummary = "Kuzey Amerika: Plazma yakma tesisleri atık yükünü hafifletti ancak baca gazları takip gerektiriyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Atık depolama standartlarını gevşeterek çöp transferlerini kırsal arazilere yönlendir.",
                        moneyDelta = 10, halkDelta = -15, iklimDelta = -12,
                        continentHealthDelta = -14f, angerDelta = -15f,
                        factoryDelta = 0f, pollutionDelta = 22f, treeDelta = -10f, renewableDelta = 0f, foodDelta = -10f, zeroWasteDelta = -25f,
                        outcomeSummary = "Kuzey Amerika: Kırsal atık sahalarında kontrolsüz sızıntı başladı; yeraltı suları kirlendi."
                    }
                }
            });

            // =========================================================================
            // 2. GÜNEY AMERİKA (Amazon Ormanları, Ormansızlaşma, Su Havzaları)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 201,
                continent = ContinentType.SouthAmerica,
                title = "Amazon Havzası Kaçak Kesim ve Yangın Kuşağı",
                description = "Kıtanın akciğerleri olan yağmur ormanlarında soya ve hayvancılık arazisi açmak için başlatılan kaçak kundaklamalar uydu radarlarına yansıdı.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Milli Muhafızları ve insansız hava araçlarını devreye sokarak kaçak kesim sahalarına doğrudan müdahale et.",
                        moneyDelta = -20, halkDelta = 10, iklimDelta = 20,
                        continentHealthDelta = 22f, angerDelta = 25f,
                        factoryDelta = 0f, pollutionDelta = -10f, treeDelta = 30f, renewableDelta = 0f, foodDelta = -5f, zeroWasteDelta = 0f,
                        outcomeSummary = "Güney Amerika: Amazon sınırında güvenlik koridoru kuruldu; orman tahribatı durduruldu."
                    },
                    new BlindChoice
                    {
                        decisionText = "Yerel kooperatiflere ve yerli halk meclislerine doğrudan agroekoloji ve koruma tazminatı bağla.",
                        moneyDelta = -35, halkDelta = 18, iklimDelta = 16,
                        continentHealthDelta = 18f, angerDelta = -15f,
                        factoryDelta = -5f, pollutionDelta = -5f, treeDelta = 20f, renewableDelta = 5f, foodDelta = 15f, zeroWasteDelta = 5f,
                        outcomeSummary = "Güney Amerika: Yerli topluluklar koruma kalkanı oluşturdu; biyoçeşitlilik nefes aldı."
                    },
                    new BlindChoice
                    {
                        decisionText = "Tarımsal ihracat gelirlerini korumak için orman temizleme ruhsat kotalarını geçici olarak esnet.",
                        moneyDelta = 25, halkDelta = -20, iklimDelta = -22,
                        continentHealthDelta = -24f, angerDelta = -25f,
                        factoryDelta = 10f, pollutionDelta = 15f, treeDelta = -35f, renewableDelta = 0f, foodDelta = 10f, zeroWasteDelta = -5f,
                        outcomeSummary = "Güney Amerika: Yağmur ormanları hızla tahrip ediliyor; karbon yutak kapasitesi çöktü."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 202,
                continent = ContinentType.SouthAmerica,
                title = "Lityum Üçgeni ve Kuruyan Yeraltı Su Havzaları",
                description = "And Dağları eteklerindeki tuz ovalarında pil hammaddesi lityum çıkarma havuzları kırsal su kaynaklarını kurutuyor; yerli çiftçiler eylemde.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Maden işletmelerine kapalı devre tuzlu su geri kazanım teknolojisini zorunlu kıl ve su çekimini sınırla.",
                        moneyDelta = -20, halkDelta = 12, iklimDelta = 14,
                        continentHealthDelta = 16f, angerDelta = 15f,
                        factoryDelta = -10f, pollutionDelta = -18f, treeDelta = 5f, renewableDelta = 10f, foodDelta = 12f, zeroWasteDelta = 10f,
                        outcomeSummary = "Güney Amerika: Su koruma protokolü devreye girdi; yeraltı akiferleri dengelendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Çiftçi köylerine deniz suyundan arıtılmış tatlı su nakil boru hattı inşa et.",
                        moneyDelta = -35, halkDelta = 15, iklimDelta = 5,
                        continentHealthDelta = 10f, angerDelta = -10f,
                        factoryDelta = 0f, pollutionDelta = 5f, treeDelta = 0f, renewableDelta = 0f, foodDelta = 20f, zeroWasteDelta = 0f,
                        outcomeSummary = "Güney Amerika: Desalinasyon hattı köylere su taşıdı; madenler üretime devam ediyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Küresel batarya talebini karşılamak için ekolojik kısıtlamaları askıya al.",
                        moneyDelta = 30, halkDelta = -18, iklimDelta = -16,
                        continentHealthDelta = -18f, angerDelta = -20f,
                        factoryDelta = 18f, pollutionDelta = 25f, treeDelta = -10f, renewableDelta = 5f, foodDelta = -20f, zeroWasteDelta = -10f,
                        outcomeSummary = "Güney Amerika: Su kaynakları kurudu; ekolojik göç dalgası başladı."
                    }
                }
            });

            // =========================================================================
            // 3. AVRUPA (Yenilenebilir Geçiş, Karbon Kotaları, Nehir Sıcaklıkları)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 301,
                continent = ContinentType.Europe,
                title = "Ren ve Tuna Nehirlerinde Aşırı Isınma ve Soğutma Krizi",
                description = "Kıta genelinde rekor sıcak hava dalgası nehir debilerini düşürdü; nükleer reaktörler ve termik santraller nehir suyunu aşırı ısıtarak canlı yaşamını tehdit ediyor.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Reaktör ve termik santrallerin kapasitesini geçici olarak %40 kıs; açığı bölgesel güneş şebekesinden karşıla.",
                        moneyDelta = -20, halkDelta = 5, iklimDelta = 15,
                        continentHealthDelta = 15f, angerDelta = 10f,
                        factoryDelta = -15f, pollutionDelta = -12f, treeDelta = 5f, renewableDelta = 20f, foodDelta = 5f, zeroWasteDelta = 0f,
                        outcomeSummary = "Avrupa: Santral çıkış suları sınırlandı; nehir ekosisteminde kitlesel balık ölümleri engellendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Kapalı devre kuru soğutma kuleleri inşaatına hızlandırılmış acil hibe desteği sağla.",
                        moneyDelta = -40, halkDelta = 12, iklimDelta = 18,
                        continentHealthDelta = 20f, angerDelta = -12f,
                        factoryDelta = -10f, pollutionDelta = -15f, treeDelta = 0f, renewableDelta = 15f, foodDelta = 10f, zeroWasteDelta = 10f,
                        outcomeSummary = "Avrupa: Modern kuru soğutma üniteleri şebeke güvenliğini sağladı."
                    },
                    new BlindChoice
                    {
                        decisionText = "Elektrik fiyatlarını dizginlemek için nehir deşarj sıcaklık tavanını esneterek santralleri tam kapasite çalıştır.",
                        moneyDelta = 15, halkDelta = -12, iklimDelta = -14,
                        continentHealthDelta = -15f, angerDelta = -18f,
                        factoryDelta = 12f, pollutionDelta = 20f, treeDelta = -5f, renewableDelta = -10f, foodDelta = -8f, zeroWasteDelta = 0f,
                        outcomeSummary = "Avrupa: Nehir ekosistemleri çöktü; kıta nehirlerinde biyolojik yıkım uyarısı yapıldı."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 302,
                continent = ContinentType.Europe,
                title = "Karasal Rüzgar Çiftlikleri ve Kuş Göç Koridoru Çatışması",
                description = "Kuzey sahil şeridinde planlanan mega rüzgar türbini koridorları nesli tükenmekte olan deniz kuşlarının göç rotasını tıkadığı gerekçesiyle yerel meclislerde kilitlendi.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Türbinlere yapay zeka radar sensörleri entegre ederek sürü yaklaştığında otomatik durdurma protokolü uygula.",
                        moneyDelta = -15, halkDelta = 10, iklimDelta = 12,
                        continentHealthDelta = 14f, angerDelta = -5f,
                        factoryDelta = -5f, pollutionDelta = -5f, treeDelta = 10f, renewableDelta = 22f, foodDelta = 0f, zeroWasteDelta = 5f,
                        outcomeSummary = "Avrupa: Akıllı rüzgar türbinleri hem temiz enerji üretiyor hem de göç rotasını koruyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Projeyi derin deniz açıklarına (offshore yüzer türbinler) kaydır; kıyı ekolojisini tamamen serbest bırak.",
                        moneyDelta = -35, halkDelta = 14, iklimDelta = 16,
                        continentHealthDelta = 18f, angerDelta = -15f,
                        factoryDelta = -10f, pollutionDelta = -8f, treeDelta = 12f, renewableDelta = 28f, foodDelta = 5f, zeroWasteDelta = 0f,
                        outcomeSummary = "Avrupa: Açık deniz rüzgar çiftlikleri kıtanın temiz elektrik rekorunu tazeledi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Ekolojik itirazları mahkeme kararıyla reddet; türbinleri kesintisiz tam devirde çalıştır.",
                        moneyDelta = 10, halkDelta = -16, iklimDelta = -5,
                        continentHealthDelta = -8f, angerDelta = 22f,
                        factoryDelta = 0f, pollutionDelta = 0f, treeDelta = -15f, renewableDelta = 10f, foodDelta = -5f, zeroWasteDelta = 0f,
                        outcomeSummary = "Avrupa: Göçmen kuş popülasyonu ağır darbe aldı; kamuoyu protestoları büyüyor."
                    }
                }
            });

            // =========================================================================
            // 4. AFRİKA (Gıda Güvenliği, Kuraklık, Temiz Su ve Sahel Çölleşmesi)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 401,
                continent = ContinentType.Africa,
                title = "Sahel Kuşağında Kuraklık ve Büyük Çölleşme Krizi",
                description = "Kıtanın orta kuşağında yağış rejiminin çökmesi milyonlarca insanı açlık sınırına itti; Büyük Yeşil Duvar ağaçlandırma sahaları susuzluktan kuruma tehdidi altında.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Kıta çapında güneş enerjili damla sulama ve derin kuyu sondaj seferberliği ilan et.",
                        moneyDelta = -30, halkDelta = 20, iklimDelta = 18,
                        continentHealthDelta = 24f, angerDelta = -20f,
                        factoryDelta = 0f, pollutionDelta = -5f, treeDelta = 25f, renewableDelta = 20f, foodDelta = 30f, zeroWasteDelta = 5f,
                        outcomeSummary = "Afrika: Güneş enerjili sulama şebekesi binlerce hektar kurak araziyi yeniden yeşertti."
                    },
                    new BlindChoice
                    {
                        decisionText = "Acil uluslararası iklim yardım fonunu gıda ve temiz su sevkiyatı için devreye sok.",
                        moneyDelta = -20, halkDelta = 15, iklimDelta = 5,
                        continentHealthDelta = 12f, angerDelta = -10f,
                        factoryDelta = 0f, pollutionDelta = 0f, treeDelta = 5f, renewableDelta = 0f, foodDelta = 25f, zeroWasteDelta = 0f,
                        outcomeSummary = "Afrika: Acil gıda konvoyları can kaybını önledi ancak kalıcı toprak iyileştirmesi gerekiyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Bütçeyi korumak adına tarım arazilerinin terk edilerek halkın şehirlere göçüne göz yum.",
                        moneyDelta = 15, halkDelta = -25, iklimDelta = -18,
                        continentHealthDelta = -22f, angerDelta = 35f,
                        factoryDelta = 10f, pollutionDelta = 15f, treeDelta = -25f, renewableDelta = -5f, foodDelta = -35f, zeroWasteDelta = -10f,
                        outcomeSummary = "Afrika: Sahel çölleşti; milyonlarca insan iklim mültecisi olarak sınırlara yığıldı."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 402,
                continent = ContinentType.Africa,
                title = "Kongo Havzası Turbalık Petrol İmtiyaz Baskısı",
                description = "Devasa karbon stoğu tutan Kongo turbalık ormanlarında uluslararası petrol şirketleri sondaj ruhsatı istiyor. Reddedilirse kalkınma kredisi kesilmekle tehdit ediliyor.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Tüm turbalık alanları 'Dokunulmaz Biyosfer Alanı' ilan et ve karbon kredisi ihracına bağla.",
                        moneyDelta = -15, halkDelta = 10, iklimDelta = 22,
                        continentHealthDelta = 25f, angerDelta = 15f,
                        factoryDelta = -15f, pollutionDelta = -20f, treeDelta = 28f, renewableDelta = 10f, foodDelta = 5f, zeroWasteDelta = 5f,
                        outcomeSummary = "Afrika: Kongo turbalıkları kurtarıldı; milyarlarca ton metan ve karbon yer altında kilitlendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Bölgeye kırsal mikro-hidroelektrik ve eko-turizm kalkınma kredisi aktar.",
                        moneyDelta = -30, halkDelta = 14, iklimDelta = 14,
                        continentHealthDelta = 16f, angerDelta = -10f,
                        factoryDelta = -5f, pollutionDelta = -5f, treeDelta = 15f, renewableDelta = 22f, foodDelta = 10f, zeroWasteDelta = 5f,
                        outcomeSummary = "Afrika: Temiz enerji kooperatifleri orman köylülerine sürdürülebilir gelir kapısı oldu."
                    },
                    new BlindChoice
                    {
                        decisionText = "Hazineye nakit girişini garantiye almak için petrol şirketlerine sınırlı arama izni ver.",
                        moneyDelta = 35, halkDelta = -18, iklimDelta = -25,
                        continentHealthDelta = -26f, angerDelta = -15f,
                        factoryDelta = 25f, pollutionDelta = 30f, treeDelta = -30f, renewableDelta = -10f, foodDelta = -12f, zeroWasteDelta = -5f,
                        outcomeSummary = "Afrika: Petrol sondajı turbalıkları yaktı; atmosferi boğan dev bir karbon bombası patladı."
                    }
                }
            });

            // =========================================================================
            // 5. ASYA (Fabrikalar, Fosil Yakıt, Ağır Kirlilik ve Muson Taşkınları)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 501,
                continent = ContinentType.Asia,
                title = "Sanayi Mega Havzasında 'Kırmızı Alarm' Hava Kirliliği",
                description = "Doğu ve Güney Asya imalat koridorunda PM2.5 partikül yoğunluğu DSÖ sınırının 20 katına çıktı. Yüz milyonlarca insan zehirli sis soluyor, hastaneler doldu.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "En kirletici 200 kömürlü termik santrali derhal acil bakım tatiline al ve üretimi kıs.",
                        moneyDelta = -25, halkDelta = 16, iklimDelta = 22,
                        continentHealthDelta = 25f, angerDelta = 25f,
                        factoryDelta = -30f, pollutionDelta = -35f, treeDelta = 5f, renewableDelta = 15f, foodDelta = 5f, zeroWasteDelta = 10f,
                        outcomeSummary = "Asya: Zehirli duman bulutları dağıldı; kentsel hava kalitesi kritik seviyeden kurtuldu."
                    },
                    new BlindChoice
                    {
                        decisionText = "Güneş tarlaları ve nükleer baz yük kapasitesine mega ölçekli kamu finansmanı pompala.",
                        moneyDelta = -45, halkDelta = 18, iklimDelta = 24,
                        continentHealthDelta = 28f, angerDelta = -15f,
                        factoryDelta = -20f, pollutionDelta = -22f, treeDelta = 5f, renewableDelta = 35f, foodDelta = 5f, zeroWasteDelta = 5f,
                        outcomeSummary = "Asya: Tarihin en büyük temiz enerji devreye alma hamlesi başladı."
                    },
                    new BlindChoice
                    {
                        decisionText = "İhracat siparişlerini riske atmamak için fabrikalara sadece maske dağıtıp üretimi sürdür.",
                        moneyDelta = 20, halkDelta = -22, iklimDelta = -20,
                        continentHealthDelta = -24f, angerDelta = -25f,
                        factoryDelta = 25f, pollutionDelta = 30f, treeDelta = -10f, renewableDelta = -5f, foodDelta = -10f, zeroWasteDelta = -15f,
                        outcomeSummary = "Asya: Hava kirliliği rekor kırdı; sanayi şehirlerinde solunum krizi felakete dönüştü."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 502,
                continent = ContinentType.Asia,
                title = "Mekong Deltası Plastik Akıntısı ve Kıyı Çöküşü",
                description = "Güneydoğu nehirleri üzerinden okyanusa günde binlerce ton plastik ve tekstil kimyasalı dökülüyor; pirinç deltaları tuzlanma ve kirlilikle boğuşuyor.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Nehir ağızlarına dev yüzer temizleme bariyerleri kur ve kıyı fabrikalarına sıfır deşarj mecburiyeti koy.",
                        moneyDelta = -25, halkDelta = 14, iklimDelta = 16,
                        continentHealthDelta = 18f, angerDelta = 10f,
                        factoryDelta = -12f, pollutionDelta = -28f, treeDelta = 8f, renewableDelta = 0f, foodDelta = 15f, zeroWasteDelta = 25f,
                        outcomeSummary = "Asya: Nehir bariyerleri tonlarca atığı durdurdu; kıyı balıkçılığı toparlanıyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Geri dönüşüm altyapısına doğrudan yabancı sermaye teşviği ve vergi muafiyeti ver.",
                        moneyDelta = -15, halkDelta = 8, iklimDelta = 10,
                        continentHealthDelta = 12f, angerDelta = -8f,
                        factoryDelta = -5f, pollutionDelta = -15f, treeDelta = 0f, renewableDelta = 5f, foodDelta = 5f, zeroWasteDelta = 20f,
                        outcomeSummary = "Asya: Geri dönüşüm sanayisi ivme kazandı ancak nehir kirliliği tam çözülemedi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Sanayi atık standartlarını hafifletip maliyeti yerel belediyelerin bütçesine yık.",
                        moneyDelta = 10, halkDelta = -18, iklimDelta = -14,
                        continentHealthDelta = -16f, angerDelta = -15f,
                        factoryDelta = 15f, pollutionDelta = 25f, treeDelta = -10f, renewableDelta = 0f, foodDelta = -20f, zeroWasteDelta = -25f,
                        outcomeSummary = "Asya: Nehir deltaları zehir aktı; pirinç tarlaları çoraklaştı."
                    }
                }
            });

            // =========================================================================
            // 6. OKYANUSYA (Resif Ağarması, Yangınlar, Deniz Seviyesi Yükselişi)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 601,
                continent = ContinentType.Oceania,
                title = "Büyük Set Resifinde Kitlesel Mercan Ağarması",
                description = "Deniz suyu sıcaklığının 2.5 derece artmasıyla dünyanın en büyük mercan resifinin %60'ı beyazladı. Deniz ekosisteminin çöküşü balıkçılık ve turizmi vuruyor.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Kıyı şeridindeki kömür limanı genişletme projelerini iptal et ve resif üzerinde yapay gölgeleme testleri başlat.",
                        moneyDelta = -20, halkDelta = 10, iklimDelta = 18,
                        continentHealthDelta = 20f, angerDelta = 20f,
                        factoryDelta = -15f, pollutionDelta = -20f, treeDelta = 10f, renewableDelta = 15f, foodDelta = 12f, zeroWasteDelta = 5f,
                        outcomeSummary = "Okyanusya: Kömür projeleri durduruldu; resif gölgeleme programı mercan ölümlerini yavaşlattı."
                    },
                    new BlindChoice
                    {
                        decisionText = "Okyanus biyologlarına ısıya dayanıklı süper mercan klonlama laboratuvarı kur.",
                        moneyDelta = -30, halkDelta = 14, iklimDelta = 14,
                        continentHealthDelta = 16f, angerDelta = -10f,
                        factoryDelta = 0f, pollutionDelta = -10f, treeDelta = 5f, renewableDelta = 10f, foodDelta = 15f, zeroWasteDelta = 0f,
                        outcomeSummary = "Okyanusya: Isıya dayanıklı mercanlar resiflere aşılandı; deniz yaşamı tutunuyor."
                    },
                    new BlindChoice
                    {
                        decisionText = "Madencilik ve deniz ticareti koridorlarını açık tutmak için çevre itirazlarını görmezden gel.",
                        moneyDelta = 25, halkDelta = -15, iklimDelta = -18,
                        continentHealthDelta = -20f, angerDelta = -20f,
                        factoryDelta = 15f, pollutionDelta = 22f, treeDelta = -5f, renewableDelta = -5f, foodDelta = -18f, zeroWasteDelta = -5f,
                        outcomeSummary = "Okyanusya: Mercan resifleri kireç mezarlığına dönüştü; biyoçeşitlilik çöktü."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 602,
                continent = ContinentType.Oceania,
                title = "Güneydoğu Yangın Mevsimi ve Okaliptüs Ormanı Tehdidi",
                description = "Erken başlayan kurak mevsim dev orman yangınlarını tetikledi. Yangın dumanları başkentleri sardı, yerel fauna tükenme eşiğinde.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Yerli Aborjin kontrollü yakma tekniklerini ulusal yangın stratejisi olarak hemen devreye sok.",
                        moneyDelta = -15, halkDelta = 15, iklimDelta = 14,
                        continentHealthDelta = 16f, angerDelta = -15f,
                        factoryDelta = 0f, pollutionDelta = -10f, treeDelta = 22f, renewableDelta = 0f, foodDelta = 5f, zeroWasteDelta = 0f,
                        outcomeSummary = "Okyanusya: Geleneksel toprak yönetimi sayesinde yangın koridorları başarıyla kesildi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Uluslararası yangın söndürme uçağı filosu kirala ve sahil hatlarına tampon su barajları kur.",
                        moneyDelta = -35, halkDelta = 12, iklimDelta = 8,
                        continentHealthDelta = 12f, angerDelta = -5f,
                        factoryDelta = 5f, pollutionDelta = -5f, treeDelta = 15f, renewableDelta = 0f, foodDelta = 0f, zeroWasteDelta = 0f,
                        outcomeSummary = "Okyanusya: Hava filosu alevlerin yerleşim yerlerine sıçramasını önledi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Yangın bütçesini kısmak için tahliyeleri geciktirip doğal sönmeyi bekle.",
                        moneyDelta = 15, halkDelta = -25, iklimDelta = -16,
                        continentHealthDelta = -18f, angerDelta = 30f,
                        factoryDelta = 0f, pollutionDelta = 20f, treeDelta = -30f, renewableDelta = 0f, foodDelta = -10f, zeroWasteDelta = 0f,
                        outcomeSummary = "Okyanusya: Milyonlarca canlı yok oldu; kıta duman felaketiyle yüzleşti."
                    }
                }
            });

            // =========================================================================
            // 7. ANTARKTİKA (Buz Tabakası Erimesi, Metan Sızıntısı, Kutup Akıntıları)
            // =========================================================================
            questions.Add(new ContinentQuestion
            {
                id = 701,
                continent = ContinentType.Antarctica,
                title = "Thwaites (Kıyamet) Buzulunda Kritik Kırılma Alarmı",
                description = "Okyanus tabanındaki ılık dip akıntısı kıta buzul tabakasını alttan eritiyor. Dev bir buz şelfi kopmak üzere; küresel deniz seviyesini 60 cm yükseltme potansiyeli var.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Buzul tabanına yapay soğutma sondajları ve su altı akıntı saptırıcı perdeler kuracak uluslararası projeyi fonla.",
                        moneyDelta = -40, halkDelta = 12, iklimDelta = 22,
                        continentHealthDelta = 25f, angerDelta = -10f,
                        factoryDelta = -10f, pollutionDelta = -10f, treeDelta = 0f, renewableDelta = 15f, foodDelta = 5f, zeroWasteDelta = 15f,
                        outcomeSummary = "Antarktika: Jeo-mühendislik bariyerleri dip sıcaklığını düşürdü; buzul çatlağı durduruldu."
                    },
                    new BlindChoice
                    {
                        decisionText = "Kıta genelinde tüm fosil yakıtlı araştırma araçlarını hidrojen ve rüzgar enerjisine dönüştür.",
                        moneyDelta = -20, halkDelta = 8, iklimDelta = 12,
                        continentHealthDelta = 14f, angerDelta = -5f,
                        factoryDelta = -15f, pollutionDelta = -18f, treeDelta = 0f, renewableDelta = 20f, foodDelta = 0f, zeroWasteDelta = 25f,
                        outcomeSummary = "Antarktika: Araştırma üsleri sıfır emisyona geçti; kutup çevresi temizlendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Maliyeti yüksek bulup buzul erimesini sadece uydularla izlemekle yetin.",
                        moneyDelta = 10, halkDelta = -15, iklimDelta = -24,
                        continentHealthDelta = -25f, angerDelta = 25f,
                        factoryDelta = 10f, pollutionDelta = 15f, treeDelta = 0f, renewableDelta = -5f, foodDelta = -15f, zeroWasteDelta = -10f,
                        outcomeSummary = "Antarktika: Devasa buz şelfi okyanusa göçtü; kıyı kentlerinde su taşkınları başladı."
                    }
                }
            });

            questions.Add(new ContinentQuestion
            {
                id = 702,
                continent = ContinentType.Antarctica,
                title = "Kutup Altı Metan Gazı Ceplerinin Isınması",
                description = "Donmuş deniz tabanındaki metan hidrat kristalleri ısınan okyanus suyu nedeniyle çözünüyor. Atmosfere gaz fışkırması halinde geri dönüşsüz sera etkisi tetiklenecek.",
                choices = new BlindChoice[]
                {
                    new BlindChoice
                    {
                        decisionText = "Derin deniz gaz yakalama ve klatrat stabilizasyon filolarını derhal bölgeye sevk et.",
                        moneyDelta = -35, halkDelta = 10, iklimDelta = 25,
                        continentHealthDelta = 26f, angerDelta = -8f,
                        factoryDelta = -5f, pollutionDelta = -15f, treeDelta = 0f, renewableDelta = 10f, foodDelta = 0f, zeroWasteDelta = 15f,
                        outcomeSummary = "Antarktika: Metan sızıntısı kapsüllenerek donduruldu; sera gazı patlaması önlendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Metan çıkış noktalarını basınçlı su kubbeleriyle örterek kontrollü difüzyon sağla.",
                        moneyDelta = -20, halkDelta = 6, iklimDelta = 14,
                        continentHealthDelta = 15f, angerDelta = 0f,
                        factoryDelta = 0f, pollutionDelta = -8f, treeDelta = 0f, renewableDelta = 5f, foodDelta = 0f, zeroWasteDelta = 10f,
                        outcomeSummary = "Antarktika: Gaz çıkışı kontrol altına alındı; risk kısmen nötrlendi."
                    },
                    new BlindChoice
                    {
                        decisionText = "Olayı doğal süreç olarak sınıflandırıp bütçe ayırmaktan kaçın.",
                        moneyDelta = 15, halkDelta = -20, iklimDelta = -28,
                        continentHealthDelta = -28f, angerDelta = 30f,
                        factoryDelta = 15f, pollutionDelta = 25f, treeDelta = 0f, renewableDelta = 0f, foodDelta = -12f, zeroWasteDelta = -10f,
                        outcomeSummary = "Antarktika: Milyonlarca ton metan atmosfere karıştı; küresel sıcaklık sıçraması yaşandı."
                    }
                }
            });
        }
    }
}
