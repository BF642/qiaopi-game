using System;using System.IO;using System.Collections;using UnityEngine;
namespace Qiaopi { public partial class WorldGame {
 bool ApplicationStills => Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-application-stills")>=0;
 string stillsOutput;
 IEnumerator RunApplicationStills(){
  AudioListener.pause=true;soundEnabled=voiceEnabled=false;musicDirector.SetEnabled(false,false);
  stillsOutput=Environment.GetEnvironmentVariable("QIAOPI_STILLS_OUTPUT");Directory.CreateDirectory(stillsOutput);
  GalleryStore.RootOverride=Path.Combine(stillsOutput,"../演示投稿包");GalleryStore.EnsureFolders();
  File.Copy(Environment.GetEnvironmentVariable("QIAOPI_SAMPLE_PATH"),Path.Combine(GalleryStore.InboxDir,"演示样张_非历史原件.png"),true);
  DemoView(new Vector3(-23,.12f,-21),new Vector3(-23,1.5f,-5));
  yield return DemoHold("01_泉州陈家院","从泉州出发｜先拥有一个家，再经历离乡",8,18);
  if(homeWife){DemoView(homeWife.position+new Vector3(0,.12f,-2.2f),homeWife.position+Vector3.up*1.45f);HandleHomeInteract();}
  yield return DemoHold("02_妻子与家庭日常","人物与日常｜妻子、母亲、妹妹都有自己的牵挂",8);
  homePanel=false;
  foreach(var actor in new[]{homeMother,homeSister})if(actor){DemoView(actor.position+new Vector3(0,.12f,-2.5f),actor.position+Vector3.up*1.3f);yield return DemoHold(actor==homeMother?"03_母亲":"04_阿满","人物",1);}
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-home-stills-only")>=0){Debug.Log("HOME_STILLS_DONE");Application.Quit(0);yield break;}
  DemoScene("passage","singapore");state.flags.Add("aboard_passage");EnterNode();DemoView(new Vector3(0,.12f,4),new Vector3(0,1.5f,22));
  yield return DemoHold("05_南下海路","离乡与选择｜海路、同乡与未知的落脚地",7,12);
  DemoScene("shop","singapore");DemoView(mission.npcPosition+new Vector3(0,.12f,-3.5f),mission.npcPosition+Vector3.up*1.4f);
  yield return DemoHold("06_新加坡商铺","海外谋生｜店铺核账、搬运与递批",7,10);
  DemoScene("dock","semarang");DemoView(new Vector3(-12,.12f,14),new Vector3(-20,2,14));
  yield return DemoHold("07_三宝垄仓库","不同落脚地｜三宝垄货栈与当地谋生任务",6,10);
  DemoScene("courier","penang");DemoView(mission.npcPosition+new Vector3(0,.12f,-4),mission.npcPosition+Vector3.up*1.5f);
  yield return DemoHold("08_槟城侨批局","银信往返｜递送、核对与追查把故事连接起来",6,8);
  DemoScene("first_pay","singapore");state.firstDayVersion=0;lifePanel=true;
  yield return DemoHold("09_自主谋生","有限自由｜工作、歇息与家事之间作决定",7);
  lifePanel=false;DemoView(writingPlace.approach+Vector3.up*.1f,writingPlace.lookAt);BeginPersonalLetterAtDesk();
  yield return DemoHold("10_室内写批","写自己的侨批｜在室内书桌前给泉州写信",5);
  var draft=PersonalLetters.EnsureDraft(state);draft.body="母亲：\n今日在南洋做工，挣得虽不多，已留好饭钱。\n这里的路还不熟，幸有同乡照应。家中若有难处，请照实写来。\n等下一封回批，我再把近况说给你听。\n文生";draft.intent="truth";draft.amount=2;
  yield return DemoHold("11_自写侨批","文字由玩家书写｜寄款与心意共同影响家庭回应",9);
  ClosePersonalLetter();OpenGallery();
  yield return DemoHold("12_侨批共建图集","核心特色：一纸共藏｜从体验故事到参与文化共建",6);
  var entries=galleryIndex.Visible();if(entries.Count>0)gallerySelected=entries[0];
  yield return DemoHold("13_图集材料详情","阅读材料｜图像、题名、说明与来源一起呈现（示意样张）",5);
  gallerySelected=null;galleryTab=1;galleryStep=0;galleryPicked.Clear();galleryPicked.AddRange(galleryInbox);
  yield return DemoHold("14_导入侨批材料","共建第1步｜放入侨批图片，扫描并选择材料",6);
  galleryDraft=new GalleryEntry{title="演示样张 · 从南洋寄回泉州的家书",category="侨批信",year="1906（示意）",place="新加坡 → 泉州",contributor="项目演示",description="本材料为游戏内自制示意样张，用于展示侨批图片、家族记忆与往返路线的整理流程；并非真实家族档案。",source="项目自制示意素材，仅用于申请材料中的操作演示，不向外递交。"};galleryStep=1;
  yield return DemoHold("15_题名与往返路线","共建第2步｜填写题名、类别、年代和往返路线",6);
  galleryStep=2;galleryConsent=true;
  yield return DemoHold("16_故事来源与授权","共建第3步｜补充家族故事、来源、署名和公开授权",7);
  galleryStep=3;
  yield return DemoHold("17_生成投稿包","共建第4步｜把图片、说明和授权整理成可递交的投稿包",7);
  CreateGallerySubmission();
  yield return DemoHold("18_我的投稿","当前版本：本机打包、手动递交；在线上传与审核服务待建设",8);

 Debug.Log("APPLICATION_STILLS_DONE");Application.Quit(0);
 }
 IEnumerator DemoHold(string shot,string caption,float seconds,float pan=0){
  yield return new WaitForSeconds(1.5f);yield return new WaitForEndOfFrame();
  var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(stillsOutput,shot+".png"),tex.EncodeToPNG());Destroy(tex);Debug.Log("APPLICATION_STILL="+shot);
 }
 void DemoView(Vector3 at,Vector3 look){controller.enabled=false;player.transform.position=at;controller.enabled=true;var d=look-(at+Vector3.up*1.6f);cameraYaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;cameraPitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;UpdateCamera(true);walkRoute.Clear();}
 void DemoScene(string node,string destination){homePanel=dialogue=journal=map=help=gallery=lifePanel=puzzle=fieldNote=firstDayPanel=nanyangPanel=tradePanel=inquiryPanel=false;state=StoryEngine.NewGame(20261015);state.nodeId=node;state.journey.destination=destination;state.firstDayVersion=0;loadedWorld="";EnterNode(true);ResetView();}
}}