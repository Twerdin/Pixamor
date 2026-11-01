using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.ClimateOS
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
        public string decisionText;
        public int moneyDelta;
        public int halkDelta;
        public int iklimDelta;
        public float continentHealthDelta;
        public float angerDelta;

        public float treeDelta;
        public float factoryDelta;
        public float pollutionDelta;
        public float renewableDelta;
        public float foodDelta;
        public float zeroWasteDelta;

        public string outcomeSummary;
    }

    [Serializable]
    public class ContinentQuestion
    {
        public int id;
        public ContinentType continent;
        public string speakerName;
        public string speakerCategory;   // "Vatandaş Talebi", "Canlı Basın Sorusu", "Bilim Kurulu Raporu", "Kıta Başkanı", "Holding CEO'su"
        public float decisionTimeLimit = 20f; // 10f (Yıldırım), 20f (Bölgesel), 30f (Küresel Başkan & Lobi)
        public string title;
        [TextArea(2, 5)]
        public string description;

        [TextArea(2, 4)]
        public string officialStatement; // Muhatap Beyanı (Ham metin)
        [TextArea(2, 4)]
        public string rawTelemetryData;  // Bakanlık Uydu & Sensör Telemetrisi (Ham sayısal veri, ipucu yok!)
        public bool hasContradiction;    // Gerçekten çelişki (yalan) var mı?

        public BlindChoice[] choices;

        public string GetOfficialStatement(string fallbackRepName)
        {
            if (!string.IsNullOrEmpty(officialStatement))
                return officialStatement;
            return $"\"{fallbackRepName}: Bölgesel emisyon oranlarımız %20 sınırının altındadır, ek denetime gerek yoktur.\"";
        }

        public string GetSatelliteSensorData()
        {
            if (!string.IsNullOrEmpty(rawTelemetryData))
                return rawTelemetryData;
            return "Karbon Yoğunluğu: %78 (Kritik)  |  Filtre Aktifliği: %12";
        }
    }

    /// <summary>
    /// "Halk ve Başkanlar V2" — 100 Diyalogluk Dinamik Kriz ve Telemetri Havuzu:
    /// - %60 Halk Kesimi (60 Diyalog: Çiftçiler, öğrenciler, maden işçileri, bağımsız bilimciler, gazeteciler, selzedeler — 10 sn ve 20 sn).
    /// - %40 Başkanlar ve Dev Şirket CEO'ları (40 Diyalog: 7 Kıta Temsilcisi, Petrol Karteli, Enerji Bakanları — 20 sn ve 30 sn).
    /// - Her diyalogda ham "Muhatap Beyanı" ve ham "Bakanlık Uydu Telemetrisi" yer alır; çelişkiyi oyuncu sayıları okuyarak kendi bulur.
    /// </summary>
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
            // ÖZEL KURGULANMIŞ ANA SENARYOLAR + SİSTEMATİK 100 V2 DİYALOG HAVUZU
            // (%60 HALK KESİMİ = 60 ADET | %40 BAŞKANLAR & ŞİRKET CEO'LARI = 40 ADET)
            // =========================================================================

            // --- BÖLÜM A: KÜRESEL BAŞKANLAR VE DEV ŞİRKET CEO'LARI (40 DİYALOG - %40) ---
            // (30 Saniyelik Stratejik Dosyalar - Bir kısmında Yalan/Çelişki var, bir kısmında Doğru Beyan var!)

            string[][] executiveTemplates = new string[][]
            {
                // [0] Kıta İndeksi, [1] Muhatap Adı, [2] Kategori, [3] Başlık, [4] Açıklama, [5] Beyan, [6] Telemetri, [7] Çelişki ("1"=Yalan, "0"=Doğru), [8] Süre
                new[]{ "0", "Başkan Jack Harper", "Kıta Başkanı", "Kuzey Sanayi Havzası Baca Filtresi Raporu",
                    "Ağır sanayi koridorundaki çelik fabrikaları için yıllık emisyon beyannamesi Bakanlık masasına sunuldu.",
                    "\"Bölgemizdeki karbon emisyon oranları %18'in altına düştü, elektrostatik filtrelerimiz %92 kapasiteyle çalışıyor.\"",
                    "Karbon Yoğunluğu: %79 (Kritik)  |  Filtre Aktifliği: %14  |  Gece Deşarjı: Aktif", "1", "30" },

                new[]{ "0", "PetroTex CEO'su Victor Vance", "Holding CEO'su", "Körfez Rafinerisi Metan Sızıntısı İddiası",
                    "Açık deniz sondaj platformları çevresinde atmosferik metan artışı yaşandığına dair ihbarlar inceleniyor.",
                    "\"Platformlarımızda sıfır sızıntı protokolü uygulanıyor; metan kaçak oranımız %2 seviyesindedir.\"",
                    "Metan Kaçak Oranı: %68 (Alarm)  |  Basınç Valfi: Açık  |  Termal Kamera: Alev İzi", "1", "30" },

                new[]{ "0", "Enerji Bakanı Sarah Jenkins", "Kıta Başkanı", "Kuzey Şebekesi Güneş Enerjisi Entegrasyonu",
                    "Kıta enerji bakanlığı güneş tarlalarının şebekeye bağlanma oranlarını resmi olarak bildirdi.",
                    "\"Yenilenebilir şebeke payımızı %64 seviyesine çıkardık, yedek kömür santrallerimiz kapalı tutuluyor.\"",
                    "Yenilenebilir Payı: %65 (Doğrulandı)  |  Kömür Üniteleri: %0 (Kapalı)  |  Emisyon: Düşük", "0", "30" },

                new[]{ "0", "AutoCorp Başkanı Marcus Cole", "Holding CEO'su", "Elektrikli Araç Batarya Geri Dönüşüm Tesisi",
                    "Üretici konsorsiyum atık lityum pillerin yüzde doksanının geri dönüştürüldüğünü beyan ederek vergi indirimi istiyor.",
                    "\"Atık bataryaların %90'ını kapalı devre tesislerimizde geri kazanıyoruz, toprağa ağır metal sızmıyor.\"",
                    "Geri Kazanım: %19  |  Kırsal Gömü Alanı Kurşun/Kadmiyum: %82 (Yüksek)  |  Atık Su: Kirli", "1", "30" },

                new[]{ "0", "Kuzey Valisi Robert Vance", "Kıta Başkanı", "Büyük Göller Temiz Su Koruma Protokolü",
                    "Sanayi havzasındaki arıtma tesislerinin modernizasyon raporu bakanlık onayına sunuldu.",
                    "\"Göller bölgesindeki tüm kimyasal arıtma ünitelerimiz %88 verimle çalışıyor, su kalitesi normaldir.\"",
                    "Arıtma Verimi: %87 (Normal)  |  Ağır Metal: %4 (Güvenli Sınırda)  |  pH: 7.2", "0", "20" },

                new[]{ "0", " PlastChem CEO'su Elena Grant", "Holding CEO'su", "Doğu Yakası Polimer Üretim Kotaları",
                    "Ambalaj tekeli tek kullanımlık plastik üretimini yarıya indirdiğini iddia ederek yaptırım muafiyeti talep ediyor.",
                    "\"Mikroplastik salınımımızı %10'un altına çektik, nehir deşarj borularımız tamamen mühürlendi.\"",
                    "Nehir Mikroplastik Yoğunluğu: %74 (Tehlikeli)  |  Gizli Deşarj Hattı: Aktif (%85 Debi)", "1", "30" },

                new[]{ "1", "Başkan Camila Silva", "Kıta Başkanı", "Amazon Havzası Koruma Kuşağı Denetimi",
                    "Yağmur ormanları sınırındaki tarım ve kereste ruhsatlarının uydu haritalarıyla karşılaştırılması gerekiyor.",
                    "\"Koruma altındaki birincil yağmur ormanlarında kesim oranı %0'dır; tüm faaliyetler izinli bölgededir.\"",
                    "Orman Kaybı: 2.400 Hektar/Hafta  |  Termal Yangın Noktası: 48 Adet (Yasak Bölge)", "1", "30" },

                new[]{ "1", "AgroSoya Holding CEO'su Diego Morales", "Holding CEO'su", "Cerrado Tarım Kuşağı Pestisit Kullanımı",
                    "İhracatçı tarım karteli yasaklı kimyasal ilaçları bıraktığını belirterek yeşil sertifika talep ediyor.",
                    "\"Tarlalarımızda biyolojik mücadeleye geçtik; kimyasal pestisit kalıntısı %5'in altındadır.\"",
                    "Toprak Toksisitesi: %81 (Kritik)  |  Nehir Pestisit Oranı: %69  |  Biyoçeşitlilik: -%45", "1", "30" },

                new[]{ "1", "And Madencilik Bakanı Carlos Mendez", "Kıta Başkanı", "Lityum Tuzlaları Su Çekim Raporu",
                    "Yüksek irtifa tuz göllerindeki su kullanım kotalarının denetim raporu masada.",
                    "\"Kapalı devre tuzlu su pompalamaya geçtik; yeraltı tatlı su çekimimiz %15 sınırının altındadır.\"",
                    "Tatlı Su Çekimi: %14 (Uyumlu)  |  Geri Kazanım Pompası: %91 Aktif  |  Akifer: Dengeli", "0", "20" },

                new[]{ "1", "Amazonia Timber Sözcüsü R. Costa", "Holding CEO'su", "Sertifikalı Kereste ve Yeniden Ağaçlandırma",
                    "Kereste konsorsiyumu kestiği her ağacın yerine üç fidan diktiğini iddia ediyor.",
                    "\"Yıllık fidan dikim oranımız %95'tir, karbon yutak alanlarımız geçen yıla göre büyüdü.\"",
                    "Dikim Sahası Canlılık Oranı: %11  |  Kaçak Tomruk Kamyonu Trafiği: +%310", "1", "30" },

                new[]{ "1", "Güney Enerji Komiseri Sofia Alvarez", "Kıta Başkanı", "Itaipu Hidroelektrik Havzası Debi Raporu",
                    "Kuraklık döneminde baraj kapaklarının ekolojik akışa uygun yönetildiği bildirildi.",
                    "\"Alt havza ekolojik su bırakma oranımız %45 seviyesindedir, nehir yatağı korunuyor.\"",
                    "Alt Havza Su Debisi: %46 (Normal)  |  Oksijen Seviyesi: %88  |  Balık Göçü: Normal", "0", "20" },

                new[]{ "1", "AndeanGold CEO'su Hector Vargas", "Holding CEO'su", "Nehir Yatağı Altın Arıtma Tesisleri",
                    "Maden şirketi cıva kullanımını tamamen sonlandırdığını resmi yazıyla beyan etti.",
                    "\"Tesislerimizde cıva ve siyanür oranı %0'dır; atık havuzlarımız sızdırmaz membranla kaplıdır.\"",
                    "Nehir Cıva Oranı: %64 (Çok Yüksek)  |  Atık Barajı Sızıntısı: Aktif  |  Su Kalitesi: %18", "1", "30" },

                new[]{ "2", "Başkan Dr. Elena Rostova", "Kıta Başkanı", "Avrupa Kış Enerji Şebekesi ve Kömür Raporu",
                    "Soğuk hava dalgası sırasında yedek linyit santrallerinin durumu hakkında resmi beyan verildi.",
                    "\"Enerji şebekemiz yeşil dönüşüm takvimine tam uyumludur; yedek kömür üniteleri devreye alınmamıştır.\"",
                    "Linyit Santrali Yükü: %89 (Tam Kapasite)  |  SO2 Salınımı: +%240  |  Şebeke Karbonu: Yüksek", "1", "30" },

                new[]{ "2", "EuroGrid CEO'su Henrik Lind", "Holding CEO'su", "Kuzey Denizi Rüzgar Koridoru Üretim Verisi",
                    "Açık deniz rüzgar türbini operatörü rekor temiz enerji üretimi bildirdi.",
                    "\"Türbin filomuz %94 aktiflikle çalışıyor, kıyı şebekesine sıfır karbonlu elektrik sağlıyoruz.\"",
                    "Türbin Aktifliği: %93 (Doğrulandı)  |  Karbon Yoğunluğu: %6 (Çok Düşük)  |  Şebeke: Temiz", "0", "20" },

                new[]{ "2", "Ren Kimya Karteli Sözcüsü K. Weber", "Holding CEO'su", "Ren Nehri Endüstriyel Soğutma Suyu Deşarjı",
                    "Kimya devleri nehir suyunu ısıtmadıklarını ve termal sınırları koruduklarını iddia ediyor.",
                    "\"Fabrika deşarj sularımız 21 derece sınırının altındadır, nehir ekosistemine zarar verilmiyor.\"",
                    "Deşarj Suyu Sıcaklığı: 34.8°C (Kritik)  |  Çözünmüş Oksijen: %19  |  Termal İzin: Aşıldı", "1", "30" },

                new[]{ "2", "Akdeniz Denizcilik Komiseri Marco Rossi", "Kıta Başkanı", "Limanlarda Düşük Kükürtlü Yakıt Denetimi",
                    "Kruvaziyer ve yük gemilerinin yeşil yakıta geçtiği raporlandı.",
                    "\"Limanlarımızdaki gemilerin %85'i düşük kükürtlü temiz yakıt ve kıyı elektriği kullanıyor.\"",
                    "Kükürt Oksit (SOx) Oranı: %76 (Ağır Fuel-Oil)  |  Kıyı Elektriği Kullanımı: %12", "1", "30" },

                new[]{ "2", "İskandinav Orman Bakanı Astrid Holm", "Kıta Başkanı", "Boreal Ormanları Karbon Yutak Raporu",
                    "Kuzey orman kuşağındaki sürdürülebilir kesim ve koruma verileri sunuldu.",
                    "\"Boreal orman varlığımız %98 oranında korunmakta olup yıllık büyüme kesimden fazladır.\"",
                    "Orman Yoğunluğu: %97 (Uyumlu)  |  Karbon Tutumu: +%14  |  Kaçak Kesim: %0", "0", "20" },

                new[]{ "2", "Continental Motors CEO'su L. Dubois", "Holding CEO'su", "Dizel Emisyon Yazılımı Güncelleme Dosyası",
                    "Otomotiv devi yeni araçlarının şehir içi azot oksit sınırlarını aşmadığını savunuyor.",
                    "\"Yeni nesil motorlarımızda azot oksit salınımı %12 seviyesindedir, testlerimiz şeffaftır.\"",
                    "Yol Testi NOx Salınımı: %84 (Standartların 7 Katı)  |  Test Modu Yazılımı: Tespit Edildi", "1", "30" },

                new[]{ "3", "Başkan Kwame Mensah", "Kıta Başkanı", "Sahel Yeşil Duvar ve Su Fonu Dağıtım Raporu",
                    "Kuraklık kuşağındaki su kuyuları ve maden sahalarının su tüketimi inceleniyor.",
                    "\"Su havzaları adil dağıtılıyor; yabancı madenlerin nehirlere ağır metal sızdırdığı iddiası asılsızdır.\"",
                    "Nehir Ağır Metal Oranı: %72 (Kritik)  |  Yeraltı Su Seviyesi: -%44  |  Maden Deşarjı: Aktif", "1", "30" },

                new[]{ "3", "Congo Basin Oil CEO'su Jean-Luc Moreau", "Holding CEO'su", "Kongo Turbalıkları Sismik Arama Ruhsatı",
                    "Petrol şirketi bataklık ormanlarında sadece yüzeysel ölçüm yaptığını iddia ediyor.",
                    "\"Turbalık koruma alanında derin sondaj yapmıyoruz; metan sızıntısı %0 seviyesindedir.\"",
                    "Derin Sondaj Kulesi: 6 Adet Aktif  |  Metan Gazı Çıkışı: +%380 (Kritik Karbon Bombası)", "1", "30" },

                new[]{ "3", "Doğu Afrika Enerji Bakanı Amina Kenyatta", "Kıta Başkanı", "Rift Vadisi Jeotermal Santral Raporu",
                    "Bölgesel temiz enerji yatırımlarının emisyon verileri bakanlığa ulaştı.",
                    "\"Bölge elektriğinin %78'ini sıfır emisyonlu jeotermal ve güneş santrallerinden üretiyoruz.\"",
                    "Jeotermal Aktiflik: %80 (Doğrulandı)  |  Karbon Emisyonu: %9 (Düşük)  |  Şebeke: Yeşil", "0", "20" },

                new[]{ "3", "SaharaSolar Direktörü Tariq Al-Mansoor", "Holding CEO'su", "Kuzey Sahra Güneş Tarlası Su Kullanımı",
                    "Çöl güneş panellerinin temizliği için vaha sularının tüketildiği iddiası gündemde.",
                    "\"Panel temizliğinde kuru robotik fırça kullanıyoruz; vaha yeraltı suyundan çekimimiz %5'tir.\"",
                    "Vaha Akifer Çekimi: %76 (Aşırı Tüketim)  |  Kuru Robotik Sistem: %8 Aktif", "1", "30" },

                new[]{ "3", "Batı Afrika Kakao Konseyi Başkanı K. Toure", "Kıta Başkanı", "Yağmur Ormanı Gölge Tarımı Programı",
                    "Kakao üreticilerinin orman kesmeden üretim yaptığına dair rapor sunuldu.",
                    "\"Kooperatiflerimiz %85 oranında orman örtüsü altında agroekolojik üretim yapmaktadır.\"",
                    "Orman Örtüsü Korunumu: %86 (Doğrulandı)  |  Kimyasal Gübre: %11 (Düşük)", "0", "20" },

                new[]{ "3", "DeltaPetro Sözcüsü Desmond Okafor", "Holding CEO'su", "Nijer Deltası Boru Hattı Bakım Beyanı",
                    "Boru hattı sızıntılarının tamamen temizlendiği ve balıkçılığın güvenli olduğu iddia ediliyor.",
                    "\"Boru hatlarımızdaki petrol sızıntısı %1'in altına indirildi, mangrov kıyı şeridi temizlendi.\"",
                    "Yüzey Petrol Filmi: %83 (Ağır Kirlilik)  |  Kıyı Toprak Hidrokarbonu: Kritik", "1", "30" },

                new[]{ "4", "Başkan Li Wei", "Kıta Başkanı", "Doğu Asya İmalat Koridoru Kömür Kotaları",
                    "Sanayi bölgelerindeki karbon kotaları ve endüstriyel atık deşarj verileri inceleniyor.",
                    "\"Üretim bölgelerimizde karbon kotalarına uyuluyor; filtreleme %90 aktif ve deşarj sıfırdır.\"",
                    "PM2.5 Yoğunluğu: %92 (Kırmızı Alarm)  |  Kömür Santrali Filtresi: %18 Aktif  |  Metan: Yüksek", "1", "30" },

                new[]{ "4", "Pacific Textile CEO'su Rajesh Patel", "Holding CEO'su", "Ganj ve Mekong Tekstil Boyası Arıtma Raporu",
                    "Tekstil devleri nehirlere zehirli boya akıtmadıklarını ve kapalı devre arıtma kullandıklarını söylüyor.",
                    "\"Fabrikalarımızdan nehirlere sıfır kimyasal boya karışmaktadır; arıtma tesislerimiz %95 aktiftir.\"",
                    "Nehir Kimyasal Toksisitesi: %86 (Kritik)  |  Arıtma Tesisi Elektrik Tüketimi: %0 (Kapalı)", "1", "30" },

                new[]{ "4", "Asya Yenilenebilir Komiseri Kenji Sato", "Kıta Başkanı", "Yüzer Güneş Adaları ve Rüzgar Kapasitesi",
                    "Kıyı kuşağındaki yeni temiz enerji tesislerinin üretim verileri paylaşıldı.",
                    "\"Son çeyrekte 40 GW yeni güneş ve rüzgar kapasitesini %91 verimle devreye aldık.\"",
                    "Yeni Temiz Kapasite: 41.2 GW (Doğrulandı)  |  Şebeke Verimi: %92  |  Emisyon: -%18", "0", "20" },

                new[]{ "4", "PalmCorp Başkanı Budi Santoso", "Holding CEO'su", "Güneydoğu Asya Turbalık Yangınları Raporu",
                    "Palmiye yağı şirketi orman yangınlarının doğal kuraklıktan çıktığını savunuyor.",
                    "\"Plantasyonlarımızda ateşle arazi açma yöntemi %0'dır; yangınlar tamamen doğal yıldırım kaynaklıdır.\"",
                    "Termal Uydu Analizi: Simetrik Izgara Kundaklama İzi (%94 İnsan Kaynaklı)  |  Duman: Kritik", "1", "30" },

                new[]{ "4", "Orta Asya Su Bakanı Timur Nazarov", "Kıta Başkanı", "Aral Havzası Damla Sulama Dönüşümü",
                    "Pamuk tarlalarında su israfının önlendiğine dair resmi bakanlık raporu.",
                    "\"Tarım arazilerimizin %75'inde basınçlı damla sulamaya geçilerek nehir su kaybı durduruldu.\"",
                    "Damla Sulama Oranı: %74 (Uyumlu)  |  Göl Su Seviyesi: +%8 Artış  |  Tuzlanma: Azaldı", "0", "20" },

                new[]{ "4", "MegaChip Foundry CEO'su David Chen", "Holding CEO'su", "Yarı İletken Fabrikası Ultra-Saf Su Tüketimi",
                    "Çip fabrikası kuraklık çeken bölgede kendi suyunu geri dönüştürdüğünü iddia ediyor.",
                    "\"Üretimde kullandığımız suyun %85'ini geri kazanıyoruz, halkın içme suyu barajlarına dokunmuyoruz.\"",
                    "Geri Kazanım: %22  |  Şehir Barajından Çekilen Su: Günde 180.000 Ton (Kritik)", "1", "30" },

                new[]{ "5", "Başkan Liam O'Connor", "Kıta Başkanı", "Büyük Set Resifi Kıyı Madenciliği ve Deniz Trafiği",
                    "Mercan resifleri yakınındaki kömür limanları ve tarama gemileri hakkında resmi rapor.",
                    "\"Mercan resifleri çevresindeki deniz trafiği ve kıyı madenciliği güvenlik sınırlarının altındadır.\"",
                    "Okyanus Asitlenmesi: Kritik  |  Yasak Bölgede Kıyı Tarama Gemisi: 11 Adet Aktif  |  Bulanıklık: %80", "1", "30" },

                new[]{ "5", "Outback Coal CEO'su Bruce Sterling", "Holding CEO'su", "Yeni Açık Ocak Kömür Madeni Metan Raporu",
                    "Maden holdingi açık ocak sahasından atmosfere metan sızmadığını iddia ediyor.",
                    "\"Maden sahamızda gaz yakalama sistemleri %88 aktiftir, emisyonlarımız yasal kotanın altındadır.\"",
                    "Atmosferik Metan Bulutu: +%310 (Aşırı Sızıntı)  |  Gaz Yakalama Ünitesi: Devre Dışı", "1", "30" },

                new[]{ "5", "Pasifik Ada Devletleri Temsilcisi Moana Tui", "Kıta Başkanı", "Mercan Restorasyonu ve Deniz Koruma Alanı",
                    "Ada ülkeleri kıyı sularını endüstriyel balıkçılığa kapattıklarını bildirdi.",
                    "\"Ekonomik bölgemizin %90'ını deniz koruma alanı ilan ettik, trol teknelerine izin verilmiyor.\"",
                    "Koruma Alanı İhlali: %0 (Tam Uyum)  |  Mercan Canlılığı: +%24  |  Balık Stoku: Artışta", "0", "20" },

                new[]{ "5", "Southern Agro CEO'su Claire Evans", "Holding CEO'su", "Murray-Darling Nehir Havzası Su Çekimi",
                    "Büyük pamuk çiftlikleri kuraklık sırasında nehir suyunu çalmadıklarını savunuyor.",
                    "\"Nehirden yalnızca yasal kotamız olan %20 oranında su çekiyoruz, sayaçlarımız mühürlüdür.\"",
                    "Gece Kaçak Pompalama: %77 (Aşırı Çekim)  |  Alt Havza Nehir Debisi: %11 (Kuruma Noktasında)", "1", "30" },

                new[]{ "5", "Yeni Zelanda Çevre Bakanı Aroha Ngata", "Kıta Başkanı", "Jeotermal ve Hidroelektrik Tam Geçiş Raporu",
                    "Ada şebekesinin fosil yakıt kullanımını minimuma indirdiği bildirildi.",
                    "\"Ulusal elektrik üretimimizin %87'si yenilenebilir kaynaklardan sağlanmaktadır.\"",
                    "Yenilenebilir Oranı: %88 (Doğrulandı)  |  Şebeke Karbonu: Çok Düşük", "0", "20" },

                new[]{ "6", "Konsey Başkanı Dr. Sven Lindholm", "Kıta Başkanı", "Antarktika Buz Altı Sondaj ve Termal Sızıntı Raporu",
                    "Kutup araştırma ve maden arama protokollerinin ihlal edilip edilmediği denetleniyor.",
                    "\"Kutup araştırma ve sondaj sahalarında termal sızıntı bulunmamaktadır; protokoller tam uygulanıyor.\"",
                    "Buz Altı Sismik Anomali: Yüksek  |  Yasak Bölge Derin Sondaj: Aktif  |  Metan Sızıntısı: %76", "1", "30" },

                new[]{ "6", "PolarKrill Corp CEO'su Magnus Olsen", "Holding CEO'su", "Güney Okyanusu Kril Avcılığı Kotaları",
                    "Endüstriyel balıkçılık filosu penguen ve balina besin zincirine zarar vermediğini iddia ediyor.",
                    "\"Koruma altındaki buz şelfi kıyılarında avlanmıyoruz; kril çekimimiz kotanın %30 altındadır.\"",
                    "AIS Uydu Takibi: 14 Fabrika Gemisi Koruma Bölgesinde  |  Kril Biyokütlesi: -%62", "1", "30" },

                new[]{ "6", "Uluslararası Kutup İstasyonu Direktörü Dr. Y. Moreau", "Kıta Başkanı", "Sıfır Atık ve Rüzgar Türbinli Kutup Üssü",
                    "Bilim üslerinin dizel jeneratörleri kapatıp rüzgar enerjisine geçtiği bildirildi.",
                    "\"İstasyonlarımızda yakıt tüketimi %80 azaltıldı, tüm atıklar gemilerle geri taşınıyor.\"",
                    "Rüzgar Türbini Payı: %82 (Doğrulandı)  |  Atık Sızıntısı: %0  |  Termal İz: Normal", "0", "20" },

                new[]{ "6", "ArcticShipping Sözcüsü Igor Volkov", "Holding CEO'su", "Buzkıran Gemileri Ağır Yakıt (HFO) Yasağı",
                    "Lojistik şirketi kutup sularında siyah karbon bırakan ağır yakıt kullanmadığını beyan etti.",
                    "\"Tüm buzkıran filomuz temiz distile deniz yakıtına geçmiştir; buz üzerinde kurum birikimi yoktur.\"",
                    "Buzul Yüzeyi Siyah Karbon (Kurum): %79 (Çok Yüksek)  |  Egzoz Spektrumu: Ağır Fuel-Oil", "1", "30" },

                new[]{ "6", "Buzul İzleme Komitesi Başkanı Dr. Hannah Vogt", "Kıta Başkanı", "Batı Antarktika Akıntı Bariyeri Projesi",
                    "Uluslararası konsorsiyum su altı gözlem ağının verilerini paylaştı.",
                    "\"Okyanus tabanı sensör ağımız %95 kapasiteyle veri aktarıyor, sismik patlayıcı kullanılmıyor.\"",
                    "Sensör Ağı Aktifliği: %96 (Doğrulandı)  |  Akustik Kirlilik: %0 (Güvenli)", "0", "20" }
            };

            for (int i = 0; i < executiveTemplates.Length; i++)
            {
                var row = executiveTemplates[i];
                ContinentType cont = (ContinentType)int.Parse(row[0]);
                bool isLie = row[7] == "1";
                float timeLim = float.Parse(row[8]);
                int qId = 1000 + i;

                questions.Add(new ContinentQuestion
                {
                    id = qId,
                    continent = cont,
                    speakerName = row[1],
                    speakerCategory = row[2],
                    decisionTimeLimit = timeLim,
                    title = row[3],
                    description = row[4],
                    officialStatement = row[5],
                    rawTelemetryData = row[6],
                    hasContradiction = isLie,
                    choices = new BlindChoice[]
                    {
                        new BlindChoice
                        {
                            decisionText = "Bakanlık yetkisini kullanarak tavizsiz yaptırım ve ağır emisyon cezası uygula.",
                            moneyDelta = -28, halkDelta = -6, iklimDelta = 7,
                            continentHealthDelta = 12f, angerDelta = 14f,
                            factoryDelta = -18f, pollutionDelta = -20f, treeDelta = 8f, renewableDelta = 12f,
                            outcomeSummary = $"{row[1]} dosyasında sıkı bakanlık yaptırımı uygulandı; iklim toparlandı fakat sanayi direnci arttı."
                        },
                        new BlindChoice
                        {
                            decisionText = "Bakanlık bütçesinden yeşil teknoloji dönüşüm teşviği vererek uzlaşmacı takvim imzala.",
                            moneyDelta = -32, halkDelta = 2, iklimDelta = 4,
                            continentHealthDelta = 6f, angerDelta = -8f,
                            factoryDelta = -6f, pollutionDelta = -8f, renewableDelta = 14f, zeroWasteDelta = 8f,
                            outcomeSummary = $"{row[3]} için bütçeden yeşil dönüşüm fonu aktarıldı; maliyet yüksek fakat geçiş dengeli."
                        },
                        new BlindChoice
                        {
                            decisionText = "Ekonomik dengeleri ve ticaret hacmini korumak için mevcut beyanı kabul edip yaptırımı ertele.",
                            moneyDelta = 22, halkDelta = -8, iklimDelta = -12,
                            continentHealthDelta = -14f, angerDelta = -14f,
                            factoryDelta = 18f, pollutionDelta = 22f, treeDelta = -12f,
                            outcomeSummary = $"{row[3]} dosyasında sanayi lehine taviz verildi; bütçe rahatladı fakat iklim ağır darbe aldı."
                        }
                    }
                });
            }

            // --- BÖLÜM B: HALK KESİMİ, BİLİM KURULU VE CANLI BASIN (60 DİYALOG - %60) ---
            // (10 Saniyelik Yıldırım Sorular & 20 Saniyelik Bölgesel Halk Krizleri)

            string[][] publicTemplates = new string[][]
            {
                // [0] Kıta, [1] Muhatap Adı, [2] Kategori, [3] Başlık, [4] Açıklama, [5] Beyan, [6] Telemetri, [7] Çelişki, [8] Süre
                // KUZEY AMERİKA (9 Halk Diyaloğu)
                new[]{ "0", "Çiftçi Temsilcisi John Miller", "Vatandaş Talebi", "Kaliforniya Vadisi Kuraklık ve Sulama Krizi",
                    "Kuruyan kanallar nedeniyle binlerce aile çiftliği iflas eşiğinde; acil su tahsisi istiyorlar.",
                    "\"Baraj su seviyesi %15'e düştü, mahsullerimiz yanıyor! Şehir golf sahalarına giden suyu tarlalarımıza verin.\"",
                    "Baraj Doluluk Oranı: %14 (Kritik Kuraklık)  |  Toprak Nemi: %9 (Doğrulandı)", "0", "10" },
                new[]{ "0", "Canlı Yayın Muhabiri Clara Vance", "Canlı Basın Sorusu", "Canlı Yayında Zehirli Tren Kazası Sorusu",
                    "Kimyasal taşıyan yük treni nehir yakınında devrildi; milyonlarca izleyici canlı yayında yanıt bekliyor!",
                    "\"Sayın Bakan, demiryolu şirketi sızıntının zararsız olduğunu iddia ediyor ama köylüler nefes alamıyor!\"",
                    "Hava Vinil Klorür Oranı: %84 (Acil Tahliye Eşiği)  |  Nehir Sızıntısı: Aktif", "1", "10" },
                new[]{ "0", "Lise Öğrenci Temsilcisi Maya Lin", "Vatandaş Talebi", "Maskeli Öğrencilerden Temiz Hava Yürüyüşü",
                    "Sanayi dumanı yüzünden teneffüse çıkamayan binlerce öğrenci bakanlık önünde toplandı.",
                    "\"Okul bahçemizde hava kalitesi endeksi 250'yi aştı, astım vakaları ikiye katlandı!\"",
                    "Okul Bölgesi PM2.5: 265 AQI (Çok Sağlıksız)  |  Rüzgar Yönü: Rafineriden Şehre", "0", "10" },
                new[]{ "0", "Maden İşçileri Sendikası Bşk. Kowalski", "Bölgesel Sendika", "Kapatılan Kömür Ocakları ve İşsizlik İkilemi",
                    "Yeşil dönüşüm kapsamında kapanan madenlerin 12 bin işçisi tazminat ve güneş paneli fabrikası istiyor.",
                    "\"Ocaklarımız kapandı, ailelerimiz aç! Bölgeye söz verilen batarya fabrikası hâlâ kurulmadı.\"",
                    "Bölgesel İşsizlik: %34  |  Yeşil İstihdam Fonu Kullanımı: %12 (Gecikmede)", "0", "20" },
                new[]{ "0", "Dr. Aris Thorne (Atmosfer Bilimci)", "Bilim Kurulu Raporu", "Kasırga Şiddetinde %40 Artış Uyarısı",
                    "Meksika Körfezi'ndeki aşırı ısınma kategori 5 kasırgaları tetikliyor; kıyı tahkimatı şart.",
                    "\"Körfez yüzey suyu sıcaklığı normallerin 3.2 derece üzerinde; kıyı bariyerleri bu fırtınaya dayanamaz.\"",
                    "Deniz Yüzey Sıcaklığı: +3.3°C (Anomali)  |  Kıyı Dalgakıran Dayanımı: %42", "0", "10" },
                new[]{ "0", "Belediye Su Müdürü Т. Reynolds", "Bölgesel Sendika", "Sanayi İlçesi İçme Suyu Kurşun Raporu",
                    "Yerel su idaresi eski borulardan suya kurşun karıştığı iddialarını reddediyor.",
                    "\"Şebeke suyumuz tamamen berraktır, kurşun oranımız güvenli sınır olan 5 ppb'nin altındadır.\"",
                    "Şebeke Kurşun Analizi: 48 ppb (Tehlikeli Sınırın 9 Katı)  |  Boru Korozyonu: %76", "1", "20" },
                new[]{ "0", "İtfaiye Şefi Marcus Reed", "Vatandaş Talebi", "Kuzeybatı Kızılçam Orman Yangını Acil Çağrısı",
                    "Rüzgarla büyüyen yangın üç kasabayı kuşattı; acil hava söndürme filosu talep ediliyor.",
                    "\"Alevler saatte 15 kilometre hızla ilerliyor, yerel bütçemiz tükendi, acil uçak desteği şart!\"",
                    "Yangın Yayılım Hızı: 16 km/sa  |  Aktif Alev Cephesi: 42 km (Kritik)", "0", "10" },
                new[]{ "0", "Araştırmacı Gazeteci Samira Khan", "Canlı Basın Sorusu", "Lobi Şirketinin Gizli Karbon Kredisi Skandalı",
                    "Hayali orman projeleriyle sahte karbon sertifikası satan şirket canlı yayında ifşa edildi.",
                    "\"Şirket '1 milyon ağaç diktik' diyerek karbon kredisi sattı ama o arazi tamamen çorak!\"",
                    "Uydu Bitki Örtüsü (NDVI): %4 (Çorak Arazi)  |  Beyan Edilen Ağaç: 1.000.000 (Sahte)", "1", "20" },
                new[]{ "0", "Balıkçı Kooperatifi Bşk. Hank", "Vatandaş Talebi", "Pasifik Kıyısı Kızıl Alg Patlaması",
                    "Isınan sular ve gübre atıkları yüzünden zehirli algler kıyı balıkçılığını felç etti.",
                    "\"Deniz kıpkırmızı oldu, oksijen bittiği için tonlarca balık kıyıya vurdu!\"",
                    "Sudaki Çözünmüş Oksijen: %14 (Hipoksi)  |  Azot/Fosfor Akışı: %78 (Yüksek)", "0", "10" },

                // GÜNEY AMERİKA (9 Halk Diyaloğu)
                new[]{ "1", "Yerli Kabile Lideri Raoni", "Vatandaş Talebi", "Kaçak Altın Arayıcılarının Nehir İşgali",
                    "Orman koruma bölgesindeki nehirlerde kaçak tarama dubaları yerli köylerin suyunu zehirliyor.",
                    "\"Nehrimize her gün cıva döküyorlar, çocuklarımız hastalanıyor; acil nehir devriyesi gönderin!\"",
                    "Nehir Cıva Seviyesi: %79 (Zehirli)  |  Kaçak Tarama Dubası: 27 Adet Tespit Edildi", "0", "10" },
                new[]{ "1", "Dr. Helena Ribeiro (Ekolojist)", "Bilim Kurulu Raporu", "Amazon Yağmur Döngüsü Devrilme Noktası",
                    "Ormansızlaşma %20 eşiğine yaklaştı; yağmur ormanı savanaya dönüşme tehlikesi yaşıyor.",
                    "\"Güneydoğu Amazon'da kuru mevsim 4 hafta uzadı; acil ağaçlandırma koridoru açılmazsa yağmur duracak.\"",
                    "Bölgesel Nem Kaybı: -%38  |  Kurak Gün Sayısı: +29 Gün (Bilimsel Rapor Doğrulandı)", "0", "20" },
                new[]{ "1", "Kahve Üreticileri Birliği Bşk. Mateo", "Vatandaş Talebi", "And Yamaçlarında Don ve Aşırı Sıcak Şoku",
                    "İklim dengesizliği bir haftada hem kuraklık hem ani don vurmasına yol açtı; küçük çiftçiler zorda.",
                    "\"Hasadımızın %60'ı yandı, gölge ağaçlandırma ve iklime dayanıklı tohum desteği istiyoruz.\"",
                    "Tarımsal Rekolte Kaybı: %62 (Doğrulandı)  |  Toprak Erozyonu: Yüksek", "0", "10" },
                new[]{ "1", "Liman Müfettişi Morales", "Bölgesel Sendika", "Kaçak Maun Kerestesi İhracat Konteynerleri",
                    "Liman gümrüğünde 'mobilya parçası' olarak beyan edilen yüzlerce konteyner koruma altındaki ağaç çıktı.",
                    "\"İhracatçı firma konteynerlerin plantasyon çamı olduğunu beyan etti, mühürleri açmamıza izin verin!\"",
                    "X-Ray ve DNA Spektrumu: %92 Asırlık Kırmızı Maun (Koruma Altındaki Tür)", "1", "20" },
                new[]{ "1", "Selzede Mahalle Temsilcisi Lucia", "Vatandaş Talebi", "Heyelan ve Aşırı Yağış Tahliye Çağrısı",
                    "Ormansız kalan dik yamaçlar sağanak yağışla çöktü; kenar mahalleler çamur altında.",
                    "\"Evlerimiz sular altında kaldı, acil prefabrik barınak ve yamaç teraslama bütçesi bekliyoruz!\"",
                    "Yağış Miktarı: 310 mm/24sa (Rekor)  |  Yamaç Tutunma Oranı: %18 (Heyelan Riski)", "0", "10" },
                new[]{ "1", "Canlı Yayın Muhabiri Pedro Silva", "Canlı Basın Sorusu", "Baraj Çökmesi Tehlikesi Canlı Yayın Sorusu",
                    "Maden atık barajında çatlaklar görüntülendi; şirket ise barajın sağlam olduğunu söylüyor.",
                    "\"Maden şirketi 'sadece yüzeysel sıva çatlağı var' diyor ama köylüler siren sesleriyle kaçıyor!\"",
                    "InSAR Uydu Radar Ölçümü: Baraj Gövdesinde 14 cm Kayma (Yıkılma Riski %88!)", "1", "10" },
                new[]{ "1", "Biyolog Dr. Gabriela Soto", "Bilim Kurulu Raporu", "And Buzullarının %50 Erimesi ve Su Krizi",
                    "Başkentlere yaz aylarında su sağlayan dağ buzulları son 10 yılda yarı yarıya küçüldü.",
                    "\"Buzul erime hızı iki katına çıktı; yüksek irtifa su tutma göletleri inşa edilmeli.\"",
                    "Buzul Kütle Kaybı: -%51 (Doğrulandı)  |  Yaz Su Rezervi: Kritik", "0", "20" },
                new[]{ "1", "Hayvancılık Birliği Sözcüsü R. Gomez", "Bölgesel Sendika", "Mera Genişletme ve Metan Emisyonu Beyanı",
                    "Büyük sığır çiftlikleri yem katkı maddesiyle metan gazını yarıya indirdiklerini iddia ediyor.",
                    "\"Sürülerimizde deniz yosunu yem katkısına geçtik, metan emisyonumuz %50 düştü.\"",
                    "Atmosferik Metan Ölçümü: +%18 Artış  |  Yem Katkısı Kullanım Oranı: %4", "1", "20" },
                new[]{ "1", "Üniversite Gençlik Meclisi", "Vatandaş Talebi", "Şehir İçi Elektrikli Otobüs Filosu Talebi",
                    "Öğrenciler siyah duman çıkaran eski dizel otobüslerin kaldırılmasını istiyor.",
                    "\"Her gün 2 saat trafikte zehirli egzoz soluyoruz, temiz ulaşım hakkımız!\"",
                    "Cadde Boyu Karbonmonoksit: %72 (Yüksek)  |  Dizel Filo Yaşı: 19 Yıl", "0", "10" },

                // AVRUPA (9 Halk Diyaloğu)
                new[]{ "2", "Alpler Turizm ve Köy Meclisi", "Vatandaş Talebi", "Kar Yağmayan Alpler ve Erimeyen Yapay Kar Tartışması",
                    "Kış ortasında sıcaklık 16 dereceye çıktı; su kaynaklarını tüketen yapay kar makinelerine tepki var.",
                    "\"Dağ göllerimizin suyu yapay kar topları için harcanıyor, köylerimizde içme suyu azaldı!\"",
                    "Kış Sıcaklık Anomalisi: +8.4°C  |  Dağ Gölleri Su Seviyesi: -%42 (Doğrulandı)", "0", "10" },
                new[]{ "2", "Canlı Yayın Muhabiri Sophie Laurent", "Canlı Basın Sorusu", "Özel Jet Vergisi ve Karbon Kaçağı Sorusu",
                    "İklim zirvesine yüzlerce özel jetle gelinmesi kamuoyunda büyük öfke yarattı!",
                    "\"Sayın Bakan, halka kemer sıkma önerirken süper zenginlerin özel jetlerine neden karbon yasağı koymuyorsunuz?\"",
                    "Özel Havacılık Kişi Başı Emisyonu: Standart Yolcunun 14 Katı (Doğrulandı)", "0", "10" },
                new[]{ "2", "Zeytin Üreticileri Birliği Bşk. Matteo", "Vatandaş Talebi", "Akdeniz Kuraklığı ve Zeytinlik Yangınları",
                    "İspanya, İtalya ve Yunanistan hattında 45 derece sıcaklık tarımı vurdu.",
                    "\"Asırlık zeytin ağaçlarımız susuzluktan kuruyor, arıtılmış atık suların tarıma verilmesini istiyoruz!\"",
                    "Toprak Kuraklık İndeksi: %89 (Aşırı Kurak)  |  Su Geri Kazanım Oranı: %14", "0", "10" },
                new[]{ "2", "Kuzey Denizi Balıkçılar Loncası", "Bölgesel Sendika", "Isınan Deniz Suyu ve Göç Eden Balık Sürüleri",
                    "Morina ve ringa sürüleri kuzeye göç etti; kıyı balıkçıları sürdürülebilir kota desteği bekliyor.",
                    "\"Av sahalarımız boşaldı, dev fabrika gemileri ise dip trolüyle deniz tabanını kazıyor!\"",
                    "Dip Trolü Tahribatı: %74  |  Kıyı Balık Biyokütlesi: -%48 (Doğrulandı)", "0", "20" },
                new[]{ "2", "Liman Atık Şirketi Müdürü V. Novak", "Bölgesel Sendika", "Avrupa Plastik Atıklarının Geri Dönüşüm Beyanı",
                    "Atık yönetim şirketi toplanan plastiklerin tamamını kıta içinde işlediğini iddia ediyor.",
                    "\"Toplanan plastik ambalajların %85'ini kendi modern tesislerimizde granüle dönüştürüyoruz.\"",
                    "Gemi Konteyner Takibi: Atıkların %72'si Kaçak Olarak Güneydoğu Asya'ya Gönderiliyor!", "1", "20" },
                new[]{ "2", "Dr. Lukas Meyer (Hidrolog)", "Bilim Kurulu Raporu", "Avrupa Nehirlerinde Kuraklık ve Yük Taşımacılığı",
                    "Ren ve Tuna nehirlerinde su seviyesi 40 santimetreye düştü; gemiler geçemiyor.",
                    "\"Taşkın ovalarını betonlaştırmak yerine doğal menderesleri ve sulak alanları geri kazandırmalıyız.\"",
                    "Nehir Su Seviyesi: -%65 (Kritik)  |  Sulak Alan Kaybı: %70 (Doğrulandı)", "0", "20" },
                new[]{ "2", "Emekli Vatandaşlar Derneği", "Vatandaş Talebi", "Aşırı Sıcak Dalgasında Yaşlıları Koruma Çağrısı",
                    "Betonlaşmış şehir merkezlerinde gece sıcaklığı 32 altına düşmüyor; yeşil çatı ve soğutma merkezi isteniyor.",
                    "\"Binalarımız fırın gibi, şehir merkezlerine acil ağaçlı serinleme koridorları ve yalıtım desteği istiyoruz!\"",
                    "Kentsel Isı Adası Etkisi: +6.5°C  |  Yeşil Alan Oranı: %8 (Yetersiz)", "0", "10" },
                new[]{ "2", "Biyokütle Enerji Santrali Müdürü", "Bölgesel Sendika", "Odun Peleti Yakma Santrali Karbon Nötr İddiası",
                    "Santral yönetimi sadece orman atığı yaktıklarını ve sıfır karbon olduklarını savunuyor.",
                    "\"Santralimizde yalnızca kuru dal ve kereste talaşı yakılıyor, sağlam ağaç kesilmiyor.\"",
                    "Uydu Lojistik Takibi: Koruma Altındaki Karpat Ormanlarından Bütün Gövde Tomruk Sevkiyatı (%81)", "1", "20" },
                new[]{ "2", "Bisiklet ve Temiz Ulaşım İnisiyatifi", "Vatandaş Talebi", "Şehir Merkezlerinin Otomobillere Kapatılması",
                    "Halk oylamasında gençler şehir merkezlerinin tamamen yaya ve bisiklete ayrılmasını talep etti.",
                    "\"Kişisel araç trafiği azaltılsın, ücretsiz tramvay ve güvenli bisiklet otoyolları kurulsun!\"",
                    "Ulaşım Kaynaklı Emisyon: %44  |  Halk Desteği Anketi: %68 Olumlu", "0", "10" },

                // AFRİKA (9 Halk Diyaloğu)
                new[]{ "3", "Kadın Çiftçiler Kooperatifi Bşk. Zara", "Vatandaş Talebi", "Güneş Enerjili Su Kuyusu ve Soğuk Hava Deposu",
                    "Köylerde elektrik olmadığı için hasat edilen sebzelerin yarısı sıcak yüzünden çürüyor.",
                    "\"Köylerimize güneş enerjili su pompası ve soğuk oda kurarsanız hem açlık biter hem göç durur!\"",
                    "Hasat Sonrası Gıda Kaybı: %52  |  Güneşlenme Potansiyeli: %96 (Çok Yüksek)", "0", "10" },
                new[]{ "3", "Dr. Samuel Osei (Epidemiyolog)", "Bilim Kurulu Raporu", "İklim Değişikliği ve Yayılan Sıtma Kuşağı",
                    "Artan sıcaklık ve sel suları sivrisinek kuşağını yüksek yaylalara taşıdı; sağlık sistemi alarmda.",
                    "\"Durgun sel suları drene edilmeli ve kliniklere güneş enerjili aşı dolapları dağıtılmalı.\"",
                    "Yüksek İrtifa Vaka Artışı: +%180  |  Klinik Elektrik Erişimi: %28 (Doğrulandı)", "0", "20" },
                new[]{ "3", "Çoban Toplulukları Temsilcisi Musa", "Vatandaş Talebi", "Kuruyan Çad Gölü Havzasında Su Çatışması",
                    "Göl yüzeyinin %90 küçülmesi çobanlarla çiftçileri son su kaynakları başında karşı karşıya getirdi.",
                    "\"Hayvanlarımız susuzluktan ölüyor, göl havzasına acil su takviyesi ve otlak restorasyonu istiyoruz!\"",
                    "Göl Yüzey Alanı: -%88 (Kritik Küçülme)  |  Çölleşme Hızı: Yılda 4 km", "0", "10" },
                new[]{ "3", "Canlı Yayın Muhabiri Nadia Mansour", "Canlı Basın Sorusu", "Elektronik Atık Mezarlığı Canlı Yayını",
                    "Gelişmiş ülkelerden 'ikinci el bilgisayar' adı altında gönderilen milyonlarca ton hurda yakılıyor!",
                    "\"Liman müdürü bunların sağlam bağış cihazları olduğunu söylüyor ama çocuklar zehirli kablo dumanı soluyor!\"",
                    "Konteyner Denetimi: %91 Çalışmayan Hurda Elektronik  |  Hava Dioksin/Kurşun Oranı: %88 (Kritik)", "1", "10" },
                new[]{ "3", "Kobalt Madeni İşçileri Temsilcisi", "Bölgesel Sendika", "Batarya Madenlerinde İşçi Sağlığı ve Toz Kirliliği",
                    "Dünyanın elektrikli araçları için kobalt çıkaran işçiler koruyucu ekipman ve temiz su talep ediyor.",
                    "\"Yeşil enerji için maden çıkarıyoruz ama kendi köylerimiz zehirli maden tozu altında!\"",
                    "Maden Çevresi Kobalt Tozu: Standartların 18 Katı  |  Temiz Su Erişimi: %15", "0", "20" },
                new[]{ "3", "Kıyı Balıkçıları Temsilcisi Ebou", "Vatandaş Talebi", "Yabancı Fabrika Gemilerinin Kıyı Talanı",
                    "Yerel balıkçıların kayıkları boş dönerken açıkta dev yabancı trol gemileri avlanıyor.",
                    "\"Kıyıdan 3 mil açıkta balık bırakmadılar, bakanlık devriye botları bu gemileri durdursun!\"",
                    "Radar Tespiti: 19 Yabancı Trol Gemisi Yasak Kıyı Şeridinde (AIS Vericileri Kapalı)", "0", "10" },
                new[]{ "3", "Liman Kömür Terminali Müdürü", "Bölgesel Sendika", "Kömür İhracat Limanı Toz Bastırma Raporu",
                    "Terminal yönetimi kömür tozunun denize ve şehre uçmadığını iddia ediyor.",
                    "\"Kömür yığınlarımızın üzeri tamamen örtülüdür ve su pülverizatörlerimiz %95 aktiftir.\"",
                    "Uydu Optik Görüntü: Kıyı Denizinde 8 km Siyah Kömür Tozu Tabakası  |  Pülverizatör: Kapalı", "1", "20" },
                new[]{ "3", "Genç Girişimciler Ağı", "Vatandaş Talebi", "Plastik Atıktan Tuğla ve Yol Yapım Atölyeleri",
                    "Genç mühendisler sokaklardaki plastik atıkları yapı malzemesine dönüştürmek için mikro-kredi istiyor.",
                    "\"Bize küçük bir geri dönüşüm fonu verin, hem şehirleri temizleyelim hem ucuz konut üretelim!\"",
                    "Kentsel Plastik Atık: Günde 4.500 Ton  |  Proje Geri Dönüşüm Verimi: %88", "0", "10" },
                new[]{ "3", "Dr. Fatima Zahra (Tarım Bilimci)", "Bilim Kurulu Raporu", "Çekirge Sürülerine Karşı Biyolojik Erken Uyarı",
                    "Kızıldeniz çevresindeki aşırı yağışlar çöl çekirgesi patlamasını tetikledi.",
                    "\"Kimyasal ilaç yerine İHA destekli biyolojik mantar spreyi kullanırsak hem ekinleri hem arıları koruruz.\"",
                    "Sürü Yoğunluğu: Kritik  |  Biyolojik Sprey Ekosistem Güvenliği: %97 (Doğrulandı)", "0", "20" },

                // ASYA (9 Halk Diyaloğu)
                new[]{ "4", "Pirinç Çiftçileri Temsilcisi Nguyen", "Vatandaş Talebi", "Mekong Deltası Tuzlu Su Basması",
                    "Deniz seviyesinin yükselmesi ve üst havzadaki barajlar yüzünden okyanus suyu pirinç tarlalarına girdi.",
                    "\"Toprağımız tuzlandı, pirinçler kuruyor! Tuzluluğa dayanıklı tohum ve kıyı seti istiyoruz.\"",
                    "Kanal Su Tuzluluğu: 12 g/L (Tarım Sınırının 4 Katı)  |  Rekolte Riski: -%55", "0", "10" },
                new[]{ "4", "Canlı Yayın Muhabiri Ananya Roy", "Canlı Basın Sorusu", "Muson Selleri ve Kentsel Drenaj Çöküşü",
                    "Mega kentte bir günde bir aylık yağmur yağdı; metro istasyonları ve hastaneler su altında!",
                    "\"İnşaat şirketi sulak alanı imara açarken 'dev yağmur tünelleri yaptık' demişti ama şehir boğuluyor!\"",
                    "Drenaj Tüneli Kapasitesi: Beyan Edilenin %15'i (Mühendislik Usulsüzlüğü!)  |  Su Baskını: Kritik", "1", "10" },
                new[]{ "4", "Dr. Kenjiro Takahashi (Deniz Biyoloğu)", "Bilim Kurulu Raporu", "Pasifik Plastik Girdabı Temizleme Filosu",
                    "Açık okyanusta biriken milyonlarca ton hayalet balık ağı ve plastik için acil toplama gemisi gerekiyor.",
                    "\"Yüzer bariyer sistemimiz ayda 500 ton okyanus plastiğini toplayabilir, yakıt ve bütçe onayı bekliyoruz.\"",
                    "Yüzer Atık Yoğunluğu: Yüksek  |  Sistem Toplama Verimi: %89 (Doğrulandı)", "0", "20" },
                new[]{ "4", "Şehir Hastanesi Başhekimi Dr. Lin", "Vatandaş Talebi", "Kış Dumanı ve Çocuk Servislerinde Acil Durum",
                    "Kömür dumanı ve terselme (inversiyon) nedeniyle şehirde görüş mesafesi 20 metreye düştü.",
                    "\"Çocuk acil servislerimizde yer kalmadı, şehir çevresindeki ağır sanayiyi 72 saat durdurun!\"",
                    "Hava Kalitesi İndeksi: 480 AQI (Tehlikeli)  |  SO2 ve PM2.5: Sınırın 19 Katı", "0", "10" },
                new[]{ "4", "Elektronik Sanayi Bölge Müdürü", "Bölgesel Sendika", "Nehir Kenarı Sanayi Parkı Gece Deşarjı",
                    "Sanayi parkı yönetimi nehirde görülen beyaz köpüklerin doğal olduğunu iddia ediyor.",
                    "\"Nehirdeki köpüklenme mevsimsel polen ve çalkantı kaynaklıdır, tesislerimizden kimyasal sızmadı.\"",
                    "Su Analizi: Yüksek Oranda PFAS (Kalıcı Kimyasal) ve Fosfat  |  Gece Pompası: 02:00-05:00 Aktif", "1", "20" },
                new[]{ "4", "Himalaya Dağ Köyleri Temsilcisi Tenzin", "Vatandaş Talebi", "Buzul Gölü Patlaması (GLOF) Erken Uyarı Talebi",
                    "Eriyen Himalaya buzullarının oluşturduğu dev gölün doğal bendi çatlamak üzere!",
                    "\"Vadideki 30 köy tehlikede, göl suyunu tahliye edecek sifon boruları ve siren sistemi kurulsun!\"",
                    "Buzul Gölü Hacmi: +%210 Artış  |  Moren Bendi Sızıntısı: Aktif (Acil Risk!)", "0", "10" },
                new[]{ "4", "Tekstil İşçileri Sendikası", "Bölgesel Sendika", "Fabrika İçi Aşırı Sıcaklık ve Havalandırma",
                    "İklim dalgasıyla fabrika içi sıcaklık 42 dereceyi buldu; işçiler güneş enerjili soğutma istiyor.",
                    "\"Çatılara güneş paneli ve soğutma sistemi kurulursa hem emisyon düşer hem işçiler bayılmaz!\"",
                    "Atölye İçi Sıcaklık: 41.5°C  |  Çatı Güneş Potansiyeli: %94 Uygun", "0", "20" },
                new[]{ "4", "Güneş Paneli Üreticisi CEO'su W. Zhang", "Holding CEO'su", "Polisilikon Üretiminde Kömür Elektriği İddiası",
                    "Güneş paneli fabrikası %100 yeşil elektrikle üretim yaptığını beyan ederek ihracat muafiyeti istiyor.",
                    "\"Güneş panellerimizi tamamen hidroelektrik enerjisiyle üretiyoruz, karbon ayak izimiz sıfırdır.\"",
                    "Şebeke Bağlantı Kaydı: Elektriğin %82'si Kaçak Kömür Santralinden Çekiliyor!", "1", "20" },
                new[]{ "4", "Mangrov Koruma Gönüllüleri", "Vatandaş Talebi", "Tayfunlara Karşı Kıyı Mangrov Ormanı Dikimi",
                    "Kıyı köylüleri karides çiftlikleri için kesilen mangrov ormanlarını yeniden dikmek istiyor.",
                    "\"Mangrov ağaçları dev dalgaları %70 kırıyor ve karbon tutuyor; fide fonunu onaylayın!\"",
                    "Dalga Enerjisi Sönümleme: %72 (Doğrulandı)  |  Karbon Tutumu: Normal Ormanın 4 Katı", "0", "10" },

                // OKYANUSYA (8 Halk Diyaloğu)
                new[]{ "5", "Ada İklim Mültecileri Sözcüsü Keanu", "Vatandaş Talebi", "Yükselen Okyanus ve Tuzlanan Mercan Adaları",
                    "Pasifik'teki alçak mercan adalarında fırtına dalgaları tatlı su kuyularını bastı.",
                    "\"Evlerimiz ve tarlalarımız sular altında kalıyor, yüzer güneşli arıtma ünitesi ve kıyı duvarı şart!\"",
                    "Gelgit Su Seviyesi: +42 cm Anomali  |  Tatlı Su Merceği Tuzlanma Oranı: %78", "0", "10" },
                new[]{ "5", "Gönüllü İtfaiyeci ve Veteriner Sarah", "Vatandaş Talebi", "Okaliptüs Ormanlarında Yaban Hayatı Kurtarma",
                    "Yangın kuşağında kalan koala ve keseli canlılar için acil su istasyonları ve yangın şeridi isteniyor.",
                    "\"Orman tabanı barut gibi kuru, İHA'larla tohum topu ve yaban hayatı su istasyonları kurmalıyız!\"",
                    "Orman Nem İndeksi: %7 (Ekstrem Yangın Riski)  |  Yaban Hayatı Kaybı: Kritik", "0", "10" },
                new[]{ "5", "Canlı Yayın Muhabiri Liam Kelly", "Canlı Basın Sorusu", "Mercan Resifi Turizm Şirketi Beyanı",
                    "Dev kruvaziyer şirketi mercanların kendi gemilerinden değil doğal döngüden beyazladığını iddia ediyor.",
                    "\"Şirket sözcüsü 'gemilerimiz resife çapa atmıyor ve atık bırakmıyor' diyor, uydu verisi ne söylüyor?\"",
                    "Deniz Tabanı Sonar Kaydı: Resif Üzerinde 600 Metre Ağır Çapa Zinciri Tahribatı ve Sintine Deşarjı!", "1", "10" },
                new[]{ "5", "Dr. Noah Vance (Okyanus Bilimci)", "Bilim Kurulu Raporu", "Bulut Parlatma ile Mercan Soğutma Deneyi",
                    "Deniz suyunu mikro-damlacıklar halinde püskürterek güneş ışığını yansıtma projesi onay bekliyor.",
                    "\"Tuzlu su buharıyla resif üzerindeki su sıcaklığını 1.5 derece düşürebiliriz, yan etkisi yoktur.\"",
                    "Test Sahası Soğutma Etkisi: -1.4°C (Doğrulandı)  |  Kimyasal Katkı: %0 (Saf Deniz Suyu)", "0", "20" },
                new[]{ "5", "Yerli Toprak Koruyucuları Meclisi", "Bölgesel Sendika", "Geleneksel Kontrollü Kış Yakımı Programı",
                    "Yerli uzmanlar yazın çıkacak mega yangınları önlemek için düşük ısılı geleneksel temizlik öneriyor.",
                    "\"Binlerce yıllık yöntemimizle kuru çalıları kontrollü temizlersek yaz yangınları %80 azalır.\"",
                    "Pilot Bölge Yangın Azalması: -%82 (Doğrulandı)  |  Toprak Verimliliği: +%15", "0", "20" },
                new[]{ "5", "Kıyı Alüminyum İzabe Tesisi Müdürü", "Bölgesel Sendika", "Florür Gazı ve Kırmızı Çamur Havuzu Raporu",
                    "Fabrika yönetimi kırmızı çamur barajından yeraltı suyuna sızıntı olmadığını iddia ediyor.",
                    "\"Atık havuzlarımızda sızıntı %0'dır ve baca florür filtrelerimiz tam kapasite çalışmaktadır.\"",
                    "Yeraltı Suyu Alkalinite/Ağır Metal: %74 (Sızıntı Var!)  |  Baca Filtre Verimi: %21", "1", "20" },
                new[]{ "5", "Koyun Yetiştiricileri Birliği", "Vatandaş Talebi", "Kurak Bozkırlarda Rüzgar + Mera Ortak Kullanımı",
                    "Çiftçiler tarlalarına rüzgar türbini dikilmesi karşılığında güneşli sulama kanalı istiyor.",
                    "\"Arazilerimizi temiz enerjiye açmaya hazırız, yeter ki hayvanlarımız için su garantisi verilsin!\"",
                    "Rüzgar Verimi: %91  |  Mera Su İhtiyacı: Karşılanabilir (Kazan-Kazan)", "0", "10" },
                new[]{ "5", "Liman Konteyner Sendikası", "Bölgesel Sendika", "Yeşil Hidrojenli Liman Vinçleri Dönüşümü",
                    "Liman işçileri dizel vinçlerin hidrojen ve elektriğe dönüştürülmesini talep ediyor.",
                    "\"Liman havası egzozdan geçilmiyor, güneş enerjili elektroliz ünitesiyle sıfır emisyona geçelim.\"",
                    "Liman NOx Yoğunluğu: %68  |  Yeşil Hidrojen Uygunluğu: %90", "0", "20" },

                // ANTARKTİKA VE KÜRESEL OKYANUSLAR (7 Halk & Bilim Diyaloğu)
                new[]{ "6", "Buzul Sondaj Ekibi Lideri Dr. E. Vance", "Bilim Kurulu Raporu", "Thwaites Buzulu Altında Sıcak Su Kuşağı",
                    "Bilim ekibi buzulun altındaki okyanus suyunun beklenenden iki kat hızlı erime yaptığını bildirdi.",
                    "\"Buzul tabanında 2.4 derecelik sıcak akıntı tespit ettik; acil otonom denizaltı gözlem ağı kurmalıyız!\"",
                    "Taban Suyu Sıcaklığı: +2.4°C Anomali (Doğrulandı)  |  Erime Hızı: Yılda 45 Milyar Ton", "0", "10" },
                new[]{ "6", "Canlı Yayın Muhabiri Erik Lind", "Canlı Basın Sorusu", "Antarktika Lüks Turizm Gemileri Skandalı",
                    "Kutup bölgesine giden lüks yolcu gemilerinin sintine suyunu buzulların arasına bıraktığı iddia edildi!",
                    "\"Turizm şirketi 'sıfır atık gemisiyiz, tüm atıklarımızı anakaraya taşıyoruz' diyor, doğru mu?\"",
                    "Kızılötesi Uydu İzi: Gemi Rotalarında 12 km Atık Su ve Yakıt Deşarj İzi (%88 İhlal!)", "1", "10" },
                new[]{ "6", "Deniz Koruma Aktivisti Capt. Paul", "Vatandaş Talebi", "Güney Okyanusu Kaçak Dişbalığı Avcıları",
                    "Buzulların arasında yasadışı ağ atan hayalet gemiler tespit edildi; devriye gemisi yakıt bekliyor.",
                    "\"Kaçak avcılar 40 kilometrelik dip ağları bıraktı, koordinatları belirledik; müdahale yetkisi verin!\"",
                    "Uydu Radar Tespiti: Bayraksız 3 Fabrika Gemisi Koruma Alanında (Doğrulandı)", "0", "10" },
                new[]{ "6", "Dr. Mei-Ling Zhou (İklim Modelcisi)", "Bilim Kurulu Raporu", "Güney Okyanusu Karbon Yutak Kapasitesi Düşüşü",
                    "Soğuk suların karbon emme gücü fırtına kuşaklarının kayması nedeniyle zayıflıyor.",
                    "\"Okyanus fitoplankton dengesini izlemek için 200 adet güneş enerjili şamandıra bırakmalıyız.\"",
                    "Karbon Emilim Düşüşü: -%14 (Doğrulandı)  |  Şamandıra Ağı Maliyeti: Düşük/Etkili", "0", "20" },
                new[]{ "6", "Kutup Lojistik Müteahhidi B. Krauss", "Bölgesel Sendika", "Kutup Üssü Yakıt Depoları Sızdırmazlık Raporu",
                    "Özel lojistik firması buz üzerindeki yakıt tanklarının çift cidarlı ve sızdırmaz olduğunu söylüyor.",
                    "\"Yakıt tanklarımızda sızıntı oranı %0'dır, kar örtüsü altında hiçbir hidrokarbon kalıntısı yoktur.\"",
                    "Buz Altı Spektrometre: Tank Çevresinde 4.200 Litre Dizel Sızıntısı Tespit Edildi!", "1", "20" },
                new[]{ "6", "Genç İklim Araştırmacıları Ekibi", "Vatandaş Talebi", "800 Bin Yıllık Buz Çekirdeği Arşivi Soğutma Arızası",
                    "Dünyanın geçmiş iklim hafızasını tutan buz çekirdeği deposunun yedek soğutucusu bozuldu!",
                    "\"Eşsiz buz örneklerinin erimemesi için acil rüzgar destekli kriyojenik soğutucu ödeneği gerekiyor!\"",
                    "Depo Sıcaklığı: -4°C'ye Yükseldi (Güvenli Sınır: -22°C)  |  Aciliyet: Çok Yüksek", "0", "10" },
                new[]{ "6", "Uluslararası Meteoroloji Ağı", "Bilim Kurulu Raporu", "Ozon Tabakası ve Yasaklı Gaz (CFC-11) Tespiti",
                    "Atmosferik sensörler yasaklanmış olan ozon delici gazların tekrar salındığını yakaladı.",
                    "\"Kutup stratosferinde CFC-11 yoğunluğu arttı; kaçak yalıtım köpüğü üreten tesislere yaptırım şart!\"",
                    "Stratosferik CFC-11: +%28 Artış (Doğrulandı)  |  Ozon İncelmesi: Kritik", "0", "20" }
            };

            for (int i = 0; i < publicTemplates.Length; i++)
            {
                var row = publicTemplates[i];
                ContinentType cont = (ContinentType)int.Parse(row[0]);
                bool isLie = row[7] == "1";
                float timeLim = float.Parse(row[8]);
                int qId = 2000 + i;

                questions.Add(new ContinentQuestion
                {
                    id = qId,
                    continent = cont,
                    speakerName = row[1],
                    speakerCategory = row[2],
                    decisionTimeLimit = timeLim,
                    title = row[3],
                    description = row[4],
                    officialStatement = row[5],
                    rawTelemetryData = row[6],
                    hasContradiction = isLie,
                    choices = new BlindChoice[]
                    {
                        new BlindChoice
                        {
                            decisionText = "Halkın ve bilim kurulunun talebini derhal onayla; acil saha müdahalesi ve koruma başlat.",
                            moneyDelta = -32, halkDelta = -6, iklimDelta = 6,
                            continentHealthDelta = 10f, angerDelta = -10f,
                            pollutionDelta = -14f, treeDelta = 10f, foodDelta = 10f, renewableDelta = 8f,
                            outcomeSummary = $"{row[3]}: Acil müdahale emri verildi; bütçe tükendi ve sanayi itiraz etti fakat felaket önlendi."
                        },
                        new BlindChoice
                        {
                            decisionText = "Yerel yönetimle ortak fon oluşturarak kademeli altyapı ve teknoloji desteği sağla.",
                            moneyDelta = -22, halkDelta = 0, iklimDelta = 3,
                            continentHealthDelta = 6f, angerDelta = -6f,
                            pollutionDelta = -8f, renewableDelta = 10f, zeroWasteDelta = 8f,
                            outcomeSummary = $"{row[3]}: Kademeli teknoloji fonu oluşturuldu; etki yavaş fakat bütçe dengesi korundu."
                        },
                        new BlindChoice
                        {
                            decisionText = "Bütçe kısıtlamalarını gerekçe göstererek talebi reddet ve mevcut akışı koru.",
                            moneyDelta = 20, halkDelta = -6, iklimDelta = -10,
                            continentHealthDelta = -12f, angerDelta = 16f,
                            pollutionDelta = 14f, foodDelta = -10f, treeDelta = -8f,
                            outcomeSummary = $"{row[3]}: Müdahale talebi reddedildi; bütçe arttı fakat ekolojik erozyon hızlandı."
                        }
                    }
                });
            }
        }
    }
}
