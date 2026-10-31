using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pixamor.Editor
{
    // Geometri yalnızca editörde oluşturulur ve ayrı sahneye kalıcı olarak kaydedilir.

    public static class SixMonitorRoomTrial
    {
        const string ScenePath="Assets/Scenes/RoomSixMonitors_Trial.unity";
        const string Folder="Assets/Materials/RoomTrial";
        [MenuItem("İklim Operatörü/Deneme/6 Monitörlü Oda Sahnesi Oluştur")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)return;
            // Önceden düzenlenmiş deneme sahnesinin üzerine yazılmaz.
            if(File.Exists(ScenePath)) { Debug.Log("Deneme sahnesi zaten var: "+ScenePath);return; }
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Materials","RoomTrial");
                var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/iklim_yönetim_odası.fbx"),scene);
                room.name="ImportedRoom_OriginalScale";
                var wall=Material("Walls",new Color(.34f,.39f,.43f));var furniture=Material("Furniture",new Color(.14f,.19f,.23f));var wood=Material("Desk",new Color(.32f,.22f,.14f));var floor=Material("Floor",new Color(.19f,.22f,.24f));
                foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial=r.name=="Plane.013"?wood:r.name=="Plane" || r.name=="Plane.014"?floor:r.name.StartsWith("Cube") || r.name=="Plane.015"?furniture:wall;
                    var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
                }
                var metal=Material("Bezel",new Color(.018f,.027f,.036f));
                var assembly=new GameObject("SixMonitors_3x2");assembly.transform.position=new Vector3(-1.75f,1.71f,-1.73f);assembly.transform.rotation=Quaternion.Euler(0,180,0);
                // 2.16 m genişlik, 0.84 m yükseklik: 2.30 m masaya sığar. Alt kenar 1.29 m.
                for(int row=0;row<2;row++)for(int col=0;col<3;col++)
                {
                    var tile=new GameObject("Monitor_"+(row*3+col+1));tile.transform.SetParent(assembly.transform,false);tile.transform.localPosition=new Vector3((col-1)*.72f,(row-.5f)*.42f,0);
                    Cube(tile.transform,"Housing",Vector3.zero,new Vector3(.718f,.418f,.055f),metal);
                    var panel=GameObject.CreatePrimitive(PrimitiveType.Quad);panel.name="Display";panel.transform.SetParent(tile.transform,false);panel.transform.localPosition=new Vector3(0,0,-.04f);panel.transform.localScale=new Vector3(.704f,.404f,1);Object.DestroyImmediate(panel.GetComponent<Collider>());
                    var screen=Material("Screen_"+row+"_"+col,Color.white,true);screen.mainTexture=Resources.Load<Texture2D>("CommandUI/Art/WorldMap");
                    // Her ekran aynı haritanın farklı bölümünü gösterir; birleşince tek görüntü olur.
                    screen.SetFloat("_Cull",0); screen.mainTextureScale=new Vector2(1f/3f,1f/2f);screen.mainTextureOffset=new Vector2(col/3f,row/2f);EditorUtility.SetDirty(screen);panel.GetComponent<Renderer>().sharedMaterial=screen;
                }
                Cube(assembly.transform,"RearCrossbar",new Vector3(0,0,.07f),new Vector3(2.1f,.06f,.06f),metal);
                foreach(float x in new[]{-.7f,.7f}) { Cube(assembly.transform,"Support",new Vector3(x,-.50f,.09f),new Vector3(.04f,1f,.04f),metal);Cube(assembly.transform,"Foot",new Vector3(x,-1f,.09f),new Vector3(.23f,.02f,.28f),metal); }
                var player=new GameObject("TrialPlayer");player.transform.position=new Vector3(-1.65f,.04f,.05f);player.transform.rotation=Quaternion.Euler(0,180,0);
                var cc=player.AddComponent<CharacterController>();cc.height=1.75f;cc.radius=.18f;cc.center=new Vector3(0,.875f,0);cc.stepOffset=.2f;
                var eye=new GameObject("TrialCamera",typeof(Camera),typeof(AudioListener));eye.tag="MainCamera";eye.transform.SetParent(player.transform,false);eye.transform.localPosition=new Vector3(0,1.6f,0);var camera=eye.GetComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=70;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.04f);
                player.AddComponent<RoomTrialWalker>().viewCamera=camera;
                Light("Ceiling",new Vector3(-.7f,2.25f,-.3f),new Color(.85f,.92f,1),3.5f,7);
                Light("DeskGlow",new Vector3(-1.75f,1.9f,-1.3f),new Color(.25f,.8f,1),.7f,2);
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.34f,.4f);
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
                // Aynı kamera ile yerleşim görselini üret; MainGame ve açık düzenlemeleri korunur.
                camera.scene=scene;var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.aspect=1.6f;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes("Library/RoomTrialPreview.png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
                Debug.Log("6 monitörlü deneme sahnesi kaydedildi: "+ScenePath);
            }
            finally { SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true); }
            TrialRoomInteractionSetup.Upgrade();
        }
        static Material Material(string name,Color color,bool unlit=false) { var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(m,path);return m; }
        static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material) { var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material; }
        static void Light(string name,Vector3 position,Color color,float intensity,float range) { var go=new GameObject(name);go.transform.position=position;var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.Soft; }
    }
}
