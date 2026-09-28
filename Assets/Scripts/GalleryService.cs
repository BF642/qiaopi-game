using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Qiaopi
{
    // 本机可选配置：gallery-config.json（persistentDataPath 根目录）。
    [Serializable] public class GalleryConfig
    {
        public string indexUrl="";
        public string repo="";
    }

    // 图集索引与图片：内置一份，线上/缓存各一份，后者按条目 id 覆盖前者。
    public static class GalleryService
    {
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static readonly HashSet<string> pending=new HashSet<string>();

        public static GalleryIndex Bundled()
        {
            var asset=Resources.Load<TextAsset>("Gallery/index");
            var index=GalleryJson.Parse<GalleryIndex>(asset!=null?asset.text:null);
            if(index==null)index=new GalleryIndex();
            if(index.entries==null)index.entries=new List<GalleryEntry>();
            if(index.contributors==null)index.contributors=new List<string>();
            if(index.notes==null)index.notes=new List<GalleryNote>();
            return index;
        }

        public static GalleryIndex Cached()
        {
            try
            {
                if(!File.Exists(GalleryStore.IndexCachePath))return null;
                return GalleryJson.Parse<GalleryIndex>(File.ReadAllText(GalleryStore.IndexCachePath,Encoding.UTF8));
            }
            catch{return null;}
        }

        public static void WriteCache(GalleryIndex index)
        {
            if(index==null)return;
            try
            {
                GalleryStore.EnsureFolders();
                File.WriteAllText(GalleryStore.IndexCachePath,GalleryJson.Write(index,true),Encoding.UTF8);
            }
            catch(Exception e){Debug.LogWarning("图集索引缓存失败："+e.Message);}
        }

        public static GalleryIndex Merge(GalleryIndex bundled,GalleryIndex overlay)
        {
            var merged=new GalleryIndex();
            if(bundled!=null)
            {
                merged.version=bundled.version;merged.updated=bundled.updated;merged.indexUrl=bundled.indexUrl;merged.repo=bundled.repo;
                if(bundled.contributors!=null)merged.contributors.AddRange(bundled.contributors);
                if(bundled.notes!=null)merged.notes.AddRange(bundled.notes);
                if(bundled.entries!=null)merged.entries.AddRange(bundled.entries);
            }
            if(overlay!=null)
            {
                if(!string.IsNullOrEmpty(overlay.updated))merged.updated=overlay.updated;
                if(!string.IsNullOrEmpty(overlay.indexUrl))merged.indexUrl=overlay.indexUrl;
                if(!string.IsNullOrEmpty(overlay.repo))merged.repo=overlay.repo;
                if(overlay.contributors!=null)foreach(string id in overlay.contributors)if(!merged.contributors.Contains(id))merged.contributors.Add(id);
                if(overlay.notes!=null)merged.notes.AddRange(overlay.notes);
                if(overlay.entries!=null)foreach(var entry in overlay.entries)
                {
                    if(entry==null)continue;
                    int at=merged.entries.FindIndex(x=>x!=null&&x.id==entry.id);
                    if(at>=0)merged.entries[at]=entry;else merged.entries.Add(entry);
                }
            }
            merged.entries.RemoveAll(x=>x==null||string.IsNullOrEmpty(x.id));
            return merged;
        }

        public static GalleryConfig Config()
        {
            try
            {
                if(File.Exists(GalleryStore.ConfigPath))
                    return GalleryJson.Parse<GalleryConfig>(File.ReadAllText(GalleryStore.ConfigPath,Encoding.UTF8))??new GalleryConfig();
            }
            catch(Exception e){Debug.LogWarning("读取 gallery-config.json 失败："+e.Message);}
            return new GalleryConfig();
        }

        public static string IndexUrl(GalleryIndex index)
        {
            var config=Config();
            if(!string.IsNullOrEmpty(config.indexUrl))return config.indexUrl.Trim();
            return index!=null?(index.indexUrl??"").Trim():"";
        }

        public static string RepoUrl(GalleryIndex index)
        {
            var config=Config();
            if(!string.IsNullOrEmpty(config.repo))return NormalizeRepo(config.repo);
            return index!=null?NormalizeRepo(index.repo):"";
        }

        static string NormalizeRepo(string repo)
        {
            if(string.IsNullOrEmpty(repo))return "";
            repo=repo.Trim();
            if(!repo.StartsWith("http"))repo="https://github.com/"+repo.Trim('/');
            return repo.TrimEnd('/');
        }

        // 投稿单链接：开发者在仓库里用 issue 表单收件（YAML 表单支持预填标题与模板）。
        public static string SubmissionIssueUrl(GalleryIndex index,GallerySubmission submission)
        {
            string repo=RepoUrl(index);
            if(string.IsNullOrEmpty(repo)||submission==null)return "";
            return repo+"/issues/new?template="+Uri.EscapeDataString("投稿.yml")+"&title="+Uri.EscapeDataString("[投稿] "+submission.title+" · "+submission.id);
        }

        public static string ImageUrl(GalleryIndex index,GalleryEntry entry)
        {
            if(entry==null)return "";
            if(!string.IsNullOrEmpty(entry.imageUrl))return entry.imageUrl;
            string repo=RepoUrl(index);
            if(!string.IsNullOrEmpty(repo)&&!string.IsNullOrEmpty(entry.image)&&entry.image.StartsWith("gallery/"))
                return repo.Replace("github.com","raw.githubusercontent.com")+"/main/"+entry.image;
            return "";
        }

        static string SafeName(string value)
        {
            var sb=new StringBuilder();
            foreach(char c in value??"")sb.Append(char.IsLetterOrDigit(c)||c=='-'||c=='_'?c:'_');
            return sb.Length==0?"entry":sb.ToString();
        }

        public static bool IsPending(string key){return pending.Contains(key);}
        public static void MarkPending(string key){if(!string.IsNullOrEmpty(key))pending.Add(key);}

        public static Texture2D Texture(GalleryEntry entry,out bool loading)
        {
            loading=false;
            if(entry==null)return null;
            string key=string.IsNullOrEmpty(entry.id)?(entry.image??""):entry.id;
            Texture2D found;
            if(textures.TryGetValue(key,out found)&&found!=null)return found;
            if(pending.Contains(key)){loading=true;return null;}
            if(!string.IsNullOrEmpty(entry.image))
            {
                var asset=Resources.Load<Texture2D>(entry.image);
                if(asset!=null){textures[key]=asset;return asset;}
            }
            string file=Path.Combine(GalleryStore.CacheDir,SafeName(key)+".png");
            if(File.Exists(file))
            {
                try
                {
                    var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    if(tex.LoadImage(File.ReadAllBytes(file))){tex.wrapMode=TextureWrapMode.Clamp;textures[key]=tex;return tex;}
                    UnityEngine.Object.Destroy(tex);
                }
                catch(Exception e){Debug.LogWarning("读取图集缓存图片失败："+e.Message);}
            }
            if(!string.IsNullOrEmpty(ImageUrl(null,entry))){loading=true;}
            return null;
        }

        public static void StoreRemoteImage(string key,byte[] data)
        {
            pending.Remove(key);
            if(data==null||data.Length==0)return;
            try
            {
                GalleryStore.EnsureFolders();
                File.WriteAllBytes(Path.Combine(GalleryStore.CacheDir,SafeName(key)+".png"),data);
                var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(tex.LoadImage(data)){tex.wrapMode=TextureWrapMode.Clamp;textures[key]=tex;}
                else UnityEngine.Object.Destroy(tex);
            }
            catch(Exception e){Debug.LogWarning("写入图集图片缓存失败："+e.Message);}
        }

        public static void ClearCache()
        {
            textures.Clear();pending.Clear();
        }
    }
}
