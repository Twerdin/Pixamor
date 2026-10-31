using System;
using UnityEngine;

namespace Pixamor.COP31
{
    public enum DialogueCategory
    {
        President = 0,    // Devlet Başkanları & Başbakanlar (1-20)
        Scientist = 1,    // Bilim İnsanları & BM Gözlemcileri (21-40)
        FirstResponder = 2, // Saha Ekipleri & Acil Kriz Şefleri (41-60)
        Citizen = 3,      // Halk, Çiftçiler & İklim Mültecileri (61-80)
        Lobbyist = 4      // Şirket Lobileri & Enerji Baronları (81-100)
    }

    [Serializable]
    public struct DialogueEntry
    {
        public int id;
        public DialogueCategory category;
        public string speaker;
        public string text;

        public DialogueEntry(int id, DialogueCategory category, string speaker, string text)
        {
            this.id = id;
            this.category = category;
            this.speaker = speaker;
            this.text = text;
        }

        public string Formatted => $"<b>{speaker}:</b> \"{text}\"";
    }

    /// <summary>
    /// Reigns ve Papers, Please tarzında 100 adet özgün iklim krizi ve diplomasi diyaloğu veri tabanı.
    /// Koddan rastgele veya kategoriye göre çekilebilir.
    /// </summary>
    public static class ClimateDialogueDatabase
    {
        static DialogueEntry[] allDialogues;

        public static DialogueEntry[] GetAll()
        {
            if (allDialogues == null) BuildDatabase();
            return allDialogues;
        }

        public static DialogueEntry GetRandom()
        {
            var list = GetAll();
            return list[UnityEngine.Random.Range(0, list.Length)];
        }

        public static DialogueEntry GetRandomFromCategory(DialogueCategory cat)
        {
            var list = GetAll();
            int start = (int)cat * 20;
            int idx = UnityEngine.Random.Range(start, start + 20);
            return list[idx];
        }

