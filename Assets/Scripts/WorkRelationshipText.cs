using System.Text;

namespace Qiaopi
{
    // These lines remember settled work, never the choice on an unfinished job.
    // Family replies are authored from this history at dispatch, not from the
    // player's free-form letter body or from events that happen during transit.
    public static class WorkRelationshipText
    {
        static readonly string[] Routes = { "dock", "shop", "courier" };

        public static string Greeting(GameState state, string route)
        {
            var item = WorkEvents.LastRecord(state, route);
            if (!Known(item)) return "";
            if (item.route == "dock") {
                if (item.level == 2) return item.choice == "cover"
                    ? "上回那阵雨，你一个人先护好了纸货，我记着。今天若肩膀还酸，就先说，别硬撑。"
                    : "上回你肯同工友搭手，纸货和布匹都安顿好了。咱们认得彼此的节奏，再碰上急活便好商量。";
                return item.choice == "teach"
                    ? "上回你放慢手头的活，教新来的同工认货签，我都看见了。下回分货，可以叫他同你搭手。"
                    : "上回那批货你自己接完，账也交清了。我知道你做得熟；有人搭手时，也不必件件都自己扛。";
            }
            if (item.route == "shop") {
                if (item.level == 2) return item.choice == "credit"
                    ? "上回熟客手头紧，你照样称足一斤半，收四、欠二都记清了。人情可以留，欠账可不能写成现钱。"
                    : "上回你同客人商量，只称一斤，四份现钱当面结清。生意少做半斤，双方心里也有个准数。";
                return item.choice == "record"
                    ? "上回两斤半的账，你把实收六份、尚欠三份分开记妥了。往后交账，也照这样一笔一笔说清。"
                    : "上回你把订单改成一斤半，六份现钱当面收清。账上没有新赊欠，客人少买的那一斤也得记得。";
            }
            if (item.returned) return item.level == 2
                ? "上回那封遇上搬家的批，你原封带回，未送达也留了登记。下回得了确切住处，再安排送。"
                : "上回邻人要代收，你没松手，原封退局也登记好了。信和银都还留在局里，不能开收讫回执。";
            return item.level == 2
                ? "上回阿福搬了家，你又绕到南街核清新住处，回执也带齐了。你肯多走这一程，我记着。"
                : "上回你没有把批交给邻人，找到阿福本人核认后才交妥。回执上的名字，是要有人负责的。";
        }

        public static string WageMemory(GameState state)
        {
            if(!NanyangLife.IsEnrolled(state)||state.nanyang.stage!="complete")return "";
            switch(state.nanyang.response) {
                case "endure":return "许生压低声音：上次你先忍下了，那四份还欠着。我记得。哪天想商量，来找我；不想说时，也来坐坐。";
                case "evidence":return "许生说：你留的工账我还记得，三份追回了，剩下的一份不能划掉。往后每次交工，咱们先把日期和数目留清。";
                case "together":return "许生招呼你：那天大家一起把四份工钱讨回来，话是一个个轮着说的。下回谁遇到难处，也先把各人的账对清，再商量怎么帮。";
                case "leave":return "许生说：换工那晚给你腾的床还记得吧？旧欠款没有凭空消失，先把吃住安稳，再接条件说清楚的活。";
                default:return "";
            }
        }

        public static string Summary(GameState state)
        {
            var text = new StringBuilder();
            foreach (string route in Routes) {
                var item = WorkEvents.LastRecord(state, route);
                if (!Known(item)) continue;
                if (text.Length > 0) text.Append("\n");
                text.Append(route == "dock" ? "码头" : route == "shop" ? "铺里" : "批局");
                text.Append(" · ").Append(SummaryFact(item));
            }
            return text.ToString();
        }

        public static string LetterParagraph(GameState state, string recipient)
        {
            var item = Latest(state);
            if (item == null) return "";
            string fact = LetterFact(item);
            if (recipient == "wife") return fact + WifeThought(item);
            if (recipient == "sister") return fact + SisterThought(item);
            return fact + MotherThought(item);
        }

        static WorkEventRecord Latest(GameState state)
        {
            WorkEventRecord latest = null;
            foreach (string route in Routes) {
                var item = WorkEvents.LastRecord(state, route);
                if (Known(item) && (latest == null || item.turn > latest.turn)) latest = item;
            }
            return latest;
        }

        static bool Known(WorkEventRecord item)
        {
            if (item == null || item.level < 2 || item.level > 3) return false;
            if (item.route == "dock") return !item.returned && (item.level == 2
                ? item.choice == "cover" || item.choice == "pair" : item.choice == "teach" || item.choice == "solo");
            if (item.route == "shop") return !item.returned && (item.level == 2
                ? item.choice == "credit" || item.choice == "portion" : item.choice == "record" || item.choice == "cash");
            return item.route == "courier" && (item.returned ? item.choice == "return"
                : item.choice == (item.level == 2 ? "trace" : "personal"));
        }

