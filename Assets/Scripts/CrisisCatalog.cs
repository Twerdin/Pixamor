using UnityEngine;

namespace Pixamor
{
    // İlk prototipin sabit sıralı 12 olayı. Değerler oynanış testinde ayarlanacak.
    public static class CrisisCatalog
    {
        public static CrisisData[] Create()
        {
            return new[]
            {
                Create("Aşırı sıcaklar", "Akdeniz", CrisisType.Heatwave, .52f, .33f,
                    "Sıcak hava dalgası kentlerde yaşamı zorlaştırıyor.",
                    Option("Soğutma merkezleri aç", 25, 8, -5, "Serinleme alanları halkı korudu; enerji tüketimi arttı."),
                    Option("Su dağıtımı ve tüketim kısıtlaması", 15, -3, 5, "Su dağıtıldı; kısıtlamalar tepki çekti."), -15, -5),
                Create("Nehir taşkını", "Güney Asya", CrisisType.Flood, .71f, .43f,
                    "Yükselen nehir, yerleşimleri ve sulak alanları tehdit ediyor.",
                    Option("Tahliye ve geçici barınak", 20, 9, -2, "Aileler güvenli barınaklara taşındı."),
                    Option("Sulak alanlara kontrollü yönlendirme", 15, -2, 9, "Taşkın suyu doğal tampon alanlara yönlendirildi."), -14, -8),
                Create("Tarımda su krizi", "Doğu Afrika", CrisisType.Drought, .57f, .57f,
                    "Uzayan kuraklık, çiftçilerin su kaynaklarını tüketiyor.",
                    Option("Acil su tankerleri gönder", 25, 10, -6, "İçme ve sulama suyu ulaştı; taşımacılık arttı."),
                    Option("Damla sulama desteği", 15, 2, 7, "Tasarruflu sulama devreye alındı."), -13, -9),
                Create("Kıyı ormanında yangın", "Batı Kuzey Amerika", CrisisType.Wildfire, .20f, .32f,
                    "Alevler kuvvetli rüzgârla yerleşimlere yaklaşıyor.",
                    Option("Hava araçlarıyla söndür", 30, 10, 5, "Yangının yerleşimlere ilerleyişi durduruldu."),
                    Option("Tahliye ve yangın koridoru", 15, 4, -2, "Halk tahliye edildi; ormanın bir bölümü kaybedildi."), -17, -12),
                Create("Gece sıcaklığı alarmı", "Batı Avrupa", CrisisType.Heatwave, .48f, .26f,
                    "Gece sıcaklıkları düşmüyor; elektrik şebekesi zorlanıyor.",
                    Option("Acil elektrik üretimi", 20, 9, -8, "Soğutma kesintisiz sürdü; emisyon yükseldi."),
                    Option("Serin kamu alanları ve ücretsiz ulaşım", 15, 6, 2, "Halk ortak serinleme alanlarına ulaştı."), -14, -5),
                Create("Kıyı seli", "Güneydoğu Asya", CrisisType.Flood, .77f, .53f,
                    "Şiddetli yağış kıyı mahallelerini su altında bırakıyor.",
                    Option("Pompalar ve acil barınma", 25, 10, -4, "Mahallelerde su seviyesi düşürüldü."),
                    Option("Tahliye ve mangrov koruması", 15, 3, 7, "Halk taşındı; doğal kıyı koruması güçlendi."), -16, -8),
                Create("Barajlar boşalıyor", "Güney Amerika", CrisisType.Drought, .32f, .65f,
                    "Kentlerin su rezervleri kritik düzeye indi.",
                    Option("Acil yeraltı suyu çıkar", 20, 10, -9, "Su kesintisi önlendi; yeraltı rezervleri azaldı."),
                    Option("Kaçak onarımı ve su kotası", 10, -2, 8, "Kayıplar azaltıldı; kota halkı zorladı."), -15, -9),
                Create("Turba yangını", "Endonezya", CrisisType.Wildfire, .79f, .62f,
                    "Turba arazisindeki yangın yoğun duman yayıyor.",
                    Option("Su basarak yangını söndür", 25, 6, 10, "Turba yeniden ıslatıldı; duman azaldı."),
                    Option("Maske dağıt ve tahliye et", 10, 8, -6, "Halk dumandan uzaklaştırıldı; yangın sürüyor."), -14, -15),
                Create("Kentte sıcaklık adası", "Doğu Asya", CrisisType.Heatwave, .82f, .35f,
                    "Yoğun yapılaşma sıcaklığı artırıyor; hastaneler doluyor.",
                    Option("Mobil sağlık ve soğutma", 25, 11, -5, "Sağlık ekipleri riskli mahallelere ulaştı."),
                    Option("Gölgelik ve trafik kısıtlaması", 10, -1, 7, "Gölgelikler kuruldu; trafik kısıtlaması tepki çekti."), -16, -6),
                Create("Ani şehir seli", "Orta Avrupa", CrisisType.Flood, .52f, .28f,
                    "Yoğun yağış, kentin drenaj kapasitesini aştı.",
                    Option("Acil tahliye ve altyapı onarımı", 25, 10, 1, "Tahliye tamamlandı; kritik altyapı onarıldı."),
                    Option("Suyu geçici toplama alanına yönlendir", 10, 2, 6, "Su, boş alanlarda güvenle toplandı."), -15, -9),
                Create("Uzayan kurak mevsim", "Avustralya", CrisisType.Drought, .87f, .76f,
                    "Kurak mevsim uzadı; kentler ve doğal yaşam su bekliyor.",
                    Option("Acil su arıtma tesisleri", 25, 10, -7, "Su arzı arttı; tesisler yoğun enerji kullandı."),
                    Option("Geri kullanım ve doğaya su payı", 15, 1, 9, "Su tekrar kullanıldı; doğal yaşam desteklendi."), -13, -11),
                Create("Orman sınırında alevler", "Güney Avrupa", CrisisType.Wildfire, .50f, .35f,
                    "Yangın kırsal yerleşimlerle koruma alanı arasında yayılıyor.",
                    Option("Kara ve hava ekiplerini birlikte gönder", 30, 9, 9, "Orman ve yerleşimler birlikte korundu."),
                    Option("Yerleşimleri tahliye et", 10, 7, -4, "Tahliye tamamlandı; orman kaybı sürdü."), -18, -13)
            };
        }

        private static InterventionData Option(string title, int cost, int halk, int iklim, string result)
        {
            return new InterventionData(title, cost, halk, iklim, result);
        }

        private static CrisisData Create(string title, string region, CrisisType type,
            float x, float y, string description, InterventionData first,
            InterventionData second, int missedHappiness, int missedClimate)
        {
            var ignored = new InterventionData("Müdahale etme", 0, missedHappiness,
                missedClimate, region + ": Müdahale edilmedi; kriz zarar verdi.", false);
            return new CrisisData
            {
                Title = title, Region = region, Type = type, Description = description,
                MapPosition = new Vector2(x, y),
                Options = new[] { first, second, ignored },
                TimeoutResult = ignored
            };
        }
    }
}
