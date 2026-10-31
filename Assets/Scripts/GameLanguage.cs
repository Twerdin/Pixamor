using System;
using System.Collections.Generic;
using System.Linq;
namespace Pixamor
{
    public enum UiLanguage { Turkish, English }
    public static class GameLanguage
    {
        private static UiLanguage current;
        public static event Action Changed;
        public static UiLanguage Current { get => current; set { if(current==value)return; current=value; Changed?.Invoke(); } }
        private static readonly KeyValuePair<string,string>[] phrases=Build();
        public static string Translate(string value)
        {
            if(Current==UiLanguage.Turkish || string.IsNullOrEmpty(value))return value;
            foreach(var phrase in phrases)value=value.Replace(phrase.Key,phrase.Value);
            return value;
        }
        private static KeyValuePair<string,string>[] Build()
        {
            var map=new Dictionary<string,string>();
            string ui=@"İKLİM OPERATÖRÜ|CLIMATE OPERATOR
BİR DÜNYA.|ONE WORLD.
ON ALTI KRİZ.|SIXTEEN CRISES.
TEK OPERATÖR.|ONE OPERATOR.
Halkı koru. Kaynaklarını yönet.|Protect people. Manage resources.
İklim dengesini ayakta tut.|Keep the climate in balance.
OPERASYON TERMİNALİ / VARDİYA BAŞLANGICI|OPERATION TERMINAL / SHIFT BRIEFING
KRİZ KOORDİNASYON MERKEZİ / OPERASYON 01|CRISIS COORDINATION CENTER / OPERATION 01
KÜRESEL KRİZ KOORDİNASYON MERKEZİ|GLOBAL CRISIS COORDINATION CENTER
VARDİYAYI BAŞLAT|START SHIFT
DÜNYA SENİ BEKLİYOR|THE WORLD IS WAITING
Her müdahalenin bir bedeli var.|Every response has a cost.
Karar senin.|The choice is yours.
Nasıl oynanır?|How to play
Ayarlar|Settings
Oyundan çık|Quit game
KOLAY|EASY
ZOR|HARD
sn müdahale|sec to respond
başlangıç bütçesi|starting budget
VARDİYA SÜRESİ|SHIFT TIME
AKTİF KRİZLER|ACTIVE CRISES
Bölgeler izleniyor.|Monitoring regions.
SON OPERASYON NOTU|LATEST OPERATION REPORT
BÜTÇE MALİYETİ|RESPONSE COST
BÜTÇE|BUDGET
HALK MUTLULUĞU|PUBLIC TRUST
İKLİM DENGESİ|CLIMATE BALANCE
HALK|PUBLIC
İKLİM|CLIMATE
CANLI|LIVE
DURDU|PAUSED
SONLANDI|ENDED
VARDİYA RAPORU HAZIR|SHIFT REPORT READY
VARDİYA DURAKLATILDI|SHIFT PAUSED
KRİTİK / HALK VE İKLİM|CRITICAL / PUBLIC AND CLIMATE
KRİTİK / HALK PROTESTOLARI|CRITICAL / PUBLIC PROTESTS
KRİTİK / İKLİM DENGESİ|CRITICAL / CLIMATE BALANCE
AKTİF KRİZ / MÜDAHALE BEKLİYOR|ACTIVE CRISES / RESPONSE REQUIRED
CANLI / TÜM BÖLGELER İZLENİYOR|LIVE / MONITORING ALL REGIONS
ESC  DURAKLAT  /  E  AYRIL|ESC  PAUSE  /  E  LEAVE
MÜDAHALE DOSYASI|RESPONSE FILE
KALAN SÜRE|TIME LEFT
SEÇENEK|OPTION
MÜDAHALEYİ UYGULA|APPLY RESPONSE
MÜDAHALE ETME|DO NOT INTERVENE
Müdahale etme|Do not intervene
Ücretsiz|Free
YETERSİZ BÜTÇE / KİLİTLİ|INSUFFICIENT BUDGET / LOCKED
ZAMAN AKIYOR / Süre dolmadan karar ver.|TIME IS RUNNING / Decide before the deadline.
DİĞER KRİZ|OTHER CRISIS
Haritaya dönerek öncelik seç.|Return to the map to prioritize.
Haritaya dön. Kararı daha sonra ver.|Return to the map. Decide later.
İlk kriz vardiyanın 5. saniyesinde gelecek.|The first crisis arrives 5 seconds into the shift.
bölgesinden yeni kriz bildirimi alındı. Müdahale bekleniyor.|reports a new crisis. A response is needed.
Süre doldu!|Time expired!
Müdahale edilmedi; kriz zarar verdi.|No response; the crisis caused damage.
Vardiya tamamlandı. Halk ve iklim korundu.|Shift complete. People and climate were protected.
Kitlesel isyan: halk mutluluğu sıfıra ulaştı.|Mass unrest: public trust reached zero.
Krizler kontrolden çıktı: iklim dengesi sıfıra ulaştı.|Crises are out of control: climate balance reached zero.
OPERASYON TERMİNALİ / DURAKLATILDI|OPERATION TERMINAL / PAUSED
Vardiya duraklatıldı.|Shift paused.
Sayaçlar, kriz süreleri ve bütçe ödemeleri bekliyor.|Shift time, crisis deadlines and budget payments are paused.
DEVAM ET|RESUME
Ana menüye dön|Return to main menu
KARAR DOSYASI KORUNDU|RESPONSE FILE PRESERVED
KALAN VARDİYA SÜRESİ|SHIFT TIME REMAINING
OPERATÖR KILAVUZU / KONTROLLER|OPERATOR GUIDE / CONTROLS
Dünyayı masandan koru.|Protect the world from your desk.
01 / MONİTÖRE YAKLAŞ|01 / APPROACH THE MONITOR
02 / MÜDAHALE SEÇ|02 / CHOOSE A RESPONSE
03 / DENGEYİ KORU|03 / MAINTAIN BALANCE
04 / VARDİYAYI TAMAMLA|04 / COMPLETE THE SHIFT
WASD ile yürü, fareyle bak. Monitöre yaklaş ve E ile ekranı kullan.|Move with WASD and look with the mouse. Approach the monitor and press E to use it.
Kriz işaretine tıkla. Maliyet ve etkileri karşılaştır. Kart açıkken zaman akar; diğer krizler de beklemez.|Click a crisis marker. Compare costs and effects. Time keeps running while the card is open.
Halk veya iklim sıfıra ulaşırsa kaybedersin. Para sıfırken ücretsiz seçenekleri kullanabilirsin.|You lose if public trust or climate reaches zero. Free options remain available at zero budget.
Her 60 saniyede bütçe gelir. Seçtiğin zorluğun süresi dolmadan cevap ver. Esc menüyü açar, E monitörden ayırır.|Budget arrives every 60 seconds. Respond before your difficulty deadline. Esc opens the menu; E leaves the monitor.
Geri|Back
OPERATÖR TERCİHLERİ / AYARLAR|OPERATOR PREFERENCES / SETTINGS
Kontrolü kendine göre ayarla.|Adjust your controls.
ANA SES|MASTER VOLUME
FARE HASSASİYETİ|MOUSE SENSITIVITY
EKRAN HAREKETLERİ|SCREEN ANIMATIONS
AÇIK|ON
KAPALI|OFF
KAYDET VE GERİ DÖN|SAVE AND RETURN
VARDİYA SONU / BAŞARILI OPERASYON|SHIFT END / SUCCESSFUL OPERATION
VARDİYA SONU / OPERASYON KAYBEDİLDİ|SHIFT END / OPERATION LOST
Dengeyi korudun.|You kept the balance.
Halkın güveni tükendi.|Public trust collapsed.
İklim kontrolden çıktı.|Climate is out of control.
MÜDAHALE|RESPONDED
KAÇIRILAN|MISSED
KALAN BÜTÇE|BUDGET REMAINING
GÖREV SÜRESİ|TIME ON DUTY
YENİ VARDİYA|NEW SHIFT
Ana menü|Main menu
Çıkış|Quit
VARDİYA SONU / OPERASYON RAPORU|SHIFT END / OPERATION REPORT
Görev tamamlandı.|Mission complete.
Kontrol kaybedildi.|Control lost.
Son göstergeler|Final indicators
YENİ VARDİYA BAŞLAT|START NEW SHIFT
VARDİYA|SHIFT
KRİZ|CRISES
TEK ODA|ONE ROOM
SEL|FLOOD
KURAKLIK|DROUGHT
ORMAN YANGINI|WILDFIRE
AŞIRI SICAKLIK|HEATWAVE
YENİDEN DENE|TRY AGAIN
OPERASYON BAŞARISIZ|OPERATION FAILED
Kaydet ve geri dön|Save and return
Monitöre bak ve E'ye bas|Look at the monitor and press E
WASD: yürü|WASD: move
Fare: bak|Mouse: look
Shift: hızlan|Shift: run
Monitöre yaklaş|Approach the monitor
Esc: duraklat|Esc: pause
 sn| sec";
            foreach(var line in ui.Split('\n')){var pair=line.TrimEnd('\r').Split('|');if(pair.Length==2)map[pair[0]]=pair[1];}
            // Aynı olay verisinin başlık, açıklama, seçenek ve sonuçlarını birlikte çeviririz.
            string events=@"Extreme heat|Mediterranean|A heatwave is making life in cities difficult.|Open cooling centers|Distribute water and restrict consumption|Cooling areas protected people; energy use increased.|Water was distributed; restrictions caused public anger.
River overflow|South Asia|A rising river threatens settlements and wetlands.|Evacuate and provide temporary shelters|Divert water into wetlands|Families moved to safe shelters.|Water was diverted; some neighborhoods had to evacuate.
Agricultural water crisis|East Africa|Prolonged drought is exhausting farmers' water supplies.|Send emergency water tankers|Support drip irrigation|Water arrived; transport emissions increased.|Efficient irrigation was installed; urgent needs remained unmet.
Coastal forest fire|Western North America|Strong winds are pushing flames toward settlements.|Deploy firefighting aircraft|Evacuate and build firebreaks|The fire's advance toward settlements was stopped.|People evacuated; the fire continued through the forest.
Hot night alert|Western Europe|Night temperatures remain high and the power grid is strained.|Generate emergency electricity|Open cool public spaces and ration power|Cooling continued; emissions increased.|Energy use was limited; cooling spaces lacked capacity.
Coastal flooding|Southeast Asia|Heavy rain is flooding coastal neighborhoods.|Pumps and emergency housing|Evacuate and protect mangroves|Water levels in neighborhoods were reduced.|Coastal habitat was protected; shelter shortages strained evacuees.
Reservoirs running dry|South America|City water reserves have reached critical levels.|Extract emergency groundwater|Repair leaks and ration water|Water cuts were avoided; groundwater reserves fell.|Water losses declined; interruptions disrupted daily life.
Peat fire|Indonesia|A peatland fire is producing dense smoke.|Flood the peat to extinguish the fire|Distribute masks and evacuate|Peat was rewetted and smoke declined.|People escaped the smoke; peat kept burning and emitting carbon.
Urban heat island|East Asia|Dense development increases heat and fills hospitals.|Mobile healthcare and cooling|Provide shade and restrict traffic|Medical teams reached high-risk neighborhoods.|Traffic declined; urgent healthcare and cooling needs remained unmet.
Flash flood in the city|Central Europe|Intense rain has exceeded the city's drainage capacity.|Emergency evacuation and infrastructure repair|Divert water into temporary storage|Evacuation was completed and critical infrastructure repaired.|Water was diverted; nearby neighborhoods evacuated.
Prolonged dry season|Australia|Cities and wildlife need water as the dry season continues.|Build emergency water treatment plants|Reuse water and reserve a share for nature|Water supply increased; plants consumed intensive energy.|Wildlife was supported; urgent urban water shortages continued.
Flames at the forest edge|Southern Europe|Fire threatens rural settlements and protected woodland.|Send ground and air crews together|Evacuate settlements|Both woodland and settlements were protected.|Settlements evacuated; protected woodland kept burning.
Drinking water alert|North Africa|Two cities request the same emergency water source as reserves fall.|Send tankers to homes and hospitals|Water quotas and reuse points|Priority areas received water; transport emissions increased.|Water use fell; restrictions caused public anger.
Night evacuation call|Central America|Floodwaters are rising and darkness complicates evacuation.|Light an emergency evacuation corridor|Gather people on higher ground|People reached safety; energy and fuel were consumed.|People moved uphill; shelter conditions remained poor.
Smoke corridor|Canada|Wind is pushing wildfire smoke toward a city with hospitals.|Fire suppression and clean-air centers|Evacuate high-risk neighborhoods|The fire was limited and vulnerable people reached clean air.|People escaped the smoke; environmental damage continued.
Power grid at critical load|Middle East|Cooling demand has pushed the grid to its limit; outages loom.|Provide backup power to hospitals|Limit consumption and open cool spaces|Critical healthcare was protected; generator emissions increased.|Grid load fell; some people opposed the restrictions.";
            var catalog=CrisisCatalog.Create();var rows=events.Split('\n');
            for(int i=0;i<catalog.Length;i++)
            {
                var en=rows[i].TrimEnd('\r').Split('|');var data=catalog[i];
                map[data.Title]=en[0];map[data.Region]=en[1];map[data.Description]=en[2];
                map[data.Options[0].Title]=en[3];map[data.Options[1].Title]=en[4];
                map[data.Options[0].Result]=en[5];map[data.Options[1].Result]=en[6];
            }
            return map.OrderByDescending(p=>p.Key.Length).ToArray();
        }
    }
}
