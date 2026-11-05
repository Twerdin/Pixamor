using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pixamor.Editor
{
    public static class OperatorRoomSetup
    {
        [MenuItem("İklim Operatörü/Monitörü Önizle (Play)")]
        public static void PreviewMonitor()
        {
            var player = Object.FindAnyObjectByType<FirstPersonOperator>();
            if (EditorApplication.isPlaying && player != null) player.EnterMonitor();
        }

        [MenuItem("İklim Operatörü/Monitörü Önizle (Play)", true)]
        public static bool CanPreviewMonitor() => EditorApplication.isPlaying;

        [MenuItem("İklim Operatörü/Odayı Kur")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Oda kurulumu için önce Play modundan çık.");
                return;
            }
            var existingRoom = GameObject.Find("OperatorRoom");
            if (existingRoom != null)
            {
                Selection.activeGameObject = existingRoom;
                Debug.Log("Oda zaten kurulu; yeniden oluşturulmadı.");
                return;
            }
            var session = Object.FindAnyObjectByType<GameSession>();
            if (session == null) session = new GameObject("GameManager").AddComponent<GameSession>();
            var manager = session.GetComponent<CrisisManager>();
            if (manager == null) manager = session.gameObject.AddComponent<CrisisManager>();
            // Önceden yanlışlıkla eklenmiş kopyaları silmeden devre dışı bırak.
            foreach (var other in session.GetComponents<CrisisManager>())
                other.enabled = other == manager;
            var monitor = session.GetComponent<MonitorPrototype>();
            if (monitor == null) monitor = session.gameObject.AddComponent<MonitorPrototype>();
            monitor.enabled = true;
            var debug = session.GetComponent<SessionDebugPanel>();
            if (debug != null) debug.enabled = false;
            var oldPanel = session.GetComponent<CrisisPrototypePanel>();
            if (oldPanel != null) oldPanel.enabled = false;

            // Kullanıcının eski deneme objelerini koru; yeni odanın görüşünü kapatmasınlar.
            var previous = new GameObject("PreviousPrototype_Disabled");
            Undo.RegisterCreatedObjectUndo(previous, "Archive previous prototype");
            foreach (var go in session.gameObject.scene.GetRootGameObjects())
            {
                if (go.name != "Cube" && go.name != "Plane" && go.name != "Main Camera") continue;
                Undo.SetTransformParent(go.transform, previous.transform, "Archive previous prototype");
            }
            previous.SetActive(false);

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            var room = new GameObject("OperatorRoom");
            Undo.RegisterCreatedObjectUndo(room, "Create operator room");
            OperatorRoomBuilder.Build(room.transform, (name, color) =>
            {
                string path = "Assets/Materials/Operator" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                    AssetDatabase.CreateAsset(material, path);
                }
                return material;
            }, manager, out var camera, out var screen, out var player);
            monitor.Configure(camera, screen, player);
            EditorUtility.SetDirty(session.gameObject);
            EditorUtility.SetDirty(monitor);
            EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
            EditorSceneManager.SaveScene(session.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = room;
            Debug.Log("Oda hazır. WASD: yürü, fare: bak, E: monitörü kullan, Esc: imleci bırak.");
        }
    }
}
