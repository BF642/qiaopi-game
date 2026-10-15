using UnityEngine;

namespace Qiaopi
{
    // Each saved branch owns a concrete world task. Mission progress is reset
    // when choosing the branch, then restored against these same definitions.
    public static class FirstDayTasks
    {
        public static void Apply(GameState s, MissionInfo m)
        {
            if (!FirstDayChapter.IsActive(s)) return;
            var d=s.firstDay;
            if (d.stage=="work" || d.stage=="encounter") {
                if (string.IsNullOrEmpty(d.choice)) return;
                switch(d.route+"/"+d.choice) {
                    case "dock/extra": Delivery(m,"加急货包",P(-23,-18),P(21,24));break;
                    case "dock/help": Delivery(m,"替工友分担的货包",P(-23,-18),P(18.4f,24));break;
                    case "dock/tally": Inspect(m,2,"货栈交接单");break;
                    case "shop/learn": Inspect(m,2,"提货与结欠记录");break;
                    case "shop/wages": Inspect(m,1,"晚班售货账");break;
                    case "shop/read": Inspect(m,1,"同乡家信");break;
                    case "courier/rush": Delivery(m,"地址齐全的批包",P(-22,-16),P(21,13));break;
                    case "courier/verify": Inspect(m,1,"待补门牌的批封");break;
                    case "courier/craft": Inspect(m,2,"款额与经手记录");break;
                }
                m.objective="办妥眼前这份差事，再找"+m.npcName+"结清今日工钱。";
                return;
            }
            m.world="quarters";m.activity="talk";m.required=0;
            m.npcPosition=d.stage=="reply"?P(22,14):P(-22,8);
            m.itemPosition=m.destination=m.npcPosition;
            m.npcName=d.stage=="reply"?"送回批的伙计":"许生";
            m.itemName="首日生活";
            m.objective=d.stage=="letter"?"走进住处的写批间，坐到桌前写下今天。":d.stage=="bed"?"回通铺院床边歇息，明早再出门。":d.stage=="reply"?"数周后，到后院门口接过泉州回批。":"到通铺院与许生聊聊。";
        }
        static void Delivery(MissionInfo m,string item,Vector3 from,Vector3 to){m.activity="deliver";m.required=1;m.itemName=item;m.itemPosition=from;m.destination=to;}
        static void Inspect(MissionInfo m,int count,string item){m.activity="inspect";m.required=count;m.itemName=item;}
        public static Vector3[] InspectionPositions(GameState s)
        {
            return IndoorInspectionPlaces.Positions(s);
        }
        public static InspectionCase Inspection(GameState s,int index)
        {
            if(!FirstDayChapter.IsActive(s)||string.IsNullOrEmpty(s.firstDay.choice))return InspectionCases.Get(s.nodeId,index);
            var d=s.firstDay;
            if(d.route=="dock") {
                if(index==0)return Case("同一批货不能算两次","装船底单","本趟应交：6包\n上午已交：4包\n两包暂放南货堆","收栈记录","上午签收：4包\n余下两包尚未签收","现在还有多少货应当补交？",1,"六包都要再搬一次","只补交余下两包，分开记录","底单写六包，直接算全部收齐","已交与待交分开，工友的货数和工钱才不会被重复扣算。");
                return Case("把见证留在纸上","补交记录","下午补交：2包\n收栈人：林伙计\n时间：未填","工头交代","上午四包已结算\n这两包须另列经手人及日期","该怎样留这张底单？",0,"补齐日期和经手人，再请双方核认","把上午四包也抄成今天补交","只写总数，名字以后再记","你将两趟交接分开留底，工头照记录结清了这一日的工钱。");
            }
            if(d.route=="shop") {
                if(d.choice=="wages")return InspectionCases.Get("shop",1);
                if(d.choice=="learn")return index==0?Case("少的一袋在哪里","午后盘点","账上应存：米7袋\n铺内实点：米6袋\n待查差额：1袋","后院提货单","另1袋已交货栈寄存\n货主仍是陈记\n尚未搬回铺内","怎样把这袋米记清？",2,"认定丢失，让新伙计赔","把实点改成七袋，略去寄存","分记铺内六袋、栈存一袋，并留底单","少的一袋有了去处。你把货在哪里、凭什么查得到，都写进了同一页账。") : InspectionCases.Get("shop",2);
                return Case("替同乡把话读明白","泉州家信（剧情示意）","前批寄来六份家用，已收妥。\n屋瓦补了两行。\n下次不必急寄，先留足饭钱。","同乡的疑问","“她说补了两行，是不是钱不够？\n是不是要我明日再寄六份？”","家人这回明确交代了什么？",0,"款已收到；先顾吃饭，不必急着再寄","必须明日再寄六份","只补了两行，证明钱没收到","你逐句读给他听，没有替家人添一句催款。他松开攥着批纸的手，问你往后还会不会在这里。");
            }
            if(d.route=="courier") {
                if(d.choice=="verify")return Case("旧门牌与新住处","待核批封（剧情示意）","收件人：林阿福\n旧址：南街十七号\n附注：现住新厝，未写巷名","收寄处留底","林阿福原住南街十七号\n新厝有同名住户\n尚无本人收妥签记","这封地址不全的批，该怎样交接？",1,"送给新厝任意同名住户","保留旧址，补问巷名与本人身份后再交","把旧地址划去，写新厝即可","你把待补的信息单独留底，师傅安排熟悉这条路的伙计核认，批件没有被交给不相干的人。");
                return index==0?Case("六份与九份","交款留底","实收款额：六份\n寄件人已核认","学徒草录","款额栏：九份\n姓名、日期相同","封批前先改哪一项？",2,"姓名相同，可以封批","以草录九份为准","核对实收，将九份订正为六份","你先对实收再落笔，师傅在更正处留了核认记号。") : Case("交出去也留得住线索","交件页","今日交给：何师傅\n件数：一封\n经手日期：空白","师傅提醒","款额已核对\n还须记录何时、由谁转出","这一页还缺什么？",0,"补上经手日期，并请经手人核认","再把款额加一遍","把日期空着，日后靠记忆","一封批的去向有了可追查的记录，往后有人来问，不必只凭一句记得。");
            }
            return InspectionCases.Get(s.nodeId,index);
        }
        static InspectionCase Case(string title,string leftTitle,string left,string rightTitle,string right,string question,int correct,string a,string b,string c,string success)
        {return new InspectionCase{title=title,leftTitle=leftTitle,leftText=left,rightTitle=rightTitle,rightText=right,question=question,correct=correct,answers=new[]{a,b,c},success=success,failure="再对照两份记录，找出已经核实的数目与仍待确认的地方。"};}
        static Vector3 P(float x,float z){return new Vector3(x,0,z);}
    }
}
