using System;
using UnityEngine;

namespace Pixamor.COP31
{
    [Serializable]
    public class EventChoice
    {
        public string title;
        public int moneyDelta;
        public int halkDelta;
        public int iklimDelta;

        public EventChoice(string title, int moneyDelta, int halkDelta, int iklimDelta)
        {
            this.title = title;
            this.moneyDelta = moneyDelta;
            this.halkDelta = halkDelta;
            this.iklimDelta = iklimDelta;
        }
    }

    [Serializable]
    public class CrisisEvent
    {
        public int id;
        public string title;
        public string locationName;
        public Vector2 mapPos; // Harita üzerindeki normalize pozisyon (-1..1)
        public string description;
        public EventChoice[] choices;
        public float spawnTime; // Vardiyanın kaçıncı saniyesinde çıkacağı
        public float duration = 40f; // 40 saniye müdahale süresi

        public CrisisEvent(int id, string title, string locationName, Vector2 mapPos, string description, float spawnTime, EventChoice[] choices)
        {
            this.id = id;
            this.title = title;
            this.locationName = locationName;
            this.mapPos = mapPos;
            this.description = description;
            this.spawnTime = spawnTime;
            this.choices = choices;
            this.duration = 40f;
        }
    }

    /// <summary>
    /// message.txt belgesindeki resmi 12 kriz olayının veri tabanı.
    /// </summary>
    public static class CrisisDatabase
    {
        public static CrisisEvent[] GetEvents()
        {
            return new CrisisEvent[]
            {
                // 1. Şehirde aşırı sıcaklar (5. sn)
                new CrisisEvent(
                    1,
                    "Şehirde Aşırı Sıcaklar",
                    "Akdeniz Metropolü",
                    new Vector2(0.12f, 0.28f),
                    "“Sıcaklık rekor seviyeye ulaştı. Hastaneler doluyor, halk serinleyebileceği alanlar istiyor.”",
                    5f,
                    new EventChoice[]
                    {
                        new EventChoice("Klimalı acil serinleme merkezleri aç", -25, +10, -5),
                        new EventChoice("Gölgelik alanlar ve içme suyu noktaları kur", -15, +4, +3),
                        new EventChoice("Sıcakların geçmesini bekle", 0, -12, -4)
                    }
                ),

                // 2. Kıyı kasabasında sel (30. sn)
                new CrisisEvent(
                    2,
                    "Kıyı Kasabasında Sel",
                    "Güneydoğu Asya Sahili",
                    new Vector2(0.68f, 0.05f),
                    "“Şiddetli yağış nedeniyle evleri su bastı. Bazı aileler çatılarda yardım bekliyor.”",
                    30f,
                    new EventChoice[]
                    {
                        new EventChoice("Kurtarma ekipleri ve geçici barınak gönder", -30, +12, 0),
                        new EventChoice("Halkı belediye binalarına tahliye et", -10, -4, 0),
                        new EventChoice("Müdahale etme", 0, -16, -4)
                    }
                ),

                // 3. Orman yangını (55. sn)
                new CrisisEvent(
                    3,
                    "Orman Yangını",
                    "Kuzey Amerika / Pasifik",
                    new Vector2(-0.62f, 0.38f),
                    "“Yangın yerleşim yerlerine yaklaşıyor. Yerel ekipler alevleri kontrol edemiyor.”",
                    55f,
                    new EventChoice[]
                    {
                        new EventChoice("Söndürme uçakları gönder", -30, +8, +10),
                        new EventChoice("Kara ekipleriyle yangının yayılmasını sınırla", -15, -3, +4),
                        new EventChoice("Müdahale etme", 0, -12, -15)
                    }
                ),

                // 4. Barajda su azalıyor (80. sn)
                new CrisisEvent(
                    4,
                    "Barajda Su Azalıyor",
                    "Orta Doğu Su Havzası",
                    new Vector2(0.24f, 0.18f),
                    "“Uzun süren kuraklık yüzünden şehrin su rezervleri kritik seviyeye düştü.”",
                    80f,
                    new EventChoice[]
                    {
                        new EventChoice("Başka bölgelerden tankerlerle su getir", -25, +8, -5),
                        new EventChoice("Zorunlu su tasarrufu uygula", -5, -8, +8),
                        new EventChoice("Su kullanımına sınır koyma", 0, -10, -10)
                    }
                ),

                // 5. Elektrik talebi patladı (105. sn)
                new CrisisEvent(
                    5,
                    "Elektrik Talebi Patladı",
                    "Doğu Asya Sanayi Bölgesi",
                    new Vector2(0.72f, 0.32f),
                    "“Sıcaklar nedeniyle klimalar aralıksız çalışıyor. Elektrik şebekesi talebi karşılayamıyor.”",
                    105f,
                    new EventChoice[]
                    {
                        new EventChoice("Kömür santralini tam kapasite çalıştır", -10, +10, -12),
                        new EventChoice("Acil temiz enerji satın al", -30, +7, +5),
                        new EventChoice("Dönüşümlü elektrik kesintisi uygula", 0, -10, +3)
                    }
                ),

                // 6. Tarlalarda kuraklık (130. sn)
                new CrisisEvent(
                    6,
                    "Tarlalarda Kuraklık",
                    "Güney Amerika Tarım Kuşağı",
                    new Vector2(-0.35f, -0.32f),
                    "“Ürünler kurumaya başladı. Çiftçiler acil sulama desteği istiyor.”",
                    130f,
                    new EventChoice[]
                    {
                        new EventChoice("Yeraltı suyuyla yoğun sulama yap", -10, +8, -10),
                        new EventChoice("Damla sulama ekipmanı ve çiftçi desteği sağla", -25, +5, +8),
                        new EventChoice("Çiftçilere destek verme", 0, -13, -5)
                    }
                ),

                // 7. Şehirde hava kirliliği (155. sn)
                new CrisisEvent(
                    7,
                    "Şehirde Hava Kirliliği",
                    "Güney Asya Metropolü",
                    new Vector2(0.52f, 0.15f),
                    "“Kirli hava şehrin üzerini kapladı. Halk nefes almakta zorlanıyor; fabrikalar üretimi durdurmak istemiyor.”",
                    155f,
                    new EventChoice[]
                    {
                        new EventChoice("Fabrikalara acil filtre desteği ver", -25, +6, +10),
                        new EventChoice("Üretimi geçici olarak durdur", -5, -7, +12),
                        new EventChoice("Fabrikaların çalışmasına izin ver", 0, -10, -12)
                    }
                ),

                // 8. Fırtına yaklaşıyor (180. sn)
                new CrisisEvent(
                    8,
                    "Fırtına Yaklaşıyor",
                    "Karayipler / Atlas Okyanusu",
                    new Vector2(-0.45f, 0.12f),
                    "“Güçlü bir fırtına kıyıya ilerliyor. Hazırlık için çok az zaman kaldı.”",
                    180f,
                    new EventChoice[]
                    {
                        new EventChoice("Binaları güçlendir ve sığınaklar hazırla", -25, +10, 0),
                        new EventChoice("Riskli mahalleleri zorunlu tahliye et", -10, -5, 0),
                        new EventChoice("Yalnızca uyarı yayınla", 0, -14, -3)
                    }
                ),

                // 9. Sulak alanı yapılaşmaya açma talebi (205. sn)
                new CrisisEvent(
                    9,
                    "Sulak Alan Yapılaşma Talebi",
                    "Orta Avrupa Taşkın Ovası",
                    new Vector2(0.08f, 0.45f),
                    "“Müteahhitler sulak alana konut yapmak istiyor. Proje bütçe sağlayacak ama doğal taşkın alanı yok olacak.”",
                    205f,
                    new EventChoice[]
                    {
                        new EventChoice("Projeyi onayla", +25, +5, -15),
                        new EventChoice("Alanı koru ve doğa parkına dönüştür", -15, +3, +10),
                        new EventChoice("Projeyi reddet, alanı olduğu gibi bırak", 0, -4, +3)
                    }
                ),

                // 10. Gıda fiyatları yükseliyor (230. sn)
                new CrisisEvent(
                    10,
                    "Gıda Fiyatları Yükseliyor",
                    "Doğu Afrika Tarım Hattı",
                    new Vector2(0.26f, -0.15f),
                    "“Aşırı hava olayları hasadı vurdu. Temel gıda fiyatları yükseliyor ve halk destek istiyor.”",
                    230f,
                    new EventChoice[]
                    {
                        new EventChoice("Acil gıda ithalatı yap", -25, +12, -5),
                        new EventChoice("Yerel gıda dağıtımı ve kişi başı sınır uygula", -10, -4, +3),
                        new EventChoice("Fiyatlara ve dağıtıma müdahale etme", 0, -16, 0)
                    }
                ),

                // 11. Fırtına sonrası enerji krizi (255. sn)
                new CrisisEvent(
                    11,
                    "Fırtına Sonrası Enerji Krizi",
                    "Kuzey Afrika Güneş Sahası",
                    new Vector2(0.10f, 0.12f),
                    "“Fırtına güneş enerjisi tesisine zarar verdi. Hastane ve evlere yeterli elektrik ulaşmıyor.”",
                    255f,
                    new EventChoice[]
                    {
                        new EventChoice("Güneş enerjisi tesisini acil onar", -30, +8, +10),
                        new EventChoice("Dizel jeneratörleri devreye al", -10, +6, -10),
                        new EventChoice("Elektriği yalnızca hastaneye yönlendir", 0, -9, 0)
                    }
                ),

                // 12. Büyük sıcak hava dalgası (280. sn)
                new CrisisEvent(
                    12,
                    "Büyük Sıcak Hava Dalgası",
                    "Avustralya / Okyanusya",
                    new Vector2(0.75f, -0.38f),
                    "“Geniş bir bölgede tehlikeli sıcaklıklar bekleniyor. Su ve elektrik kaynakları aynı anda baskı altında.”",
                    280f,
                    new EventChoice[]
                    {
                        new EventChoice("Kapsamlı acil yardım operasyonu başlat", -35, +15, -5),
                        new EventChoice("Su ve elektrik kısıtlamasıyla halk merkezlerini açık tut", -15, -6, +8),
                        new EventChoice("Mevcut önlemlerle devam et", 0, -20, -12)
                    }
                )
            };
        }
    }
}
