using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Qiaopi
{
    // -qiaopi-gallery-test：走一遍图集浏览、投稿与「我的投稿」，并留下截图。
    // 截图由 ScreenCapture 在帧末写出，所以每次 CaptureGallery 之后先让出一帧，
    // 再切换页面状态，否则相邻两张会拍到同一个画面。
    public partial class WorldGame
    {
        bool GalleryQA=>Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-gallery-test")>=0;

        IEnumerator RunGalleryQA()
        {
            GalleryStore.RootOverride=Path.Combine(Application.persistentDataPath,"gallery-qa");
            if(Directory.Exists(GalleryStore.RootOverride))Directory.Delete(GalleryStore.RootOverride,true);
            GalleryStore.EnsureFolders();
            File.WriteAllBytes(Path.Combine(GalleryStore.InboxDir,"QA-投稿样张.png"),QaPng(420,280));
            yield return new WaitForSeconds(1.2f);
            OpenGallery();
            yield return new WaitForSeconds(1.0f);
            CaptureGallery("gallery-1-entries");
            yield return new WaitForSeconds(.9f);

            galleryTab=1;galleryStep=0;
            yield return new WaitForSeconds(.9f);
            CaptureGallery("gallery-2-submit");
            yield return new WaitForSeconds(.9f);

            galleryDraft.title="QA 投稿 · 南洋回批";
            galleryDraft.category="回批";
            galleryDraft.year="1907";
            galleryDraft.place="新加坡 → 泉州";
            galleryDraft.contributor="QA 投稿人";
            galleryDraft.description="这是一次图集投稿自检：扫描投稿目录、生成投稿包，并检查审核额度与状态提示。";
            galleryDraft.source="QA 自检生成，仅用于验证流程";
            galleryConsent=true;
            galleryPicked.Clear();galleryPicked.AddRange(galleryInbox);
            yield return new WaitForSeconds(.6f);
            CreateGallerySubmission();
            yield return new WaitForSeconds(.9f);
            CaptureGallery("gallery-3-submitted");
            yield return new WaitForSeconds(.9f);

            if(galleryState.submissions.Count!=1||galleryState.submissions[0].status!="packaged")throw new InvalidOperationException("本机投稿包被误标为已递交。");
            galleryTab=2;
            yield return new WaitForSeconds(.9f);
            CaptureGallery("gallery-4-mine");
            yield return new WaitForSeconds(.9f);

            var visible=galleryIndex.Visible();
            gallerySelected=visible.Count>0?visible[0]:null;
            galleryTab=0;
            yield return new WaitForSeconds(.9f);
            CaptureGallery("gallery-5-detail");
            yield return new WaitForSeconds(.9f);

            Debug.Log("QIAOPI_GALLERY_QA=entries:"+visible.Count+" submissions:"+galleryState.SubmissionCount+" package:"+(galleryState.SubmissionCount>0?galleryState.submissions[0].id:"-")+" dir:"+GalleryStore.Root);
            Application.Quit(0);
        }

        // 真机流程截图只写独立 QA 目录，玩家的草稿与投稿记录不参与测试。
        IEnumerator RunMobileGalleryReview()
        {
            string previousRoot=GalleryStore.RootOverride;
            string testRoot=Path.Combine(Application.persistentDataPath,"gallery-mobile-qa");
            try
            {
                GalleryStore.RootOverride=testRoot;
                if(Directory.Exists(testRoot))Directory.Delete(testRoot,true);
                GalleryStore.EnsureFolders();
                File.WriteAllBytes(Path.Combine(GalleryStore.InboxDir,"QA-流程样张.png"),QaPng(420,280));
                OpenGallery();yield return new WaitForSeconds(.3f);
                yield return CaptureWorld("iPhone_侨批新图集");
                galleryTab=1;galleryStep=0;yield return new WaitForSeconds(.25f);
                yield return CaptureWorld("iPhone_投稿1选材料");
                galleryFolderHelp=GalleryStore.InboxDir;yield return new WaitForSeconds(.25f);
                yield return CaptureWorld("iPhone_投稿文件App步骤");galleryFolderHelp="";
                galleryDraft=new GalleryEntry{title="QA 样张 · 泉州来批",category="侨批信",year="1906",place="石叻 → 泉州",contributor="QA 流程测试",description="这是流程测试生成的示意图片，用于验证投稿步骤，并非真实历史档案。",source="程序生成的 QA 样张，仅用于测试，不对外递交。"};
                galleryPicked.Clear();galleryPicked.AddRange(galleryInbox);galleryConsent=true;galleryStep=2;
                yield return new WaitForSeconds(.25f);yield return CaptureWorld("iPhone_投稿2来源授权");
                galleryStep=3;yield return new WaitForSeconds(.25f);yield return CaptureWorld("iPhone_投稿3生成前说明");
                CreateGallerySubmission();
                if(galleryState.submissions.Count!=1||galleryState.submissions[0].status!="packaged"||galleryState.HasPending)
                    throw new InvalidOperationException("MOBILE_GALLERY_FAIL: package must remain local and unsubmitted");
                yield return new WaitForSeconds(.25f);yield return CaptureWorld("iPhone_投稿4未递交状态");
                Debug.Log("MOBILE_GALLERY_PASSED: isolated draft, materials, consent, packaged-not-submitted; no external transmission");
            }
            finally{gallery=false;galleryFolderHelp="";galleryDelivery=null;GalleryStore.RootOverride=previousRoot;}
        }

        void CaptureGallery(string name){ScreenCapture.CaptureScreenshot(Path.Combine(GalleryStore.Root,name+".png"));}

        byte[] QaPng(int w,int h)
        {
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
            var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float edge=(x<22||y<22||x>w-23||y>h-23)?.70f:1f;
                pixels[y*w+x]=new Color(.96f*edge,.92f*edge,.84f*edge,1f);
            }
            texture.SetPixels(pixels);texture.Apply();
            byte[] bytes=texture.EncodeToPNG();
            Destroy(texture);
            return bytes;
        }
    }
}
