using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
namespace Qiaopi
{
    public static class WorldSaveStore
    {
        public static bool IsValid(WorldSave s)
        {
            if(s==null||!StoryEngine.Validate(s.story)||s.missionNode!=s.story.nodeId) return false;
            var m=WorldMissions.Get(s.story);
            if(s.progress<0||s.progress>m.required||!Finite(s.px)||!Finite(s.pz))return false;
            if(s.layoutVersion<2){if(s.px< -13||s.px>13||s.pz< -12.5f||s.pz>15.5f)return false;}
            else if(s.layoutVersion!=2||!WorldRegions.Get(m.world).IsGround(new Vector3(s.px,0,s.pz)))return false;
            if(s.inspected==null)s.inspected=new List<int>();
            var seen=new HashSet<int>();
            foreach(int i in s.inspected)if(i<0||i>2||!seen.Add(i))return false;
            if(m.activity=="inspect"&&s.progress!=s.inspected.Count)return false;
            if(s.carrying&&(m.activity!="deliver"||s.progress>=m.required))return false;
            if(s.carrying&&s.sideCarrying)return false;
            return true;
        }
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        public static WorldSave Load(string path,out bool recovered)
        {
            recovered=false;
            foreach(string candidate in new[]{path,path+".bak"}) {
                if(!File.Exists(candidate))continue;
                try {var s=JsonUtility.FromJson<WorldSave>(File.ReadAllText(candidate,Encoding.UTF8));if(IsValid(s)){recovered=candidate!=path;return s;}}catch{ }
            }
            return null;
        }
        public static void Write(string path,WorldSave save)
        {
            if(!IsValid(save))throw new InvalidDataException("Invalid world save");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+".tmp";
            using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){
                byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(save,true));file.Write(bytes,0,bytes.Length);file.Flush(true);
            }
            if(File.Exists(path)) {
                bool validExisting=false;
                try{validExisting=IsValid(JsonUtility.FromJson<WorldSave>(File.ReadAllText(path,Encoding.UTF8)));}catch{}
                File.Replace(temp,path,validExisting?path+".bak":null);
            } else File.Move(temp,path);
        }
    }
}
