using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pixamor.Editor
{
    public static class TrialRoomInteractionSetup
    {
        public const string ModelPath="Assets/Prefabs/iklim_yönetim_odası (3).fbx";
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
                // Yeni FBX, yalnızca deneme sahnesindeki oda örneğinin yerini alır.
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(room)!=ModelPath)
                {
                    var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),scene);
                    replacement.name=room.name;
                    PrepareImportedRoom(replacement);
                    // Kullanıcının sahneye sürüklediği aynı FBX kopyası üst üste görünmesin.
                    foreach(var duplicate in roots.Where(r=>r!=room && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r)==ModelPath))
                        Undo.DestroyObjectImmediate(duplicate);
                    Undo.DestroyObjectImmediate(room);
                    room=replacement;
                    roots=scene.GetRootGameObjects();
                }
                // Modelde Plane.013 yataktır; sağ duvar önündeki masa Plane.015'tir.
                var desk=room.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Plane.015");
                var bounds=desk.bounds;
                var rig=roots.Single(r=>r.name=="SixMonitors_3x2").transform;
                Physics.SyncTransforms();
                float tableTop=bounds.max.y;
                var tableCollider=desk.GetComponent<Collider>();
                // En yüksek kenarlık yerine, ayağın altındaki gerçek tabla yüzeyini ölç.
                if(tableCollider.Raycast(new Ray(new Vector3(bounds.max.x-.26f,bounds.max.y+1,bounds.center.z),Vector3.down),out var hit,2))tableTop=hit.point.y;
                // Kullanıcının belirlediği istasyon konumu ve dönüşü.
                rig.SetPositionAndRotation(new Vector3(-.011f,1.475f,.721f),Quaternion.identity);
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
                var spawn=rig.position-rig.forward*1.8f; spawn.y=.04f;
                actor.transform.SetPositionAndRotation(spawn,rig.rotation);
                var walker=actor.GetComponent<RoomTrialWalker>();if(walker!=null)walker.enabled=false;
                var camera=actor.GetComponentInChildren<Camera>();camera.transform.localRotation=Quaternion.identity;
                var player=actor.GetComponent<FirstPersonOperator>();if(player==null)player=actor.AddComponent<FirstPersonOperator>();
                var managerObject=roots.FirstOrDefault(r=>r.name=="TrialGameManager")??new GameObject("TrialGameManager");
                var session=managerObject.GetComponent<GameSession>()??managerObject.AddComponent<GameSession>();
                var crises=managerObject.GetComponent<CrisisManager>()??managerObject.AddComponent<CrisisManager>();
                var monitor=managerObject.GetComponent<MonitorPrototype>()??managerObject.AddComponent<MonitorPrototype>();
                player.Configure(camera,mount,crises);player.ConfigureMonitorView(1.5f,1.8f);
                player.ConfigureRoomScale(1.65f,1.48f,1.7f);
                monitor.Configure(camera,mount,player);monitor.ConfigureSurface(new Vector2(1.8f,1.05f),previews.ToArray());
                var glow=roots.Single(r=>r.name=="DeskGlow");glow.transform.position=rig.position-rig.forward*.45f+Vector3.up*.35f;
                EditorUtility.SetDirty(player);EditorUtility.SetDirty(monitor);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log("Masa üzerindeki 6 ekran oyun sistemine bağlandı.");
            }
            finally {SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        public static void PrepareImportedRoom(GameObject room)
        {
            var convertedMaterials=new System.Collections.Generic.Dictionary<Material,Material>();
            foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
            {
                // Tüm yuvaları koru: yatağın kumaşı ile ahşabı ayrı malzemeler olabilir.
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ConvertMaterial(m,convertedMaterials)).ToArray();
                if(renderer.GetComponent<MeshCollider>()==null)
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        static Material ConvertMaterial(Material source,System.Collections.Generic.Dictionary<Material,Material> cache)
        {
            if(source==null)return null;
            if(cache.TryGetValue(source,out var found))return found;
            const string folder="Assets/Materials/RoomTrial/Model3";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Materials/RoomTrial","Model3");
            string path=folder+"/"+source.name.Replace("/","_")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name};
                if(source.HasProperty("_Color"))material.SetColor("_BaseColor",source.GetColor("_Color"));
                if(source.HasProperty("_BaseColor"))material.SetColor("_BaseColor",source.GetColor("_BaseColor"));
                material.SetTexture("_BaseMap",source.mainTexture);
                material.SetTextureScale("_BaseMap",source.mainTextureScale);
                material.SetTextureOffset("_BaseMap",source.mainTextureOffset);
                if(source.HasProperty("_Metallic"))material.SetFloat("_Metallic",source.GetFloat("_Metallic"));
                if(source.HasProperty("_Glossiness"))material.SetFloat("_Smoothness",source.GetFloat("_Glossiness"));
                if(source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap")!=null){material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));material.EnableKeyword("_NORMALMAP");}
                AssetDatabase.CreateAsset(material,path);
            }
            cache[source]=material;return material;
        }
        static void Beam(Transform root,string name,Vector3 position,Vector3 scale,Material material)
        {
            var t=root.Find(name);if(t==null){t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(root,false);}
            t.localPosition=position;t.localScale=scale;t.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
