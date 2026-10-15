using UnityEngine;

namespace Qiaopi
{
    public sealed class NanyangStep
    {
        public string world,npcId,title,body,confirm;
        public Vector3 person,point;
        public bool inspect;
    }

    // Physical work changes with practice. Wage-dispute steps are saved by the
    // rules, while this table only assigns their places and readable evidence.
    public static class NanyangTasks
    {
        public static bool Applies(GameState s)=>NanyangLife.IsEnrolled(s)&&LifeJourney.IsActive(s);
        public static string RouteWorld(string route)=>route=="dock"?"port":route=="shop"?"market":"postoffice";
        public static Vector3 Employer(string route)=>WorldMissions.Get(route).npcPosition;
        public static Vector3 Witness(string route)=>NanyangPlaces.Get(RouteWorld(route),"colleague").position;
        public static string OtherTrade(GameState s)=>s.nanyang.wageRoute=="shop"?"courier":"shop";
        public static NanyangStep Step(GameState s)
        {
            var d=s.nanyang;string route=d.wageRoute;int i=d.taskProgress;
            var step=new NanyangStep{world=RouteWorld(route),npcId=CharacterRoster.ForNode(route),person=Employer(route),point=Employer(route),confirm="把这一件事办妥"};
            if(d.response=="endure") {
                step.title="只核收已经发给的部分";step.body="结账人催你把已发的钱点清。你看着账上尚欠的四份，暂且把话咽了回去。今天先收妥已发的 "+d.paidWage+" 份，未发的仍记尚欠；这一步不会再领一遍工钱，也不是承认已经结清。";step.confirm="核收已发部分，暂不追问";
            } else if(d.response=="evidence") {
                if(i==0){step.inspect=true;step.point=NanyangPlaces.Get(step.world,"evidence").position;step.title="取回自己的工账";step.body="先核清约定工钱与实领数目，把尚欠的一笔单独留下。";}
                else if(i==1){step.npcId="xusheng";step.person=step.point=Witness(route);step.title="请许生核认见证";step.body="许生翻看副单：那天你做完的活，我记得。我只能证明亲眼见到的这一趟，不能替未做过的工凑数。把日期和我的名字都记下，我陪你再去一趟。";step.confirm="记下日期与见证，再去结账处";}
                else {step.title="带副单追讨工钱";step.body="约定、实领和许生的见证对上了。结账处愿意补还三份，余下一份仍有争议。你把补还的数目写在原欠账旁，保留没有解决的那一笔。";step.confirm="收回三份，保存尚欠记录";}
            } else if(d.response=="together") {
                if(i==0){step.npcId="xusheng";step.person=step.point=Witness(route);step.title="先同工友对清各自的账";step.body="许生答应陪你，但要先把各人的工钱分开：只讲自己做过、能核实的事。大家约定一同到结账处，轮流说明，不替彼此许下不能做到的保证。";step.confirm="对清工账，一起去交涉";}
                else {step.title="与工友一起交涉";step.body="几份分开的工账摆到柜台上，结账人终于补还四份。等候和反复交涉耗去了半日，你又饿又累。许生说，下次先问清结算办法，也留出吃饭的时间。";step.confirm="核收四份，记住这次照应";}
            } else {
                if(i==0){string next=OtherTrade(s);step.world=RouteWorld(next);step.npcId=CharacterRoster.ForNode(next);step.person=step.point=Employer(next);step.inspect=true;step.point=NanyangPlaces.Get(step.world,"evidence").position;step.title="先看清新工作的条件";step.body="换一份工作之前，把工钱、结算时候与住处各问清。新机会不等于旧欠款已经偿清。";}
                else {step.world="quarters";step.npcId="xusheng";step.person=step.point=P(-22,8);step.title="安顿新的落脚处";step.body="许生腾出靠窗的一张床，陪你把行李放稳。旧欠款没有追回，你把它留在簿上，先离开那处不肯照约定结账的工作。下次接另一行的活，有同乡介绍可以多领两份。";step.confirm="安顿下来，保留旧账";}
            }
            return step;
        }
        public static void Apply(GameState s,MissionInfo m)
        {
            if(!Applies(s))return;
            if(NanyangLife.IsBlocking(s)) {
                var d=s.nanyang;m.activity="talk";m.required=0;
                if(d.stage=="choice"){m.world=RouteWorld(d.wageRoute);m.npcPosition=Employer(d.wageRoute);m.itemName="被扣下的工钱";m.objective="核清实领工钱，决定怎样应对。";}
                else if(d.stage=="tasks") {var st=Step(s);m.world=st.world;m.npcPosition=st.person;m.itemName=st.title;m.objective=st.title;m.activity=st.inspect?"inspect":"talk";m.required=st.inspect?1:0;}
                else {m.world=d.stage=="reply"?"postoffice":"quarters";m.npcPosition=d.stage=="reply"?NanyangPlaces.Get("postoffice","reply").position:P(-22,8);m.itemName=d.stage=="reply"?"泉州家人的回批":"一段日子与一封家书";m.objective=d.stage=="letter"?"到住处写批间，把这段经历写给家人。":d.stage=="waiting"?"与许生聊聊之后的日子，等数周后的回批。":"到信局柜台，亲手接过泉州回批。";}
                m.itemPosition=m.destination=m.npcPosition;return;
            }
            string job=s.journey.pendingJob;
            if(job=="rest"){m.world="quarters";m.activity="rest";m.required=0;m.npcPosition=P(-22,8);m.itemPosition=P(-27.5f,4.1f);m.itemName="通铺床位";m.objective="走到通铺床边，吃饭歇肩，留一日养身。";return;}
            if(string.IsNullOrEmpty(job)){
                m.world=s.nanyang.visitingWorld;
                m.npcPosition=m.world=="quarters"?P(-22,8):Employer(m.world=="port"?"dock":m.world=="market"?"shop":"courier");
                m.activity="talk";m.required=0;m.itemPosition=m.destination=m.npcPosition;m.objective="沿街走走、和熟人聊聊，或打开「安排生活」接下一份工。";return;
            }
            int level=NanyangLife.CareerLevel(s,job);
            m.itemName=level==1?(job=="dock"?"待入栈货包":job=="shop"?"进货单":"已核地址的批包"):(job=="dock"?"货物与记工账":job=="shop"?"柜台经营账":"款额与转交记录");
            if(level>=2){m.activity="inspect";m.required=2;m.objective="办妥两处"+m.itemName+"的交接，再找"+m.npcName+"结账。";}
            else {m.objective="亲手完成这份入行工作，再去结账。";}
        }
        public static string NpcId(GameState s)
        {
            if(!Applies(s))return null;
            if(!NanyangLife.IsBlocking(s)) {
                if(!string.IsNullOrEmpty(s.journey.pendingJob))return s.journey.pendingJob=="rest"?"xusheng":CharacterRoster.ForNode(s.journey.pendingJob);
                string world=s.nanyang.visitingWorld;return world=="quarters"?"xusheng":CharacterRoster.ForNode(world=="port"?"dock":world=="market"?"shop":"courier");
            }
            var d=s.nanyang;
            if(d.stage=="choice")return CharacterRoster.ForNode(d.wageRoute);
            if(d.stage=="tasks")return Step(s).npcId;
            return d.stage=="reply"?"clerk":"xusheng";
        }
        public static Vector3[] InspectionPositions(GameState s)
        {
            if(!Applies(s))return FirstDayTasks.InspectionPositions(s);
            if(NanyangLife.IsBlocking(s)&&s.nanyang.stage=="tasks")return new[]{Step(s).point,P(21,7),P(0,28)};
            string route=s.journey.pendingJob;
            if(!string.IsNullOrEmpty(route)&&route!="rest"&&NanyangLife.CareerLevel(s,route)>=2){
                string world=RouteWorld(route);
                var first=NanyangPlaces.Get(world,route=="courier"?"archive":"accounts").position;
                var second=NanyangPlaces.Get(world,route=="dock"?"count":route=="shop"?"deliverylist":"verify").position;
                return new[]{first,second,P(0,28)};
            }
            return FirstDayTasks.InspectionPositions(s);
        }
        public static bool FurnishedInspection(GameState s)=>Applies(s)&&(NanyangLife.IsBlocking(s)||NanyangLife.CareerLevel(s,s.journey.pendingJob)>=2);
        public static InspectionCase Inspection(GameState s,int index)
        {
            if(NanyangLife.IsBlocking(s)) {
                var d=s.nanyang;
                if(d.response=="evidence")return Case("一笔工钱，三项数字","约定与完工单","本轮工钱："+d.promisedWage+"份\n工作已完成\n许生见过当天的交接","结账记录","实际发给："+d.paidWage+"份\n四份被划作临时押钱\n没有事先约定","应怎样留下这笔记录？",1,"把实领改成约定数，算已结清","分列应付、实领、尚欠四份，留下日期","再记一笔整份工钱，重复追讨","工账已经核清；接着去找见证人。尚欠的一笔不会被误写成已收。 ");
                return Case("换工之前问明白","新工作条款","工钱按完成的班次结算\n交接单由双方核认\n住处与工钱分开计","口头招呼","“明日可以来，条件回头说。”\n还没有约定哪天结算\n旧工作的四份仍未收到","哪一步能让换工有根据？",2,"先签空白单，以后再填工钱","请新雇主把旧欠款算成已付","确认本次工钱和结算日，旧欠款另留底","新工作的条件问清了。下一步回住处安顿行李，旧账仍保留。 ");
            }
            string route=s.journey.pendingJob;int level=NanyangLife.CareerLevel(s,route);
            if(level<2)return InspectionCases.Get(route,index);
            if(route=="dock") {
                if(level==2)return index==0?Case("核货员的第一张分栈单","货船交接","米货共十二包\n东栈已收五包\n西栈已收四包","栈外待点","三包仍在棚下\n尚无入栈签记","眼前应该怎样交接？",0,"把三包列为待交，分栈数目各自留底","把棚下三包也记作东栈已收","十二包都到岸，就算两栈收齐","待交货和已收货分清，下一处要核实谁接了手。") : Case("谁接过这一批货","货栈副单","货数已对齐\n两位搬工分趟入栈","收货页","日期已有\n最后一趟的收货人空白","收工前补哪项？",2,"只请搬工保证没有差错","划掉最后一趟","请实际收货人核认并补经手姓名","两处交接都留得住凭据，可以回去结算核货工钱。 ");
                return index==0?Case("管货时先分轻重缓急","今日货情","一批怕雨的纸货仍在露天\n一批米货已入遮棚","工班安排","两名工人可先转移一批\n没有额外加班约定","先怎样安排？",1,"两批都留露天，等工头回来","先转纸货入棚，米货按原序核交","让工人同时扛两包，省一半工钱","纸货先入棚，安排要照顾货，也要照顾搬运的人。") : Case("工钱不能被货数遮住","两名搬工的记录","各完成一趟\n每趟按约定结算","账房草录","把两人的趟数合成一趟\n准备只发一份工钱","作为管货人怎样签字？",0,"分别核认两人的趟数，订正后再签","总货数没错，直接签字","先扣钱，等搬工来问","你没有重复自己吃过的亏。各人的工作与工钱都被分别记下。 ");
            }
            if(route=="shop") {
                if(level==2)return index==0?Case("柜台经营先算清进出","今天进货","布六匹，每匹三份\n进价合计十八份","售货簿","卖出两匹，每匹四份\n另外四匹仍在货架","今天应记哪笔实收？",2,"二十四份，按六匹全卖出计算","十八份，把进价当营业收入","实收八份，存货四匹另记","钱与存货分开记，柜台上的收入才有依据。") : Case("赊账也要讲清约定","熟客买米","米价五份\n今天付三份","约定","余款两份，下次赶集偿还\n双方已核认","怎样写在同一页账上？",0,"实收三份、余欠两份，注明约定","收到三份便记五份全部结清","已经收三份，另把五份全记欠账","你记住来往的人，也把双方的约定写清。 ");
                return index==0?Case("独立看店的一笔进货","货架和近两日销售","盐还够卖三日\n米只够卖半日","可用本钱","今天只够补一批\n未问清借款条件","怎样安排进货？",1,"全补盐，米价留待顾客来问","先补紧缺的米，留下实收和进货凭据","用还未约定的借款把两批全买下","你按铺中实际需要安排本钱，下一处再把收铺账核清。") : Case("独自关店前的账","当日流水","开门现钱十份\n卖货实收八份\n进货支出六份","收铺钱匣","实点十二份\n另有客欠两份未收","怎样向掌柜交接？",0,"现钱十二份，客欠两份单列","现钱十四份，把未收的钱一并算入","现钱十二份，把客欠划作已经还清","店里的钱、货、欠款都有去处，掌柜能据这一页继续经营。 ");
            }
            if(level==2)return index==0?Case("核账先对款额","寄件人核认","附银八份\n姓名与村名已核清","收寄草录","款额误抄为十份\n尚未封批","先如何处理？",2,"名字对就直接封批","按十份增加寄件人的欠款","按实收八份订正，保留核认记号","实收决定款额，下一处再核验收件地址。") : Case("同名不是同一个收件人","两位同名者","同叫陈阿福\n一人住南街，一人住北巷","批封","指定北巷陈阿福\n南街来人说彼此是族亲","能怎样交批？",1,"同姓同名即可交给南街来人","按指定住处核认本人，亲属关系另记","删去街巷，只保留姓名","姓名与住处一起核认，等候的人才不会失去自己的信。 ");
            return index==0?Case("迟批还差哪一段","海外与厦门底单","海外交寄有记录\n厦门转交有日期","泉州回函","尚无最终收妥签记\n家中仍在等待","目前能确定什么？",0,"已到转交环节，继续询问泉州收妥记录","有厦门日期便算家人已经领到","家中催问说明寄件人没有交钱","先划清已知和未知，不能把猜测当作送达。") : Case("给等待的人一个实在答复","追查进度","两处经手记录已核\n最终收款仍待核认","家属来问","“是不是钱已经丢了？”\n目前没有遗失证明","怎样写进回话？",2,"保证明天一定拿到钱","认定遗失，让家里重新寄钱","照实写已查到哪里、仍待谁答复","你把迟批的进度讲清，没有把等候的人留在没有根据的保证里。 ");
        }
        static InspectionCase Case(string title,string lt,string left,string rt,string right,string question,int correct,string a,string b,string c,string success)
        {return new InspectionCase{title=title,leftTitle=lt,leftText=left,rightTitle=rt,rightText=right,question=question,correct=correct,answers=new[]{a,b,c},success=success,failure="再看两份记录，把已经确定的事实与仍待核认的部分分开。"};}
        static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    }
}
