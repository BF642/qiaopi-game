using System;
using UnityEngine;

namespace Qiaopi
{
    [Serializable]
    public sealed class MissionInfo
    {
        public string world;
        public string npcName;
        public string objective;
        public string activity;
        public string itemName;
        public int required;
        public Vector3 npcPosition;
        public Vector3 itemPosition;
        public Vector3 destination;
    }

    /// <summary>
    /// Physical objectives that precede StoryEngine choices. Get returns fresh data
    /// and never changes the narrative state or decides a player's story choice.
    /// Talk/board objectives are completed by reaching their speaker, not a timer.
    /// A zero requirement means no additional task gates the final conversation.
    /// Inspect objectives use three distinct stops: itemPosition, destination,
    /// and npcPosition. The last stop is also where the NPC conversation opens.
    /// Deliver objectives repeat the item-to-destination trip required times.
    /// </summary>
    public static class WorldMissions
    {
        public static MissionInfo Get(string nodeId)
        {
            var m=Legacy(nodeId);
            switch(nodeId){
                case "peace":Set(m,"quanzhou",P(-23,-5),P(-23,-5),P(-23,-5));m.required=1;m.objective="走进陈家院，坐到家人身边，过一段不必急着离开的日子。";break;
                case "pressure":Set(m,"quanzhou",P(-23,-5),P(-23,-5),P(-23,-5));m.required=1;m.objective="雨灾之后，回到家门前，和母亲商量催债人留下的难题。";break;
                case "home":Set(m,"quanzhou",P(-23,-5),P(-26,-13),P(-23,-5));break;
                case "funding":Set(m,"quanzhou",P(21,18),P(21,18),P(21,18));m.objective="穿过晒埕和东巷，到族亲堂屋商量盘缠。";break;
                case "farewell":Set(m,"quanzhou",P(0,30),P(0,30),P(0,30));m.objective="沿村路走到北面的田埂村口，与家人道别。";break;
                case "passage":Set(m,"harbor",P(0,30),P(0,30),P(0,30));m.npcName="登船处";m.objective="穿过厦门出洋街，沿栈桥走到船口按 E 登船。";break;
                case "shore":Set(m,"port",P(-21,-7),P(-21,-7),P(-21,-7));m.objective="离开新加坡登岸路，到接应棚与同乡会面。";break;
                case "dock":Set(m,"port",P(23,15),P(-23,-18),P(21,24));m.objective="从南侧货堆取两件货包，分次运到沿岸货栈，再找工头。";break;
                case "shop":Set(m,"market",P(-20,19),P(-22,-4),P(21,7));m.objective="走访棚市、进货后院与北街的三份单据，再回陈记柜台。";break;
                case "courier":Set(m,"postoffice",P(-21,10),P(-22,-16),P(21,13));m.objective="从收件处取批包，穿过批局院落送到留底柜台，再找何师傅。";break;
                case "first_pay":Set(m,"quarters",P(-22,8),P(-22,8),P(-22,8));m.objective="回到客工住处，走进通铺院，与同住乡亲商量第一笔积蓄。";break;
                case "first_letter":Set(m,"postoffice",P(-21,10),P(23,-15),P(-21,10));m.objective="在东南纸铺取纸笔，再穿过街口到批局写批台。";break;
                case "reply":Set(m,"quarters",P(22,14),P(22,14),P(22,14));m.objective="到住处的晾衣后院，接过从故乡寄来的回批。";break;
                case "records":case "trace_desk":Set(m,"postoffice",P(-21,10),P(-22,-4),P(21,7));m.objective=nodeId=="records"?"走访收寄处、留底柜台和封袋后院，核对三份记录。":"沿收寄柜台、账房和后院查单，再找何师傅核认。";break;
                case "missing":Set(m,"quarters",P(0,27),P(0,27),P(0,27));break;
                case "trace_harbor":Set(m,"port",P(23,15),P(-18,7),P(24,23));break;
                case "trace_family":Set(m,"quarters",P(22,14),P(22,14),P(22,14));break;
                case "storms":Set(m,"market",P(22,-18),P(22,-18),P(22,-18));break;
                case "horizon":Set(m,"port",P(0,29),P(0,29),P(0,29));m.objective="穿过货运港，去北端候船路口问清下一程的安排。";break;
                case "return_choice":case "end_home_lamp":case "end_home_scar":Set(m,"quanzhou",P(-23,-5),P(-23,-5),P(-23,-5));break;
                case "end_new_harbor":Set(m,"ship",P(0,18),P(0,18),P(0,18));break;
                case "end_shop_bridge":Set(m,"market",P(-20,19),P(-20,19),P(-20,19));break;
                case "end_trusted_route":Set(m,"postoffice",P(-21,10),P(-21,10),P(-21,10));break;
                default:Set(m,"quarters",P(-22,8),P(-22,8),P(-22,8));break;
            }
            return m;
        }
        public static MissionInfo Get(GameState state)
        {
            var m=Get(state.nodeId);
            if(LifeJourney.IsActive(state)){
                string job=state.journey.pendingJob;
                if(!string.IsNullOrEmpty(job)){
                    m=Get(job);m.required=1;
                    m.objective=job=="dock"?"把一件货包从货堆运到货栈，再到工头处结清工钱。":job=="shop"?"核清棚市的一张货单，再回陈记柜台结账。":"从收件处取批包，亲自送到留底柜台，再回信局结账。";
                }else{m.activity="talk";m.required=0;m.objective="打开「安排生活」，决定下一轮接活、养身或寄钱。做满三轮后可继续家书主线。";}
            }
            if(state.nodeId=="passage"&&state.flags.Contains("aboard_passage")){
                Set(m,"ship",P(0,18),P(0,18),P(0,18));m.npcName="许生";m.objective="走过货舱口和旅客甲板，到船首与同行的许生交谈。";m.activity="talk";
            }
            if(state.nodeId.StartsWith("end_")&&state.flags.Contains("return_ticket"))Set(m,"quanzhou",P(-23,-5),P(-23,-5),P(-23,-5));
            m.objective=LifeJourney.Localize(state,m.objective);
            return m;
        }
        static void Set(MissionInfo m,string world,Vector3 npc,Vector3 item,Vector3 destination){m.world=world;m.npcPosition=npc;m.itemPosition=item;m.destination=destination;}
        static MissionInfo Legacy(string nodeId)
        {
            switch (nodeId)
            {
                case "home":
                    return M("quanzhou", "母亲", "到屋旁取回待整理的行囊，再回到母亲身边。", "collect", "待整理的行囊", 1,
                        P(-8, 4), P(7, -5), P(-8, 4));
                case "funding":
                    return M("quanzhou", "族亲", "走到堂屋前，与族亲商量离乡的盘缠。", "talk", "旅费借据与介绍信", 1,
                        P(-8, 4), P(-8, 4), P(-8, 4));
                case "farewell":
                    return M("quanzhou", "母亲与阿满", "到村口与母亲、阿满道别，听完家里的叮嘱。", "talk", "临行的叮嘱", 1,
                        P(0, 12), P(7, 5), P(0, 12));
                case "passage":
                    return M("harbor", "船上同乡许生", "沿码头走到登船口，与同行的许生交谈。", "board", "南下的船票", 1,
                        P(0, 12), P(7, 5), P(0, 12));
                case "shore":
                    return M("harbor", "接应的同乡", "离开栈桥，到岸边与接应的同乡打听落脚处。", "talk", "招工消息", 1,
                        P(-8, 4), P(7, 5), P(-8, 4));
                case "dock":
                    return M("singapore", "码头工头", "从货堆取起两件货包，逐件送到货栈，再与工头交谈。", "deliver", "码头货包", 2,
                        P(-8, 4), P(8, -5), P(0, 12));
                case "shop":
                    return M("singapore", "陈掌柜", "走到三个记账处核对进货单，最后到柜台向陈掌柜回话。", "inspect", "进货单", 3,
                        P(-8, 4), P(7, 5), P(-7, -4));
                case "courier":
                    return M("singapore", "何师傅", "把一件待核对的批件送到信局柜台，再向何师傅请教。", "deliver", "封好的批件", 1,
                        P(-8, 4), P(8, -5), P(-7, 6));
                case "first_pay":
                    return M("singapore", "同住的乡亲", "走回住处，与同住的乡亲商量怎样安排第一笔积蓄。", "talk", "工钱袋", 1,
                        P(-7, -4), P(7, 5), P(-7, -4));
                case "first_letter":
                    return M("singapore", "信局伙计", "先到纸铺取一份纸笔，再到写批桌前寄出第一封家书。", "collect", "空白批纸与笔", 1,
                        P(-8, 4), P(8, -5), P(-8, 4));
                case "reply":
                    return M("singapore", "送信人", "到住处门口接过故乡的回批，听送信人说明来处。", "talk", "母亲的第一封回批", 1,
                        P(7, 5), P(7, 5), P(7, 5));
                case "records":
                    return M("singapore", "信局伙计", "依次查看寄件底单、门牌记录与柜台款额，再决定如何留底。", "inspect", "寄批核对单", 3,
                        P(-8, 4), P(7, 5), P(-7, -4));
                case "missing":
                    return M("singapore", "捎信的同乡", "到街口找捎信的同乡，问清家中为什么仍在等款。", "talk", "家中的短话", 1,
                        P(8, -5), P(7, 5), P(8, -5));
                case "trace_desk":
                    return M("singapore", "何师傅", "走访三处记录桌，核对底单、经手日期与旧门牌，再与何师傅会合。", "inspect", "迟批经手记录", 3,
                        P(-8, 4), P(7, 5), P(-7, -4));
                case "trace_harbor":
                    return M("singapore", "货栈旧识", "把待查款项的询问单送到货栈，再向旧识打听转交记录。", "deliver", "迟批询问单", 1,
                        P(-8, 4), P(8, -5), P(0, 12));
                case "trace_family":
                    return M("singapore", "送信人", "到街角接家里的补信，核实旧屋与姨家的两处门牌。", "talk", "家里寄来的地址补信", 1,
                        P(7, 5), P(7, 5), P(7, 5));
                case "storms":
                    return M("singapore", "熟识的乡亲", "回到旧街与乡亲会面，商量旧账、家用和今后的力气。", "talk", "多年往来账", 1,
                        P(-7, -4), P(7, 5), P(-7, -4));
                case "horizon":
                    return M("singapore", "带来消息的同乡", "去街口与同乡会面，问清回乡与再远行的安排。", "talk", "下一程的消息", 1,
                        P(0, 12), P(7, 5), P(0, 12));
                case "return_choice":
                    return M("quanzhou", "母亲与阿满", "你已回到家门前，走近家人，说出往后的打算。", "talk", "归家的行囊", 0,
                        P(-8, 4), P(7, 5), P(-8, 4));
                case "stay_choice":
                    return M("singapore", "熟识的乡亲", "走到写信的桌前，决定此后如何安顿生活。", "talk", "此后的家书", 0,
                        P(-8, 4), P(7, 5), P(-8, 4));
                case "end_home_lamp":
                case "end_home_scar":
                    return End("quanzhou", "母亲与阿满", "回到家人身边，读这一程最后的家书。");
                case "end_new_harbor":
                    return End("harbor", "同行的乡亲", "站在下一程的船口，读写给家人的新地址。");
                case "end_school_window":
                    return End("singapore", "阿满的回信", "读阿满亲笔写来的信，看看那些字走到了哪里。");
                case "end_shop_bridge":
                    return End("singapore", "陈掌柜", "走到亮灯的柜台，读留给故乡的家书。");
                case "end_trusted_route":
                    return End("singapore", "何师傅", "回到信局的桌前，读递批人写给家里的信。");
                case "end_unsent_letter":
                    return End("singapore", "家人的回声", "读终于写全的那一页，把沉默留在身后。");
                case "end_silver_thread":
                    return End("singapore", "同住的乡亲", "回到熟悉的街巷，读照实写下的平常日子。");
                default:
                    // Safe presentation fallback only; StoryEngine still validates node IDs.
                    return M("singapore", "乡亲", "走近乡亲，继续这一程的故事。", "talk", "家书", 0,
                        P(-8, 4), P(7, 5), P(-8, 4));
            }
        }

        private static MissionInfo End(string world, string npcName, string objective)
        {
            return M(world, npcName, objective, "talk", "最后一封家书", 0,
                world == "harbor" ? P(0, 12) : P(-8, 4), P(7, 5), P(0, 12));
        }

        private static MissionInfo M(string world, string npcName, string objective,
            string activity, string itemName, int required, Vector3 npc, Vector3 item, Vector3 destination)
        {
            return new MissionInfo { world = world, npcName = npcName, objective = objective,
                activity = activity, itemName = itemName, required = required,
                npcPosition = npc, itemPosition = item, destination = destination };
        }

        private static Vector3 P(float x, float z) { return new Vector3(x, 0f, z); }
    }
}