        static void BuildDatabase()
        {
            allDialogues = new DialogueEntry[]
            {
                // 1. Grup: Devlet Başkanları & Başbakanlar (1-20)
                new DialogueEntry(1, DialogueCategory.President, "Almanya Şansölyesi", "Kömür santrallerini kapatmamı istiyorsunuz ama kış kapıda. Halkım donarsa hükümetim düşer, bütçeden sübvansiyon talep ediyorum!"),
                new DialogueEntry(2, DialogueCategory.President, "Brezilya Devlet Başkanı", "Amazon ormanları bizim egemenlik alanımız. Batı dünyası yüz yıl boyunca dünyayı kirletti, şimdi bize emir veremezsiniz!"),
                new DialogueEntry(3, DialogueCategory.President, "Tuvalu Başbakanı", "Ülkemizin en yüksek noktası denizden sadece 4 metre yukarıda. Bir sonraki fırtınada tüm tarihimiz sular altında kalacak, acil tahliye koridoru açın!"),
                new DialogueEntry(4, DialogueCategory.President, "ABD Başkanı", "Teksas rafinerilerini kısıtlarsam borsa çöker. Seçim yılındayız; bana iklim değil, enflasyonu düşürecek bir formülle gelin."),
                new DialogueEntry(5, DialogueCategory.President, "Çin Devlet Başkanı", "Güneş paneli üretimini iki katına çıkardık fakat fabrikaları aniden durduramayız. Süreci 2035'e ertelemezseniz masadan kalkarız."),
                new DialogueEntry(6, DialogueCategory.President, "Hindistan Başbakanı", "Muson yağmurları gelmedi, 400 milyon çiftçim mahsul alamadı. Bize buğday hibe etmezseniz su savaşı başlar."),
                new DialogueEntry(7, DialogueCategory.President, "Suudi Arabistan Enerji Bakanı", "Petrol vanalarını kısmamızı istiyorsunuz ama küresel dönüşüm fonunuzun %30'unu biz finanse ediyoruz. Bu talebi geri çekin."),
                new DialogueEntry(8, DialogueCategory.President, "Fransa Cumhurbaşkanı", "Yakıt vergisine 5 sent zam yaptık diye sokaklar sarı yeleklilerle doldu, Paris yanıyor! Yeşil dönüşümü halka anlatamıyoruz."),
                new DialogueEntry(9, DialogueCategory.President, "Japonya Başbakanı", "Fukuşima'dan sonra nükleeri kapattık, kömüre mahkumuz. Salınımı düşürmemiz için bize acil füzyon lisansı vermelisiniz."),
                new DialogueEntry(10, DialogueCategory.President, "Norveç Başbakanı", "Elektrikli araç oranımız %90 fakat Kuzey Denizi petrolünü durdurursak refah fonumuz erir. Hem çevreci hem zengin kalmak imkansız mı?"),
                new DialogueEntry(11, DialogueCategory.President, "Nijerya Cumhurbaşkanı", "Lagos sular altında, petrol boruları deltayı zehirledi. Çok uluslu petrol şirketlerini kovarsak iflas ederiz, kalırlarsa zehirleniriz."),
                new DialogueEntry(12, DialogueCategory.President, "İspanya Başbakanı", "Endülüs resmen çöle dönüştü. Zeytinlikler kurudu, Avrupa'nın sebze ambarı boşaldı. Acil arıtma tesisi hibesi istiyoruz."),
                new DialogueEntry(13, DialogueCategory.President, "Kanada Başbakanı", "Kuzey Kutbu'nda buzlar eridikçe yeni ticaret yolları açılıyor. Çevre felaketini anlıyorum ama bu rota trilyon dolarlık fırsat sunuyor."),
                new DialogueEntry(14, DialogueCategory.President, "Yunanistan Başbakanı", "Rodos ve Eğriboz alev alev, turistler tahliye ediliyor. Bu gece itfaiye uçakları gelmezse turizmimiz tamamen biter!"),
                new DialogueEntry(15, DialogueCategory.President, "Endonezya Devlet Başkanı", "Jakarta her yıl 25 cm batıyor. Başkenti Borneo ormanlarına taşımaktan başka çaremiz kalmadı, bize doğayı yok etmeden inşa etmeyi öğretin."),
                new DialogueEntry(16, DialogueCategory.President, "Mısır Cumhurbaşkanı", "Nil üzerindeki Rönesans Barajı suyumuzu kesti. 100 milyon Mısırlı aç kalır, diplomatik baskı kurmazsanız barajı vurmak zorunda kalacağız."),
                new DialogueEntry(17, DialogueCategory.President, "İtalya Başbakanı", "Po Nehri kurudu, Venedik sular altında kaldı, Alpler eriyor. Ülke aynı anda hem kuraklık hem sellerle boğuşuyor, bütçe yetmiyor!"),
                new DialogueEntry(18, DialogueCategory.President, "Güney Afrika Devlet Başkanı", "Cape Town'da şebeke suları tamamen kesildi. Halk sokak çeşmelerinde bidonlarla kavga ediyor, ordu düzeni sağlayamıyor."),
                new DialogueEntry(19, DialogueCategory.President, "Bangladeş Başbakanı", "Her tayfunda 2 milyon insan evsiz kalıyor. Biz kirletmedik ama faturasını biz ödüyoruz, İklim Tazminat Fonu derhal ödenmelidir!"),
                new DialogueEntry(20, DialogueCategory.President, "Rusya Devlet Başkanı", "Sibirya'da donmuş topraklar eridikçe petrol ve doğalgaza ulaşmamız kolaylaşıyor. İklim anlaşmaları büyümemizi engelleyemez."),

                // 2. Grup: Bilim İnsanları & BM Gözlemcileri (21-40)
                new DialogueEntry(21, DialogueCategory.Scientist, "IPCC Baş Bilim İnsanı", "1.5 derece hedefini dün gece resmi olarak aştık. Geri dönüşü olmayan devrilme noktası tetiklendi, artık sadece hayatta kalma modundayız."),
                new DialogueEntry(22, DialogueCategory.Scientist, "Kutup Buzul Bilimci", "Grönland'dan Manhattan büyüklüğünde buzul koptu. Küresel deniz seviyesi 48 saat içinde 3 milimetre sıçrayacak."),
                new DialogueEntry(23, DialogueCategory.Scientist, "Okyanus Bilimci", "Mercan resiflerinin %93'ü ağardı. Bu, denizel besin zincirinin 6 ay içinde tamamen çökmesi demektir."),
                new DialogueEntry(24, DialogueCategory.Scientist, "Sibirya Enstitüsü", "Eriyen permafrost tabakasından atmosfere milyonlarca ton metan fışkırıyor. Felaket katlanarak hızlanıyor!"),
                new DialogueEntry(25, DialogueCategory.Scientist, "Atmosfer Fiziği Uzmanı", "Kutup Girdabı parçalandı. Ekvator fırtınaları kutuplara, kutup soğukları ise Akdeniz'e iniyor."),
                new DialogueEntry(26, DialogueCategory.Scientist, "BM Kuraklık Gözlemcisi", "Doğu Afrika'da üst üste 6. yağmur sezonu kurak geçti. 20 milyon insan açlık sınırında, acil yardım koridoru açılmalı."),
                new DialogueEntry(27, DialogueCategory.Scientist, "Zooloji Profesörü", "Tozlaşmayı sağlayan arı popülasyonu %40 çöktü. Seneye buğday ve elma hasadı alamayacaksınız."),
                new DialogueEntry(28, DialogueCategory.Scientist, "Hidroloji Uzmanı", "Himalaya buzulları tükendiğinde Ganj ve İndus nehirleri kuruyacak. 1.5 milyar insanın içme suyu yok olmak üzere."),
                new DialogueEntry(29, DialogueCategory.Scientist, "Epidemiyolog", "Dang humması taşıyan sivrisinekler Kuzey Avrupa ve Kanada'ya ulaştı. Yeni bir pandemi kapımızda."),
                new DialogueEntry(30, DialogueCategory.Scientist, "Orman Ekolojisi Uzmanı", "Amazon artık karbon yutan değil, karbon salan bir ekosisteme dönüştü. Ağaçlar aşırı sıcaktan fotosentezi durdurdu."),
                new DialogueEntry(31, DialogueCategory.Scientist, "Jeofizik Mühendisi", "Yeraltı suları aşırı çekildi. Konya Ovası'nda 300 yeni obruk oluştu, Meksika şehri çöküyor."),
                new DialogueEntry(32, DialogueCategory.Scientist, "Dünya Meteoroloji Örgütü", "Önümüzdeki hafta Kuzey Yarımküre'de Isı Kubbesi oluşacak. Asfaltlar eriyecek, açık alanda çalışmak ölüm demek."),
                new DialogueEntry(33, DialogueCategory.Scientist, "BM İklim Tazminat Komisyoneri", "Kaybettiklerimiz sadece toprak değil; kültürler ve diller yok oluyor. Pasifik'teki 3 ada ülkesi haritadan silinecek."),
                new DialogueEntry(34, DialogueCategory.Scientist, "Jeomühendislik Uzmanı", "Stratosfere kükürt dioksit püskürterek güneşi karartabiliriz. Asit yağmuru riski var ama küresel ısıyı 1 derece düşürürüz."),
                new DialogueEntry(35, DialogueCategory.Scientist, "Toprak Verimliliği Uzmanı", "Aşırı sıcaklar yüzünden tarım topraklarının üst katmanı tükendi. İnsanlığın sadece 60 hasatlık toprağı kaldı."),
                new DialogueEntry(36, DialogueCategory.Scientist, "Okyanus Akıntıları Uzmanı", "Gulf Stream tarihin en yavaş seviyesinde. Durursa Kuzey Avrupa buzul çağına gömülür, tropikler kaynar."),
                new DialogueEntry(37, DialogueCategory.Scientist, "Uydusal İzleme Operatörü", "Termal uydular Sibirya'da 1500 noktada Zombi Yangınları tespit etti; turba toprakları kar altında için için yanıyor."),
                new DialogueEntry(38, DialogueCategory.Scientist, "Antarktika Sondaj Lideri", "Kıyamet Günü buzulunun tabanına ılık okyanus suyu sızdı. Deniz tabanından koptuğu an küresel kıyılar 3 metre su altında kalır."),
                new DialogueEntry(39, DialogueCategory.Scientist, "Hava Kalitesi Merkezi", "Yeni Delhi ve Lahor'da hava kirliliği indeksi 999 sınırını aştı. Bu havayı 1 gün solumak 40 paket sigaraya eşdeğer."),
                new DialogueEntry(40, DialogueCategory.Scientist, "BM Modelleme Birimi", "2028 yılına kadar mevcut tüketim sürerse dünya nüfusunun %15'i iklim göçmeni olacak."),

                // 3. Grup: Saha Ekipleri & Acil Kriz Şefleri (41-60)
                new DialogueEntry(41, DialogueCategory.FirstResponder, "İtfaiye Komutanı", "Alevler 70 metreye ulaştı ve kendi fırtınasını yarattı. Ekiplerimi geri çekmek zorundayım, bir şehri feda etmeliyiz!"),
                new DialogueEntry(42, DialogueCategory.FirstResponder, "Baraj Başmühendisi", "Gelen sel baraj kapasitesinin 3 katı! Savakları açarsam köyleri su basacak, açmazsam baraj gövdesi patlayacak!"),
                new DialogueEntry(43, DialogueCategory.FirstResponder, "Nükleer Santral Direktörü", "Nehir suyu sıcaklığı 28 dereceyi geçti, soğutma suyu yetersiz. Santrali durdurursam 5 milyon insan karanlıkta kalır."),
                new DialogueEntry(44, DialogueCategory.FirstResponder, "Sahil Güvenlik Komutanı", "Fırtına 9 kuvvete ulaştı, göçmen botları batıyor. Ekiplerim kimi kurtaracağını şaşırdı; deniz ceset kaynıyor!"),
                new DialogueEntry(45, DialogueCategory.FirstResponder, "Elektrik Şebekesi Operatörü", "Klimalar yüzünden trafolar patlamak üzere. Ya kontrollü elektrik kesintisi yapacağız ya da tüm kıta karanlığa gömülecek."),
                new DialogueEntry(46, DialogueCategory.FirstResponder, "Sahra Hastanesi Başhekimi", "Hissedilen sıcaklık 54 derece, morglar taştı. Hastaları koridorlarda buz kalıplarına yatırıyoruz, serum bitti!"),
                new DialogueEntry(47, DialogueCategory.FirstResponder, "Tahliye Koordinatörü", "Sığınaklar 100 bin kişilik ama sahilde 1 milyon insan bekliyor. Kimin kalıp kimin gideceğine siz karar verin!"),
                new DialogueEntry(48, DialogueCategory.FirstResponder, "Karayolları Genel Müdürü", "Asfalt sıcaktan sakız gibi eridi, kamyonlar tekerlekleri saplandığı için kontak kapattı. Gıda ve ilaç tedariki felç oldu."),
                new DialogueEntry(49, DialogueCategory.FirstResponder, "Metro İşletmecisi", "Tüneller fırına döndü, raylar genleşip büküldü. Seferleri durdurmazsak trenler raydan çıkacak, durdurursak şehir felç."),
                new DialogueEntry(50, DialogueCategory.FirstResponder, "Havalimanı Kriz Masası", "Hava çok sıcak olduğu için uçaklar havalanamıyor. Bütün uluslararası uçuşlar iptal edildi, 80 bin yolcu mahsur."),
                new DialogueEntry(51, DialogueCategory.FirstResponder, "Şehir Su İdaresi Şefi", "Şebeke basıncı sıfıra indi. İtfaiyenin suyu yok. Yangın musluklarını halka mı açalım, itfaiyeye mi ayıralım?"),
                new DialogueEntry(52, DialogueCategory.FirstResponder, "Dağ Kurtarma Lideri", "Eriyen buzullar yüzünden dağlar ufalanıyor. Kasabaya kaya heyelanı inmek üzere, sirenleri çalıp tahliye edelim mi?"),
                new DialogueEntry(53, DialogueCategory.FirstResponder, "Orman Muhafızı", "Yasadışı tarla açmak için ormanı yakan çetelerle çatışmaya girdik. Ordu desteği gelmezse bu orman bu gece kül olur."),
                new DialogueEntry(54, DialogueCategory.FirstResponder, "Deniz Limanı Yöneticisi", "Fırtına kabarması bariyerleri aşmak üzere. Liman kapılarını kapatırsak ticaret durur, kapatmazsak limanı su basar."),
                new DialogueEntry(55, DialogueCategory.FirstResponder, "Belediye Temizlik Müdürü", "Aşırı sıcakta çöp dağları kendiliğinden alev alıp zehirli gaz yayıyor. Şehir merkezine gaz maskesi dağıtmalıyız."),
                new DialogueEntry(56, DialogueCategory.FirstResponder, "Hızlı Tren Şefi", "Tayfun rüzgarı saatte 220 km'ye ulaştı. Trenler elektriksiz kaldı, viyadüklerin üzerinde yolcular sallanıyor!"),
                new DialogueEntry(57, DialogueCategory.FirstResponder, "Boru Hattı Mühendisi", "Sibirya'daki boru hatlarının altı bataklığa döndü. Borular esneme sınırını aştı, her an patlama ve gaz sızıntısı olabilir."),
                new DialogueEntry(58, DialogueCategory.FirstResponder, "Kızılay Saha Amiri", "Temiz su tükendiği için kolera salgını başladı. Sahra çadırlarımıza her saat 50 hasta geliyor, klor tableti gönderin!"),
                new DialogueEntry(59, DialogueCategory.FirstResponder, "Hava Ambulansı Pilotu", "Yoğun duman yüzünden görüş sıfıra indi. Yangın bölgesinde mahsur kalan 40 kişi çığlık atıyor ama helikopter kalkamıyor!"),
                new DialogueEntry(60, DialogueCategory.FirstResponder, "Tahıl Silosu Güvenlik Şefi", "Kuraklık yüzünden un fiyatı 4 katına çıktı. Yağmacılar siloları basıyor, özel güvenlik ateş açmak üzere, polis gönderin!"),

                // 4. Grup: Halk, Çiftçiler & İklim Mültecileri (61-80)
                new DialogueEntry(61, DialogueCategory.Citizen, "Öfkeli Çiftçi", "40 yıllık zeytin ağaçlarım kurudu, toprağım taşa döndü. Banka traktörüme haciz koydu. Başkente yürüyoruz, yolumuzu kesmeyin!"),
                new DialogueEntry(62, DialogueCategory.Citizen, "İklim Aktivisti", "Geleceğimizi petrol şirketlerinin kâr hırsına kurban ettiniz! Meclis kapısına kendimizi zincirledik, karar çıkmadan gitmeyiz!"),
                new DialogueEntry(63, DialogueCategory.Citizen, "Tekne Sahibi Balıkçı", "30 yıldır bu denizden ekmek yerim. Deniz ısındığından beri balık kalmadı, sadece zehirli balon balığı takılıyor."),
                new DialogueEntry(64, DialogueCategory.Citizen, "Gecekondu Sakini", "Zenginler klimalı plazalarda otururken biz teneke çatılarda 50 derecede pişiyoruz! Elektrik faturasını ödeyemiyoruz."),
                new DialogueEntry(65, DialogueCategory.Citizen, "İklim Mültecisi", "Toprağımız çöle döndüğü, suyumuz bittiği için yollara düştük. Sınır telleri arkasında hayvan gibi tutulmayı reddediyoruz!"),
                new DialogueEntry(66, DialogueCategory.Citizen, "Tüketici Temsilcisi", "Bir somun ekmek altın değerinde! Asgari ücretli ailesini doyuramıyor, marketler yağmalanmaya başladı, açlıkla pazarlık olmaz!"),
                new DialogueEntry(67, DialogueCategory.Citizen, "Madenci Sendika Başkanı", "Kömür madenini kapatırsanız 15 bin aile aç kalır. Bize yeşil istihdam masalı anlatmayın, yarın maden kapısında grevdeyiz!"),
                new DialogueEntry(68, DialogueCategory.Citizen, "Yerli Kabile Lideri", "Atalarımızın nehirlerini siyanürle kirlettiniz, ağaçlarımızı kestiniz. Bu toprakları canımız pahasına savunacağız."),
                new DialogueEntry(69, DialogueCategory.Citizen, "Şehirli Anne", "Çocuğumun astımı var, dışarıdaki hava o kadar zehirli ki pencereleri bantla kapattık. Çocuklarımız dışarıda oynayamayacak mı?"),
                new DialogueEntry(70, DialogueCategory.Citizen, "Sokak Esnafı", "Gündüz sıcaktan dükkan açamıyoruz, gece elektrikler kesik. Hükümet klimaları kapatın diyor; kasadaki açığı kim kapatacak?"),
                new DialogueEntry(71, DialogueCategory.Citizen, "Öğrenci Temsilcisi", "Diplomalarımızın anlamı kalmadı, yaşayabileceğimiz bir dünya yokken hangi kariyer için çalışacağız? Kampüs boykotu başlıyor!"),
                new DialogueEntry(72, DialogueCategory.Citizen, "Güneş Kooperatifi Kurucusu", "Kendi elektriğimizi üretmek istiyoruz ama dağıtım şirketleri önümüze yüzlerce engel çıkarıyor, bizi tekellere mahkum etmeyin!"),
                new DialogueEntry(73, DialogueCategory.Citizen, "Tahliye Edilen Yaşlı", "80 yıldır yaşadığım köy sel altında kaldı. Fotoğraflarım, anılarım, mezarlarımız gitti. Çadırda yaşamak istemiyorum!"),
                new DialogueEntry(74, DialogueCategory.Citizen, "Kamyon Şoförü", "Akaryakıt vergileri yüzünden depoyu dolduramıyorum. Kontak kapattık, nakliyat durdu; üç güne market rafları boş kalacak!"),
                new DialogueEntry(75, DialogueCategory.Citizen, "Doğa Gönüllüsü", "Kuraklıktan binlerce flamingo göllerde can verdi. Sular tarlalara çekildi, doğanın sessiz çığlığını duyun artık!"),
                new DialogueEntry(76, DialogueCategory.Citizen, "İnşaat İşçisi", "50 derece güneşin altında dış cephe yapmamızı istiyorlar. Dün arkadaşımız iskelede sıcaktan düşüp öldü! Çalışmıyoruz!"),
                new DialogueEntry(77, DialogueCategory.Citizen, "Sivil İtaatsizlik Sözcüsü", "Yarın sabah başkentin tüm ana arterlerini trafiğe kapatacağız. Ekonomiyi durdurmadan kulaklarınızı açmayacaksınız!"),
                new DialogueEntry(78, DialogueCategory.Citizen, "Ekmek Fırını İşletmecisi", "Un fabrikası un vermiyor, buğday gelmiyor. Yarından itibaren kişi başına 1 ekmek satacağız, kapıda polis nöbet tutmalı!"),
                new DialogueEntry(79, DialogueCategory.Citizen, "Şehir Plancısı", "Şehirleri beton çölüne çevirdik. Ağaç kalmadı, binalar rüzgarı kesti. Şehir merkezleri dev birer fırın gibi ısı yayıyor."),
                new DialogueEntry(80, DialogueCategory.Citizen, "Göçmen Karşıtı Gösterici", "Biz kendi halkımıza su bulamıyoruz, sınırlardan yüz binlerce iklim mültecisi akın ediyor. Sınırları kapatın!"),

                // 5. Grup: Şirket Lobileri & Enerji Baronları (81-100)
                new DialogueEntry(81, DialogueCategory.Lobbyist, "Fosil Yakıt Konsorsiyumu CEO'su", "Rafinerilerimize karbon vergisi koyarsanız yarın benzini iki katına çıkarırım. Hükümetinizin ömrü 24 saat sürmez."),
                new DialogueEntry(82, DialogueCategory.Lobbyist, "Tarım Karteli Başkanı", "Kuraklığa dayanıklı tohumlarımızın patentini istiyorsanız telif bedelini ödeyin; hissedarlarımıza kâr taahhüdümüz var."),
                new DialogueEntry(83, DialogueCategory.Lobbyist, "Elektrik Dağıtım Sahibi", "Yenilenebilir enerjiye geçmek altyapı ister. Devlet kasasından 10 milyar dolar hibe vermezseniz eski şebekeyi yenilemeyiz."),
                new DialogueEntry(84, DialogueCategory.Lobbyist, "Otomotiv Devi Temsilcisi", "Emisyon hedeflerini 2 yıl erteleyin, yoksa fabrikaları çevre kanunu olmayan Asya ülkelerine taşırız; 20 bin kişi işsiz kalır."),
                new DialogueEntry(85, DialogueCategory.Lobbyist, "Yeşil Aklama Danışmanı", "Fabrikayı kapatmanıza gerek yok. Afrika'ya birkaç bin fidan dikmiş gibi gösterip Karbon Nötr sertifikası basalım, halk alkışlar."),
                new DialogueEntry(86, DialogueCategory.Lobbyist, "Madencilik Holdingi Avukatı", "Maden ruhsatımızı iptal ederseniz tahkim mahkemesine gider, ülkenizi milyarlarca dolar tazminata mahkum ederiz."),
                new DialogueEntry(87, DialogueCategory.Lobbyist, "Hava Yolları Birliği Başkanı", "Uçak yakıtına ek vergi getirilirse turizm sektörü çöker, oteller batar. Havacılığı iklim yaptırımlarından muaf tutun."),
                new DialogueEntry(88, DialogueCategory.Lobbyist, "Karbon Borsası Spekülatörü", "Karbon kredisi fiyatlarını manipüle ederek bu çeyrekte %300 kâr ettik. Emisyonlar düşmüyor ama finansal piyasalar bayram ediyor!"),
                new DialogueEntry(89, DialogueCategory.Lobbyist, "Kimya Sanayicileri Derneği", "Plastikleri yasaklarsanız paketleme maliyeti fırlar, gıda enflasyonu patlar. Plastik ambalaj halk sağlığı için şarttır!"),
                new DialogueEntry(90, DialogueCategory.Lobbyist, "Nükleer Lobi Sözcüsü", "Rüzgar esmeyince, güneş batınca ne yapacaksınız? Tek çare nükleer. Bize 50 yıllık işletme garantisi verin, santralleri kuralım."),
                new DialogueEntry(91, DialogueCategory.Lobbyist, "Armatörler Lideri", "Gemilere pahalı yakıt kullanma zorunluluğu getirirseniz navlun fiyatlarını üçe katlarız; ithal ettiğiniz her ürün zamlanır."),
                new DialogueEntry(92, DialogueCategory.Lobbyist, "Özel Su Şirketi Yöneticisi", "Su artık stratejik bir emtiadır. Belediyelerin iflas eden şebekelerini bize devredin; suyu piyasa fiyatından satıp sızıntıyı önleyelim."),
                new DialogueEntry(93, DialogueCategory.Lobbyist, "Silah Sanayii Temsilcisi", "İklim göçleri ve sınır çatışmaları için sınır duvarları ve İHA'lar üretiyoruz. Bütçeyi çevreye değil, güvenliğe ayırın."),
                new DialogueEntry(94, DialogueCategory.Lobbyist, "Gayrimenkul Fonu Müdürü", "Kıyıdaki lüks projelerimizi sel riski diye durduramazsınız. Bankalar milyarlarca kredi verdi, batmalarına izin veremezsiniz."),
                new DialogueEntry(95, DialogueCategory.Lobbyist, "Çimento Üreticileri Birliği", "Çimento karbon salıyor olabilir ama betonsuz sel barajı veya sığınak yapamazsınız; bizi kapatırsanız afetle savaşamazsınız."),
                new DialogueEntry(96, DialogueCategory.Lobbyist, "Kripto Konsorsiyumu", "Sunucu çiftliklerimiz küçük bir ülke kadar elektrik tüketiyor ama devlete büyük vergi veriyoruz; keserseniz sunucuları taşırız."),
                new DialogueEntry(97, DialogueCategory.Lobbyist, "Süpermarket Zinciri CEO'su", "Son kullanma tarihi yaklaşan gıdayı çöpe atmak zorundayız, hijyen yasaları bunu emrediyor; yoksula dağıtmak suç!"),
                new DialogueEntry(98, DialogueCategory.Lobbyist, "Lobi Şirketi Stratejisti", "Seçim kampanyanızın en büyük bağışçısı petrol holdingleridir. Bakanlık koltuğunuzu onlara borçlusunuz, bunu unutmayın."),
                new DialogueEntry(99, DialogueCategory.Lobbyist, "Hızlı Moda Markası Sahibi", "Pamuk çok su tüketiyor olabilir ama her ay yeni koleksiyon çıkarmazsak tüketim durur, ekonominin çarkları döner mi sanıyorsunuz?"),
                new DialogueEntry(100, DialogueCategory.Lobbyist, "Gölge Finansör", "Dünya yanabilir Sayın Operatör, önemli olan yangından kimin kâr sağladığıdır. Kıtlık olacak sektörlere yatırım yapın, kazanın.")
            };
        }
    }
}
