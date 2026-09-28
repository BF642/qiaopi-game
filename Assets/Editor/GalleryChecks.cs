using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Qiaopi.Editor
{
    // 图集：内置索引、字段规则、投稿包、审核额度与驳回重投。
    public static class GalleryChecks
    {
        public static void Run()
        {
            CheckIndex();
            CheckRules();
            CheckFlow();
            Debug.Log("QIAOPI GALLERY CHECKS PASSED: 内置索引、字段规则、草稿、未递交/递交状态、逐份审核与旧记录兼容。");
        }

        static void CheckIndex()
        {
            var asset=Resources.Load<TextAsset>("Gallery/index");
            Require(asset!=null,"内置图集索引缺失：Assets/Resources/Gallery/index.json");
            var index=GalleryJson.Parse<GalleryIndex>(asset.text);
            Require(index!=null&&index.entries!=null&&index.entries.Count>=3,"内置索引至少收入三件藏品");
            var ids=new HashSet<string>();
            foreach(var entry in index.entries)
            {
                Require(entry!=null&&!string.IsNullOrEmpty(entry.id),"条目缺少 id");
                Require(ids.Add(entry.id),"条目 id 重复："+entry.id);
                string reason;
                Require(GalleryRules.Validate(entry,out reason),"内置条目不合规 "+entry.id+"："+reason);
                Require(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/"+entry.image+".png")!=null,"条目图片缺失："+entry.image);
            }
            Require(index.contributors!=null&&index.notes!=null,"索引需包含 contributors 与 notes");
            var merged=GalleryService.Merge(index,new GalleryIndex());
            Require(merged.Visible().Count==index.entries.Count,"合并后条目数应保持不变");
            var overlay=new GalleryIndex();
            overlay.contributors.Add("QZ-TEST0001");
            overlay.entries.Add(new GalleryEntry{id=index.entries[0].id,title="覆盖标题",category="批封",description="覆盖用的说明文字。",source="测试",contributor="测试",visible=true});
            var replaced=GalleryService.Merge(index,overlay);
            Require(replaced.Visible().Count==index.entries.Count,"同 id 条目应被覆盖而不是新增");
            Require(replaced.entries.Find(x=>x.id==index.entries[0].id).title=="覆盖标题","覆盖未生效");
            Require(replaced.IsApproved("QZ-TEST0001"),"提供者编号应被识别为已通过");
        }

        static void CheckRules()
        {
            string reason;
            Require(!GalleryRules.Validate(null,out reason),"空条目应被拒绝");
            Require(!GalleryRules.Validate(new GalleryEntry{title="x",category="侨批信",description="说明说明说明说明",source="来源",contributor="甲"},out reason),"过短题名应被拒绝");
            Require(!GalleryRules.Validate(new GalleryEntry{title="正常题名",category="不存在的类别",description="说明说明说明说明",source="来源",contributor="甲"},out reason),"未知类别应被拒绝");
            Require(!GalleryRules.Validate(new GalleryEntry{title="正常题名",category="侨批信",description="太短",source="来源",contributor="甲"},out reason),"过短说明应被拒绝");
            Require(!GalleryRules.Validate(new GalleryEntry{title="正常题名",category="侨批信",description="说明说明说明说明",source="",contributor="甲"},out reason),"缺少来源应被拒绝");
            Require(!GalleryRules.Validate(new GalleryEntry{title="正常题名",category="侨批信",description="说明说明说明说明",source="来源",contributor=""},out reason),"缺少署名应被拒绝");
            Require(GalleryRules.Validate(new GalleryEntry{title="正常题名",category="批封",description="说明说明说明说明",source="来源",contributor="甲"},out reason),"合规条目应通过");
            Require(!GalleryRules.ValidateFiles(new List<string>(),out reason),"空图片列表应被拒绝");
            Require(!GalleryRules.ValidateFiles(new List<string>{"/tmp/不存在.png"},out reason),"不存在的文件应被拒绝");
            Require(!GalleryRules.ValidateFiles(new List<string>{"/tmp/说明.txt"},out reason),"非图片文件应被拒绝");
            Require(GalleryRules.Categories.Length>=8,"类别应覆盖侨批与相关材料");
        }

        static void CheckFlow()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../work/gallery-checks"));
            GalleryStore.RootOverride=root;
            try
            {
                if(Directory.Exists(root))Directory.Delete(root,true);
                GalleryStore.EnsureFolders();
                File.WriteAllBytes(Path.Combine(GalleryStore.InboxDir,"来信-1.png"),Png(64,48,new Color(.90f,.85f,.70f)));
                File.WriteAllBytes(Path.Combine(GalleryStore.InboxDir,"回批-2.png"),Png(48,64,new Color(.70f,.80f,.85f)));
                File.WriteAllBytes(Path.Combine(GalleryStore.InboxDir,"说明.txt"),Encoding.UTF8.GetBytes("这不是图片"));

                var state=GalleryStore.LoadState();
                Require(state.contributorId.StartsWith("QZ-"),"应生成提供者编号");
                var inbox=GalleryStore.ScanInbox(state);
                Require(inbox.Count==2,"投稿目录应只列出两张图片，实际 "+inbox.Count);
                string quota;
                Require(GalleryStore.CanSubmit(state,null,out quota),"首次投稿应无需审核即可提交");

                string error;
                Require(GalleryStore.CreateSubmission(state,new GalleryEntry(),inbox,out error)==null,"空条目不应生成投稿包");
                Require(GalleryStore.CreateSubmission(state,new GalleryEntry{title="缺类别",description="说明说明说明说明",source="来源",contributor="甲"},inbox,out error)==null,"缺类别不应生成投稿包");

                var draft=new GalleryEntry{title="测试投稿 · 泉州批封",category="批封",year="1906",place="泉州",description="这是一段用于校验投稿包生成与审核额度的说明文字。",source="家中旧物，已获家人同意公开",contributor="测试者"};
                var saved=GalleryStore.CreateSubmission(state,draft,inbox,out error,true);
                Require(saved!=null,"投稿包应生成："+error);
                string dir=saved.PackageDir(GalleryStore.Root);
                Require(File.Exists(Path.Combine(dir,"entry.json")),"投稿包缺少 entry.json");
                Require(File.Exists(Path.Combine(dir,"投稿说明.txt")),"投稿包缺少投稿说明.txt");
                Require(saved.files.Count==2&&saved.hashes.Count==2,"投稿包应记录两张图片与哈希");
                Require(saved.hashes[0].Length==64,"哈希应为 sha256");
                var roundtrip=GalleryJson.Parse<GallerySubmission>(File.ReadAllText(Path.Combine(dir,"entry.json"),Encoding.UTF8));
                Require(roundtrip!=null&&roundtrip.id==saved.id&&roundtrip.category=="批封","entry.json 应能读回");
                Require(GalleryStore.ScanInbox(state).Count==0,"已收进投稿包的文件不应再次列出");
                Require(saved.status=="packaged","本机打包不能被标成待审核");
                Require(GalleryStore.CanSubmit(state,null,out quota),"未递交的本地包不能锁死继续准备");
                Require(!state.HasPending,"未递交不应计为待审");
                Require(!GalleryStore.MarkHandedOver(state,saved,"",out error),"没有递交凭据不能登记已递交");
                Require(GalleryStore.MarkHandedOver(state,saved,"已当面交给维护者（自检）",out error),"应能登记手动递交");
                Require(saved.status=="handed_over"&&state.HasPending,"手动登记后单独标记待审");
                Require(!GalleryStore.MarkHandedOver(state,saved,"再次记录",out error),"不重复登记递交");
                state.draft=new GalleryDraft{entry=new GalleryEntry{title="尚未生成包的草稿"},consent=true,step=2};
                GalleryStore.WriteState(state);
                Require(GalleryStore.LoadState().draft.entry.title=="尚未生成包的草稿","草稿应单独保存");
                Require(GalleryStore.LoadState().submissions.Count==1,"投稿记录应写入状态文件");

                var approved=new GalleryIndex();
                approved.contributors.Add(state.contributorId);
                approved.entries.Add(new GalleryEntry{id=saved.id,title=saved.title,contributorId=state.contributorId,visible=true});
                string summary;
                GalleryStore.ApplyReview(state,approved,out summary);
                Require(state.submissions[0].status=="approved","审核通过后应标记为已通过："+state.submissions[0].status);
                Require(GalleryStore.CanSubmit(state,approved,out quota),"通过审核后应可继续投稿");

                var second=GalleryStore.CreateSubmission(state,new GalleryEntry{title="第二份投稿 · 回批",category="回批",description="第二份用于校验驳回与重投流程的说明文字。",source="同乡提供，同意公开",contributor="测试者"},inbox,out error);
                Require(second!=null,"通过后应能提交第二份："+error);
                var rejected=new GalleryIndex();
                rejected.notes.Add(new GalleryNote{contributorId=state.contributorId,submissionId=second.id,status="rejected",note="图片偏模糊，请换一张更清楚的"});
                var unrelated=GalleryStore.CreateSubmission(state,new GalleryEntry{title="另一份仍在准备的材料",category="批封",description="这份不应被别的投稿审核意见牵连。",source="测试授权",contributor="测试者"},inbox,out error,true);
                GalleryStore.ApplyReview(state,rejected,out summary);
                Require(unrelated.status=="packaged","退回意见不能牵连同一提供者的其他投稿");
                Require(second.status=="rejected","驳回意见应写回投稿记录");
                Require(second.note.Contains("模糊"),"驳回原因应保留");
                Require(GalleryStore.CanSubmit(state,rejected,out quota),"被驳回后应允许改好再投一次");

                var legacy=new GalleryState{contributorId="QZ-LEGACY"};
                legacy.submissions.Add(new GallerySubmission{id="GJ-LEGACY",status="pending",title="旧版材料"});
                Require(GalleryRules.StatusLabel(legacy.submissions[0].status).Contains("待核对"),"旧pending必须明确需核对，不能自动算已递交");
                Require(!legacy.HasPending&&GalleryStore.CanSubmit(legacy,null,out quota),"旧pending不能锁死本地草稿准备");
                var legacyIndex=new GalleryIndex();
                legacyIndex.notes.Add(new GalleryNote{contributorId=legacy.contributorId,status="rejected",note="旧版审核意见"});
                GalleryStore.ApplyReview(legacy,legacyIndex,out summary);
                Require(legacy.submissions[0].status=="rejected","单份旧记录应兼容旧版审核意见");

                var repoIndex=new GalleryIndex{repo="https://github.com/example/qiaopi-gallery"};
                Require(string.IsNullOrEmpty(GalleryService.SubmissionIssueUrl(new GalleryIndex(),saved)),"未配置仓库时不应生成提交链接");
                Require(GalleryService.RepoUrl(repoIndex)=="https://github.com/example/qiaopi-gallery","仓库地址应被规范化");
                Require(GalleryService.SubmissionIssueUrl(repoIndex,saved).Contains("issues/new"),"配置仓库后应生成 issue 提交链接");
                Require(GalleryService.RepoUrl(new GalleryIndex{repo="example/qiaopi-gallery"})=="https://github.com/example/qiaopi-gallery","短仓库地址应补全");
            }
            finally
            {
                GalleryStore.RootOverride="";
                if(Directory.Exists(root))Directory.Delete(root,true);
            }
        }

        static byte[] Png(int w,int h,Color color)
        {
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
            var pixels=new Color[w*h];
            for(int i=0;i<pixels.Length;i++)pixels[i]=color;
            texture.SetPixels(pixels);texture.Apply();
            byte[] bytes=texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }
        static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException("QIAOPI GALLERY CHECK FAILED: "+message);
        }
    }
}
