using UnityEngine;

namespace Pixamor
{
    // Sabit sıralı 12 olay. Ucuz uzun vadeli önlemler acil halk ihtiyacını
    // karşılamayabilir; hızlı tahliye ise devam eden çevre zararını durdurmaz.
    public static class CrisisCatalog
    {
        public static CrisisData[] Create()
        {
            return new[]
            {
                Create("Aşırı sıcaklar", "Akdeniz", CrisisType.Heatwave, .5556f, .4578f,
                    "Sıcak hava dalgası kentlerde yaşamı zorlaştırıyor.",
                    Option("Soğutma merkezleri aç", 25, 8, -5, "Serinleme alanları halkı korudu; enerji tüketimi arttı."),
                    Option("Su dağıtımı ve tüketim kısıtlaması", 15, -3, 5, "Su dağıtıldı; kısıtlamalar tepki çekti."), -15, -5),
                Create("Nehir taşkını", "Güney Asya", CrisisType.Flood, .7500f, .5341f,
                    "Yükselen nehir, yerleşimleri ve sulak alanları tehdit ediyor.",
                    Option("Tahliye ve geçici barınak", 20, 9, -2, "Aileler güvenli barınaklara taşındı."),
                    Option("Sulak alanlara kontrollü yönlendirme", 10, -12, 7, "Su sulak alanlara yönlendirildi; bazı mahalleler boşaltılmak zorunda kaldı."), -14, -8),
                Create("Tarımda su krizi", "Doğu Afrika", CrisisType.Drought, .6056f, .6445f,
                    "Uzayan kuraklık, çiftçilerin su kaynaklarını tüketiyor.",
                    Option("Acil su tankerleri gönder", 25, 10, -6, "İçme ve sulama suyu ulaştı; taşımacılık arttı."),
                    Option("Damla sulama desteği", 10, -12, 8, "Tasarruflu sulama kuruldu; acil su ihtiyacı tam karşılanamadı."), -13, -12),
                Create("Kıyı ormanında yangın", "Batı Kuzey Amerika", CrisisType.Wildfire, .1611f, .4458f,
                    "Alevler kuvvetli rüzgârla yerleşimlere yaklaşıyor.",
                    Option("Hava araçlarıyla söndür", 30, 10, 5, "Yangının yerleşimlere ilerleyişi durduruldu."),
                    Option("Tahliye ve yangın koridoru", 10, 4, -14, "Halk tahliye edildi; yangın ormanın içinde yayılmaya devam etti."), -17, -12),
                Create("Gece sıcaklığı alarmı", "Batı Avrupa", CrisisType.Heatwave, .5139f, .3870f,
                    "Gece sıcaklıkları düşmüyor; elektrik şebekesi zorlanıyor.",
                    Option("Acil elektrik üretimi", 20, 9, -8, "Soğutma kesintisiz sürdü; emisyon yükseldi."),
                    Option("Serin kamu alanları ve elektrik kotası", 10, -10, 4, "Enerji tüketimi sınırlandı; serinleme alanlarının kapasitesi yetersiz kaldı."), -14, -9),
                Create("Kıyı seli", "Güneydoğu Asya", CrisisType.Flood, .8056f, .5929f,
                    "Şiddetli yağış kıyı mahallelerini su altında bırakıyor.",
                    Option("Pompalar ve acil barınma", 25, 10, -4, "Mahallelerde su seviyesi düşürüldü."),
                    Option("Tahliye ve mangrov koruması", 15, -12, 8, "Doğal kıyı korundu; barınak yetersizliği tahliye edilen halkı zorladı."), -16, -8),
                Create("Barajlar boşalıyor", "Güney Amerika", CrisisType.Drought, .3333f, .7197f,
                    "Kentlerin su rezervleri kritik düzeye indi.",
                    Option("Acil yeraltı suyu çıkar", 20, 10, -9, "Su kesintisi önlendi; yeraltı rezervleri azaldı."),
                    Option("Kaçak onarımı ve su kotası", 10, -12, 8, "Su kayıpları azaltıldı; kesintiler günlük yaşamı zorlaştırdı."), -15, -9),
                Create("Turba yangını", "Endonezya", CrisisType.Wildfire, .8111f, .6584f,
                    "Turba arazisindeki yangın yoğun duman yayıyor.",
                    Option("Su basarak yangını söndür", 25, 6, 10, "Turba yeniden ıslatıldı; duman azaldı."),
                    Option("Maske dağıt ve tahliye et", 10, 6, -15, "Halk dumandan uzaklaştırıldı; turba yanmaya ve emisyon üretmeye devam etti."), -14, -15),
                Create("Kentte sıcaklık adası", "Doğu Asya", CrisisType.Heatwave, .8611f, .4695f,
                    "Yoğun yapılaşma sıcaklığı artırıyor; hastaneler doluyor.",
                    Option("Mobil sağlık ve soğutma", 25, 11, -5, "Sağlık ekipleri riskli mahallelere ulaştı."),
                    Option("Gölgelik ve trafik kısıtlaması", 10, -12, 7, "Trafik azaltıldı; acil sağlık ve serinleme ihtiyacı tam karşılanamadı."), -16, -10),
                Create("Ani şehir seli", "Orta Avrupa", CrisisType.Flood, .5417f, .3940f,
                    "Yoğun yağış, kentin drenaj kapasitesini aştı.",
                    Option("Acil tahliye ve altyapı onarımı", 25, 10, 1, "Tahliye tamamlandı; kritik altyapı onarıldı."),
                    Option("Suyu geçici toplama alanına yönlendir", 10, -10, 6, "Su toplama alanlarına yönlendirildi; çevredeki mahalleler tahliye edildi."), -15, -12),
                Create("Uzayan kurak mevsim", "Avustralya", CrisisType.Drought, .8750f, .7692f,
                    "Kurak mevsim uzadı; kentler ve doğal yaşam su bekliyor.",
                    Option("Acil su arıtma tesisleri", 25, 10, -7, "Su arzı arttı; tesisler yoğun enerji kullandı."),
                    Option("Geri kullanım ve doğaya su payı", 15, -12, 8, "Doğal yaşam desteklendi; kentlerdeki acil su sıkıntısı devam etti."), -13, -11),
                Create("Orman sınırında alevler", "Güney Avrupa", CrisisType.Wildfire, .5639f, .4519f,
                    "Yangın kırsal yerleşimlerle koruma alanı arasında yayılıyor.",
                    Option("Kara ve hava ekiplerini birlikte gönder", 30, 9, 9, "Orman ve yerleşimler birlikte korundu."),
                    Option("Yerleşimleri tahliye et", 10, 5, -15, "Yerleşimler boşaltıldı; koruma alanındaki orman kaybı sürdü."), -18, -16)
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
