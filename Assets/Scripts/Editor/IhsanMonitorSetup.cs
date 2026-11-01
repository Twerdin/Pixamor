using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pixamor.Editor
{
    public static class IhsanMonitorSetup
    {
        public const string ScenePath="Assets/Scenes/ihsanın.unity";
        // Eski monitörün yerine yeni istasyon gelir; oda ve mobilyalar salt okunur kalır.
        public static string ModelFingerprint(Scene scene)
        {
            var rows=new System.Collections.Generic.List<string>();
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root.name=="Computer_Workstation" || root.GetComponent<OperatorSceneIntegration>()!=null)continue;
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var t=renderer.transform;string path=t.name;for(var p=t.parent;p!=null;p=p.parent)path=p.name+"/"+path;
                    var mesh=renderer.GetComponent<MeshFilter>();
                    rows.Add(path+"|"+t.position.ToString("F6")+"|"+t.rotation.ToString("F6")+"|"+t.lossyScale.ToString("F6")+"|"+(mesh!=null?GlobalObjectId.GetGlobalObjectIdSlow(mesh.sharedMesh).ToString():"")+"|"+string.Join(",",renderer.sharedMaterials.Select(m=>GlobalObjectId.GetGlobalObjectIdSlow(m).ToString()))+"|"+renderer.enabled);
                }
            }
            return string.Join("\n",rows.OrderBy(x=>x));
        }
        [MenuItem("İklim Operatörü/İhsan Sahnesine Altı Monitörü Bağla")]
        public static void Setup()
        {
            if(EditorApplication.isPlaying)return;
            var previous=SceneManager.GetActiveScene();var target=SceneManager.GetSceneByPath(ScenePath);
            bool opened=!target.IsValid()||!target.isLoaded;
            if(opened)target=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            var source=SceneManager.GetSceneByPath(TrialRoomInteractionSetup.ScenePath);bool openedSource=!source.IsValid()||!source.isLoaded;
            try
            {
                if(target.GetRootGameObjects().Any(r=>r.GetComponent<OperatorSceneIntegration>()!=null)){Debug.Log("Altı monitör sistemi bu sahneye zaten bağlı.");return;}
                string before=ModelFingerprint(target);
                File.WriteAllText("Library/IhsanModelsBefore.txt",before);
                var roots=target.GetRootGameObjects();
                var room=roots.Single(r=>r.name=="Iklim_Yonetim_Odasi");
                var door=room.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Plane.002");
                var floor=room.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Plane");
                if(openedSource)source=EditorSceneManager.OpenScene(TrialRoomInteractionSetup.ScenePath,OpenSceneMode.Additive);
                var sourceRig=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="SixMonitors_3x2");
                SceneManager.SetActiveScene(target);
                var integration=new GameObject("OperatorSystem_SixMonitors");var marker=integration.AddComponent<OperatorSceneIntegration>();
                Undo.RegisterCreatedObjectUndo(integration,"Connect six monitor system");
                var rig=UnityEngine.Object.Instantiate(sourceRig.gameObject,integration.transform).transform;
                rig.name="SixMonitors_3x2";rig.SetPositionAndRotation(sourceRig.position,sourceRig.rotation);rig.localScale=sourceRig.lossyScale;
                var mount=rig.Find("InteractiveScreenMount");
                var managerObject=new GameObject("OperatorGameManager");managerObject.transform.SetParent(integration.transform,false);
                var session=managerObject.AddComponent<GameSession>();var crises=managerObject.AddComponent<CrisisManager>();var monitor=managerObject.AddComponent<MonitorPrototype>();
                var actor=new GameObject("OperatorPlayer");actor.transform.SetParent(integration.transform,false);
                // Kapının iç tarafı: kapı modelini açmadan veya oynatmadan güvenli başlangıç.
                var spawn=door.bounds.center+Vector3.right*.6f;spawn.y=floor.bounds.max.y+.04f;
                Vector3 direction=rig.position-spawn;direction.y=0;
                actor.transform.SetPositionAndRotation(spawn,Quaternion.LookRotation(direction));
                var body=actor.AddComponent<CharacterController>();body.radius=.18f;
                var eye=new GameObject("OperatorCamera");eye.tag="MainCamera";eye.transform.SetParent(actor.transform,false);
                var camera=eye.AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=70;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.04f);eye.AddComponent<AudioListener>();
                var player=actor.AddComponent<FirstPersonOperator>();player.Configure(camera,mount,crises);player.ConfigureMonitorView(1.5f,1.8f);player.ConfigureRoomScale(1.65f,1.48f,1.7f);
                var previews=rig.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Display").Cast<Renderer>().ToArray();
                if(previews.Length!=6)throw new InvalidOperationException("Kaynak istasyonda altı ekran bulunamadı.");
                monitor.Configure(camera,mount,player);monitor.ConfigureSurface(new Vector2(1.8f,1.05f),previews);
                marker.doorway=door.transform;marker.monitorRig=rig;marker.player=player;
                // Odanın eski oyun/arayüz sistemi saklanır; aynı anda iki kontrolcü çalışmaz.
                foreach(var root in roots)
                {
                    if(root.name=="SceneAutoBuilder"||root.name=="GameManager"||root.name=="OS_UIManager"||root.name=="Computer_Workstation"||root.name=="FirstPersonPlayer")
                    {Undo.RecordObject(root,"Disable previous monitor system");root.SetActive(false);}
                    foreach(var oldCamera in root.GetComponentsInChildren<Camera>(true)){Undo.RecordObject(oldCamera,"Disable previous camera");oldCamera.enabled=false;}
                    foreach(var listener in root.GetComponentsInChildren<AudioListener>(true)){Undo.RecordObject(listener,"Disable previous listener");listener.enabled=false;}
                }
                string after=ModelFingerprint(target);
                if(before!=after)throw new InvalidOperationException("Oda modellerinin bütünlük kontrolü başarısız.");
                File.WriteAllText("Library/IhsanIntegrationResult.txt","PASS: oda ve mobilyaların mesh, malzeme, konum, dönüş ve ölçüleri aynı. Altı ekran bağlandı. Başlangıç="+spawn.ToString("F3"));
                EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);AssetDatabase.SaveAssets();
                Selection.activeGameObject=integration;
                Debug.Log("İhsan sahnesine altı monitör bağlandı. Modeller korundu; oyuncu kapı önünde başlar.");
            }
            finally{if(openedSource && source.IsValid() && source.isLoaded)EditorSceneManager.CloseScene(source,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened && target.IsValid()&&target.isLoaded)EditorSceneManager.CloseScene(target,true);}
        }
    }
}


