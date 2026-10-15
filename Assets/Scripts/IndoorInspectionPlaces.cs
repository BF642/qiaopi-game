using UnityEngine;

namespace Qiaopi
{
    public sealed class IndoorInspectionPlace
    {
        public readonly string world,title;
        public readonly Vector3 position,documentPoint;
        public readonly Quaternion paperRotation;
        public IndoorInspectionPlace(string world,string title,Vector3 position,Vector3 documentPoint,Quaternion rotation)
        {this.world=world;this.title=title;this.position=position;this.documentPoint=documentPoint;paperRotation=rotation;}
    }

    /// <summary>Visitors stand beside furnished indoor work surfaces, never inside a road desk.</summary>
    public static class IndoorInspectionPlaces
    {
        public static bool HasWorld(string world)=>world=="market"||world=="postoffice"||world=="port";
        public static IndoorInspectionPlace Get(string world,int index)
        {
            index=Mathf.Clamp(index,0,2);
            if(world=="market") {
                if(index==0)return From("market","accounts",new Vector3(-14.67f,Floor(-14.6f,16.55f)+.915f,16.32f),Quaternion.identity);
                if(index==1)return From("market","deliverylist",new Vector3(-12.235f,Floor(-11.85f,19.25f)+1.14f,18.7f),Quaternion.Euler(0,0,90));
                return new IndoorInspectionPlace(world,"陈记柜台留底",P(-21.6f,21.3f),new Vector3(-21.6f,.976f,23.03f),Quaternion.identity);
            }
            if(world=="postoffice") {
                if(index==0)return From(world,"verify",new Vector3(11.49f,.996f,-.55f),Quaternion.identity);
                if(index==1)return From(world,"archive",new Vector3(-14.47f,.973f,28.96f),Quaternion.identity);
                return new IndoorInspectionPlace(world,"封袋后院留底台",P(14,28.6f),new Vector3(14,.933f,30.07f),Quaternion.identity);
            }
            if(world=="port") {
                if(index==0)return From(world,"accounts",new Vector3(-27.45f,Floor(-27.4f,28.65f)+.935f,28.49f),Quaternion.identity);
                if(index==1)return From(world,"count",new Vector3(-29.93f,Floor(-30.5f,30.65f)+.64f,30.115f),Quaternion.Euler(-90,0,0));
                return new IndoorInspectionPlace(world,"点收间副单台",InquiryPlaces.Evidence("trace_harbor",0),InquiryPlaces.DocumentPoint("trace_harbor",0),Quaternion.identity);
            }
            return null;
        }
        public static IndoorInspectionPlace ForState(GameState state,int index)
        {
            string route=FirstDayChapter.IsActive(state)?state.firstDay.route:
                LifeJourney.IsActive(state)&&!string.IsNullOrEmpty(state.journey.pendingJob)?state.journey.pendingJob:null;
            string world=route=="dock"?"port":route=="shop"?"market":route=="courier"?"postoffice":WorldMissions.Get(state.nodeId).world;
            if(FirstDayChapter.IsActive(state)&&route=="shop") {
                string choice=state.firstDay.choice;
                if(choice=="wages"||choice=="read")index=(index+2)%3;
                else if(choice=="learn")index=(index+1)%3;
            }
            return Get(world,index);
        }
        public static Vector3[] Positions(GameState state)
        {
            var result=new Vector3[3];
            for(int i=0;i<3;i++){var place=ForState(state,i);result[i]=place==null?Vector3.zero:place.position;}
            return result;
        }
        public static Vector3[] Positions(string world)
        {
            return new[]{Get(world,0).position,Get(world,1).position,Get(world,2).position};
        }
        public static IndoorInspectionPlace At(string world,Vector3 position)
        {
            if(!HasWorld(world))return null;
            for(int i=0;i<3;i++){var place=Get(world,i);if((place.position-position).sqrMagnitude<.01f)return place;}
            foreach(var place in NanyangPlaces.ForWorld(world))if((place.position-position).sqrMagnitude<.01f)
                return new IndoorInspectionPlace(world,place.title,place.position,place.facePoint,Quaternion.identity);
            return null;
        }
        static IndoorInspectionPlace From(string world,string role,Vector3 document,Quaternion rotation)
        {var place=NanyangPlaces.Get(world,role);return new IndoorInspectionPlace(world,place.title,place.position,document,rotation);}
        static float Floor(float x,float z)=>WorldFactory.CharacterGroundPosition(P(x,z)).y;
        static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    }

    public partial class WorldGame
    {
        string InspectionPlaceTitle(int index)
        {var place=IndoorInspectionPlaces.At(loadedWorld,inspectionPositions[index]);return place==null?"核对凭据":place.title;}
        string IndoorInspectionObjective()
        {for(int i=0;i<mission.required&&i<inspectionPositions.Length;i++)if(!inspected.Contains(i))return "到"+InspectionPlaceTitle(i)+"核单";return "找"+mission.npcName+"回话";}
        void FaceInspectionDocument(int index)
        {
            var place=IndoorInspectionPlaces.At(loadedWorld,inspectionPositions[index]);if(place==null)return;
            Vector3 direction=place.documentPoint-CameraEyePoint();
            cameraYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            cameraPitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
            UpdateCamera(true);
        }
    }
}
