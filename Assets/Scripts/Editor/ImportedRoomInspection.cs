using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Pixamor.Editor
{

    public static class ImportedRoomInspection
    {

        [MenuItem("İklim Operatörü/Deneme/Oda Ölçülerini İncele")]
        public static void Inspect()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/iklim_yönetim_odası (3).fbx");
            if (model == null) return;
            var text = new StringBuilder();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                text.AppendLine(r.name + " | center=" + r.bounds.center.ToString("F3") + " size=" + r.bounds.size.ToString("F3") + " | materials=" + string.Join(",", System.Array.ConvertAll(r.sharedMaterials,m=>m == null ? "null" : m.name)));
            foreach(var meshFilter in model.GetComponentsInChildren<MeshFilter>()) if(meshFilter.name=="Plane.015") { var groups=new System.Collections.Generic.SortedDictionary<float,int>(); foreach(var v in meshFilter.sharedMesh.vertices) { float y=Mathf.Round(meshFilter.transform.TransformPoint(v).y*1000)/1000; if(!groups.ContainsKey(y)) groups[y]=0; groups[y]++; } foreach(var pair in groups) text.AppendLine("Desk vertex Y="+pair.Key+" count="+pair.Value); }
            File.WriteAllText("Library/ImportedRoomMeasurements.txt", text.ToString());
        }
    }
}
