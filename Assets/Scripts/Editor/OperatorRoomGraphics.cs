using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pixamor.Editor
{
    public static class OperatorRoomGraphics
    {
        [MenuItem("İklim Operatörü/Grafik/Odayı İyileştir")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlaying) { Debug.LogWarning("Önce Play modundan çık."); return; }
            AssetDatabase.ImportAsset("Assets/Resources/CommandUI/Art/WorldMap.png",ImportAssetOptions.ForceUpdate);
            var room=GameObject.Find("OperatorRoom");
            var session=Object.FindAnyObjectByType<GameSession>();
            if(room==null || session==null) { Debug.LogWarning("Önce Odayı Kur menüsünü kullan."); return; }
            var previous=room.transform.Find("VisualDetails");
            if(previous!=null) { Selection.activeGameObject=previous.gameObject; return; }
            Undo.IncrementCurrentGroup();
            int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Improve operator room graphics");
            var root=new GameObject("VisualDetails");
            root.transform.SetParent(room.transform,false);
            Undo.RegisterCreatedObjectUndo(root,"Add visual details");
            if(!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets","Materials");
            var metal=Material("GraphicsMetal",new Color(.045f,.065f,.08f),.65f,.45f);
            var panels=Material("GraphicsPanels",new Color(.09f,.13f,.16f),.15f,.2f);
            var keys=Material("GraphicsKeys",new Color(.15f,.20f,.23f),.1f,.25f);
            var cyan=Material("GraphicsCyan",new Color(.1f,.4f,.38f),.25f,.5f,CommandUI.Teal*2.5f);
            var warm=Material("GraphicsWarm",new Color(.6f,.48f,.3f),.1f,.4f,new Color(1f,.74f,.42f)*2f);
            var strips=new List<Renderer>();

            // Mevcut monitörün ölçüsü ve ekran bağlantısı korunur.
            foreach(float x in new[]{-2.81f,2.81f})
                strips.Add(Box(root.transform,"MonitorEdge",new Vector3(x,3f,1.53f),new Vector3(.025f,3.62f,.025f),cyan));
            strips.Add(Box(root.transform,"DeskLight",new Vector3(0,.88f,-.6f),new Vector3(6.7f,.025f,.025f),cyan));
            foreach(float x in new[]{-4f,-2f,0f,2f,4f})
            {
                Box(root.transform,"AcousticWallPanel",new Vector3(x,2.5f,4.74f),new Vector3(1.8f,3.6f,.08f),panels);
                Box(root.transform,"WallPanelRail",new Vector3(x,4.4f,4.68f),new Vector3(1.8f,.035f,.035f),metal);
            }
            foreach(float x in new[]{-5.83f,5.83f})
                Box(root.transform,"WallLightStrip",new Vector3(x,3.8f,-.5f),new Vector3(.05f,.05f,9f),warm);
            foreach(float x in new[]{-4f,0f,4f})
                Box(root.transform,"CeilingBeam",new Vector3(x,4.9f,-1),new Vector3(.12f,.15f,11.7f),metal);

            // Küçük detaylar editör nesneleridir; oyuncu bunları elle düzenleyebilir.
            for(int row=0;row<4;row++)
            for(int col=0;col<13;col++)
                Box(root.transform,"KeyboardKey",new Vector3(-.84f+col*.085f,1.177f,-.29f+row*.075f),
                    new Vector3(.067f,.018f,.055f),keys);
            for(int i=0;i<8;i++)
                Box(root.transform,"PCVent",new Vector3(2.5f,.24f+i*.08f,.535f),new Vector3(.35f,.025f,.025f),keys);
            Box(root.transform,"PCPowerLight",new Vector3(2.66f,.91f,.53f),new Vector3(.025f,.025f,.025f),cyan);
            Box(root.transform,"ServerCabinet",new Vector3(4.7f,1.25f,3.75f),new Vector3(.8f,2.5f,.8f),metal);
            for(int i=0;i<9;i++)
            {
                Box(root.transform,"ServerTray",new Vector3(4.7f,.22f+i*.25f,3.335f),new Vector3(.69f,.13f,.025f),keys);
                Box(root.transform,"ServerIndicator",new Vector3(4.96f,.22f+i*.25f,3.31f),new Vector3(.025f,.025f,.025f),cyan);
            }
            var key=Light(root.transform,"DeskKeyLight",new Vector3(0,4.7f,-2.2f),new Color(1f,.83f,.66f),20,9);
            key.type=LightType.Spot;
            key.spotAngle=75;
            key.transform.rotation=Quaternion.Euler(42,0,0);
            key.shadows=LightShadows.Soft;
            Light(root.transform,"CoolRoomFill",new Vector3(-4,3.8f,.5f),new Color(.4f,.65f,1f),6,8);
            var indicator=Light(root.transform,"MonitorGlow",new Vector3(0,2.7f,.6f),CommandUI.Teal,3,4);
            root.AddComponent<RoomAtmosphere>().Configure(session,strips.ToArray(),indicator);
            foreach(var light in Object.FindObjectsByType<Light>())
            {
                if(light.type!=LightType.Directional) continue;
                Undo.RecordObject(light,"Balance room lighting");
                light.intensity=.4f;
            }
            AddPostProcessing(root.transform,room);
            EditorSceneManager.MarkSceneDirty(room.scene);
            EditorSceneManager.SaveScene(room.scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject=root;
            Debug.Log("Grafik detayları, ışıklar ve monitör parlaması sahneye kaydedildi.");
        }

        private static Material Material(string name,Color color,float metallic,float smoothness,Color? emission=null)
        {
            string path="Assets/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color=color;
            material.SetFloat("_Metallic",metallic);
            material.SetFloat("_Smoothness",smoothness);
            if(emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",emission.Value);
            }
            AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static Renderer Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name=name;
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;
            obj.transform.localScale=scale;
            var renderer=obj.GetComponent<Renderer>();
            renderer.sharedMaterial=material;
            return renderer;
        }

        private static Light Light(Transform parent,string name,Vector3 position,Color color,float intensity,float range)
        {
            var obj=new GameObject(name);
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;
            var light=obj.AddComponent<Light>();
            light.type=LightType.Point;
            light.color=color;
            light.intensity=intensity;
            light.range=range;
            return light;
        }

        private static void AddPostProcessing(Transform parent,GameObject room)
        {
            const string path="Assets/Settings/OperatorAtmosphere.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,path);
                var bloom=profile.Add<Bloom>(true);
                bloom.intensity.Override(.18f);
                bloom.threshold.Override(1.15f);
                var vignette=profile.Add<Vignette>(true);
                vignette.intensity.Override(.16f);
                vignette.smoothness.Override(.45f);
                foreach(var component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            var volumeObject=new GameObject("RoomPostProcessing");
            volumeObject.transform.SetParent(parent,false);
            var volume=volumeObject.AddComponent<Volume>();
            volume.isGlobal=true;
            volume.sharedProfile=profile;
            foreach(var camera in room.GetComponentsInChildren<Camera>())
            {
                var data=camera.GetComponent<UniversalAdditionalCameraData>();
                if(data==null) data=camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing=true;
            }
        }
    }
}
