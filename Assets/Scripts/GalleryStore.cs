using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Qiaopi
{
    // 本机图集仓库：投稿目录、投稿包、额度状态与索引缓存。全部落在 persistentDataPath 下。
    public static class GalleryStore
    {
        public const string StateFile="gallery-state.json";
        public const string ConfigFile="gallery-config.json";
        public const string IndexCacheFile="gallery-index-cache.json";

        public static string RootOverride="";
        public static string Root{get{return string.IsNullOrEmpty(RootOverride)?Path.Combine(Application.persistentDataPath,"gallery"):RootOverride;}}
        public static string InboxDir{get{return Path.Combine(Root,"投稿");}}
        public static string SubmissionsDir{get{return Path.Combine(Root,"submissions");}}
        public static string CacheDir{get{return Path.Combine(Root,"cache");}}
        public static string StatePath{get{return Path.Combine(Root,StateFile);}}
        public static string ConfigPath{get{return Path.Combine(Application.persistentDataPath,ConfigFile);}}
        public static string IndexCachePath{get{return Path.Combine(Root,IndexCacheFile);}}

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(InboxDir);
            Directory.CreateDirectory(SubmissionsDir);
            Directory.CreateDirectory(CacheDir);
        }

        // ── 玩家身份与投稿历史 ──────────────────────────────────────────────
        public static GalleryState LoadState()
        {
            EnsureFolders();
            var state=ReadState(StatePath);
            if(state==null)state=ReadState(StatePath+".bak");
            if(state==null)state=new GalleryState();
            if(state.submissions==null)state.submissions=new List<GallerySubmission>();
            if(state.draft==null)state.draft=new GalleryDraft();
            if(state.draft.entry==null)state.draft.entry=new GalleryEntry();
            if(state.draft.files==null)state.draft.files=new List<string>();
            foreach(var submission in state.submissions){if(submission.files==null)submission.files=new List<string>();if(submission.hashes==null)submission.hashes=new List<string>();}
            if(string.IsNullOrEmpty(state.contributorId))state.contributorId=NewContributorId();
            return state;
        }
        static GalleryState ReadState(string path)
        {
            if(!File.Exists(path))return null;
            try
            {
                var state=GalleryJson.Parse<GalleryState>(File.ReadAllText(path,Encoding.UTF8));
                if(state==null)return null;
                foreach(var s in state.submissions??new List<GallerySubmission>())if(s==null||string.IsNullOrEmpty(s.id))return null;
                return state;
            }
            catch{return null;}
        }
        public static void WriteState(GalleryState state)
        {
            if(state==null)return;
            EnsureFolders();
            string json=GalleryJson.Write(state,true);
            string temp=StatePath+".tmp";
            using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(json);
                file.Write(bytes,0,bytes.Length);file.Flush(true);
            }
            if(File.Exists(StatePath))File.Replace(temp,StatePath,StatePath+".bak");
            else File.Move(temp,StatePath);
        }
        public static string NewContributorId()
        {
            var bytes=new byte[4];RandomNumberGenerator.Create().GetBytes(bytes);
            var sb=new StringBuilder("QZ-");
            foreach(byte b in bytes)sb.Append(b.ToString("x2"));
            return sb.ToString().ToUpperInvariant();
        }

        // ── 投稿目录扫描 ────────────────────────────────────────────────────
        // 已收进投稿包的文件不再重复列出。
        public static List<string> ScanInbox(GalleryState state)
        {
            EnsureFolders();
            var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if(state!=null&&state.submissions!=null)
                foreach(var s in state.submissions)
                    if(s!=null&&s.status!="rejected"&&s.files!=null)foreach(string f in s.files)used.Add(f);
            var found=new List<string>();
            foreach(string file in Directory.GetFiles(InboxDir))
            {
                string name=Path.GetFileName(file);
                if(name.StartsWith("."))continue;
                if(!GalleryRules.IsImage(file))continue;
                if(used.Contains(name))continue;
                found.Add(file);
            }
            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found;
        }
        public static bool InboxFileUsed(GalleryState state,string fileName)
        {
            if(state==null||state.submissions==null)return false;
            foreach(var s in state.submissions)if(s!=null&&s.files!=null&&s.files.Contains(fileName))return true;
            return false;
        }

        // ── 生成投稿包 ──────────────────────────────────────────────────────
        public static GallerySubmission CreateSubmission(GalleryState state,GalleryEntry draft,List<string> files,out string error,bool rightsConfirmed=false)
        {
            error="";
            if(state==null){error="投稿状态不可用。";return null;}
            if(!GalleryRules.Validate(draft,out error))return null;
            if(!GalleryRules.ValidateFiles(files,out error))return null;
            EnsureFolders();
            string id=NewSubmissionId(state);
            string dir=Path.Combine(SubmissionsDir,id);
            Directory.CreateDirectory(dir);
            var saved=new GallerySubmission
            {
                id=id,title=draft.title.Trim(),category=draft.category.Trim(),year=(draft.year??"").Trim(),
                place=(draft.place??"").Trim(),contributor=draft.contributor.Trim(),contributorId=state.contributorId,
                description=draft.description.Trim(),source=draft.source.Trim(),
                createdAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm"),status="packaged",note="尚未递交；本机生成文件不会发送给维护者。",rightsConfirmed=rightsConfirmed
            };
            foreach(string source in files)
            {
                string name=Path.GetFileName(source);
                string target=Path.Combine(dir,name);
                File.Copy(source,target,true);
                saved.files.Add(name);
                saved.hashes.Add(HashFile(target));
            }
            File.WriteAllText(Path.Combine(dir,"entry.json"),GalleryJson.Write(saved,true),Encoding.UTF8);
            File.WriteAllText(Path.Combine(dir,"投稿说明.txt"),SubmissionReadme(saved),Encoding.UTF8);
            state.submissions.Add(saved);
            WriteState(state);
            return saved;
        }

        public static string SubmissionReadme(GallerySubmission s)
        {
            var sb=new StringBuilder();
            sb.AppendLine("侨批图集 · 共建投稿包");
            sb.AppendLine("投稿编号："+s.id);
            sb.AppendLine("题名："+s.title);
            sb.AppendLine("类别："+s.category);
            sb.AppendLine("年代："+(string.IsNullOrEmpty(s.year)?"（未填）":s.year));
            sb.AppendLine("地点："+(string.IsNullOrEmpty(s.place)?"（未填）":s.place));
            sb.AppendLine("署名："+s.contributor+"（提供者编号 "+s.contributorId+"）");
            sb.AppendLine("说明："+s.description);
            sb.AppendLine("来源与授权："+s.source);
            sb.AppendLine("生成时间："+s.createdAt);
            sb.AppendLine();
            sb.AppendLine("当前步骤：已生成投稿包，尚未自动发送。请手动把整个文件夹交给图集维护者。");
            sb.AppendLine("授权确认："+(s.rightsConfirmed?"投稿者已在游戏内确认可公开材料并署名":"旧版投稿包，需维护者另行确认授权"));
            sb.AppendLine("图片、entry.json 与本说明缺一不可；可先压缩整个文件夹再递交。");
            sb.AppendLine("递交后回到游戏的‘我的投稿’，登记收件方式或帖子链接；登记不代表对方收件确认。");
            sb.AppendLine("以下命令由维护者收件后使用：");
            sb.AppendLine("  python3 tools/review_submission.py \""+s.id+"\"");
            sb.AppendLine("审核通过后，条目会出现在下一版 gallery/index.json 里，");
            sb.AppendLine("未配置在线图集时，需要维护者把审核结果随游戏更新，或提供可刷新的图集地址。");
            return sb.ToString();
        }

        static string NewSubmissionId(GalleryState state)
        {
            string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string candidate="GJ-"+stamp;
            int suffix=1;
            while(Directory.Exists(Path.Combine(SubmissionsDir,candidate))||HasId(state,candidate))
                candidate="GJ-"+stamp+"-"+(++suffix).ToString("00");
            return candidate;
        }
        static bool HasId(GalleryState state,string id)
        {
            if(state==null||state.submissions==null)return false;
            foreach(var s in state.submissions)if(s!=null&&s.id==id)return true;
            return false;
        }

        public static string HashFile(string path)
        {
            using(var sha=SHA256.Create())
            using(var stream=File.OpenRead(path))
            {
                byte[] hash=sha.ComputeHash(stream);
                var sb=new StringBuilder();
                foreach(byte b in hash)sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string HumanSize(long bytes)
        {
            if(bytes<1024)return bytes+" B";
            if(bytes<1024*1024)return (bytes/1024f).ToString("0.0")+" KB";
            return (bytes/1024f/1024f).ToString("0.0")+" MB";
        }

        public static void OpenFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Application.OpenURL(new Uri(path).AbsoluteUri);
            }
            catch(Exception e){Debug.LogWarning("打开目录失败："+e.Message);}
        }
        public static void OpenUrl(string url)
        {
            if(string.IsNullOrEmpty(url))return;
            try{Application.OpenURL(url);}catch(Exception e){Debug.LogWarning("打开链接失败："+e.Message);}
        }

        // 本地准备不消耗投稿额度，也不会把材料当作已经发送。
        public static bool CanSubmit(GalleryState state,GalleryIndex index,out string reason)
        {
            reason=state==null?"投稿记录不可用。":"可以准备多份投稿包。生成文件不会上传；交给维护者后再记录递交。";
            return state!=null;
        }

        public static bool MarkHandedOver(GalleryState state,GallerySubmission submission,string reference,out string reason)
        {
            reason="";
            if(state==null||submission==null||state.submissions==null||!state.submissions.Contains(submission)){reason="找不到这份投稿记录。";return false;}
            if(submission.status!="packaged"&&submission.status!="pending"){reason="这份记录已登记递交或已有审核结果。";return false;}
            if(string.IsNullOrEmpty(reference)||reference.Trim().Length<4){reason="请填写递交给谁、通过什么方式，或粘贴投稿链接（至少 4 个字）。";return false;}
            submission.status="handed_over";submission.handedOverAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            submission.deliveryReference=reference.Trim();submission.note="由投稿者自行登记递交，未确认维护者收件。";
            WriteState(state);return true;
        }

        // 审核严格关联投稿编号。旧版仅有提供者编号的退回意见只匹配唯一旧 pending 记录。
        public static bool ApplyReview(GalleryState state,GalleryIndex index,out string summary)
        {
            summary="";if(state==null||index==null)return false;
            bool changed=false;int approved=0,rejected=0;
            foreach(var s in state.submissions??new List<GallerySubmission>())
            {
                if(s==null||s.status=="approved"||s.status=="rejected")continue;
                bool listed=false;
                foreach(var e in index.entries??new List<GalleryEntry>())
                    if(e!=null&&e.id==s.id&&e.contributorId==state.contributorId&&e.visible){listed=true;break;}
                if(listed){s.status="approved";s.note="维护者已收入共建图集。";changed=true;approved++;continue;}
                GalleryNote note=null;
                var notes=index.notes??new List<GalleryNote>();
                for(int i=notes.Count-1;i>=0;i--)
                {
                    var n=notes[i];if(n==null||n.contributorId!=state.contributorId)continue;
                    if(n.submissionId==s.id){note=n;break;}
                    if(s.status=="pending"&&string.IsNullOrEmpty(n.submissionId))
                    {
                        int legacy=0;foreach(var candidate in state.submissions)if(candidate!=null&&candidate.status=="pending")legacy++;
                        if(legacy==1){note=n;break;}
                    }
                }
                if(note!=null&&note.status=="rejected")
                {s.status="rejected";s.note=note.note??"请联系维护者了解修改要求。";changed=true;rejected++;}
            }
            if(changed){WriteState(state);summary="审核更新：收入 "+approved+" 份，退回 "+rejected+" 份。";}
            else if(state.HasPending)summary="暂未收到新的审核结果；递交记录由你自行登记。";
            return changed;
        }
    }
}
