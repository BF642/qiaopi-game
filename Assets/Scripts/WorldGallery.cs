using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Qiaopi
{
    // 侨批图集 · 共建：浏览已通过审核的藏品，并把玩家手里的材料做成投稿包。
    public partial class WorldGame
    {
        bool gallery;
        int galleryTab;
        Vector2 galleryScroll,galleryDetailScroll,galleryFilesScroll;
        int galleryStep,galleryDragFrame=-1;
        string galleryDraftSnapshot="",galleryFolderHelp="",galleryDeliveryReference="";
        GallerySubmission galleryDelivery;
        Rect galleryBody;
        GalleryState galleryState;
        GalleryIndex galleryIndex;
        GalleryEntry gallerySelected;
        string galleryFilter="全部";
        string galleryStatus="",galleryRemoteNote="";
        bool galleryFetching;
        List<string> galleryInbox=new List<string>();
        readonly List<string> galleryPicked=new List<string>();
        bool galleryConsent;
        GalleryEntry galleryDraft=new GalleryEntry();
        GUIStyle fieldStyle,areaStyle;

        bool GalleryTyping{get{return gallery&&!string.IsNullOrEmpty(GUI.GetNameOfFocusedControl());}}

        void OpenGallery()
        {
            gallery=true;galleryTab=0;gallerySelected=null;galleryStatus="";journal=map=help=false;
            GalleryStore.EnsureFolders();
            galleryIndex=GalleryService.Merge(GalleryService.Bundled(),GalleryService.Cached());
            galleryState=GalleryStore.LoadState();
            galleryInbox=GalleryStore.ScanInbox(galleryState);
            galleryDraft=galleryState.draft.entry;galleryConsent=galleryState.draft.consent;galleryStep=Mathf.Clamp(galleryState.draft.step,0,3);
            galleryPicked.Clear();
            foreach(string f in galleryState.draft.files){string path=System.IO.Path.Combine(GalleryStore.Root,f);if(System.IO.File.Exists(path)){galleryPicked.Add(path);if(!galleryInbox.Contains(path))galleryInbox.Add(path);}}
            galleryDraftSnapshot="";galleryFolderHelp="";galleryDelivery=null;
            string note;GalleryStore.ApplyReview(galleryState,galleryIndex,out note);galleryRemoteNote=note;
            string url=GalleryService.IndexUrl(galleryIndex);
            if(!string.IsNullOrEmpty(url)&&!galleryFetching)StartCoroutine(FetchGalleryIndex(url));
            Sound();
        }

        IEnumerator FetchGalleryIndex(string url)
        {
            galleryFetching=true;galleryRemoteNote="正在更新共建图集…";
            using(var request=UnityWebRequest.Get(url))
            {
                request.timeout=15;
                yield return request.SendWebRequest();
                if(request.result==UnityWebRequest.Result.Success)
                {
                    var remote=GalleryJson.Parse<GalleryIndex>(request.downloadHandler.text);
                    if(remote!=null&&remote.entries!=null)
                    {
                        GalleryService.WriteCache(remote);
                        galleryIndex=GalleryService.Merge(GalleryService.Bundled(),remote);
                        string note;GalleryStore.ApplyReview(galleryState,galleryIndex,out note);
                        galleryRemoteNote="共建图集已更新 · 共 "+galleryIndex.Visible().Count+" 件"+(string.IsNullOrEmpty(galleryIndex.updated)?"":" · "+galleryIndex.updated);
                    }
                    else galleryRemoteNote="线上索引格式不对，先看本机与缓存里的图集。";
                }
                else galleryRemoteNote="现在连不上共建图集，先看本机与缓存里的内容。";
            }
            galleryFetching=false;
        }

        void RequestImage(GalleryEntry entry)
        {
            bool loading;
            var texture=GalleryService.Texture(entry,out loading);
            if(texture==null&&loading&&!GalleryService.IsPending(entry.id))
            {
                GalleryService.MarkPending(entry.id);
                StartCoroutine(FetchImage(entry));
            }
        }
        IEnumerator FetchImage(GalleryEntry entry)
        {
            string url=GalleryService.ImageUrl(galleryIndex,entry);
            if(string.IsNullOrEmpty(url))yield break;
            using(var request=UnityWebRequest.Get(url))
            {
                request.timeout=20;
                yield return request.SendWebRequest();
                if(request.result==UnityWebRequest.Result.Success)GalleryService.StoreRemoteImage(entry.id,request.downloadHandler.data);
            }
        }

        // 纸面上的大字号分页，手机与电脑共用同一份真实投稿流程。
        void GalleryPanel()
        {
            Rect safe=MobileControls?MobileUiBounds():new Rect(0,0,1600,1000);
            Rect panel=MobileControls?new Rect(safe.x+30,153,safe.width-60,808):new Rect(115,132,1370,808);
            Box(new Rect(safe.xMin-100,-100,safe.width+200,1200),new Color(.08f,.16f,.13f,.34f));
            PaperPanel(panel);Stamp(new Rect(panel.x+32,panel.y+26,66,66),"共藏",26);
            Label(new Rect(panel.x+118,panel.y+23,panel.width-300,65),"侨批图集 · 一纸共藏",38,ink,true);
            if(LetterButton(new Rect(panel.xMax-182,panel.y+25,148,70),"收起",false,28)){SaveGalleryDraft();gallery=false;GUI.FocusControl(null);return;}
            string[] tabs={"看图集","准备投稿","我的投稿"};float tabWidth=(panel.width-88)/3;
            for(int i=0;i<3;i++)if(LetterButton(new Rect(panel.x+32+i*(tabWidth+12),panel.y+111,tabWidth,64),tabs[i],galleryTab==i,28))
            {galleryTab=i;gallerySelected=null;galleryScroll=Vector2.zero;galleryFolderHelp="";galleryDelivery=null;GUI.FocusControl(null);}
            galleryBody=new Rect(panel.x+36,panel.y+197,panel.width-72,panel.height-222);
            if(!string.IsNullOrEmpty(galleryFolderHelp))GalleryFolderHelp();
            else if(galleryDelivery!=null)GalleryDeliveryForm();
            else if(galleryTab==0&&gallerySelected!=null)GalleryDetail();
            else if(galleryTab==0)GalleryEntries();
            else if(galleryTab==1)GallerySubmit();
            else GalleryMine();
            SaveGalleryDraft();
        }

        void SaveGalleryDraft()
        {
            if(galleryState==null)return;
            var draft=new GalleryDraft{entry=galleryDraft,consent=galleryConsent,step=galleryStep};
            foreach(string file in galleryPicked)
            {
                string prefix=GalleryStore.Root+System.IO.Path.DirectorySeparatorChar;
                if(file.StartsWith(prefix,StringComparison.Ordinal))draft.files.Add(file.Substring(prefix.Length));
            }
            string current=GalleryJson.Write(draft,false);if(current==galleryDraftSnapshot)return;
            draft.updatedAt=DateTime.Now.ToString("MM-dd HH:mm");galleryState.draft=draft;
            try{GalleryStore.WriteState(galleryState);galleryDraftSnapshot=current;}
            catch(Exception e){galleryStatus="草稿暂未保存："+e.Message;}
        }

        // Unity IMGUI 的滚动条保留给鼠标；触屏可直接在内容区上下滑动。
        void GalleryTouchScroll(Rect viewport,ref Vector2 scroll,float contentHeight)
        {
            if(!MobileControls||Event.current.type!=EventType.Repaint||galleryDragFrame==Time.frameCount)return;
            for(int i=0;i<Input.touchCount;i++)
            {
                Touch touch=Input.GetTouch(i);if(touch.phase!=TouchPhase.Moved)continue;
                Vector2 point=TouchUiPoint(touch.position);
                if(!viewport.Contains(point))continue;
                float k,x,y;GetUiMetrics(out k,out x,out y);
                scroll.y=Mathf.Clamp(scroll.y+touch.deltaPosition.y/k,0,Mathf.Max(0,contentHeight-viewport.height));
                galleryDragFrame=Time.frameCount;break;
            }
        }

        GUIStyle FieldStyle()
        {
            if(fieldStyle==null)
            {
                fieldStyle=new GUIStyle(style){fontSize=30,wordWrap=false,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,12,4,4)};
                fieldStyle.normal.textColor=ink;fieldStyle.focused.textColor=ink;fieldStyle.hover.textColor=ink;
                fieldStyle.normal.background=null;fieldStyle.focused.background=null;fieldStyle.hover.background=null;fieldStyle.active.background=null;
            }
            return fieldStyle;
        }
        GUIStyle AreaStyle()
        {
            if(areaStyle==null)
            {
                areaStyle=new GUIStyle(style){fontSize=29,wordWrap=true,alignment=TextAnchor.UpperLeft,padding=new RectOffset(12,10,8,8)};
                areaStyle.normal.textColor=ink;areaStyle.focused.textColor=ink;areaStyle.hover.textColor=ink;
                areaStyle.normal.background=null;areaStyle.focused.background=null;areaStyle.hover.background=null;areaStyle.active.background=null;
            }
            return areaStyle;
        }
        string GalleryField(Rect r,string control,string value,int max)
        {
            Box(r,C("EFE7D2"));Stroke(r,line);
            GUI.SetNextControlName(control);
            return GUI.TextField(new Rect(r.x+2,r.y+3,r.width-4,r.height-6),value??"",max,FieldStyle());
        }
        string GalleryArea(Rect r,string control,string value,int max)
        {
            Box(r,C("EFE7D2"));Stroke(r,line);
            GUI.SetNextControlName(control);
            return GUI.TextArea(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),value??"",max,AreaStyle());
        }
        bool Chip(Rect r,string text,bool active)
        {
            Box(r,active?C("40665E"):(r.Contains(Event.current.mousePosition)?C("E4DDC9"):C("EFE7D2")));
            Stroke(r,active?C("40665E"):line);
            Label(r,text,26,active?paper:ink,false,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,blank);
        }
        void GalleryImage(Rect r,GalleryEntry entry)
        {
            Box(r,C("DFD5BD"));Stroke(r,line);
            RequestImage(entry);
            bool loading;
            var texture=GalleryService.Texture(entry,out loading);
            if(texture!=null)
            {
                float scale=Mathf.Min(r.width/texture.width,r.height/texture.height);
                float w=texture.width*scale,h=texture.height*scale;
                GUI.DrawTexture(new Rect(r.x+(r.width-w)*.5f,r.y+(r.height-h)*.5f,w,h),texture,ScaleMode.StretchToFill,true);
            }
            else Label(r,loading?"图片载入中…":"暂无图片",16,sub,false,TextAnchor.MiddleCenter);
        }
        string EntryMeta(GalleryEntry entry)
        {
            var parts=new List<string>();
            if(!string.IsNullOrEmpty(entry.category))parts.Add(entry.category);
            if(!string.IsNullOrEmpty(entry.year))parts.Add(entry.year);
            if(!string.IsNullOrEmpty(entry.place))parts.Add(entry.place);
            if(!string.IsNullOrEmpty(entry.sample))parts.Add(entry.sample);
            return string.Join(" · ",parts.ToArray());
        }

        // ── 图集 ────────────────────────────────────────────────────────────
        void GalleryEntries()
        {
            Rect b=galleryBody;var all=galleryIndex.Visible();
            string[] filters=new string[GalleryRules.Categories.Length+1];filters[0]="全部";
            Array.Copy(GalleryRules.Categories,0,filters,1,GalleryRules.Categories.Length);
            float chipWidth=(b.width-48)/5;
            for(int i=0;i<filters.Length;i++)if(Chip(new Rect(b.x+i%5*(chipWidth+12),b.y+i/5*56,chipWidth,48),filters[i],galleryFilter==filters[i])){galleryFilter=filters[i];galleryScroll=Vector2.zero;}
            var shown=new List<GalleryEntry>();foreach(var entry in all)if(galleryFilter=="全部"||entry.category==galleryFilter)shown.Add(entry);
            string origin=string.IsNullOrEmpty(GalleryService.IndexUrl(galleryIndex))?"内置图集 · 示意样张均已标明，非历史原件":"共建图集 · "+(galleryFetching?"正在刷新…":galleryRemoteNote);
            Label(new Rect(b.x,b.y+116,b.width,44),origin,25,sub);
            if(shown.Count==0){Label(new Rect(b.x+30,b.y+220,b.width-60,180),"这一类还没有藏品。\n你可以在「准备投稿」里整理手中的材料。",32,sub,true,TextAnchor.MiddleCenter);return;}
            Rect view=new Rect(b.x,b.y+168,b.width,b.height-168);float height=shown.Count*225;
            GalleryTouchScroll(view,ref galleryScroll,height);
            galleryScroll=GUI.BeginScrollView(view,galleryScroll,new Rect(0,0,b.width-30,height),false,false);
            float y=0;
            foreach(var entry in shown)
            {
                Rect card=new Rect(0,y,b.width-42,207);PaperPanel(card);
                GalleryImage(new Rect(16,y+16,240,175),entry);
                float textX=278,textW=card.width-300;
                Label(new Rect(textX,y+15,textW,76),entry.title,31,ink,true);
                Label(new Rect(textX,y+98,textW,64),EntryMeta(entry),26,red);
                Label(new Rect(textX,y+165,textW,32),"由 "+(entry.contributor??"佚名")+" 提供 · 点开读批",23,sub);
                if(GUI.Button(card,GUIContent.none,blank)){gallerySelected=entry;galleryDetailScroll=Vector2.zero;Sound();}
                y+=225;
            }
            GUI.EndScrollView();
        }

        void GalleryDetail()
        {
            Rect b=galleryBody;var entry=gallerySelected;float imageWidth=b.width*.43f,right=b.x+imageWidth+36,textWidth=b.width-imageWidth-36;
            GalleryImage(new Rect(b.x,b.y,imageWidth,b.height-104),entry);
            float bodyHeight=Height(entry.description??"",30,textWidth-30,true);
            float titleHeight=Height(entry.title,35,textWidth-30,true);
            string source="来源与授权："+(string.IsNullOrEmpty(entry.source)?"未注明":entry.source);
            float sourceHeight=Height(source,27,textWidth-30,true);
            float contentHeight=titleHeight+bodyHeight+sourceHeight+220;
            Rect view=new Rect(right,b.y,textWidth,b.height-104);GalleryTouchScroll(view,ref galleryDetailScroll,contentHeight);
            galleryDetailScroll=GUI.BeginScrollView(view,galleryDetailScroll,new Rect(0,0,textWidth-30,contentHeight),false,false);
            Label(new Rect(0,0,textWidth-30,titleHeight+6),entry.title,35,ink,true);
            Label(new Rect(0,titleHeight+18,textWidth-30,80),EntryMeta(entry),25,red);
            Label(new Rect(0,titleHeight+101,textWidth-30,44),"提供者："+(entry.contributor??"佚名"),26,sub);
            Label(new Rect(0,titleHeight+153,textWidth-30,bodyHeight+10),entry.description??"",30,ink,true);
            Label(new Rect(0,titleHeight+bodyHeight+178,textWidth-30,sourceHeight+10),source,27,sub,true);
            GUI.EndScrollView();
            if(LetterButton(new Rect(b.x,b.yMax-78,270,72),"返回图集",false,28))gallerySelected=null;
            string imageUrl=GalleryService.ImageUrl(galleryIndex,entry);
            if(!string.IsNullOrEmpty(imageUrl)&&LetterButton(new Rect(b.x+292,b.yMax-78,320,72),"查看原图",false,28))GalleryStore.OpenUrl(imageUrl);
            Label(new Rect(b.x+630,b.yMax-76,b.width-640,72),"上下滑动，可读完整说明。",25,sub,false,TextAnchor.MiddleRight);
        }

        // ── 准备投稿：四步只做本地整理，生成后再清楚说明如何递交 ──────────
        void GallerySubmit()
        {
            Rect b=galleryBody;string[] steps={"一 · 选材料","二 · 写题名","三 · 说明与授权","四 · 检查成包"};
            float sw=(b.width-36)/4;
            for(int i=0;i<4;i++)if(Chip(new Rect(b.x+i*(sw+12),b.y,sw,52),steps[i],galleryStep==i)){galleryStep=i;GUI.FocusControl(null);}
            if(galleryStep==0)GalleryMaterials();else if(galleryStep==1)GalleryMetadata();else if(galleryStep==2)GalleryRights();else GalleryPackageReview();
            float by=b.yMax-75;
            if(galleryStep>0&&LetterButton(new Rect(b.x,by,210,70),"上一步",false,28)){galleryStep--;GUI.FocusControl(null);}
            if(galleryStep<3&&LetterButton(new Rect(b.xMax-260,by,260,70),"下一步",true,30)){galleryStep++;GUI.FocusControl(null);}
            if(galleryStep<3)Label(new Rect(b.x+235,by+2,b.width-520,68),"草稿自动保存在这台设备\n生成投稿包后仍需手动递交",24,sub,false,TextAnchor.MiddleCenter);
        }

        void GalleryMaterials()
        {
            Rect b=galleryBody;float half=(b.width-40)*.5f,right=b.x+half+40,y=b.y+77;
            Label(new Rect(b.x,y,half,46),"先把材料放进来",32,ink,true);
            string how=MobileControls?"在「文件」App → 我的 iPhone → 侨批·纸短情长 → gallery → 投稿，存入图片，再回这里扫描。":"点下方打开投稿文件夹，把图片放进去，再回游戏扫描。";
            Label(new Rect(b.x,y+57,half,164),how,29,ink,true);
            Label(new Rect(b.x,y+231,half,129),"收：侨批、回批、批封、汇票、老照片等。\n1–8 张 PNG / JPG / WebP，每张不超过 8 MB。请拍清文字，遮去不愿公开的个人资料。",25,sub,true);
            if(LetterButton(new Rect(b.x,y+363,half,68),MobileControls?"查看文件 App 存放步骤":"打开投稿文件夹",false,27))OpenGalleryFolder(GalleryStore.InboxDir);
            Label(new Rect(right,y,half,46),"已选 "+galleryPicked.Count+" / 8 张",31,ink,true);
            if(LetterButton(new Rect(right+half-215,y-4,215,64),"重新扫描",false,27))
            {
                galleryInbox=GalleryStore.ScanInbox(galleryState);galleryPicked.RemoveAll(f=>!System.IO.File.Exists(f));
                foreach(string f in galleryPicked)if(!galleryInbox.Contains(f))galleryInbox.Add(f);
                galleryStatus="扫描到 "+galleryInbox.Count+" 张，请点击要使用的图片。";
            }
            Rect view=new Rect(right,y+76,half,354);float height=Mathf.Max(view.height,galleryInbox.Count*85);
            GalleryTouchScroll(view,ref galleryFilesScroll,height);
            galleryFilesScroll=GUI.BeginScrollView(view,galleryFilesScroll,new Rect(0,0,half-30,height),false,false);
            if(galleryInbox.Count==0)Label(new Rect(12,60,half-54,210),"还没有图片。\n放入后点「重新扫描」，\n再点图片名称勾选。",29,sub,true,TextAnchor.MiddleCenter);
            for(int i=0;i<galleryInbox.Count;i++)
            {
                string path=galleryInbox[i];bool picked=galleryPicked.Contains(path);string title=(picked?"已选 · ":"选择 · ")+System.IO.Path.GetFileName(path);
                if(Chip(new Rect(0,i*85,half-40,73),title,picked))
                {if(picked)galleryPicked.Remove(path);else if(galleryPicked.Count<GalleryRules.MaxImages)galleryPicked.Add(path);else Toast("每份最多 8 张，请先取消一张。");}
            }
            GUI.EndScrollView();
        }

        void GalleryMetadata()
        {
            Rect b=galleryBody;float y=b.y+77,half=(b.width-34)*.5f;
            Label(new Rect(b.x,y,b.width,38),"题名 *　例如：阿公从石叻寄回泉州的一封批",27,sub);
            galleryDraft.title=GalleryField(new Rect(b.x,y+45,b.width,65),"gallery-title",galleryDraft.title,40);
            Label(new Rect(b.x,y+127,b.width,39),"材料类别 *",27,sub);
            float cw=(b.width-36)/4;
            for(int i=0;i<GalleryRules.Categories.Length;i++)if(Chip(new Rect(b.x+i%4*(cw+12),y+175+i/4*61,cw,51),GalleryRules.Categories[i],galleryDraft.category==GalleryRules.Categories[i]))galleryDraft.category=GalleryRules.Categories[i];
            Label(new Rect(b.x,y+309,half,35),"年代（不确定可留空）",26,sub);Label(new Rect(b.x+half+34,y+309,half,35),"地点 / 往返路线（选填）",26,sub);
            galleryDraft.year=GalleryField(new Rect(b.x,y+350,half,65),"gallery-year",galleryDraft.year,20);
            galleryDraft.place=GalleryField(new Rect(b.x+half+34,y+350,half,65),"gallery-place",galleryDraft.place,24);
        }

        void GalleryRights()
        {
            Rect b=galleryBody;float half=(b.width-38)*.5f,right=b.x+half+38,y=b.y+77;
            Label(new Rect(b.x,y,half,38),"材料说明 *（至少 8 个字）",28,ink,true);
            Label(new Rect(right,y,half,38),"来源与授权 *",28,ink,true);
            Label(new Rect(b.x,y+43,half,76),"写下谁寄给谁、与泉州的联系，\n以及你知道的这段家族记忆。",25,sub);
            Label(new Rect(right,y+43,half,76),"写清持有人与授权人；转载材料\n请注明原出处及允许公开的依据。",25,sub);
            galleryDraft.description=GalleryArea(new Rect(b.x,y+129,half,145),"gallery-desc",galleryDraft.description,600);
            galleryDraft.source=GalleryArea(new Rect(right,y+129,half,145),"gallery-source",galleryDraft.source,240);
            Label(new Rect(b.x,y+292,146,64),"公开署名 *",26,sub,false,TextAnchor.MiddleLeft);
            galleryDraft.contributor=GalleryField(new Rect(b.x+160,y+292,b.width-160,64),"gallery-name",galleryDraft.contributor,24);
            if(LetterButton(new Rect(b.x,y+361,b.width,62),(galleryConsent?"已确认 · ":"点此确认 · ")+"我有权公开这些材料，并同意以以上署名收入图集",galleryConsent,26))galleryConsent=!galleryConsent;
        }

        bool GalleryReady(out string reason)
        {
            if(!GalleryRules.Validate(galleryDraft,out reason)||!GalleryRules.ValidateFiles(galleryPicked,out reason))return false;
            if(!galleryConsent){reason="请在第三步确认材料的公开授权。";return false;}
            return GalleryStore.CanSubmit(galleryState,galleryIndex,out reason);
        }

        void GalleryPackageReview()
        {
            Rect b=galleryBody;float y=b.y+80;
            Label(new Rect(b.x,y,b.width,54),string.IsNullOrEmpty(galleryDraft.title)?"请先补齐题名":galleryDraft.title,35,ink,true);
            Label(new Rect(b.x,y+66,b.width,45),(galleryDraft.category??"未选类别")+" · "+galleryPicked.Count+" 张图片 · 署名："+(galleryDraft.contributor??"未填"),29,sub);
            PaperPanel(new Rect(b.x,y+132,b.width,168));
            Label(new Rect(b.x+26,y+150,b.width-52,130),"生成后得到：图片 + 材料说明 + 授权信息。\n这一步只保存在本机，不会上传，也不代表维护者已收件。\n接下来请在「我的投稿」里查看投稿包和递交方法。",29,ink,true);
            string reason;bool ready=GalleryReady(out reason);
            Label(new Rect(b.x,y+329,b.width,78),ready?"资料齐全。生成后会保留一份本机记录。":reason,28,ready?sub:red,true);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&ready;
            if(LetterButton(new Rect(b.xMax-540,b.yMax-75,540,70),"生成投稿包 · 尚未递交",true,29))CreateGallerySubmission();
            GUI.enabled=enabled;
        }

        void CreateGallerySubmission()
        {
            string error;if(!GalleryReady(out error)){galleryStatus=error;Toast(error);return;}
            GallerySubmission saved;
            try{saved=GalleryStore.CreateSubmission(galleryState,galleryDraft,galleryPicked,out error,galleryConsent);}
            catch(Exception e){galleryStatus="生成失败，草稿仍保留："+e.Message;Toast(galleryStatus);return;}
            if(saved==null){galleryStatus=error;Toast(error);return;}
            galleryInbox=GalleryStore.ScanInbox(galleryState);galleryPicked.Clear();galleryDraft=new GalleryEntry();
            galleryConsent=false;galleryStep=0;galleryTab=2;galleryScroll=Vector2.zero;
            galleryStatus="已打包，还没有递交。请把投稿包交给维护者。";SaveGalleryDraft();Toast(galleryStatus);Sound();
        }

        // ── 我的投稿：本机文件与实际递交、审核分开显示 ─────────────────────
        void GalleryMine()
        {
            Rect b=galleryBody;int packaged=0,delivered=0,approved=0;
            foreach(var s in galleryState.submissions){if(s.status=="packaged"||s.status=="pending")packaged++;else if(s.status=="handed_over")delivered++;else if(s.status=="approved")approved++;}
            Label(new Rect(b.x,b.y,b.width-310,46),"未递交 / 待核对 "+packaged+"　已记递交 "+delivered+"　收入图集 "+approved,29,ink,true);
            if(LetterButton(new Rect(b.xMax-292,b.y-3,292,65),"刷新审核结果",false,27))
            {
                string url=GalleryService.IndexUrl(galleryIndex);
                if(string.IsNullOrEmpty(url))galleryStatus="尚未接通在线审核，请向维护者索取结果；本机不会自动显示已通过。";
                else if(!galleryFetching)StartCoroutine(FetchGalleryIndex(url));
            }
            bool configured=!string.IsNullOrEmpty(GalleryService.RepoUrl(galleryIndex));
            string route=configured?"递交方式：打开收件网页，手动附上完整投稿包。网页打开不会自动上传。":"当前没有在线收件入口。请手动把完整投稿包交给游戏维护者；保存到本机不算投稿成功。";
            Label(new Rect(b.x,b.y+74,b.width,81),string.IsNullOrEmpty(galleryStatus)?route:galleryStatus,27,sub,true);
            if(galleryState.submissions.Count==0)
            {Label(new Rect(b.x+40,b.y+227,b.width-80,176),"还没有生成的投稿包。\n草稿留在「准备投稿」，四步填完后可生成。",32,sub,true,TextAnchor.MiddleCenter);return;}
            Rect view=new Rect(b.x,b.y+174,b.width,b.height-183);float height=galleryState.submissions.Count*346;
            GalleryTouchScroll(view,ref galleryScroll,height);
            galleryScroll=GUI.BeginScrollView(view,galleryScroll,new Rect(0,0,b.width-30,height),false,false);
            float y=0;
            for(int i=galleryState.submissions.Count-1;i>=0;i--)
            {
                var s=galleryState.submissions[i];float w=b.width-44;PaperPanel(new Rect(0,y,w,328));
                Label(new Rect(20,y+18,w-360,75),s.title,32,ink,true);
                Stamp(new Rect(w-326,y+19,302,55),GalleryRules.StatusLabel(s.status),25);
                Label(new Rect(20,y+103,w-40,37),s.category+" · "+s.files.Count+" 张 · "+s.createdAt+" · "+s.id,24,sub);
                string note=s.status=="pending"?"旧版曾写作“待审”，但没有递交凭据。请核对是否真正交给维护者。":(s.note??"");
                if(s.status=="handed_over")note+=" 递交记录："+s.deliveryReference;
                Label(new Rect(20,y+151,w-40,90),note,27,s.status=="rejected"?red:sub,true);
                float buttonWidth=(w-64)/3;
                if(LetterButton(new Rect(20,y+249,buttonWidth,62),MobileControls?"查看投稿包位置":"打开投稿包",false,26))OpenGalleryFolder(s.PackageDir(GalleryStore.Root));
                if((s.status=="packaged"||s.status=="pending")&&LetterButton(new Rect(32+buttonWidth,y+249,buttonWidth,62),"我已交给维护者",true,26))
                {galleryDelivery=s;galleryDeliveryReference="";GUI.FocusControl(null);}
                else if(s.status=="rejected"&&LetterButton(new Rect(32+buttonWidth,y+249,buttonWidth,62),"按意见修改草稿",true,26))GalleryRevise(s);
                if(configured&&LetterButton(new Rect(44+buttonWidth*2,y+249,buttonWidth,62),"打开收件网页",false,26))GalleryStore.OpenUrl(GalleryService.SubmissionIssueUrl(galleryIndex,s));
                y+=346;
            }
            GUI.EndScrollView();
        }

        void GalleryRevise(GallerySubmission s)
        {
            galleryDraft=new GalleryEntry{title=s.title,category=s.category,year=s.year,place=s.place,contributor=s.contributor,description=s.description,source=s.source};
            galleryPicked.Clear();foreach(string file in s.files){string path=System.IO.Path.Combine(s.PackageDir(GalleryStore.Root),file);if(System.IO.File.Exists(path)){galleryPicked.Add(path);if(!galleryInbox.Contains(path))galleryInbox.Add(path);}}
            galleryConsent=false;galleryStep=2;galleryTab=1;galleryStatus="已复制为草稿，请按意见修改并重新确认授权。";
        }

        void GalleryDeliveryForm()
        {
            Rect b=galleryBody;
            Stamp(new Rect(b.x,b.y,248,57),"手动登记递交",25);
            Label(new Rect(b.x,b.y+82,b.width,105),"只有已通过其他方式把投稿包交给维护者后，才填写这一步。\n这里仅保存你的递交记录，不会发送文件，也不会确认对方已收件。",30,ink,true);
            Label(new Rect(b.x,b.y+216,b.width,48),"交给谁 / 递交方式 / 帖子链接 *",29,sub);
            galleryDeliveryReference=GalleryArea(new Rect(b.x,b.y+274,b.width,129),"gallery-delivery",galleryDeliveryReference,240);
            Label(new Rect(b.x,b.y+421,b.width,65),"例如：9 月 27 日，已当面把完整投稿包交给图集维护者。",25,sub,true);
            if(LetterButton(new Rect(b.x,b.yMax-77,260,70),"尚未递交 · 返回",false,27)){galleryDelivery=null;GUI.FocusControl(null);}
            if(LetterButton(new Rect(b.xMax-370,b.yMax-77,370,70),"保存我的递交记录",true,28))
            {
                string reason;if(GalleryStore.MarkHandedOver(galleryState,galleryDelivery,galleryDeliveryReference,out reason))
                {galleryDelivery=null;galleryStatus="递交记录已保存。仍需维护者收件并审核。";GUI.FocusControl(null);}
                else Toast(reason);
            }
        }

        void OpenGalleryFolder(string path)
        {
            if(MobileControls){galleryFolderHelp=path;GUI.FocusControl(null);}
            else GalleryStore.OpenFolder(path);
        }
        void GalleryFolderHelp()
        {
            Rect b=galleryBody;bool inbox=galleryFolderHelp==GalleryStore.InboxDir;
            string relative="gallery / "+(inbox?"投稿":"submissions / "+System.IO.Path.GetFileName(galleryFolderHelp));
            Label(new Rect(b.x,b.y,b.width,58),inbox?"把图片放进「投稿」文件夹":"取出这一份投稿包",36,ink,true);
            PaperPanel(new Rect(b.x,b.y+89,b.width,83));
            Label(new Rect(b.x+22,b.y+102,b.width-44,57),"文件 App → 我的 iPhone → 侨批·纸短情长 → "+relative,28,ink,true);
            string how=inbox?"1　在照片 App 选好照片，点分享 → 存储到「文件」。\n2　选择上面的「投稿」文件夹并保存。\n3　回到游戏，点「重新扫描」，再勾选要使用的图片。\n\n支持 PNG / JPG / WebP；HEIC 照片需先导出为 JPG。":"1　在「文件」App 按上面的路径找到整份文件夹。\n2　长按文件夹 → 压缩，再通过你与维护者约定的方式分享。\n3　确认已经递交后，回游戏填写「我已交给维护者」。\n\n请保留文件夹中的图片、entry.json 和投稿说明。";
            Label(new Rect(b.x,b.y+202,b.width,285),how,30,ink,true);
            if(LetterButton(new Rect(b.x,b.yMax-77,280,70),"复制存放路径",false,28)){GUIUtility.systemCopyBuffer="我的 iPhone / 侨批·纸短情长 / "+relative;Toast("已复制文件 App 中的路径。");}
            if(LetterButton(new Rect(b.xMax-310,b.yMax-77,310,70),"明白了 · 返回游戏",true,28))galleryFolderHelp="";
        }
    }
}
