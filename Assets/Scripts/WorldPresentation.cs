using UnityEngine;
namespace Qiaopi
{
    public partial class WorldGame
    {
        void AddRestCorner()
        {
            Cube("歇脚木凳",actors.transform,restPosition+new Vector3(-.45f,.35f,0),new Vector3(1.45f,.16f,.52f),wood);
            for(int i=0;i<2;i++)Cube("凳脚",actors.transform,restPosition+new Vector3(-.95f+i,.17f,0),new Vector3(.13f,.34f,.40f),wood);
            Cube("茶盘",actors.transform,restPosition+new Vector3(.92f,.54f,0),new Vector3(.72f,.1f,.62f),wood);
            Cube("茶盘支架",actors.transform,restPosition+new Vector3(.92f,.25f,0),new Vector3(.14f,.50f,.32f),wood);
            Shape(PrimitiveType.Sphere,"茶壶",actors.transform,restPosition+new Vector3(.83f,.71f,0),new Vector3(.23f,.25f,.24f),cream);
            Shape(PrimitiveType.Cylinder,"茶杯",actors.transform,restPosition+new Vector3(1.11f,.64f,.13f),new Vector3(.13f,.045f,.13f),cream);
        }
        void RestAtTea()
        {
            string key="rested_at_"+loadedWorld;
            if(state.flags.Contains(key)){Toast("在这里歇过脚了，继续这一程吧。");return;}
            if(state.health>=100){Toast("身体已经休息好了，暂时不用花钱。");return;}
            if(state.money<2){Toast("饮茶歇脚需要 2 盘缠。可以先帮乡亲送一趟货。");return;}
            int gain=Mathf.Min(8,100-state.health);state.money-=2;state.health+=gain;state.flags.Add(key);
            walkRoute.Clear();Sound();Toast("一碗热茶，缓一口气。身体 +"+gain+"，盘缠 −2。");Save();
        }
        Vector2 MapPoint(Vector3 v,Rect r){return new Vector2(Mathf.Lerp(r.x+10,r.xMax-10,Mathf.InverseLerp(region.bounds.xMin,region.bounds.xMax,v.x)),Mathf.Lerp(r.yMax-10,r.y+10,Mathf.InverseLerp(region.bounds.yMin,region.bounds.yMax,v.z)));}
        void DrawMapLine(Vector2 a,Vector2 b,float width,Color c)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/(width*.5f)));
            for(int i=0;i<=steps;i++){Vector2 p=Vector2.Lerp(a,b,(float)i/steps);Box(new Rect(p.x-width*.5f,p.y-width*.5f,width,width),c);}
        }
    }
}
