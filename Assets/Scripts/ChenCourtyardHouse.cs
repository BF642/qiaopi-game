using UnityEngine;
namespace Qiaopi {
    // Complete house, with collision derived from its actual wall triangles.
    public static class ChenCourtyardHouse {
        const string Path="EnvironmentArt/QuanzhouCourtyardHome/Model";
        public static bool Ready => Resources.Load<GameObject>(Path)!=null;
        public static bool Spawn(Transform parent) {
            var prefab=Resources.Load<GameObject>(Path);if(!prefab)return false;
            var house=Object.Instantiate(prefab,parent,false);house.name="陈家整屋 · 红砖厝";
            house.transform.localPosition=new Vector3(-23,0,-7);
            AddDetails(house.transform);
            Debug.Log("CHEN_WHOLE_HOUSE_SPAWNED native8K realSideDoors interiorMealAndWriting");
            return true;
        }
        static void AddDetails(Transform root) {
            var wood=WorldSurfaceMaterials.Create("Chen side-door old timber",new Color(.27f,.20f,.14f));
            Frame(root,wood,5.15f,-1.55f,2.2f);
            Frame(root,wood,-5.15f,-1.9f,6.4f);
            Frame(root,wood,-5.15f,3.5f,1.8f);
            Frame(root,wood,-2.15f,5.2f,2f);
            var g=new TradeScenery.Geometry(root);
            var stone=WorldSurfaceMaterials.Create("Chen interior worn granite",new Color(.49f,.48f,.42f));
            g.Box(new Vector3(0,.058f,6.25f),new Vector3(16.2f,.012f,5.4f),stone);
            foreach(float side in new[]{-1f,1f}) {
                g.Box(new Vector3(side*6.75f,.058f,-3.1f),new Vector3(2.8f,.012f,11.8f),stone);
                // Timber soffits close holes in the generated underside while
                // keeping the original tile silhouette and all windows visible.
                for(int plank=0;plank<56;plank++)g.Box(new Vector3(side*6.75f,2.36f,-8.8f+plank*.21f),new Vector3(2.7f,.045f,.205f),wood);
            }
            for(int plank=0;plank<27;plank++)g.Box(new Vector3(0,2.78f,3.6f+plank*.20f),new Vector3(16.1f,.055f,.196f),wood);
            g.Flush();
            var lamp=new GameObject("Hall reflected daylight");lamp.transform.SetParent(root,false);
            lamp.transform.localPosition=new Vector3(0,2.2f,5.5f);
            var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=9;light.intensity=.38f;light.color=new Color(1,.9f,.75f);light.shadows=LightShadows.None;
        }
        static void Frame(Transform root,Material wood,float x,float z,float opening) {
            foreach(float side in new[]{-1f,1f}) Beam(root,wood,new Vector3(x,1.2f,z+side*(opening*.5f+.04f)),new Vector3(.36f,2.4f,.08f));
            Beam(root,wood,new Vector3(x,2.36f,z),new Vector3(.36f,.12f,opening+.16f));
        }
        static void Beam(Transform root,Material mat,Vector3 at,Vector3 size) {
            var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.name="Side-door timber reveal";beam.transform.SetParent(root,false);beam.transform.localPosition=at;beam.transform.localScale=size;
            beam.GetComponent<Renderer>().sharedMaterial=mat;
        }
    }
}
