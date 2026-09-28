using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class CharacterInfo
    {
        public string id, name, role, model;
    }

    /// <summary>Speaker identities are independent of the active mission's label.</summary>
    public static class CharacterRoster
    {
        private static readonly CharacterInfo[] People = {
            Person("wensheng", "陈文生", "从泉州晋江远行的青年", "Wensheng"),
            Person("mother", "母亲", "留在故乡照应家人的陈母", "Mother"),
            Person("aman", "阿满", "文生的妹妹", "Aman"),
            Person("uncle", "族亲", "替晚辈张罗旅费的长辈", "Uncle"),
            Person("xusheng", "许生", "同渡海路、后来相互照应的同乡", "Xusheng"),
            Person("guide", "接应同乡", "往来码头、替乡人传递消息", "Guide"),
            Person("foreman", "码头工头", "熟悉货栈与转交门路", "Foreman"),
            Person("shopkeeper", "陈掌柜", "杂货铺掌柜", "Shopkeeper"),
            Person("master_he", "何师傅", "教人核批、经手银信的老师傅", "MasterHe"),
            Person("clerk", "信局伙计", "照应柜台、递送来往信件", "Clerk"),
            Person("worker", "帮工乡亲", "在街巷与货栈间做工的乡亲", "Worker"),
            Person("narrator", "旁白", "情境叙述与未说出口的心绪", "Wensheng")
        };

        public static CharacterInfo Get(string id)
        {
            foreach (CharacterInfo person in People)
                if (person.id == id) return Person(person.id, person.name, person.role, person.model);
            throw new ArgumentException("Unknown character: " + id, "id");
        }

        public static List<CharacterInfo> All()
        {
            var result = new List<CharacterInfo>();
            foreach (CharacterInfo person in People) result.Add(Get(person.id));
            return result;
        }

        public static string ForNode(string nodeId)
        {
            switch (nodeId)
            {
                case "peace": case "pressure": case "home": case "farewell": case "return_choice":
                case "end_home_lamp": case "end_home_scar": case "end_unsent_letter": return "mother";
                case "funding": return "uncle";
                case "passage": case "first_pay": case "storms": case "stay_choice":
                case "end_new_harbor": case "end_silver_thread": return "xusheng";
                case "shore": case "missing": case "horizon": return "guide";
                case "dock": case "trace_harbor": return "foreman";
                case "shop": case "end_shop_bridge": return "shopkeeper";
                case "courier": case "trace_desk": case "end_trusted_route": return "master_he";
                case "first_letter": case "reply": case "records": case "trace_family": return "clerk";
                case "end_school_window": return "clerk";
                default: throw new ArgumentException("Unknown story node: " + nodeId, "nodeId");
            }
        }

        private static CharacterInfo Person(string id, string name, string role, string model)
        {
            return new CharacterInfo { id = id, name = name, role = role, model = model };
        }
    }
}
