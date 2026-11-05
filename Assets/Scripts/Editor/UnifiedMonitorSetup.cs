using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pixamor.Editor
{
 [InitializeOnLoad]public static class UnifiedMonitorSetup
 {
  static UnifiedMonitorSetup(){EditorApplication.delayCall+=ApplyPending;EditorApplication.playModeStateChanged+=OnPlayState;}
  static void OnPlayState(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=ApplyPending;}
  static void ApplyPending(){if(!File.Exists("Library/UnifiedMonitor.request")||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Library/UnifiedMonitor.request");Apply();}
  static void Frame(Transform parent,string name,Vector3 position,Vector3 size,Material material)
  {
   var frame=parent.Find(name);if(frame==null){frame=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;frame.name=name;frame.SetParent(parent,false);}
   frame.localPosition=position;frame.localScale=size;frame.GetComponent<Renderer>().sharedMaterial=material;
  }
  [MenuItem("İklim Operatörü/İhsan Monitörünü Tek Ekran Yap")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)return;
   var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(IhsanMonitorSetup.ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(IhsanMonitorSetup.ScenePath,OpenSceneMode.Additive);
   try{
    SceneManager.SetActiveScene(scene);var marker=scene.GetRootGameObjects().Select(r=>r.GetComponent<OperatorSceneIntegration>()).First(m=>m!=null);var rig=marker.monitorRig;
    string before=IhsanMonitorSetup.ModelFingerprint(scene);
    // Oda modellerine dokunmadan yalnızca istasyonun altı parçasını değiştir.
    foreach(Transform child in rig)if(child.name.StartsWith("Monitor_")||child.name.StartsWith("VerticalSeam")||child.name=="HorizontalSeam")child.gameObject.SetActive(false);
    var housing=rig.Find("SingleMonitorHousing");if(housing==null){housing=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;housing.name="SingleMonitorHousing";housing.SetParent(rig,false);}housing.localPosition=Vector3.zero;housing.localScale=new Vector3(1.85f,1.10f,.065f);housing.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomTrial/Bezel.mat");
    var panel=rig.Find("ContinuousDisplay");if(panel==null){panel=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;panel.name="ContinuousDisplay";panel.SetParent(rig,false);Object.DestroyImmediate(panel.GetComponent<Collider>());}panel.localPosition=new Vector3(0,0,-.0375f);panel.localScale=new Vector3(1.8f,1.05f,1);
    const string path="Assets/Materials/RoomTrial/UnifiedMonitorScreen.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.name="UnifiedMonitorScreen";material.mainTexture=Resources.Load<Texture2D>("CommandUI/Art/WorldMap");material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,path);}panel.GetComponent<Renderer>().sharedMaterial=material;
    // Ön çerçeve ekranın dışına taşar; ortada ayırıcı veya ikinci bir yüzey yoktur.
    var bezel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomTrial/Bezel.mat");
    Frame(rig,"BezelTop",new Vector3(0,.5375f,-.0355f),new Vector3(1.85f,.025f,.012f),bezel);
    Frame(rig,"BezelBottom",new Vector3(0,-.5375f,-.0355f),new Vector3(1.85f,.025f,.012f),bezel);
    Frame(rig,"BezelLeft",new Vector3(-.9125f,0,-.0355f),new Vector3(.025f,1.05f,.012f),bezel);
    Frame(rig,"BezelRight",new Vector3(.9125f,0,-.0355f),new Vector3(.025f,1.05f,.012f),bezel);
    var mount=rig.Find("InteractiveScreenMount");mount.localPosition=panel.localPosition;
    var monitor=marker.GetComponentInChildren<MonitorPrototype>();monitor.ConfigureSurface(new Vector2(1.8f,1.05f),new[]{panel.GetComponent<Renderer>()});EditorUtility.SetDirty(monitor);
    if(before!=IhsanMonitorSetup.ModelFingerprint(scene))throw new System.Exception("Oda modeli değişti");
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Library/UnifiedMonitorResult.txt","PASS: tek kesintisiz ekran, dört dış çerçeve, UI ve ekran yüzeyi aynı konumda, oda modelleri aynı.");
   }finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
 }
}

