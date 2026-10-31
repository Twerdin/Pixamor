using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pixamor.Editor
{
    public static class TrialRoomInteractionSetup
    {
        public const string ScenePath="Assets/Scenes/RoomSixMonitors_Trial.unity";
        [MenuItem("İklim Operatörü/Deneme/Masa Yerleşimini ve Etkileşimi Güncelle")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying)return;
            var previous=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var roots=scene.GetRootGameObjects();
                var room=roots.Single(r=>r.name=="ImportedRoom_OriginalScale");
                // Modelde Plane.013 yataktır; sağ duvar önündeki masa Plane.015'tir.
                var desk=room.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Plane.015");
                var bounds=desk.bounds;
                var rig=roots.Single(r=>r.name=="SixMonitors_3x2").transform;
                rig.SetPositionAndRotation(new Vector3(bounds.max.x-.35f,bounds.max.y+.675f,bounds.center.z),Quaternion.Euler(0,90,0));
                var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomTrial/Bezel.mat");
                var previews=new System.Collections.Generic.List<Renderer>();
                for(int row=0;row<2;row++)for(int col=0;col<3;col++)
                {
                    var tile=rig.Find("Monitor_"+(row*3+col+1));tile.localPosition=new Vector3((col-1)*.6f,(row-.5f)*.525f,0);
                    tile.Find("Housing").localScale=new Vector3(.598f,.523f,.055f);
                    var display=tile.Find("Display");display.localScale=new Vector3(.586f,.511f,1);previews.Add(display.GetComponent<Renderer>());
                }
                foreach(Transform t in rig)
                {
                    if(t.name=="Support"){t.localPosition=new Vector3(Mathf.Sign(t.localPosition.x)*.55f,-.33f,.09f);t.localScale=new Vector3(.04f,.66f,.04f);}
                    if(t.name=="Foot"){t.localPosition=new Vector3(Mathf.Sign(t.localPosition.x)*.55f,-.665f,.09f);t.localScale=new Vector3(.23f,.02f,.28f);}
                    if(t.name=="RearCrossbar")t.localScale=new Vector3(1.75f,.06f,.06f);
                }
                // İnce çerçeveler, tek etkileşim alanının önünde altı fiziksel ekranı belirtir.
                for(int i=0;i<2;i++)Beam(rig,"VerticalSeam"+i,new Vector3((i-.5f)*.6f,0,-.065f),new Vector3(.006f,1.05f,.006f),metal);
                Beam(rig,"HorizontalSeam",new Vector3(0,0,-.065f),new Vector3(1.8f,.006f,.006f),metal);
                var mount=rig.Find("InteractiveScreenMount");if(mount==null){mount=new GameObject("InteractiveScreenMount").transform;mount.SetParent(rig,false);}mount.localPosition=new Vector3(0,0,-.055f);
                var actor=roots.Single(r=>r.name=="TrialPlayer");
                actor.transform.SetPositionAndRotation(new Vector3(bounds.min.x-.65f,.04f,bounds.center.z),Quaternion.Euler(0,90,0));
                var walker=actor.GetComponent<RoomTrialWalker>();if(walker!=null)walker.enabled=false;
                var camera=actor.GetComponentInChildren<Camera>();camera.transform.localRotation=Quaternion.identity;
                var player=actor.GetComponent<FirstPersonOperator>();if(player==null)player=actor.AddComponent<FirstPersonOperator>();
                var managerObject=roots.FirstOrDefault(r=>r.name=="TrialGameManager")??new GameObject("TrialGameManager");
                var session=managerObject.GetComponent<GameSession>()??managerObject.AddComponent<GameSession>();
                var crises=managerObject.GetComponent<CrisisManager>()??managerObject.AddComponent<CrisisManager>();
                var monitor=managerObject.GetComponent<MonitorPrototype>()??managerObject.AddComponent<MonitorPrototype>();
                player.Configure(camera,mount,crises);player.ConfigureMonitorView(1.5f,1.8f);
                monitor.Configure(camera,mount,player);monitor.ConfigureSurface(new Vector2(1.8f,1.05f),previews.ToArray());
                var glow=roots.Single(r=>r.name=="DeskGlow");glow.transform.position=new Vector3(bounds.max.x-.8f,1.9f,bounds.center.z);
                EditorUtility.SetDirty(player);EditorUtility.SetDirty(monitor);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log("Masa üzerindeki 6 ekran oyun sistemine bağlandı.");
            }
            finally {SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void Beam(Transform root,string name,Vector3 position,Vector3 scale,Material material)
        {
            var t=root.Find(name);if(t==null){t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(root,false);}
            t.localPosition=position;t.localScale=scale;t.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
