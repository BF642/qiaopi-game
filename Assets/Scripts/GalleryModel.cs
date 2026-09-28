using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    // 图集条目：一件通过审核的侨批（或相关材料）。
    [Serializable] public class GalleryEntry
    {
        public string id,title,category,year,place,contributor,contributorId,description,source,image,imageUrl,submittedAt,sample;
        public bool visible=true;
    }

    // 开发者对某位提供者的审核结论，随索引一起发布。
    [Serializable] public class GalleryNote
    {
        public string contributorId,submissionId,status,note,date;
    }

    // gallery/index.json：游戏读到的权威索引（内置一份，线上更新一份）。
    [Serializable] public class GalleryIndex
    {
        public int version=1;
        public string updated="";
        public string indexUrl="";
        public string repo="";
        public List<string> contributors=new List<string>();
        public List<GalleryNote> notes=new List<GalleryNote>();
        public List<GalleryEntry> entries=new List<GalleryEntry>();

        public bool IsApproved(string contributorId)
        {
            return !string.IsNullOrEmpty(contributorId)&&contributors!=null&&contributors.Contains(contributorId);
        }
        public string NoteFor(string contributorId)
        {
            if(notes==null)return null;
            for(int i=notes.Count-1;i>=0;i--)if(notes[i]!=null&&notes[i].contributorId==contributorId)return notes[i].status+"|"+(notes[i].note??"");
            return null;
        }
        public List<GalleryEntry> Visible()
        {
            var list=new List<GalleryEntry>();
            if(entries!=null)foreach(var e in entries)if(e!=null&&e.visible&&!string.IsNullOrEmpty(e.title))list.Add(e);
            return list;
        }
    }

    // 玩家本机的一份投稿记录。
    [Serializable] public class GallerySubmission
    {
        public string id,title,category,year,place,contributor,contributorId,description,source,createdAt,status,note,handedOverAt,deliveryReference;
        public bool rightsConfirmed;
        public List<string> files=new List<string>();
        public List<string> hashes=new List<string>();
        public string PackageDir(string root){return System.IO.Path.Combine(root,"submissions",id);}
    }

    [Serializable] public class GalleryDraft
    {
        public GalleryEntry entry=new GalleryEntry();
        public List<string> files=new List<string>();
        public bool consent;
        public int step;
        public string updatedAt="";
    }

    // gallery-state.json：玩家身份、草稿与投稿历史。
    [Serializable] public class GalleryState
    {
        public int version=2;
        public string contributorId="";
        public GalleryDraft draft=new GalleryDraft();
        public List<GallerySubmission> submissions=new List<GallerySubmission>();
        public int SubmissionCount{get{return submissions==null?0:submissions.Count;}}
        public bool HasPending{get{ if(submissions==null)return false;foreach(var s in submissions)if(s!=null&&s.status=="handed_over")return true;return false;}}
        public int RejectedCount{get{int n=0;if(submissions!=null)foreach(var s in submissions)if(s!=null&&s.status=="rejected")n++;return n;}}
    }

    // 投稿字段校验：游戏内与审核工具用同一套规则。
    public static class GalleryRules
    {
        public const int MaxImages=8;
        public const long MaxImageBytes=8L*1024*1024;
        public static readonly string[] Categories={"侨批信","回批","批封","汇票与单据","信局与器物","老照片","口述与文字","其他"};
        public static readonly string[] ImageExtensions={".png",".jpg",".jpeg",".webp"};

        public static string StatusLabel(string status)
        {
            switch(status){case "packaged":return "已打包 · 未递交";case "handed_over":return "已记递交 · 待审";case "approved":return "已收入图集";case "rejected":return "退回修改";case "pending":return "旧记录 · 待核对";default:return "本机记录";}
        }

        public static bool IsImage(string path)
        {
            if(string.IsNullOrEmpty(path))return false;
            string ext=System.IO.Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(ImageExtensions,ext)>=0;
        }
        public static bool IsCategory(string value)
        {
            return !string.IsNullOrEmpty(value)&&Array.IndexOf(Categories,value)>=0;
        }
        public static bool Validate(GalleryEntry entry,out string reason)
        {
            reason="";
            if(entry==null){reason="投稿内容为空。";return false;}
            string title=(entry.title??"").Trim();
            if(title.Length<2||title.Length>40){reason="题名需要 2 至 40 个字。";return false;}
            if(!IsCategory((entry.category??"").Trim())){reason="请选择材料类别。";return false;}
            if((entry.description??"").Trim().Length<8){reason="说明至少写 8 个字，方便他人读懂它的来处。";return false;}
            if((entry.source??"").Trim().Length<2){reason="请写明来源与授权情况。";return false;}
            if((entry.contributor??"").Trim().Length<1){reason="请留下署名。";return false;}
            if((entry.year??"").Length>20||(entry.place??"").Length>24){reason="年代或地点写得太长。";return false;}
            if(!string.IsNullOrEmpty(entry.sample)&&entry.sample.Length>40){reason="样张标记过长。";return false;}
            return true;
        }
        public static bool ValidateFiles(IList<string> files,out string reason)
        {
            reason="";
            if(files==null||files.Count==0){reason="至少需要一张图片。";return false;}
            if(files.Count>MaxImages){reason="一次最多 "+MaxImages+" 张图片。";return false;}
            foreach(string path in files)
            {
                if(!System.IO.File.Exists(path)){reason="找不到文件："+System.IO.Path.GetFileName(path);return false;}
                if(!IsImage(path)){reason="只支持 PNG / JPG / WebP："+System.IO.Path.GetFileName(path);return false;}
                var info=new System.IO.FileInfo(path);
                if(info.Length<=0){reason="文件是空的："+info.Name;return false;}
                if(info.Length>MaxImageBytes){reason=info.Name+" 超过 8MB，请先压缩。";return false;}
            }
            return true;
        }
    }

    public static class GalleryJson
    {
        public static T Parse<T>(string json) where T:class
        {
            if(string.IsNullOrEmpty(json))return null;
            try{return JsonUtility.FromJson<T>(json);}catch{return null;}
        }
        public static string Write(object value,bool pretty){return JsonUtility.ToJson(value,pretty);}
    }
}
