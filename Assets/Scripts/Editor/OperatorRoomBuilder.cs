using System;
using UnityEngine;

namespace Pixamor.Editor
{
    // Yalnızca editör aracıdır; oyuncuya verilen oyuna dahil edilmez.
    // Oluşturulan parçalar sahnede normal, elle düzenlenebilir Unity nesneleridir.
    public static class OperatorRoomBuilder
    {
        public static void Build(Transform root, Func<string, Color, Material> material,
            CrisisManager crises, out Camera camera, out Transform screen, out FirstPersonOperator player)
        {
            Cube(root, "Zemin", new Vector3(0,-.15f,-1), new Vector3(12,.3f,12), material("Floor",new Color(.16f,.19f,.21f)));
            var wall = material("Wall", new Color(.24f,.30f,.34f));
            Cube(root,"ArkaDuvar", new Vector3(0,2.5f,4.9f),new Vector3(12,5,.2f),wall);
            Cube(root,"SolDuvar",new Vector3(-6,2.5f,-1),new Vector3(.2f,5,12),wall);
            Cube(root,"SagDuvar",new Vector3(6,2.5f,-1),new Vector3(.2f,5,12),wall);
            Cube(root,"GirisDuvari",new Vector3(0,2.5f,-6.9f),new Vector3(12,5,.2f),wall);
            var wood = material("Desk",new Color(.30f,.19f,.11f));
            var metal = material("Metal",new Color(.055f,.07f,.085f));
            Cube(root,"MasaTablasi",new Vector3(0,1f,.7f),new Vector3(7,.16f,2.6f),wood);
            foreach(float x in new[]{-3.1f,3.1f})
            foreach(float z in new[]{-.3f,1.7f})
                Cube(root,"MasaAyagi",new Vector3(x,.46f,z),new Vector3(.18f,.92f,.18f),metal);
            Cube(root,"DevMonitorGovdesi",new Vector3(0,3f,1.7f),new Vector3(5.74f,3.84f,.24f),metal);
            Cube(root,"MonitorStand",new Vector3(0,1.25f,1.8f),new Vector3(.35f,.5f,.35f),metal);
            Cube(root,"Klavye",new Vector3(-.3f,1.13f,-.15f),new Vector3(1.3f,.07f,.4f),metal);
            Cube(root,"Fare",new Vector3(.65f,1.14f,-.15f),new Vector3(.18f,.09f,.28f),metal);
            Cube(root,"BilgisayarKasasi",new Vector3(2.5f,.5f,1),new Vector3(.5f,1,.9f),metal);
            var trim = material("Accent",new Color(.08f,.65f,.65f));
            Cube(root,"MonitorAltCizgi",new Vector3(0,1.1f,1.55f),new Vector3(5.4f,.035f,.03f),trim);

            screen = new GameObject("MonitorScreenMount").transform;
            screen.SetParent(root,false);
            screen.localPosition = new Vector3(0,3f,1.565f);

            var playerObject = new GameObject("OperatorPlayer");
            playerObject.transform.SetParent(root,false);
            playerObject.transform.localPosition = new Vector3(0,.05f,-3f);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .3f;
            controller.center = new Vector3(0,.9f,0);
            controller.stepOffset = .25f;
            var eye = new GameObject("OperatorCamera");
            eye.transform.SetParent(playerObject.transform,false);
            eye.transform.localPosition = new Vector3(0,1.65f,0);
            camera = eye.AddComponent<Camera>();
            camera.nearClipPlane = .05f;
            camera.fieldOfView = 60;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.02f,.035f,.045f);
            camera.depth = 100;
            eye.AddComponent<AudioListener>();
            player = playerObject.AddComponent<FirstPersonOperator>();
            player.Configure(camera,screen,crises);

            var lightObject = new GameObject("OdaIsigi");
            lightObject.transform.SetParent(root,false);
            lightObject.transform.localPosition = new Vector3(0,4.5f,-1);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 16;
            light.intensity = 5;
        }

        private static void Cube(Transform root,string name,Vector3 position,Vector3 scale,Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root,false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
