using System.Collections.Generic;
using UnityEngine;
namespace Qiaopi
{
    public static partial class WorldFactory
    {
        static void CoastalHorizon()
        {
            CoastalIsland(new Vector3(-77,0,164),106,34,8,1.7f);
            CoastalIsland(new Vector3(66,0,208),148,43,13,4.2f);
        }
        static void CoastalIsland(Vector3 center,float width,float depth,float height,float phase)
        {
            var land=new List<Vector3>();var landIndices=new List<int>();
            var shore=new List<Vector3>();var shoreIndices=new List<int>();
            const int nx=48,nz=20;
            var points=new Vector3[nx+1,nz+1];
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){
                float u=x*2f/nx-1,v=z*2f/nz-1;
                float bend=v+Mathf.Sin(u*4+phase)*.12f;
                float ridge=Mathf.Max(0,1-u*u-bend*bend);
                float y=-1.2f+height*Mathf.Pow(ridge,1.7f)*(.72f+.20f*Mathf.Sin(u*7+phase)+.08f*Mathf.Sin(v*11+u*5));
                points[x,z]=center+new Vector3(u*width*.5f,y,v*depth*.5f);
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){
                Vector3 a=points[x,z],b=points[x,z+1],c=points[x+1,z+1],d=points[x+1,z];
                bool green=(a.y+b.y+c.y+d.y)>.8f;
                Quad(green?land:shore,green?landIndices:shoreIndices,a,b,c,d);
            }
            MeshObject("远岸连绵林丘",FlatMesh(land,landIndices),Vector3.zero,Mat("far wooded coastal slope","637B67"));
            MeshObject("远岸潮水石滩",FlatMesh(shore,shoreIndices),Vector3.zero,Mat("far coastal tidal shore","879282"));
        }
    }
}