        static string SummaryFact(WorkEventRecord item)
        {
            if (item.route == "dock") {
                if (item.level == 2) return item.choice == "cover" ? "记工人记着你独自护纸。" : "工友记着与你分头护货。";
                return item.choice == "teach" ? "新同工得过指点，可以搭手。" : "记工人认得你独自理货的手艺。";
            }
            if (item.route == "shop") {
                if (item.level == 2) return item.choice == "credit" ? "熟客记得宽限，尚欠二份。" : "掌柜记着少称半斤、现钱结清。";
                return item.choice == "record" ? "掌柜记着你把现钱、赊欠分清。" : "掌柜记着改小订单、现钱结清。";
            }
            if (item.returned) return "原封退批已登记，尚未送达。";
            return item.level == 2 ? "批局记着你追到新址、取回回执。" : "批局记着你坚持本人核收。";
        }

        static string LetterFact(WorkEventRecord item)
        {
            if (item.route == "dock") {
                if (item.level == 2) return item.choice == "cover"
                    ? "你那回独自赶在雨前护好纸货，急活做完了，肩上的担子却都压在自己身上。"
                    : "你那回和工友分头护纸、搬布，分给他两份工钱，肩上的担子也有人分担。";
                return item.choice == "teach"
                    ? "你那回教新同工认货签、分货，把布箱交给他接手，自己的工钱少了一份。"
                    : "你那回选择自己接完三件货，多领两份工钱，担子也由自己从头扛到尾。";
            }
            if (item.route == "shop") {
                if (item.level == 2) return item.choice == "credit"
                    ? "你给熟客称足一斤半，收了四份，另记欠二份；帮他渡过眼前，也留下了一笔还没收回的钱。"
                    : "你和熟客商量改买一斤，四份现钱当面结清；他少买半斤，铺里也少做半斤的生意。";
                return item.choice == "record"
                    ? "那笔两斤半、价九份的米，你实收六份，另记赊欠三份；账写清了，欠下的钱仍要等。"
                    : "你把那笔米改成一斤半，价六份、实收六份；少卖了一斤，也没有添上新的赊欠。";
            }
            if (item.returned) return item.level == 2
                ? "那封遇上收件人搬家的批，你原封退回批局并登记，少领了工钱；信和银还未送到本人手里，这一趟没有收讫回执。"
                : "邻人提出代收那封批，你没有交出去，原封退局并登记，也少领了工钱；信和银还留在局里，这一趟没有收讫回执。";
            return item.level == 2
                ? "阿福搬了家，你又绕到南街核对新住处，亲手交妥才取回执；为找对一个人，多走了一程。"
                : "邻人想代收的那封批，你坚持找到阿福本人，核认后交妥再取回执；赶路之外，又费了一番核人的工夫。";
        }

        static string WifeThought(WorkEventRecord item)
        {
            if (item.route == "dock") return item.choice == "pair" || item.choice == "teach"
                ? "我接针线订单，也想把能同邻里分着做的活先讲明白；愿意帮人，和把自己累垮，是两回事。"
                : "我这边针线订单仍只接白日做得完的。你也给自己留一点歇息，不必每回都靠多出力撑过去。";
            if (item.route == "shop") return item.choice == "credit" || item.choice == "record"
                ? "我的针线账也要把已收的、还欠的分开；答应宽几日可以，买布线的钱却不能当成已经回来了。"
                : "我的针线订单也要先把价钱和份量说妥，做得少些无妨，手头要留得下买布线的钱。";
            return item.returned
                ? "我知道你把关口守住了，也惦记那边还等着信的人。事情没办成便如实记着，往后有准信再办；我交针线活也要当面把收付说清。"
                : "我交针线订单，也要把谁来取、收了多少记明白。你肯把一封批认真交到人手里，我懂这份费事。";
        }

        static string SisterThought(WorkEventRecord item)
        {
            if (item.route == "dock") return item.choice == "teach"
                ? "阿兄也会教人了。我认货签上的字还慢，下回想请你把易认错的写给我，别只替我把答案填好。"
                : "我想把货签上的字认熟，再学着列一张分工单；能少认错一件，也许就能少搬一趟。";
            if (item.route == "shop") return item.choice == "credit" || item.choice == "record"
                ? "我拿这笔数练账，收到的写一栏，欠着的另写一栏；纸上合得上，也不等于钱已经全在匣里。"
                : "我拿这笔数练账，也记着人家少买了多少。数字算对以后，还要知道这一笔是怎样商量成的。";
            return item.returned
                ? "我练着写“退回”和“收讫”，这两个词可不能混。也盼那位等批的人，往后能有确切的音信。"
                : "我近来练认批封上的乡名和门牌，才知道同名也可能是两个人。阿兄多核的一遍，我会记住。";
        }

        static string MotherThought(WorkEventRecord item)
        {
            if (item.route == "dock") return item.choice == "pair" || item.choice == "teach"
                ? "出门在外，肯同人搭手是好事。帮了别人，也要把自己的饭食和歇息顾上。"
                : "母亲知道你肯出力，只怕你把辛苦都收在自己身上。饭照时吃，肩膀累了就歇一歇。";
            if (item.route == "shop") return item.choice == "credit" || item.choice == "record"
                ? "邻里手紧时留点余地，也是过日子的办法；记账要清楚，等着收的钱不要先花出去。"
                : "多少货、多少钱，当面讲明白便少一层为难。也记着人家手头的难处，话说得和气些。";
            return item.returned
                ? "守住银信是你的本分，等批的人却还没等到。下回有确切住处，便请局里接着问，莫把未送达当成已办完。"
                : "海那头的人盼这封批，同我们盼你的信一样。你把姓名住处核清再交，母亲心里明白这份认真。";
        }
    }
}
