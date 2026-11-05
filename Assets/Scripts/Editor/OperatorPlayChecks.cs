using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pixamor.Editor
{
    // Görsel akışı gerçek Unity bileşenleriyle kontrol eder. Oyuncunun sürümüne dahil olmaz.
    public static class OperatorPlayChecks
    {
        private static Action pending;

        [MenuItem("İklim Operatörü/Arayüz Testi/Akışı Kontrol Et (Play)")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying) return;
            if(pending!=null) EditorApplication.update-=Update;
            var flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            var game=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonOperator>();
            if(flow==null || game==null || player==null) return;
            CrisisManager manager=null;
            foreach(var candidate in game.GetComponents<CrisisManager>()) if(candidate.enabled) manager=candidate;
            Require(manager!=null,"Etkin kriz yöneticisi bulunamadı.");
            flow.StartShift();
            player.EnterMonitor();
            flow.Pause();
            float remaining=game.RemainingTime;
            double started=EditorApplication.timeSinceStartup;
            int step=0;
            pending=()=>
            {
                if(step==0 && EditorApplication.timeSinceStartup-started>=2)
                {
                    Require(game.RemainingTime==remaining && game.Money==60,"Duraklatma sırasında zaman veya bütçe değişti.");
                    flow.Resume();
                    Require(!game.IsPaused && !player.UIBlocked,"Devam Et kontrolleri açmadı.");
                    started=EditorApplication.timeSinceStartup;
                    step=1;
                }
                else if(step==1 && EditorApplication.timeSinceStartup-started>=5.5)
                {
                    var pause=GameObject.Find("PauseShortcut").GetComponent<Button>();
                    var canvas=pause.GetComponentInParent<Canvas>();
                    var rect=pause.GetComponent<RectTransform>();
                    Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center));
                    var hits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    Require(hits.Exists(hit=>hit.gameObject==pause.gameObject),"Monitörün duraklatma düğmesine UI ışını ulaşmıyor.");
                    Require(manager.Active.Count>0,"İlk kriz çıkmadı.");
                    manager.Open(manager.Active[0]);
                    flow.Pause();
                    flow.Resume();
                    Require(!game.IsPaused && manager.OpenCard!=null,"Menüden dönüş açık kartla saati devam ettirmedi.");
                    manager.CloseCard();
                    Require(!game.IsPaused,"Kart kapanınca oyun devam etmedi.");
                    game.ApplyResourceChanges(-100,0);
                    Require(flow.Screen==GameScreen.Results && game.State==SessionState.Lost,"Kayıp ekranı açılmadı.");
                    flow.StartShift();
                    game.ApplyResourceChanges(0,-100);
                    Require(flow.Screen==GameScreen.Results && game.Climate==0,"İklim çöküşü ekranı açılmadı.");
                    flow.MainMenu();
                    Require(game.IsMenuPaused && game.ElapsedTime==0 && manager.Active.Count==0,"Ana menüye dönüş sıfırlanmadı.");
                    Debug.Log("PLAY CHECKS PASS: menü, zaman, bütçe, fiziksel ekran ışını, kart açıkken zaman akışı ve iki kayıp ekranı.");
                    pending=null;
                }
            };
            EditorApplication.update+=Update;
        }

        private static void Update()
        {
            if(!EditorApplication.isPlaying || pending==null) { pending=null;EditorApplication.update-=Update;return; }
            try { pending(); }
            catch(Exception error)
            {
                pending=null;
                EditorApplication.update-=Update;
                Debug.LogException(error);
            }
        }

        private static void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
        }

        [MenuItem("İklim Operatörü/Arayüz Testi/Kazanma Ekranını Önizle (Play)")]
        public static void PreviewVictory()
        {
            if(!EditorApplication.isPlaying) return;
            var flow=UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            var game=UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if(flow==null || game==null) return;
            flow.StartShift();
            // Yalnızca editörde, altı dakika beklemeden görsel yerleşimi incelemek içindir.
            typeof(GameSession).GetMethod("Finish",BindingFlags.NonPublic|BindingFlags.Instance)
                .Invoke(game,new object[]{SessionState.Won,"Görsel önizleme: vardiya başarıyla tamamlandı."});
        }
    }
}
