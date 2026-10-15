using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        bool homePanel;
        Transform homeArt, homeWife, homeMother, homeSister;
        GameObject homeBasketAtStall, homeBasketOnTable, homeGrainTray, homeStoredGrain, homeMeal;
        GameObject homeHeldBasket, homeHeldGrain;
        TextMesh homeSlate, homeMarketSign;
        string homeStageSeen = "";
        bool homeWifeMarketArrived;
        List<Vector3> homeWifeRoute = new List<Vector3>();
        int homeWifeRouteIndex;
        readonly List<Vector3> homeMotherRoute = new List<Vector3>(), homeSisterRoute = new List<Vector3>();
        int homeMotherRouteIndex, homeSisterRouteIndex;
        static readonly Vector3 HomeWifeMarket = new Vector3(-25.35f, 0, -21.55f);
        static Vector3 HomeMotherDinner => new Vector3(-24.35f, 0, (ChenCourtyardHouse.Ready ? -.25f : -4.6f));
        static Vector3 HomeSisterDinner => new Vector3(-23f, 0, (ChenCourtyardHouse.Ready ? .8f : -3.55f));
        bool InHomeChapter => HomeChapter.IsActive(state) && (!QA || ChapterQA || ApplicationStills);
        bool HomeCarrying => InHomeChapter && (state.homeChapter.stage == "bring_home" || state.homeChapter.stage == "store");
        static readonly Vector3 HomeWifeStart = new Vector3(-21.8f, 0, -7.2f);
        static readonly Vector3 HomeMarket = new Vector3(-24.1f, 0, -20.1f);
        static readonly Vector3 HomeKitchen = new Vector3(-25.3f, 0, -7.6f);
        static readonly Vector3 HomeDrying = new Vector3(-24.7f, 0, -11.5f);
        static readonly Vector3 HomeStorage = new Vector3(-27.6f, 0, -10.2f);
        static Vector3 HomeLesson => ChenCourtyardHouse.Ready ? new Vector3(-26.6f, 0, -2.1f) : new Vector3(-25.5f, 0, -3.1f);
        static Vector3 HomeDinner => new Vector3(-23f, 0, (ChenCourtyardHouse.Ready ? -2.05f : -6.4f));

        void AlignChenHomeMission()
        {
            // After dinner, continuing the family conversation stays at the table.
            // Do not leave its compass marker at the former outdoor gathering spot.
            if (mission != null && state.nodeId == "peace" && HomeChapter.IsEnrolled(state)
                && !HomeChapter.IsActive(state) && ChenCourtyardHouse.Ready)
                mission.npcPosition = mission.itemPosition = mission.destination = HomeDinner;
        }
        Vector3 HomeTarget()
        {
            if (!InHomeChapter) return HomeDinner;
            switch (state.homeChapter.stage)
            {
                case "wife": return HomeWifeStart;
                case "market": return HomeMarket;
                case "bring_home": return HomeKitchen;
                case "grain": return HomeDrying;
                case "store": return HomeStorage;
                case "lesson": return HomeLesson;
                default: return HomeDinner;
            }
        }
        string HomePrompt()
        {
            if (!InHomeChapter) return null;
            if (!FirstDayCanReach(HomeTarget(), 2.15f)) return "";
            switch (state.homeChapter.stage)
            {
                case "wife": return "E  听妻子说今日的打算";
                case "market": return "E  挑一篮家用";
                case "bring_home": return "E  把竹篮放到灶边";
                case "grain": return "E  端起晒干的谷子";
                case "store": return "E  把谷子收进米瓮";
                case "lesson": return "E  陪阿满认字";
                default: return "E  和家人一起端饭";
            }
        }
        bool HandleHomeInteract()
        {
            if (!InHomeChapter) return false;
            if (homePanel || HomePerformanceBusy || !FirstDayCanReach(HomeTarget(), 2.15f)) return true;
            string step = state.homeChapter.stage;
            if (step == "market" && !HomeWifeAtMarket()) { Toast("妻子正从院门口走来，等她一起挑。也可以回头迎她两步。"); return true; }
            if (step == "bring_home") SelectHomeAction("put_basket");
            else if (step == "grain") SelectHomeAction("lift_grain");
            else if (step == "store") SelectHomeAction("store_grain");
            else {
                homePanel = true; walkRoute.Clear(); dialogue = false;
                ShowPortrait(HomeSpeaker());
                var actor = HomeSpeaker() == "aman" ? homeSister : HomeSpeaker() == "mother" ? homeMother : homeWife;
                if (actor) {
                    MakeHomeConversationSpace(actor);
                    var pose = CharacterPerformance.For(actor.gameObject); pose.LookAt(CameraEyePoint()); pose.Play(CharacterGesture.Nod, .85f);
                    // Turn the player's view toward the person they approached,
                    // rather than leaving the conversation facing a wall or sky.
                    float height=ModelLibrary.CharacterHeight(CharacterRoster.Get(HomeSpeaker()).model);
                    Vector3 gaze=actor.position+Vector3.up*(height*.87f)-CameraEyePoint();
                    if(gaze.sqrMagnitude>.01f){cameraYaw=Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg;cameraPitch=-Mathf.Atan2(gaze.y,new Vector2(gaze.x,gaze.z).magnitude)*Mathf.Rad2Deg;UpdateCamera(true);}
                }
                if(actor==homeWife)AimHomeWifeConversation();
                CancelCameraPointer();ResetMobileControls();
            }
            return true; // No unrelated jobs or story choices can skip this opening.
        }
        void SelectHomeAction(string action)
        {
            if (!InHomeChapter || HomePerformanceBusy || HomeConversationSettling || !FirstDayCanReach(HomeTarget(), 2.15f)) return;
            if (state.homeChapter.stage == "market" && !HomeWifeAtMarket()) return;
            if (!HomeChapter.TryAct(state, action)) return;
            homePanel = false;
            BeginHomePresentation(action);
            mission = WorldMissions.Get(state);
            AlignChenHomeMission();
            progress = Mathf.Min(HomeChapter.Progress(state), mission.required);
            RefreshHomeArt();
            if (targetMarker) targetMarker.transform.position = InHomeChapter ? HomeTarget() : mission.npcPosition;
            Save(); Sound();
            if (action == "walk_market") Toast("妻子先往院门外走。摊子就在门外，不用穿过整个村子。");
            else if (action == "rice" || action == "tea") Toast(action == "rice" ? "米和灯油已放进竹篮。把它提回家，妻子与你同行。" : "添些茶叶，留着饭后一起喝。提好竹篮，和妻子回家。");
            else if (action == "put_basket") Toast("妻子：东西放这里就好。趁太阳还在，帮母亲把谷子收了吧。");
            else if (action == "lift_grain") Toast("这一簸箕已经晒干。墙边的米瓮就在几步之外。");
            else if (action == "store_grain") { Toast("母亲：有你搭把手，我就能早些把饭端上桌。阿满等着你认字呢。"); if(homeMother)CharacterPerformance.For(homeMother.gameObject).Play(CharacterGesture.Nod); }
            else if (action == "home" || action == "return") { Toast(action == "home" ? "阿满把「家」描了一遍，笑着指向你和母亲。饭香从院里飘来。" : "阿满写下「归」，说出门再远，也该认得回来的路。饭已好了。"); if(homeSister)CharacterPerformance.For(homeSister.gameObject).Play(CharacterGesture.Nod); }
            else if (action == "share_meal") {
                ResetConversation(); progress = mission.required; dialogue = false; textScroll = Vector2.zero;
                Toast("米饭、家常菜和四只碗都上了桌。先把今日慢慢过完。");
            }
        }
        bool HomeWifeAtMarket()
        {
            if (!homeWife) return false;
            if (homeWifeMarketArrived) return true;
            Vector3 offset = homeWife.position - HomeWifeMarket; offset.y = 0;
            // Opening requires arrival at the actual front-of-stall position.
            // Once there, a small courtesy step during this conversation must
            // not revoke the choice the player has already opened.
            if (offset.magnitude < .35f && homeWifeRouteIndex >= homeWifeRoute.Count) homeWifeMarketArrived = true;
            return homeWifeMarketArrived;
        }
        string HomeSpeaker()
        { return state.homeChapter.stage == "lesson" ? "aman" : state.homeChapter.stage == "meal" ? "mother" : "wife"; }
        string HomeWords()
        {
            switch (state.homeChapter.stage)
            {
                case "wife": return "灯油快见底了。今日回来得早，陪我到门外挑点家用吧。娘晒着谷子，阿满也等着给你看她新学的字。";
                case "market": return "添些米和灯油，还是带包茶？米缸还够吃几日。我想着，晚饭后咱们也能坐下来喝一碗。";
                case "lesson": return "哥哥，自己的名字我会写啦。今天教我一个能写进家信里的字，好不好？我会在这块石板上慢慢练。";
                default: return "谷子收好了，饭也好了。把碗端来吧，等你们都坐下，咱们就开饭。";
            }
        }
        void DrawHomeChapterPanel()
        {
            if (!InHomeChapter) { homePanel = false; return; }
            bool mobile = MobileControls;
            Rect safe = mobile ? MobileUiBounds() : new Rect(0, 0, W, H);
            float height = mobile ? 332 : 310;
            Rect r = new Rect(safe.xMin + 32, Mathf.Min(H - 24, safe.yMax - 24) - height, safe.width - 64, height);
            PaperPanel(r, true, .90f); Box(new Rect(r.x, r.y, r.width, 3), letterSeal);
            Rect portrait = new Rect(r.x + 20, r.y + 22, 142, 180);
            if (portraitTexture) GUI.DrawTexture(portrait, portraitTexture, ScaleMode.ScaleToFit, false);
            var person = CharacterRoster.Get(HomeSpeaker());
            Label(new Rect(portrait.x - 4, portrait.yMax + 10, portrait.width + 8, 38), person.name, 26, ink, true, TextAnchor.MiddleCenter);
            float x = r.x + 184, width = r.width - 210;
            Label(new Rect(x, r.y + 18, width - 150, 34), "陈家日常", 19, sub);
            if (LetterButton(new Rect(r.xMax - 132, r.y + 12, 112, 44), "收起", false, 21)) { homePanel = false; return; }
            Label(new Rect(x, r.y + 58, width, mobile ? 119 : 107), HomeWords(), mobile ? 28 : 25, ink, true);
            float y = r.y + (mobile ? 194 : 176), gap = 16, choiceWidth = (width - gap) * .5f;
            string step = state.homeChapter.stage;
            if (step == "wife") {
                if (HomeChoice(new Rect(x, y, width, 92), "一起去挑家用", "陪妻子走到门外。")) SelectHomeAction("walk_market");
            } else if (step == "market") {
                if (HomeChoice(new Rect(x, y, choiceWidth, 92), "添米和灯油", "把家里的日用备齐。")) SelectHomeAction("rice");
                else if (HomeChoice(new Rect(x + choiceWidth + gap, y, choiceWidth, 92), "带一包茶", "晚饭后一起喝。")) SelectHomeAction("tea");
            } else if (step == "lesson") {
                if (HomeChoice(new Rect(x, y, choiceWidth, 92), "教她写「家」", "一家人住在同一个屋檐下。")) SelectHomeAction("home");
                else if (HomeChoice(new Rect(x + choiceWidth + gap, y, choiceWidth, 92), "教她写「归」", "出去以后，还认得回来的路。")) SelectHomeAction("return");
            } else if (HomeChoice(new Rect(x, y, width, 92), "端饭，一起吃", "今日的事，留在饭桌上慢慢说。")) SelectHomeAction("share_meal");
        }
        bool HomeChoice(Rect r, string title, string hint)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            bool enabled = GUI.enabled; GUI.enabled = enabled && !HomeConversationSettling;
            Box(r, new Color(.82f, .75f, .62f, hover ? .33f : .16f)); Box(new Rect(r.x, r.y, 3, r.height), letterSeal);
            Label(new Rect(r.x + 16, r.y + 8, r.width - 32, 37), title, MobileControls ? 28 : 25, ink, true);
            Label(new Rect(r.x + 16, r.y + 48, r.width - 32, 35), hint, MobileControls ? 23 : 21, sub);
            bool clicked = GUI.Button(r, GUIContent.none, blank); GUI.enabled = enabled; return clicked;
        }
        void HomeWorldLabels()
        {
            if (!InHomeChapter) return;
            Tag(HomeTarget()+Vector3.up*1.6f,HomeChapter.ShortObjective(state),true);
        }

        void SetupHomeChapter()
        {
            CancelHomePresentation(false); homeConversationRetreat = false; homeWifeMarketArrived = false;
            homePanel=false; homeStageSeen=""; homeWifeRoute.Clear(); homeWifeRouteIndex=0;
            homeMotherRoute.Clear();homeSisterRoute.Clear();homeMotherRouteIndex=homeSisterRouteIndex=0;homeDetourUntil.Clear();
            if(homeArt) { homeArt.gameObject.SetActive(false); Destroy(homeArt.gameObject); }
            if(homeHeldBasket)Destroy(homeHeldBasket); if(homeHeldGrain)Destroy(homeHeldGrain); if(homeHeldMeal)Destroy(homeHeldMeal);
            homeArt=null; homeHeldBasket=homeHeldGrain=homeHeldMeal=null; homeWife=homeMother=homeSister=null;homeSlate=homeMarketSign=null;
            if(QA&&!ChapterQA&&!ApplicationStills)return;
            if(!actors||loadedWorld!="quanzhou"||!HomeChapter.IsEnrolled(state))return;
            homeArt=new GameObject("陈家日常 · 灶边、晒谷与饭桌").transform;homeArt.SetParent(actors.transform,false);
            if(state.nodeId=="peace"){homeMother=FamilyActor("mother");homeWife=FamilyActor("wife");homeSister=FamilyActor("aman");}
            bool dinnerReady=HomeChapter.Progress(state)>=6;
            HomePlace(homeMother,dinnerReady?HomeMotherDinner:new Vector3(-28.7f,0,-8.4f),dinnerReady?HomeDinner:HomeDrying);
            HomePlace(homeSister,dinnerReady?HomeSisterDinner:new Vector3(-26.2f,0,(ChenCourtyardHouse.Ready ? .1f : -2.7f)),dinnerReady?HomeDinner:HomeLesson);
            string stage=state.homeChapter.stage;
            Vector3 wifeAt=dinnerReady?new Vector3(-21.75f,0,(ChenCourtyardHouse.Ready ? -.25f : -4.6f)):stage=="market"||stage=="bring_home"?HomeWifeMarket:HomeWifeStart;
            HomePlace(homeWife,wifeAt,HomeTarget());
            BuildHomeStall();
            HomeTable("灶边小案",new Vector3(-26.6f,0,-7.6f),1.2f,.7f,.74f);
            homeBasketOnTable=BuildHomeBasket(homeArt,new Vector3(-26.6f,.77f,-7.6f));
            homeGrainTray=BuildHomeTray(homeArt,new Vector3(-25.7f,.30f,-11.5f));
            HomeBox("矮竹架",homeArt,new Vector3(-25.7f,.15f,-11.5f),new Vector3(.63f,.3f,.45f),wood,true);
            var jar=TradeScenery.Group(homeArt,"收谷米瓮",new Vector3(-28.6f,0,-10.2f));
            var jarGeo=new TradeScenery.Geometry(jar);
            jarGeo.Lathe(Vector3.zero,new[]{new Vector2(.28f,.03f),new Vector2(.38f,.2f),new Vector2(.42f,.52f),new Vector2(.30f,.78f),new Vector2(.26f,.79f),new Vector2(.26f,.71f)},TradeScenery.Wood);jarGeo.Flush();
            homeStoredGrain=BuildHomeGrain(jar,new Vector3(0,.73f,0),.27f);
            HomeTable("阿满的小书案",new Vector3(-26.6f,0,(ChenCourtyardHouse.Ready ? -.9f : -3.7f)),1.08f,.65f,.54f);
            HomeBox("描字石板",homeArt,new Vector3(-26.6f,.574f,(ChenCourtyardHouse.Ready ? -.9f : -3.7f)),new Vector3(.66f,.025f,.44f),Mat("55615A"));
            homeSlate=HomeText("石板上的字","人",new Vector3(-26.6f,.592f,(ChenCourtyardHouse.Ready ? -.9f : -3.7f)),C("F1E6C8"));
            // TextMesh's readable side is -Z. Lay that face upward; rotate the
            // baseline toward the lesson's north/east approach, never mirror it.
            Vector3 slateUp=homeSlate.transform.position-HomeLesson;slateUp.y=0;
            homeSlate.transform.localRotation=Quaternion.LookRotation(Vector3.down,slateUp.normalized);
            FitHomeText(homeSlate,new Vector2(.40f,.29f));
            HomeTable("陈家饭桌",new Vector3(-23,0,(ChenCourtyardHouse.Ready ? -.3f : -4.65f)),1.65f,1.02f,.76f);
            homeMeal=new GameObject("一家人的四副碗筷");homeMeal.transform.SetParent(homeArt,false);
            // One modest rice bowl at each person's place, rather than four
            // large serving bowls in the corners. Dimensions are in metres.
            Vector3 mealCentre = new Vector3(-23, .765f, (ChenCourtyardHouse.Ready ? -.3f : -4.65f));
            Vector3[] placeOffsets = { new Vector3(-.63f,0,0), new Vector3(.63f,0,0),
                new Vector3(0,0,.34f), new Vector3(0,0,-.34f) };
            for(int i=0;i<placeOffsets.Length;i++){
                Vector3 at=mealCentre+placeOffsets[i];
                HomeBowl(homeMeal.transform,at,i==2?.065f:.075f);
                Quaternion setting=Quaternion.LookRotation(-placeOffsets[i].normalized);
                for(int k=0;k<2;k++) {
                    var chopstick=HomeBox("木筷",homeMeal.transform,
                        at+setting*new Vector3(.105f+k*.014f,.009f,0),new Vector3(.006f,.006f,.24f),wood);
                    chopstick.transform.localRotation=setting;
                }
            }
            HomeBowl(homeMeal.transform,new Vector3(-23,.765f,(ChenCourtyardHouse.Ready ? -.3f : -4.65f)),.16f);
            homeHeldBasket=BuildHomeBasket(heldRig.transform,new Vector3(0,-.155f,.04f));homeHeldBasket.transform.localScale=Vector3.one*.65f;
            homeHeldGrain=BuildHomeTray(heldRig.transform,new Vector3(0,.025f,.04f));homeHeldGrain.transform.localScale=Vector3.one*.67f;
            SetWorkHeldLayer(homeHeldBasket.transform);SetWorkHeldLayer(homeHeldGrain.transform);
            BuildHomeServingTray();
            BuildHomeEnvironment();
            RefreshHomeArt(); Physics.SyncTransforms();WalkPath.Invalidate();
            if(targetMarker)targetMarker.transform.position=InHomeChapter?HomeTarget():mission.npcPosition;
        }
        void BuildHomeStall()
        {
            Vector3 center=new Vector3(-26.4f,0,-20.3f);
            HomeTable("村口日用摊",center,2.15f,1.0f,.83f);
            for(int side=-1;side<=1;side+=2)HomeBox("摊棚竹柱",homeArt,center+new Vector3(side*1.08f,1.28f,.35f),new Vector3(.055f,2.56f,.055f),wood);
            HomeBox("旧布凉棚",homeArt,center+new Vector3(0,2.54f,0),new Vector3(2.55f,.06f,1.6f),cloth);
            Vector3 signCenter=center+new Vector3(.7f,1.93f,-.08f);
            Vector3 signFacing=HomeMarket-signCenter;signFacing.y=0;signFacing.Normalize();
            Quaternion signRotation=Quaternion.LookRotation(-signFacing,Vector3.up);
            var signBoard=HomeBox("日用摊的小木牌",homeArt,signCenter,new Vector3(1.10f,.20f,.045f),wood);
            signBoard.transform.localRotation=signRotation;
            var hanging=new TradeScenery.Geometry(homeArt);Vector3 signRight=signRotation*Vector3.right;
            foreach(float side in new[]{-.42f,.42f}){
                Vector3 bottom=signCenter+signRight*side+Vector3.up*.10f;
                Vector3 top=new Vector3(bottom.x,2.51f,bottom.z);
                hanging.Beam(bottom,top,.013f,TradeScenery.Linen);
            }
            hanging.Flush();
            homeMarketSign=HomeText("木牌上的字","米 · 油 · 茶",signCenter+signFacing*.028f,C("F2E6C9"));
            homeMarketSign.transform.localRotation=signRotation;
            FitHomeText(homeMarketSign,new Vector2(.98f,.14f));
            homeBasketAtStall=BuildHomeBasket(homeArt,center+new Vector3(.52f,.86f,0));
            for(int i=0;i<3;i++)HomeBox("包好的茶叶",homeArt,center+new Vector3(-.75f+i*.22f,.94f,0),new Vector3(.18f,.18f,.27f),cream);
            var vendor=CharacterAppearanceProfiles.Spawn("home_vendor",homeArt,WorldFactory.CharacterGroundPosition(center+new Vector3(-.55f,0,.98f)));
            if(vendor){vendor.name="村口日用摊主";vendor.transform.rotation=Quaternion.Euler(0,180,0);vendor.AddComponent<CharacterIdle>();}
        }
        void RefreshHomeArt()
        {
            if(!homeArt||!HomeChapter.IsEnrolled(state))return;
            var h=state.homeChapter; int step=HomeChapter.Progress(state);
            if(homeBasketAtStall)homeBasketAtStall.SetActive(step<2);
            if(homeBasketOnTable)homeBasketOnTable.SetActive(step>=3 && homePresentationAction!="put_basket");
            if(homeGrainTray)homeGrainTray.SetActive(step<4);
            if(homeStoredGrain)homeStoredGrain.SetActive(h.grainStored && homePresentationAction!="store_grain");
            if(homeMeal)homeMeal.SetActive(state.nodeId=="peace" && step>=7 && homePresentationAction!="share_meal");
            HomeBasketContents(homeBasketAtStall);HomeBasketContents(homeBasketOnTable);HomeBasketContents(homeHeldBasket);
            if(homeSlate){homeSlate.text=h.word=="home"?"家":h.word=="return"?"归":"人";FitHomeText(homeSlate,new Vector2(.40f,.29f));}
            if(h.stage!=homeStageSeen){
                homeStageSeen=h.stage;
                Vector3 wifeGoal=h.stage=="market"?HomeWifeMarket:step>=6?new Vector3(-21.75f,0,(ChenCourtyardHouse.Ready ? -.25f : -4.6f)):HomeWifeStart;
                SetHomeWifeGoal(wifeGoal);
                if(step>=6){
                    SetHomeFamilyRoute(homeMother,HomeMotherDinner,homeMotherRoute,ref homeMotherRouteIndex);
                    SetHomeFamilyRoute(homeSister,HomeSisterDinner,homeSisterRoute,ref homeSisterRouteIndex);
                }
            }
            UpdateHomeHeldProps();
        }
        void SetHomeWifeGoal(Vector3 position)
        { SetHomeFamilyRoute(homeWife,position,homeWifeRoute,ref homeWifeRouteIndex); }
        void SetHomeFamilyRoute(Transform person,Vector3 position,List<Vector3> route,ref int index)
        {
            index=0;route.Clear();
            if(person) {
                route.AddRange(WalkPath.Find(person.position,position,"quanzhou"));
                Debug.Log("HOME_FAMILY_ROUTE actor="+person.name+" from="+person.position+" target="+position+" points="+route.Count);
            }
        }
        void TickHomeChapter()
        {
            if(!homeArt)return;
            TickHomePresentation();
            TickHomeConversationSpace();
            // Dynamic Chinese font atlases may build glyph bounds one frame
            // later; fit against actual local mesh bounds, not font-size guesses.
            FitHomeText(homeMarketSign,new Vector2(.98f,.14f));
            FitHomeText(homeSlate,new Vector2(.40f,.29f));
            if(homePanel&&InHomeChapter){ShowPortrait(HomeSpeaker());if(portraitActor&&portraitCamera){CharacterPerformance.For(portraitActor).LookAt(portraitCamera.transform.position);portraitCamera.Render();}}
            bool pause=Blocked&&!homePanel&&!HomePerformanceBusy&&!(dialogue&&state.nodeId=="peace");
            TickHomeFamilyRoute(homeWife,homeWifeRoute,ref homeWifeRouteIndex,1.65f,pause);
            TickHomeFamilyRoute(homeMother,homeMotherRoute,ref homeMotherRouteIndex,1.25f,pause);
            TickHomeFamilyRoute(homeSister,homeSisterRoute,ref homeSisterRouteIndex,1.55f,pause);
            TickHomeFamilyMeal();
        }
        void TickHomeFamilyRoute(Transform person,List<Vector3> route,ref int index,float speed,bool paused)
        {
            if(!person)return;
            var performance=CharacterPerformance.For(person.gameObject);
            if(paused){performance.SetLocomotion(0);return;}
            if(index<route.Count){
                performance.SetSeated(false);
                Vector3 toward=route[index]-person.position;toward.y=0;
                if(toward.magnitude<.15f){index++;performance.SetLocomotion(0);return;}
                // Family members wait for the player's body to clear their route.
                Vector3 playerOffset = player.transform.position - person.position; playerOffset.y = 0;
                // Only yield when the player is ahead on this route. A player
                // behind the spouse no longer pins her in place on the way home.
                float alongPlayer=Vector3.Dot(playerOffset,toward.normalized);
                float segmentClearance=(playerOffset-toward.normalized*Mathf.Clamp(alongPlayer,0,toward.magnitude)).magnitude;
                if(playerOffset.magnitude < 1.05f && alongPlayer>0 && segmentClearance<.64f){
                    TryHomeFamilyDetour(person,route,ref index);
                    performance.SetLocomotion(0);return;
                }
                performance.ClearLook();performance.SetLocomotion(speed*.34f);
                person.rotation=Quaternion.RotateTowards(person.rotation,Quaternion.LookRotation(toward),Time.deltaTime*150);
                Vector3 next=person.position+toward.normalized*Mathf.Min(toward.magnitude,Time.deltaTime*speed);
                person.position=WorldFactory.CharacterGroundPosition(next);
            }else{
                performance.SetLocomotion(0);
                bool speaking=homePanel&&((HomeSpeaker()=="wife"&&person==homeWife)||(HomeSpeaker()=="mother"&&person==homeMother)||(HomeSpeaker()=="aman"&&person==homeSister));
                if(speaking&&!HomeConversationSettling&&!performance.IsSeated){
                    Vector3 facing=CameraEyePoint()-person.position;facing.y=0;
                    if(facing.sqrMagnitude>.01f)person.rotation=Quaternion.RotateTowards(person.rotation,Quaternion.LookRotation(facing),Time.unscaledDeltaTime*160);
                }
                if(speaking||Dist(person.position)<3)performance.LookAt(CameraEyePoint());else performance.ClearLook();
            }
        }
        bool UpdateHomeHeldProps()
        {
            if(HomePerformanceBusy)return true;
            bool active=HomeCarrying&&!Blocked;
            if(HomeCarrying&&workHandGrip)workHandGrip.SetHouseholdPose(state.homeChapter.stage=="bring_home"?50:22);
            if(homeHeldBasket)homeHeldBasket.SetActive(active&&state.homeChapter.stage=="bring_home");
            if(homeHeldGrain)homeHeldGrain.SetActive(active&&state.homeChapter.stage=="store");
            if(!HomeCarrying)return false;
            if(heldLetter)heldLetter.SetActive(false);if(heldBox)heldBox.SetActive(false);
            UpdateWorkHeldParcel(false);if(heldHands)heldHands.SetActive(true);if(heldRig)heldRig.SetActive(active);
            return true;
        }
        void HomePlace(Transform actor,Vector3 at,Vector3 look)
        {
            if(!actor)return;actor.position=WorldFactory.CharacterGroundPosition(at);
            Vector3 facing=look-at;facing.y=0;if(facing.sqrMagnitude>.01f)actor.rotation=Quaternion.LookRotation(facing);
        }
        GameObject HomeBox(string name,Transform parent,Vector3 at,Vector3 size,Material material,bool solid=false)
        {
            var box=Cube(name,parent,at,size,material);
            if(solid){var col=box.AddComponent<BoxCollider>();col.size=Vector3.one;}
            return box;
        }
        void HomeTable(string name,Vector3 at,float width,float depth,float height)
        {
            HomeBox(name,homeArt,at+Vector3.up*(height-.035f),new Vector3(width,.07f,depth),wood,true);
            for(int i=0;i<4;i++)HomeBox("案脚",homeArt,at+new Vector3((i%2==0?-1:1)*(width*.5f-.09f),height*.48f,(i<2?-1:1)*(depth*.5f-.09f)),new Vector3(.075f,height-.07f,.075f),wood);
        }
        GameObject BuildHomeBasket(Transform parent,Vector3 at)
        {
            var basket=TradeScenery.Group(parent,"装家用的编竹篮",at);
            var g=new TradeScenery.Geometry(basket);
            g.Box(new Vector3(0,.01f,0),new Vector3(.53f,.025f,.36f),TradeScenery.Wood);
            for(int y=0;y<5;y++){
                float h=.038f+y*.038f;
                g.Box(new Vector3(0,h,-.18f),new Vector3(.55f,.021f,.023f),TradeScenery.Wood);
                g.Box(new Vector3(0,h,.18f),new Vector3(.55f,.021f,.023f),TradeScenery.Wood);
                g.Box(new Vector3(-.265f,h,0),new Vector3(.023f,.021f,.36f),TradeScenery.Wood);
                g.Box(new Vector3(.265f,h,0),new Vector3(.023f,.021f,.36f),TradeScenery.Wood);
            }
            for(int i=0;i<9;i++)for(int side=-1;side<=1;side+=2)g.Box(new Vector3(-.24f+i*.06f,.11f,side*.18f),new Vector3(.012f,.22f,.018f),TradeScenery.Linen);
            for(int side=-1;side<=1;side+=2){g.Beam(new Vector3(side*.255f,.18f,-.065f),new Vector3(side*.255f,.31f,-.065f),.019f,TradeScenery.Linen);g.Beam(new Vector3(side*.255f,.31f,-.065f),new Vector3(side*.255f,.31f,.065f),.019f,TradeScenery.Linen);g.Beam(new Vector3(side*.255f,.31f,.065f),new Vector3(side*.255f,.18f,.065f),.019f,TradeScenery.Linen);}
            g.Flush();
            Transform rice = TradeScenery.Group(basket,"米与灯油",Vector3.zero);var riceItems = new TradeScenery.Geometry(rice);
            riceItems.Box(new Vector3(-.10f,.12f,0),new Vector3(.24f,.20f,.24f),TradeScenery.Paper);
            riceItems.Lathe(new Vector3(.14f,.025f,0),new[]{new Vector2(.065f,0),new Vector2(.07f,.15f),new Vector2(.03f,.2f),new Vector2(.03f,.25f)},TradeScenery.Wood);riceItems.Flush();
            Transform tea = TradeScenery.Group(basket,"一包茶叶",Vector3.zero);var teaItems = new TradeScenery.Geometry(tea);
            teaItems.Box(new Vector3(0,.105f,0),new Vector3(.30f,.16f,.25f),TradeScenery.Paper);
            teaItems.Box(new Vector3(0,.189f,0),new Vector3(.06f,.011f,.26f),TradeScenery.Red);teaItems.Flush();
            return basket.gameObject;
        }
        void HomeBasketContents(GameObject basket)
        {
            if(!basket)return;
            bool tea=state.homeChapter.purchase=="tea";
            var rice=basket.transform.Find("米与灯油");var packet=basket.transform.Find("一包茶叶");
            if(rice)rice.gameObject.SetActive(!tea);if(packet)packet.gameObject.SetActive(tea);
        }
        GameObject BuildHomeTray(Transform parent,Vector3 at)
        {
            var tray=TradeScenery.Group(parent,"晒谷的簸箕",at);var g=new TradeScenery.Geometry(tray);
            g.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(.31f,0),new Vector2(.35f,.05f),new Vector2(.35f,.07f),new Vector2(.31f,.03f),new Vector2(0,.03f)},TradeScenery.Linen);g.Flush();
            BuildHomeGrain(tray,new Vector3(0,.033f,0),.29f);return tray.gameObject;
        }
        GameObject BuildHomeGrain(Transform parent,Vector3 at,float radius)
        {
            var grain=TradeScenery.Group(parent,"饱满的晒谷",at);var g=new TradeScenery.Geometry(grain);
            g.Lathe(Vector3.zero,new[]{new Vector2(0,0),new Vector2(radius,0),new Vector2(radius*.8f,.02f),new Vector2(0,.035f)},TradeScenery.Rice);
            for(int i=0;i<53;i++){float a=i*2.399f,r=radius*Mathf.Sqrt((i+.5f)/53)*.90f;g.Box(new Vector3(Mathf.Cos(a)*r,.024f,Mathf.Sin(a)*r),new Vector3(.013f,.009f,.026f),TradeScenery.Linen);}
            g.Flush();return grain.gameObject;
        }
        void HomeBowl(Transform parent,Vector3 at,float radius)
        {
            var bowl=TradeScenery.Group(parent,"粗瓷饭碗",at);var g=new TradeScenery.Geometry(bowl);
            float h=Mathf.Min(.095f,radius*.85f);
            g.Lathe(Vector3.zero,new[]{new Vector2(radius*.40f,0),new Vector2(radius*.55f,h*.20f),new Vector2(radius,h*.94f),new Vector2(radius*.90f,h),new Vector2(radius*.6f,h*.31f),new Vector2(0,h*.31f)},TradeScenery.Paper);g.Flush();
            var contents=BuildHomeGrain(bowl,new Vector3(0,h*.86f,0),radius*.84f);
            contents.transform.localScale=new Vector3(1,Mathf.Min(1,radius/.15f),1);
        }
        TextMesh HomeText(string name,string value,Vector3 at,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(homeArt,false);go.transform.localPosition=at;
            var text=go.AddComponent<TextMesh>();text.font=font;text.fontSize=60;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            if(font){font.RequestCharactersInTexture(value+"人家归",60,FontStyle.Normal);go.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
            text.text=value;
            var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            return text;
        }
        void FitHomeText(TextMesh text,Vector2 maximum)
        {
            if(!text||string.IsNullOrEmpty(text.text))return;
            Vector3 size=text.GetComponent<MeshRenderer>().localBounds.size;
            if(size.x<.0001f||size.y<.0001f)return;
            float fit=Mathf.Min(maximum.x/size.x,maximum.y/size.y);
            text.transform.localScale=Vector3.one*Mathf.Min(3,fit);
        }
    }
}
