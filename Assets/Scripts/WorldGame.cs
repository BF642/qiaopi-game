using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    [Serializable] public class WorldSave
    {
        public GameState story;
        public string missionNode;
        public int layoutVersion;
        public int progress;
        public bool carrying;
        public bool sideCarrying;
        public List<int> inspected = new List<int>();
        public float px,pz;
    }
    public partial class WorldGame : MonoBehaviour
    {
        const float W=1600,H=1000;
        GameState state;
        MissionInfo mission;
        int progress;
        bool carrying,dialogue,journal,map,help,resetAsk,puzzle,sideCarrying,fieldNote;
        int puzzleIndex=-1;
        List<int> inspected=new List<int>();
        GameObject world,actors,player,model,carriedObject,carriedCrate,targetMarker,taskObject,npc,sideBox;
        CharacterController controller;
        Transform leftLeg,rightLeg,leftArm,rightArm;
        Camera cam;
        Font font,titleFont;
        GUIStyle style,serif,blank;
        Texture2D white;
        Material teal,skin,hair,cloth,wood,gold,cream;
        Vector2 textScroll,journalScroll;
        string loadedWorld="",toast="";
        bool soundEnabled=true; bool saveHealthy=true;
        float arrivalTime;
        string puzzleFeedback="";
        Vector3 restPosition,sidePosition,sideDestination;
        RegionInfo region;
        Quaternion baseLeftLeg,baseRightLeg,baseLeftArm,baseRightArm;
        float toastUntil,stepTime,fade,scale,offsetX,offsetY;
        Vector3 targetCamera;
        readonly List<GameObject> inspectionObjects=new List<GameObject>();
        readonly Vector3[] inspectionPositions={new Vector3(-22,0,-4),new Vector3(21,0,7),new Vector3(0,0,28)};
        readonly Color paper=C("F4EAD3"),ink=C("294440"),sub=C("747363"),red=C("A34837"),line=C("CCBC9B");
        string savePath=>Path.Combine(Application.persistentDataPath,"qiaopi-world-v1.json");
        bool DetailQA=>LifeQA||MobileQA||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-detail-test")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-camera-test")>=0;
        bool QA=>StoryDemo||DetailQA||GalleryQA||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-qa")>=0;
        bool Blocked=>dialogue||journal||map||help||resetAsk||puzzle||fieldNote||gallery||letterEditor||lifePanel;
        bool Complete=>progress>=mission.required;
        AudioSource sfx;
        QiaopiMusic musicDirector;
        AudioClip paperSound,stepSound;
        float lastStep;
        List<Vector3> walkRoute=new List<Vector3>();
        int routeIndex;

        void Awake()
        {
            SetupMobileDefaults();
            Application.targetFrameRate=60;
            font=Resources.Load<Font>("Fonts/Body"); titleFont=Resources.Load<Font>("Fonts/Title")??font;
            white=Texture2D.whiteTexture;
            teal=Mat("40665E"); skin=Mat("E7B78E"); hair=Mat("2D302C"); cloth=Mat("6C8C83"); wood=Mat("9D6B40"); gold=Mat("EACA7F"); cream=Mat("FFF1CF");
            state=StoryEngine.NewGame();
            WorldSave saved=null;
            bool recovered=false;
            if(!QA) {saved=WorldSaveStore.Load(savePath,out recovered);if(saved!=null)state=saved.story;}
            LifeJourney.Ensure(state);
            soundEnabled=PlayerPrefs.GetInt("qiaopi-sound-enabled",1)==1;
            WorldAtmosphere.SetSoundEnabled(soundEnabled);
            ConfigureCamera(); CreatePlayer(); SetupFirstPersonBody(); SetupAudio(); musicDirector=gameObject.AddComponent<QiaopiMusic>(); SetupConversation();
            EnterNode(true);
            if(saved!=null&&saved.missionNode==state.nodeId) {
                progress=Mathf.Clamp(saved.progress,0,mission.required); carrying=saved.carrying;sideCarrying=saved.sideCarrying; inspected=saved.inspected??new List<int>();
                controller.enabled=false; player.transform.position=saved.layoutVersion>=2?region.Clamp(new Vector3(saved.px,.1f,saved.pz)):region.spawn+Vector3.up*.1f; controller.enabled=true;
                RefreshProps();FaceCurrentTask();UpdateCamera(true);
            }
            dialogue=state.awaitingContinue||StoryEngine.GetScene(state).isEnding;
            if(recovered)Toast("已从上一份备份恢复旅程。");
            else if(saved!=null&&saved.layoutVersion<2)Toast("街区已扩建：原有选择、物品和任务进度保留，已到达本区入口。");
            if(saved==null&&!QA)Toast(state.nodeId=="peace"?"先回陈家院看看家人。主线约 25～40 分钟，途中可随时保存。":MobileControls?"左侧摇杆行走，右侧滑动转头。到行囊旁点击「互动」。":"点击石板路，或按 WASD 走动。到行囊旁按 E。 ");
            if(QA) StartCoroutine(LifeQA?RunLifeQA():MobileQA?RunMobileQA():StoryDemo?RunStoryDemo():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-camera-test")>=0?RunCameraQA():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-detail-test")>=0?RunDetailQA():(Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-landscape-test")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-landscape-review")>=0)?RunLandscapeQA():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-region-review")>=0?RunRegionReview():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-region-test")>=0?RunRegionQA():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-music-test")>=0?CheckMusic():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-dialogue-test")>=0?RunDialogueQA():Array.IndexOf(Environment.GetCommandLineArgs(),"-qiaopi-world-test")>=0?RunWorldQA():GalleryQA?RunGalleryQA():QACapture());
        }
        IEnumerator QACapture(){yield return new WaitForSeconds(4); ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"qiaopi-world-qa.png"));Debug.Log("QIAOPI_3D_READY="+Application.persistentDataPath);}
        void ConfigureCamera()
        {
            cam=Camera.main;
            if(cam==null){var go=new GameObject("World Camera");cam=go.AddComponent<Camera>();go.tag="MainCamera";go.AddComponent<AudioListener>();}
            cam.cullingMask=~((1<<30)|(1<<29));cam.orthographic=false; cam.nearClipPlane=.1f;cam.farClipPlane=180;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=C("C4CBC2");
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=C("7E8E98");RenderSettings.ambientEquatorColor=C("626A60");RenderSettings.ambientGroundColor=C("3E4034");RenderSettings.fog=false;
            var lightGO=new GameObject("Afternoon Sun");var light=lightGO.AddComponent<Light>();light.type=LightType.Directional;light.color=C("FFF1D7");light.intensity=1.0f;light.shadows=LightShadows.Soft;light.shadowStrength=.88f;light.shadowBias=.025f;light.shadowNormalBias=.10f;lightGO.transform.rotation=Quaternion.Euler(43,-38,0);
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=Application.isMobilePlatform?42:110;QualitySettings.shadowCascades=Application.isMobilePlatform?1:4;QualitySettings.antiAliasing=Application.isMobilePlatform?2:4;QualitySettings.shadowResolution=ShadowResolution.High;
            targetCamera=new Vector3(0,0,2); UpdateCamera(true);
        }
        void CreatePlayer()
        {
            player=new GameObject("陈文生 · 可操控角色");player.layer=2;controller=player.AddComponent<CharacterController>();controller.height=1.72f;controller.radius=.28f;controller.center=new Vector3(0,.86f,0);controller.stepOffset=.3f;controller.slopeLimit=46;
            model=ModelLibrary.Spawn("Wensheng",player.transform,Vector3.zero)??Person(player.transform,Vector3.zero,teal,true);
            leftLeg=ModelLibrary.Find(model.transform,"LeftLeg"); rightLeg=ModelLibrary.Find(model.transform,"RightLeg");leftArm=ModelLibrary.Find(model.transform,"LeftArm");rightArm=ModelLibrary.Find(model.transform,"RightArm");
            baseLeftLeg=leftLeg.localRotation;baseRightLeg=rightLeg.localRotation;baseLeftArm=leftArm.localRotation;baseRightArm=rightArm.localRotation;
            carriedObject=ModelLibrary.Spawn("LetterBundle",model.transform,new Vector3(0,1.05f,.72f))??Cube("手中的包裹",model.transform,new Vector3(0,1.05f,.72f),new Vector3(.74f,.55f,.56f),wood);carriedObject.SetActive(false);
            carriedCrate=new GameObject("手中的货箱");carriedCrate.transform.SetParent(model.transform,false);carriedCrate.transform.localPosition=new Vector3(0,1.1f,.68f);
            Cube("木箱",carriedCrate.transform,Vector3.zero,new Vector3(.74f,.6f,.62f),wood);
            Cube("横扎带",carriedCrate.transform,Vector3.zero,new Vector3(.77f,.12f,.65f),cream);
            Cube("竖扎带",carriedCrate.transform,Vector3.zero,new Vector3(.12f,.62f,.65f),cream);carriedCrate.SetActive(false);
        }
        void EnterNode(bool initial=false)
        {
            if(!QA&&state.nodeId=="first_pay"&&state.journey!=null&&state.journey.legacy&&!state.awaitingContinue){state.journey.legacy=false;state.journey.completed=false;}
            ResetConversation();
            mission=WorldMissions.Get(state);
            string npcId=CharacterRoster.ForNode(TaskNode);
            if(state.nodeId=="passage"&&mission.world=="harbor")npcId="guide";
            if(state.nodeId=="end_school_window"&&state.flags.Contains("return_ticket"))npcId="aman";
            mission.npcName=CharacterRoster.Get(npcId).name;
            if(state.nodeId=="passage"&&mission.world=="harbor")mission.npcName="登船处";
            if(npcId=="mother"&&(state.nodeId=="farewell"||state.nodeId=="return_choice"||state.nodeId.StartsWith("end_")))mission.npcName="母亲与阿满";
            region=DestinationWorld.Describe(WorldRegions.Get(mission.world),LifeJourney.DestinationId(state));restPosition=region.rest;sidePosition=region.sidePickup;sideDestination=region.sideDrop;
            bool changed=loadedWorld!=mission.world||renderedDestination!=LifeJourney.DestinationId(state);
            if(changed){if(world){world.SetActive(false);Destroy(world);}world=WorldFactory.Build(mission.world);DestinationWorld.Dress(world,mission.world,LifeJourney.DestinationId(state));renderedDestination=LifeJourney.DestinationId(state);loadedWorld=mission.world;ConfigureLandscape(loadedWorld);WalkPath.Invalidate();controller.enabled=false;player.transform.position=region.spawn+Vector3.up*.1f;controller.enabled=true;player.transform.rotation=Quaternion.identity;ResetCameraTracking();fade=1;arrivalTime=Time.time;}
            if(actors){actors.SetActive(false);Destroy(actors);}actors=new GameObject("本段人物与任务物件");
            progress=0;carrying=false;sideCarrying=false;walkRoute.Clear();routeIndex=0;inspected.Clear();textScroll=Vector2.zero;puzzle=false;
            string npcModel=CharacterRoster.Get(npcId).model;
            npc=ModelLibrary.Spawn(npcModel,actors.transform,mission.npcPosition)??Person(actors.transform,mission.npcPosition,Mat(mission.world=="quanzhou"?"9D5144":"8D7857"),false);
            npc.name=mission.npcName;npc.transform.rotation=Quaternion.Euler(0,180,0);npc.AddComponent<CharacterIdle>();
            if(npcModel=="Mother") {var sister=ModelLibrary.Spawn("Aman",actors.transform,mission.npcPosition+new Vector3(1.4f,0,.2f));if(sister){sister.transform.rotation=Quaternion.Euler(0,180,0);sister.AddComponent<CharacterIdle>();}}
            AddRestCorner();
            AddExplorationNotes();
            targetMarker=new GameObject("目标指引");targetMarker.transform.SetParent(actors.transform);
            var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="目标光圈";ring.transform.SetParent(targetMarker.transform);ring.transform.localPosition=new Vector3(0,.025f,0);ring.transform.localScale=new Vector3(1.5f,.015f,1.5f);ring.GetComponent<Renderer>().sharedMaterial=gold;Destroy(ring.GetComponent<Collider>());
            var pointer=Cube("浮动标记",targetMarker.transform,new Vector3(0,2.9f,0),new Vector3(.28f,.28f,.28f),gold);pointer.transform.localRotation=Quaternion.Euler(0,0,45);
            var side=ModelLibrary.Spawn("Worker",actors.transform,sidePosition)??Person(actors.transform,sidePosition,Mat("7F7861"),false);side.AddComponent<CharacterIdle>();side.name="帮工乡亲";
            sideBox=Cube("帮工货箱",actors.transform,sidePosition+new Vector3(.75f,.4f,0),new Vector3(.7f,.8f,.65f),wood);
            CreateTaskProps();
            if(StoryEngine.GetScene(state).isEnding)dialogue=true;
            RefreshProps(); targetMarker.transform.position=Target(); FaceCurrentTask(); UpdateCamera(true);
            musicDirector.SetScene(loadedWorld,state.nodeId);
            if(!QA&&LifeJourney.IsActive(state)&&string.IsNullOrEmpty(state.journey.pendingJob)){lifePanel=true;dialogue=false;}
        }
        void CreateTaskProps()
        {
            inspectionObjects.Clear();
            if(mission.activity=="inspect") {
                for(int i=0;i<Mathf.Min(inspectionPositions.Length,mission.required);i++) {
                    var desk=ModelLibrary.Spawn("PaperDesk",actors.transform,inspectionPositions[i]);
                    if(!desk){var table=Cube("案几",actors.transform,inspectionPositions[i]+new Vector3(0,.65f,0),new Vector3(1.3f,.13f,.75f),wood);for(int j=0;j<4;j++)Cube("桌腿",actors.transform,inspectionPositions[i]+new Vector3(j%2==0?-.45f:.45f,.32f,j<2?-.27f:.27f),new Vector3(.08f,.64f,.08f),wood);}
                    var letter=Cube("待核对批封 "+(i+1),actors.transform,inspectionPositions[i]+new Vector3(0,.77f,0),new Vector3(.8f,.06f,.52f),cream);inspectionObjects.Add(letter);
                }
            } else if(mission.activity=="collect"||mission.activity=="deliver") {
                string prop=state.nodeId=="home"?"TravelBag":TaskNode=="dock"?"":"LetterBundle";
                taskObject=prop.Length>0?ModelLibrary.Spawn(prop,actors.transform,mission.itemPosition):null;
                if(!taskObject){taskObject=Cube(mission.itemName,actors.transform,mission.itemPosition+new Vector3(0,.38f,0),new Vector3(.86f,.7f,.68f),wood);Cube("捆扎带",taskObject.transform,new Vector3(0,.505f,0),new Vector3(.12f,.02f,1.015f),cream);}
            } else taskObject=null;
        }
        void RefreshProps()
        {
            if(taskObject)taskObject.SetActive(!Complete&&!carrying);
            bool hasCrate=sideCarrying||(carrying&&TaskNode=="dock");
            carriedObject.SetActive(carrying&&!hasCrate);carriedCrate.SetActive(hasCrate);
            for(int i=0;i<inspectionObjects.Count;i++)if(inspectionObjects[i])inspectionObjects[i].SetActive(!inspected.Contains(i));
            if(sideBox)sideBox.SetActive(!sideCarrying&&!state.flags.Contains("odd_job_"+loadedWorld));
        }
        Vector3 Target()
        {
            if(sideCarrying)return sideDestination;
            if(Complete||mission.activity=="talk"||mission.activity=="board")return mission.npcPosition;
            if(mission.activity=="inspect"){for(int i=0;i<Mathf.Min(inspectionPositions.Length,mission.required);i++)if(!inspected.Contains(i))return inspectionPositions[i];return mission.npcPosition;}
            return carrying?mission.destination:mission.itemPosition;
        }
        void Update()
        {
            UpdatePersonalLetterKeyboard();
            fade=Mathf.Max(0,fade-Time.deltaTime*1.4f);
            musicDirector.SetReading(journal);
            musicDirector.SetSpeechActive(voiceSource&&voiceEnabled&&voiceSource.isPlaying);
            if(StoryDemo){UpdateConversation();AnimateWalk(0);return;}
            if(!DetailQA) {
            if(Input.GetKeyDown(KeyCode.Escape)){if(letterEditor)ClosePersonalLetter();else if(lifePanel)lifePanel=false;else if(fieldNote)fieldNote=false;else if(resetAsk)resetAsk=false;else if(puzzle)puzzle=false;else if(help)help=false;else if(journal)journal=false;else if(map)map=false;else if(gallery)gallery=false;else if(dialogue){if(!StoryEngine.GetScene(state).isEnding)dialogue=false;}else help=true;}
            if(Input.GetKeyDown(KeyCode.J)&&!dialogue&&!puzzle&&!resetAsk&&!fieldNote&&!GalleryTyping&&!PersonalLetterTyping&&!lifePanel){journal=!journal;map=help=gallery=false;journalScroll=Vector2.zero;}
            if(Input.GetKeyDown(KeyCode.M)&&!dialogue&&!puzzle&&!resetAsk&&!fieldNote&&!GalleryTyping&&!PersonalLetterTyping&&!lifePanel){map=!map;journal=help=gallery=false;journalScroll=Vector2.zero;}
            if(Input.GetKeyDown(KeyCode.G)&&!dialogue&&!puzzle&&!resetAsk&&!fieldNote&&!GalleryTyping&&!PersonalLetterTyping&&!lifePanel){if(gallery)gallery=false;else OpenGallery();}
            if(map){if(Input.GetKeyDown(KeyCode.Alpha1))mapTab=0;if(Input.GetKeyDown(KeyCode.Alpha2))mapTab=1;if(Input.GetKeyDown(KeyCode.Alpha3))mapTab=2;if(mapTab==0&&Input.GetKeyDown(KeyCode.Return))GuideToMission();}
            if(puzzle){for(int i=0;i<3;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))AnswerInspection(i);}
            if(resetAsk && Input.GetKeyDown(KeyCode.Return))NewGame();
            }
            UpdateConversation();UpdateMobileInput();
            if(Blocked){CancelCameraPointer();AnimateWalk(0);UpdateFirstPersonPresentation();return;}
            UpdateCameraInput();
            if(!DetailQA&&!MobileControls)PointNavigation();
            float horizontal=DetailQA?0:(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            float vertical=DetailQA?0:(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            horizontal+=mobileMove.x;vertical+=mobileMove.y;
            Vector3 forward=cam.transform.forward;forward.y=0;forward.Normalize();Vector3 right=cam.transform.right;right.y=0;right.Normalize();Vector3 move=Vector3.ClampMagnitude(right*horizontal+forward*vertical,1);
            if(move.sqrMagnitude>.01f){walkRoute.Clear();routeIndex=0;}
            else if(routeIndex<walkRoute.Count){
                Vector3 toward=walkRoute[routeIndex]-player.transform.position;toward.y=0;
                if(toward.magnitude<.24f){routeIndex++;if(routeIndex<walkRoute.Count)toward=walkRoute[routeIndex]-player.transform.position;toward.y=0;}
                move=routeIndex<walkRoute.Count?toward.normalized:Vector3.zero;
                if(move.sqrMagnitude>.01f&&cameraDragButton<0&&lookFinger<0)cameraYaw=Mathf.MoveTowardsAngle(cameraYaw,Mathf.Atan2(move.x,move.z)*Mathf.Rad2Deg,Time.deltaTime*110f);
            }
            bool run=(!DetailQA&&Input.GetKey(KeyCode.LeftShift)||MobileControls&&mobileRun)&&!carrying&&!sideCarrying;
            float speed=(carrying||sideCarrying)?3.7f:run?7.2f:4.8f;
            player.transform.rotation=Quaternion.Euler(0,cameraYaw,0);
            controller.Move((move*speed+Vector3.down*6)*Time.deltaTime);
            Vector3 pos=player.transform.position;
            if(pos.y<-.5f) {controller.enabled=false;pos.y=.05f;player.transform.position=pos;controller.enabled=true;}
            if(!region.IsGround(pos)){controller.enabled=false;pos=region.Clamp(pos);pos.y=Mathf.Max(.05f,pos.y);player.transform.position=pos;controller.enabled=true;}
            AnimateWalk(move.magnitude*(run?1.5f:1));
            if(move.sqrMagnitude>.01f&&Time.time-lastStep>(run?.26f:.38f)){lastStep=Time.time;if(soundEnabled)sfx.PlayOneShot(stepSound,.17f);}
            UpdateCamera(false);UpdateFirstPersonPresentation();
            if(npc&&Dist(mission.npcPosition)<4){Vector3 face=player.transform.position-npc.transform.position;face.y=0;if(face.sqrMagnitude>.1f)npc.transform.rotation=Quaternion.Slerp(npc.transform.rotation,Quaternion.LookRotation(face),Time.deltaTime*3);}
            if(targetMarker){targetMarker.transform.position=Target();var t=targetMarker.transform.GetChild(1);t.localPosition=new Vector3(0,2.7f+Mathf.Sin(Time.time*2)*.14f,0);t.Rotate(Vector3.up,Time.deltaTime*55,Space.World);}

            if(ConsumeMobileInteract()||!DetailQA&&!MobileControls&&Input.GetKeyDown(KeyCode.E))Interact();
        }
        void PointNavigation()
        {
            if(!Input.GetMouseButtonDown(0)||cameraDragButton>=0||PointerOverWorldUI())return;
            if(Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out RaycastHit hit,180,~((1<<2)|(1<<29)|(1<<30)))) {
                walkRoute=WalkPath.Find(player.transform.position,hit.point,loadedWorld);routeIndex=0;
                if(walkRoute.Count==0)Toast("这里走不到，试试石板路或空地。");
            }
        }
        void UpdateCamera(bool immediate){PositionControlledCamera(immediate);}
        void AnimateWalk(float speed)
        {
            walkVisual=speed;stepTime+=Time.deltaTime*10*speed;float swing=Mathf.Sin(stepTime)*27*speed;
            leftLeg.localRotation=baseLeftLeg*Quaternion.Euler(swing,0,0);rightLeg.localRotation=baseRightLeg*Quaternion.Euler(-swing,0,0);
            leftArm.localRotation=baseLeftArm*Quaternion.Euler(carrying||sideCarrying?-65:-swing*.7f,0,0);rightArm.localRotation=baseRightArm*Quaternion.Euler(carrying||sideCarrying?-65:swing*.7f,0,0);
            model.transform.localPosition=new Vector3(0,speed>0?Mathf.Abs(Mathf.Sin(stepTime))*.055f:0,0);
        }
        float Dist(Vector3 p){var d=player.transform.position-p;d.y=0;return d.magnitude;}
        string Prompt()
        {
            if(LifeJourney.IsActive(state)&&!carrying&&!sideCarrying&&Dist(mission.npcPosition)<2.5f){
                if(string.IsNullOrEmpty(state.journey.pendingJob))return "E  安排生活";
                if(Complete)return "E  领取工钱";
            }
            if(state.nodeId=="passage"&&loadedWorld=="harbor"&&Dist(mission.npcPosition)<2.5f)return "E  登船";
            if(sideCarrying)return Dist(sideDestination)<2.2f?"E  放下货物 · 领取工钱":"";
            if(!Complete&&mission.activity=="inspect"){for(int i=0;i<Mathf.Min(inspectionPositions.Length,mission.required);i++)if(!inspected.Contains(i)&&Dist(inspectionPositions[i])<2.2f)return "E  核对凭据";}
            if(!Complete&&(mission.activity=="collect"||mission.activity=="deliver")){
                if(!carrying&&Dist(mission.itemPosition)<2.2f)return "E  拿起"+mission.itemName;
                if(carrying&&Dist(mission.destination)<2.2f)return "E  交付"+mission.itemName;
            }
            if(Dist(mission.npcPosition)<2.5f)return Complete||mission.activity=="talk"||mission.activity=="board"?"E  与"+mission.npcName+"交谈":"";
            if(!carrying&&Dist(restPosition)<1.8f)return state.flags.Contains("rested_at_"+loadedWorld)?"":"E  歇脚 · 盘缠 −2 / 身体 +8";
            if(!carrying&&!state.flags.Contains("odd_job_"+loadedWorld)&&Dist(sidePosition)<2.2f)return "E  帮工 · 报酬 6 盘缠";
            var nearby=NearestNote();if(nearby!=null)return "E  见闻 · "+nearby.title;
            return "";
        }
        void Interact()
        {
            if(HandleLifeInteract())return;
            if(sideCarrying&&Dist(sideDestination)<2.2f){sideCarrying=false;state.money+=6;state.health=Mathf.Max(0,state.health-3);state.trust=Mathf.Min(100,state.trust+1);state.flags.Add("odd_job_"+loadedWorld);Toast("帮工完成：盘缠 +6，身体 −3，信任 +1");RefreshProps();Save();return;}
            if(sideCarrying)return;
            if(!carrying&&Dist(restPosition)<1.8f){RestAtTea();return;}
            if(!Complete&&mission.activity=="inspect")for(int i=0;i<Mathf.Min(inspectionPositions.Length,mission.required);i++)if(!inspected.Contains(i)&&Dist(inspectionPositions[i])<2.2f){puzzle=true;puzzleIndex=i;puzzleFeedback="";walkRoute.Clear();return;}
            if(!Complete&&(mission.activity=="collect"||mission.activity=="deliver")) {
                if(!carrying&&Dist(mission.itemPosition)<2.2f){if(mission.activity=="collect"){progress=mission.required;Toast("已收好"+mission.itemName+"，去找"+mission.npcName+"。");}else{carrying=true;Toast("抱起了"+mission.itemName+"。走到金色标记处交付。");}Sound();RefreshProps();Save();return;}
                if(carrying&&Dist(mission.destination)<2.2f){carrying=false;progress++;Sound();Toast(Complete?"事情办妥了，去找"+mission.npcName+"。":"已交付 "+progress+" / "+mission.required+"，还有一趟。");RefreshProps();Save();return;}
            }
            if(Dist(mission.npcPosition)<2.5f){
                if(state.nodeId=="passage"&&loadedWorld=="harbor"){
                    state.flags.Add("aboard_passage");dialogue=false;EnterNode();Save();Toast("已登船。走过甲板，到船首找到同乡许生。");return;
                }
                if(Complete||mission.activity=="talk"||mission.activity=="board"){progress=mission.required;FaceConversationPartner();dialogue=true;walkRoute.Clear();textScroll=Vector2.zero;Sound();Save();}else Toast("先"+mission.objective);return;
            }
            if(!carrying&&!state.flags.Contains("odd_job_"+loadedWorld)&&Dist(sidePosition)<2.2f){sideCarrying=true;Toast("帮乡亲送一箱货，送达可挣 6 盘缠。");RefreshProps();Save();return;}
            var note=NearestNote();if(note!=null)ReadNote(note);
        }
        void Choose(string id){if(LifeJourney.IsActive(state)&&!StoryDemo){lifePanel=true;dialogue=false;return;}if(StoryEngine.Choose(state,id)){Save();Sound();textScroll=Vector2.zero;}}
        void Next(){if(StoryEngine.Continue(state)){dialogue=false;EnterNode();Save();Sound();}}
        void Save()
        {
            if(QA)return;
            try{
                var saved=new WorldSave{story=state,missionNode=state.nodeId,layoutVersion=2,progress=progress,carrying=carrying,sideCarrying=sideCarrying,inspected=new List<int>(inspected),px=player.transform.position.x,pz=player.transform.position.z};
                WorldSaveStore.Write(savePath,saved);saveHealthy=true;
            }catch(Exception e){saveHealthy=false;Debug.LogWarning("Save failed: "+e.Message);Toast("进度暂时未能保存，请保持游戏开启。");}
        }
        void OnApplicationQuit(){UpdatePersonalLetterKeyboard();CancelCameraPointer();if(state!=null&&mission!=null)Save();}
        void OnApplicationFocus(bool focus){if(!focus){UpdatePersonalLetterKeyboard();CancelCameraPointer();ResetMobileControls();if(state!=null&&mission!=null)Save();}}
        void OnApplicationPause(bool paused){if(paused){UpdatePersonalLetterKeyboard();ResetMobileControls();CancelCameraPointer();if(state!=null&&mission!=null)Save();}}
        void NewGame(){if(letterEditor)ClosePersonalLetter();lifePanel=false;state=StoryEngine.NewGame();dialogue=journal=map=help=resetAsk=puzzle=fieldNote=gallery=false;loadedWorld="";toast="";toastUntil=0;EnterNode(true);Save();}
        void SetupAudio(){sfx=gameObject.AddComponent<AudioSource>();sfx.volume=.35f;var random=new System.Random(18);float[] a=new float[2205];for(int i=0;i<a.Length;i++)a[i]=((float)random.NextDouble()*2-1)*Mathf.Exp(-i/350f)*.22f;paperSound=AudioClip.Create("纸响",a.Length,1,22050,false);paperSound.SetData(a,0);for(int i=0;i<a.Length;i++)a[i]=Mathf.Sin(i*.055f)*Mathf.Exp(-i/180f)*.2f;stepSound=AudioClip.Create("脚步",a.Length,1,22050,false);stepSound.SetData(a,0);}
        void Sound(){if(soundEnabled)sfx.PlayOneShot(paperSound);}
        void Toast(string t){toast=t;toastUntil=Time.time+4;}

        void InitStyles(){if(style!=null)return;style=new GUIStyle(GUI.skin.label){font=font,fontSize=22,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};serif=new GUIStyle(style){font=titleFont};blank=new GUIStyle(GUIStyle.none);}
        void OnGUI()
        {
            if(QA&&Event.current.isMouse)Debug.Log("QIAOPI_INPUT "+Event.current.type+" raw="+Event.current.mousePosition+" screen="+Screen.width+"x"+Screen.height+" detail="+DetailQA);
            if(DetailQA&&(Event.current.isMouse||Event.current.isKey))Event.current.Use();
            InitStyles();GetUiMetrics(out scale,out offsetX,out offsetY);
            GUI.matrix=Matrix4x4.TRS(new Vector3(offsetX,offsetY,0),Quaternion.identity,Vector3.one*scale);
            SceneData scene=StoryEngine.GetScene(state);
            if(!Blocked)WorldLabels();
            GUI.enabled=!(journal||map||help||resetAsk||puzzle||fieldNote||gallery||letterEditor||lifePanel);HUD(scene);GUI.enabled=true;
            bool cover=journal||map||help||resetAsk||puzzle||fieldNote||gallery||letterEditor||lifePanel;
            GUI.enabled=!cover; if(dialogue)Dialogue(scene); GUI.enabled=true;
            if(journal)Journal();if(map)RegionMap();if(help)Help();if(resetAsk)Restart();if(puzzle)Puzzle();if(fieldNote)FieldNote();if(gallery)GalleryPanel();if(lifePanel&&!letterEditor&&!journal)DrawLifePanel();if(letterEditor)DrawPersonalLetter();
            if(!Blocked){FirstPersonOverlay();DrawLifeEntry();}
            if(!Blocked&&toastUntil>Time.time){HudSurface(new Rect(375,416,850,70),.64f);HudText(new Rect(399,424,802,54),toast,19,hudText,false,TextAnchor.MiddleCenter);}
            if(fade>0)Box(new Rect(-100,-100,1800,1200),new Color(.16f,.24f,.20f,fade));
            if(!DetailQA)KeyDialogue(scene);
            if(Blocked){CancelCameraPointer();ResetMobileControls();}
            DrawStoryDemo();
        }
        void HUD(SceneData scene)
        {
            if(MobileControls){MobileHUD(scene);return;}
            LetterHeading(new Rect(28,22,1544,66),false);
            HudText(new Rect(372,30,108,23),"盘缠",14,hudMuted);
            HudText(new Rect(372,49,108,31),state.money.ToString(),25,hudText,true);

            AttributeBars(new Rect(504,31,496,46),false);
            if(HudNav(new Rect(1019,29,167,51),"侨批","letter",journal,19)){journal=!journal;map=help=gallery=false;journalScroll=Vector2.zero;}
            if(HudNav(new Rect(1190,29,141,51),"图集","album",gallery,19)){if(gallery)gallery=false;else OpenGallery();}
            if(HudNav(new Rect(1335,29,131,51),"地图","map",map,19)){map=!map;journal=help=gallery=false;journalScroll=Vector2.zero;}
            if(HudNav(new Rect(1470,29,82,51),"","help",help,21)){help=!help;gallery=false;}
            if(Blocked)return;
            HudSurface(new Rect(24,105,474,116),.44f);
            HudText(new Rect(46,116,422,25),scene.year+" · "+new[]{"离乡","渡海","落脚","寄批","风浪","归途"}[scene.chapter],14,hudMuted);
            HudText(new Rect(46,149,422,43),ShortObjective(),25,hudText,true);
            HudText(new Rect(46,194,422,24),ShortProgress(),16,hudGold);
            HudText(new Rect(1196,111,365,28),region.title,16,hudMuted,false,TextAnchor.MiddleRight);
        }
        void WorldLabels()
        {
            Tag(mission.npcPosition+Vector3.up*2.14f,mission.npcName,Complete||mission.activity=="talk"||mission.activity=="board");
            if(!Complete){if(mission.activity=="collect"||mission.activity=="deliver")Tag((carrying?mission.destination:mission.itemPosition)+Vector3.up*1.35f,carrying?"交付地点":mission.itemName,true);
                else if(mission.activity=="inspect")for(int i=0;i<Mathf.Min(inspectionPositions.Length,mission.required);i++)if(!inspected.Contains(i))Tag(inspectionPositions[i]+Vector3.up*1.3f,"批封 "+(i+1),true);}
            if(!state.flags.Contains("odd_job_"+loadedWorld))Tag(sidePosition+Vector3.up*2.14f,"乡亲 · 可帮工",false);
            if(Dist(restPosition)<6)Tag(restPosition+Vector3.up*1.35f,"歇脚处 · 茶",false);
            if(sideCarrying)Tag(sideDestination+Vector3.up*1.2f,"帮工送达处",true);
            foreach(var n in region.notes)if(Dist(n.position)<10)Tag(n.position+Vector3.up*1.9f,"见闻 · "+n.title,false);
        }
        void Tag(Vector3 p,string t,bool target)
        {
            float distance=Vector3.Distance(CameraEyePoint(),p);if(distance>(target?24f:8f))return;
            if(world&&Physics.Linecast(CameraEyePoint(),p,out RaycastHit obstruction,~((1<<2)|(1<<29)|(1<<30)),QueryTriggerInteraction.Ignore)&&obstruction.transform.IsChildOf(world.transform))return;
            Vector3 s=cam.WorldToScreenPoint(p);if(s.z<0)return;float x=(s.x-offsetX)/scale,y=(Screen.height-s.y-offsetY)/scale;
            if(x<45||x>1555||y<275||y>872)return;
            float width=Mathf.Max(90,t.Length*18+26);Rect r=new Rect(x-width*.5f,y-19,width,36);
            HudSurface(r,.28f);HudText(r,t,target?18:16,target?hudGold:hudMuted,false,TextAnchor.MiddleCenter);
        }
        void Overlay(string title)
        {
            Box(new Rect(-600,-100,2800,1200),new Color(.09f,.18f,.15f,.34f));PaperPanel(new Rect(165,136,1270,753));Box(new Rect(165,136,1270,4),letterSeal);Label(new Rect(211,178,1000,58),title,34,ink,true);
            if(SmallButton(new Rect(1310,168,80,48),"关闭")){journal=map=help=puzzle=resetAsk=fieldNote=gallery=false;}
        }
        void OldJournal()
        {
            Overlay("渡过海的银信 · 侨批匣");Label(new Rect(214,242,1090,29),"寄出的批，等来的回信，都按岁月收在这里。",18,sub);
            if(state.letters.Count==0){Label(new Rect(290,412,1020,155),"第一封批，还在心里。\n先去泉州老厝里收拾行李，走到家人身边告别。",27,sub,true,TextAnchor.MiddleCenter);return;}
            float total=0;foreach(var l in state.letters)total+=200+Height(l.body,23,1090,true);
            journalScroll=GUI.BeginScrollView(new Rect(211,294,1170,544),journalScroll,new Rect(0,0,1145,total),false,false);
            float y=0;for(int i=state.letters.Count-1;i>=0;i--){var l=state.letters[i];float bh=Height(l.body,23,1075,true);float hh=183+bh;
                Box(new Rect(0,y,1126,hh),C("EBE0C8"));Label(new Rect(24,y+16,960,27),l.date+" · "+l.route,15,sub);Label(new Rect(24,y+56,1046,39),l.title,27,ink,true);Label(new Rect(24,y+113,1075,bh+5),l.body,23,ink,true);Label(new Rect(680,y+hh-46,418,29),l.signature,20,sub,true,TextAnchor.MiddleRight);y+=hh+25;
            }GUI.EndScrollView();
        }
        void Journey()
        {
            Overlay("行路记 · 每一步都有来处");Label(new Rect(214,242,1090,29),"泉州 → 厦门 → "+LifeJourney.DestinationName(state)+"。回看你的选择与它留下的后果。",18,sub);
            if(state.history.Count==0){Label(new Rect(290,420,1000,100),"走出老厝，故事才刚开始。",30,sub,true,TextAnchor.MiddleCenter);return;}
            float total=0;foreach(var e in state.history)total+=150+Height(e.outcome,21,1070,true);
            journalScroll=GUI.BeginScrollView(new Rect(212,294,1170,544),journalScroll,new Rect(0,0,1145,total),false,false);
            float y=0;for(int i=state.history.Count-1;i>=0;i--){var e=state.history[i];float h=Height(e.outcome,21,1070,true);Label(new Rect(12,y,1070,25),e.year+" · "+e.location,15,sub);Label(new Rect(12,y+34,1070,41),e.title,27,ink,true);Label(new Rect(12,y+80,1070,33),"你选择："+e.choice,19,red);Label(new Rect(12,y+124,1070,h+5),e.outcome,21,ink,true);y+=150+h;}GUI.EndScrollView();
        }
        void Help()
        {
            if(MobileControls){MobileHelp();return;}
            Overlay("亲自走一程，再写一封批。");
            if(SmallButton(new Rect(230,239,315,45),musicDirector.MusicEnabled?"背景音乐：开启":"背景音乐：关闭"))musicDirector.SetEnabled(!musicDirector.MusicEnabled);
            Label(new Rect(566,239,120,45),"音乐音量",18,sub,false,TextAnchor.MiddleLeft);
            float musicLevel=GUI.HorizontalSlider(new Rect(692,255,178,25),musicDirector.Level,0,1);
            if(Mathf.Abs(musicLevel-musicDirector.Level)>.002f)musicDirector.SetLevel(musicLevel);
            Label(new Rect(880,239,80,45),Mathf.RoundToInt(musicDirector.Level*100)+"%",17,sub,false,TextAnchor.MiddleLeft);
            if(SmallButton(new Rect(978,239,370,45),soundEnabled?"环境声与音效：开启":"环境声与音效：关闭")){soundEnabled=!soundEnabled;WorldAtmosphere.SetSoundEnabled(soundEnabled);PlayerPrefs.SetInt("qiaopi-sound-enabled",soundEnabled?1:0);}
            Label(new Rect(230,289,1090,326),"WASD / 点击近处地面：走路    Shift：快走    E：交谈、拾取或交付\n按住右键移动鼠标：转头、抬头、低头；松开后恢复鼠标指针。\n从文生眼中看世界，镜头保持自然眼高。\n滚轮：调整视野宽窄    Home：面向当前目的地\nJ：侨批匣    G：侨批图集 · 共建    M：地图与见闻    Esc：收起面板或打开帮助\n\n跟随金色光圈完成任务，再找人物交谈。乡亲可提供一次帮工；茶桌可花2盘缠恢复8身体，每处一次。\n对话按 Enter / 空格继续，最后按 1 / 2 / 3 回答；互动后自动存档。",22,ink);
            Label(new Rect(230,629,1090,29),musicDirector.TrackTitle+"  ·  原创南音器乐意象配乐；交谈时自动放轻。",18,red);
            Label(new Rect(230,671,1090,50),"图集：浏览示意资料，准备本机投稿包；材料须经授权与审核后收入。",19,sub,true);
            Label(new Rect(230,725,1090,30),"故事从1905年泉州晋江出发，经厦门去往南洋。人物与情节虚构；盘缠为游戏化数值。",17,sub,true);
            if(Button(new Rect(230,788,370,61),"继续探索",true))help=false;if(Button(new Rect(970,788,385,61),"重新启程",false)){resetAsk=true;help=false;}
        }
        void Restart(){Overlay("让故事，重新从泉州开始。");Label(new Rect(245,327,1100,142),"当前这一程的选择、任务与信件会被新的旅程替换。\n\n你可以选择不同的谋生方式，亲手走向另一种结局。",25,ink,true);if(Button(new Rect(245,628,512,75),"重新启程  Enter",true))NewGame();if(Button(new Rect(825,628,510,75),"留在这一程",false))resetAsk=false;}
        void Puzzle()
        {
            var entry=InspectionCases.Get(TaskNode,puzzleIndex);
            Overlay("核对凭据 · "+entry.title);
            Label(new Rect(214,244,1135,48),entry.question,22,ink,true);
            Rect left=new Rect(213,308,557,231),right=new Rect(793,308,586,231);
            EvidencePanel(left,entry.leftTitle,entry.leftText); EvidencePanel(right,entry.rightTitle,entry.rightText);
            for(int i=0;i<entry.answers.Length;i++)if(Button(new Rect(215,568+i*78,1160,64),(i+1)+"  "+entry.answers[i],false))AnswerInspection(i);
            Label(new Rect(216,810,1140,54),puzzleFeedback.Length>0?puzzleFeedback:"比对两份记录，再作判断。按 1 / 2 / 3 或点击选择。",18,puzzleFeedback.Length>0?red:sub);
        }
        void EvidencePanel(Rect r,string title,string body)
        {
            Box(r,C("ECE6D1"));Stroke(r,C("CFC4A3"));Label(new Rect(r.x+25,r.y+18,r.width-50,32),title,17,red);
            Label(new Rect(r.x+25,r.y+65,r.width-50,r.height-80),body,23,ink,true);
        }
        void AnswerInspection(int answer)
        {
            if(!puzzle||puzzleIndex<0)return;
            var entry=InspectionCases.Get(TaskNode,puzzleIndex);
            if(answer!=entry.correct){puzzleFeedback=entry.failure;Sound();return;}
            ResolveInspection();Toast(entry.success+"  信用 +1");
        }
        void ResolveInspection(){if(puzzleIndex<0||inspected.Contains(puzzleIndex))return;inspected.Add(puzzleIndex);progress=inspected.Count;puzzle=false;state.trust=Mathf.Min(100,state.trust+1);RefreshProps();Save();Toast("核对完成 · 信用 +1。还有 "+Mathf.Max(0,mission.required-progress)+" 张需要查看。");}
        void KeyDialogue(SceneData scene)
        {
            var e=Event.current;if(StoryDemo||e.type!=EventType.KeyDown||!dialogue||journal||map||help||resetAsk||puzzle||fieldNote||gallery||letterEditor||lifePanel)return;
            if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.Space){if(!LastDialogueLine)NextDialogueLine();else if(state.awaitingContinue)Next();e.Use();return;}
            if(scene.isEnding&&e.keyCode==KeyCode.R){resetAsk=true;e.Use();return;}
            if(state.awaitingContinue||scene.isEnding||!LastDialogueLine)return;
            int n=e.keyCode==KeyCode.Alpha1?0:e.keyCode==KeyCode.Alpha2?1:e.keyCode==KeyCode.Alpha3?2:-1;
            if(n>=0&&n<scene.choices.Count&&scene.choices[n].enabled){Choose(scene.choices[n].id);e.Use();}
        }
        bool SmallButton(Rect r,string t){bool h=GUI.enabled&&r.Contains(Event.current.mousePosition);if(h)Box(r,C("E4D5B8"));Label(r,t,17,ink,false,TextAnchor.MiddleCenter);Box(new Rect(r.x+10,r.yMax-2,r.width-20,1),h?letterSeal:line);return GUI.Button(r,GUIContent.none,blank);}
        bool Button(Rect r,string t,bool primary){return LetterButton(r,t,primary,22);}
        float Height(string s,int size,float width,bool useSerif){var st=useSerif?serif:style;st.fontSize=size;st.alignment=TextAnchor.UpperLeft;return st.CalcHeight(new GUIContent(s??""),width);}
        void Label(Rect r,string t,int size,Color color,bool useSerif=false,TextAnchor align=TextAnchor.UpperLeft){var st=useSerif?serif:style;st.fontSize=size;st.normal.textColor=color;st.alignment=align;GUI.Label(r,t??"",st);}
        void Box(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,white);GUI.color=old;}
        void Stroke(Rect r,Color c){Box(new Rect(r.x,r.y,r.width,1),c);Box(new Rect(r.x,r.yMax-1,r.width,1),c);Box(new Rect(r.x,r.y,1,r.height),c);Box(new Rect(r.xMax-1,r.y,1,r.height),c);}
        static Color C(string h){ColorUtility.TryParseHtmlString("#"+h,out var c);return c;}
        static Material Mat(string hex){var m=new Material(Resources.Load<Material>("WorldMaterial"));ColorUtility.TryParseHtmlString("#"+hex,out var c);m.color=c;m.SetFloat("_Glossiness",.05f);return m;}
        static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Destroy(g.GetComponent<Collider>());return g;}
        static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Destroy(g.GetComponent<Collider>());return g;}
        GameObject Person(Transform parent,Vector3 pos,Material outfit,bool isPlayer)
        {
            var p=new GameObject(isPlayer?"文生身体":"乡人");p.transform.SetParent(parent,false);p.transform.localPosition=pos;
            Shape(PrimitiveType.Capsule,"衣身",p.transform,new Vector3(0,1.18f,0),new Vector3(.68f,.5f,.47f),outfit);
            Shape(PrimitiveType.Sphere,"面容",p.transform,new Vector3(0,1.97f,0),new Vector3(.48f,.52f,.47f),skin);
            Shape(PrimitiveType.Sphere,"头发",p.transform,new Vector3(0,2.15f,-.035f),new Vector3(.5f,.26f,.46f),hair);
            Shape(PrimitiveType.Sphere,"左眼",p.transform,new Vector3(-.10f,2.0f,.225f),new Vector3(.04f,.045f,.023f),hair);Shape(PrimitiveType.Sphere,"右眼",p.transform,new Vector3(.10f,2,.225f),new Vector3(.04f,.045f,.023f),hair);
            foreach(bool left in new[]{true,false}){float sign=left?-1:1;var leg=new GameObject(left?"LeftLeg":"RightLeg");leg.transform.SetParent(p.transform,false);leg.transform.localPosition=new Vector3(sign*.17f,.82f,0);Cube("裤腿",leg.transform,new Vector3(0,-.34f,0),new Vector3(.25f,.67f,.26f),cloth);Cube("布鞋",leg.transform,new Vector3(0,-.72f,.06f),new Vector3(.28f,.13f,.40f),hair);
                var arm=new GameObject(left?"LeftArm":"RightArm");arm.transform.SetParent(p.transform,false);arm.transform.localPosition=new Vector3(sign*.43f,1.57f,0);Cube("衣袖",arm.transform,new Vector3(0,-.25f,0),new Vector3(.20f,.48f,.24f),outfit);Shape(PrimitiveType.Sphere,"手",arm.transform,new Vector3(0,-.56f,0),new Vector3(.18f,.21f,.19f),skin);}
            if(isPlayer){Cube("背包",p.transform,new Vector3(0,1.25f,-.34f),new Vector3(.57f,.6f,.26f),wood);Cube("肩带",p.transform,new Vector3(.2f,1.40f,0),new Vector3(.06f,.65f,.61f),cream);}
            return p;
        }
    }
}
