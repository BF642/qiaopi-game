using System;
using System.Text;

namespace Qiaopi
{
    // Authored correspondence, selected from recorded events and explicit letter
    // choices. The free-form body is deliberately never inspected or interpreted.
    public static class ExperienceLetters
    {
        public static string Reply(GameState state, PersonalLetterDraft draft)
        {
            if(state==null)throw new ArgumentNullException("state");
            if(draft==null)throw new ArgumentNullException("draft");
            string who=draft.recipient;
            var text=new StringBuilder(who=="wife"?"文生：\n":who=="sister"?"阿兄：\n":"文生吾儿：\n");
            text.Append(Intention(who,draft.intent));
            // The envelope's explicit intention is the disclosure boundary.
            // Family cannot infer unpaid wages or exhaustion from hidden state,
            // and the player's freely written words are never classified.
            bool disclosed=draft.intent=="truth";
            if(disclosed) {
                string wage=WageExperience(state,who);
                if(!string.IsNullOrEmpty(wage))text.Append("\n").Append(wage);
                string firstDay=FirstDayChapter.LetterMemory(state,who);
                // The first letter already recalls the particular first-day
                // choice. Add career progress once later work has occurred.
                string work=string.IsNullOrEmpty(firstDay)||state.journey!=null&&state.journey.turns>0
                    ?WorkExperience(state,who):"";
                if(!string.IsNullOrEmpty(work))text.Append("\n").Append(work);
                if(!string.IsNullOrEmpty(firstDay))text.Append("\n").Append(firstDay);
                string relationship=WorkRelationshipText.LetterParagraph(state,who);
                if(!string.IsNullOrEmpty(relationship))text.Append("\n").Append(relationship);
            }
            if(disclosed&&state.health<40)text.Append("\n").Append(who=="wife"
                ?"近来体力已经吃紧，下一轮少接一点也无妨。我的活有我的安排，你先把饭和歇息留出来。"
                :who=="sister"?"阿兄近来体力不济，就先歇一轮。我练字可以慢慢来，不要拿自己的力气去换赶出来的书资。"
                :"你近来体力不济，母亲最挂念这一点。饭要按时吃，累了便留一日歇息，家用不要从饭钱里硬挤。");
            string memory=HomeMemory(state,who);
            if(!string.IsNullOrEmpty(memory))text.Append("\n").Append(memory);
            text.Append("\n").Append(HomeLife(who,state.personalLettersSent));
            text.Append("\n").Append(Remittance(who,draft.intent,draft.amount));
            return text.ToString();
        }

        static string Intention(string who,string intent)
        {
            if(who=="wife") {
                if(intent=="truth")return "你愿意把真实的打算说给我，我们便能商量着过。我的日子也不只有等信，下面把手头的事写给你。";
                if(intent=="study")return "你惦记阿满读书，我也想把常用的字认起来。往后家里的账和自己的工钱，我想亲手记明白。";
                return "平安批收到了。知道你还惦记着寻常日子，我心里踏实些；家里也有新的小事想同你说。";
            }
            if(who=="sister") {
                if(intent=="truth")return "你肯把真心话寄来，我也愿意认真同你商量。家里的事不是阿兄一个人的担子，我也在学着做自己能做的事。";
                if(intent=="study")return "阿兄记着我的读书事，我很欢喜。我想学会自己读懂家书和账目，也想慢慢找到往后能做的事。";
                return "平安批到了！屋前的路还是原来的样子，我认得的字却比从前多些了，下回想多写几句给你看。";
            }
            if(intent=="truth")return "你愿意同家里说实话，母亲便能与你商量。日子有快有慢，不必把每件事都说成顺遂，也不必一人承担家里的打算。";
            if(intent=="study")return "你惦记阿满读书，母亲记着。她想学什么，我们也听她自己说；家用与笔墨的开支会分开记清。";
            return "平安批已到，家中都安。你从前坐的凳子还在灯下，饭钱先给自己留足，家里的日常也慢慢同你说。";
        }

        static string WageExperience(GameState s,string who)
        {
            // A pending job, a generic truth intention, a low health value, or a
            // Penang/Rangoon hardship is not evidence of unpaid wages.
            if(NanyangLife.IsEnrolled(s)&&s.nanyang.stage!="free"&&s.nanyang.owed>0) {
                var n=s.nanyang;
                string action;
                switch(n.response) {
                    case "evidence":action="你留下工单、找见证人核账";break;
                    case "together":action="你和同工一道商量、一起追问工钱";break;
                    case "leave":action="你决定离开那处扣工钱的地方，另找门路";break;
                    case "endure":action="你暂且忍下，只收妥已经发给你的那一部分";break;
                    default:return WageVoice(who,"那次被扣下的 "+n.owed+" 份工钱，眼下还没有处理完");
                }
                if(n.stage=="choice"||n.stage=="tasks")return WageVoice(who,action+"，事情还在办理，尚不能当作已经结清");
                int remaining=Math.Max(0,n.owed-n.recovered);
                string result=n.recovered>0?"，已取回 "+n.recovered+" 份":"，那笔工钱还未取回";
                if(remaining>0&&n.recovered>0)result+="，尚有 "+remaining+" 份未清";
                return WageVoice(who,action+result);
            }
            if(s.journeyVersion!=1||s.journey==null||s.journey.hardshipId!="singapore"||s.journey.turns<2)return "";
            string response="";
            if(s.journey.log!=null)foreach(var entry in s.journey.log) {
                if(entry==null)continue;
                if(entry.actionId=="hardship_evidence")response="evidence";
                else if(entry.actionId=="hardship_mutual_aid")response="together";
                else if(entry.actionId=="hardship_endure")response="endure";
            }
            if(!s.journey.hardshipResolved)return WageVoice(who,"被扣下的四份工钱还没有处理完");
            if(response=="evidence")return WageVoice(who,"你留了工账并请同乡作证，追回三份，再扣还两份奔走费；那次损失没有全补回来");
            if(response=="together")return WageVoice(who,"你同人结伴换工，先安顿落脚处，先前扣下的工钱仍未追回");
            if(response=="endure")return WageVoice(who,"你暂且忍下，又做急工挣了四份；这是新挣的工钱，先前被扣的那笔并没有归还");
            return "";
        }
        static string WageVoice(string who,string fact)
        {
            if(who=="wife")return fact+"。追回多少、接下来往哪里做工，我们都可以摊开商量；不必为了寄银，再把自己逼回原处。";
            if(who=="sister")return fact+"。我把这些数目记在纸上，也学着分清挣到的、取回的和还没拿到的，不能混成一笔。";
            return fact+"。母亲心疼这份不易。留好凭据，有可信的人同行便一起商量，吃住先安顿妥当。";
        }

        static string WorkExperience(GameState s,string who)
        {
            string route="";int count=0;
            if(s.journeyVersion==1&&s.journey!=null&&s.journey.log!=null) {
                for(int i=s.journey.log.Count-1;i>=0;i--) {
                    var item=s.journey.log[i];
                    if(item!=null&&Trade(item.actionId)){route=item.actionId;break;}
                }
                if(route!="")foreach(var item in s.journey.log)if(item!=null&&item.actionId==route)count++;
            }
            if(route==""&&s.firstDayVersion==1&&s.firstDay!=null&&s.firstDay.workSettled&&Trade(s.firstDay.route))route=s.firstDay.route;
            if(route=="")return "";
            string place=route=="dock"?"码头":route=="shop"?"杂货铺":"信局";
            string name=NanyangLife.CareerName(s,route);
            string fact=count==0?"你在"+place+"做完了头一日的活，往后先从"+name+"做起"
                :"你在"+place+"已做过 "+count+" 轮活"+(NanyangLife.IsEnrolled(s)?"，如今可以接"+name+"的差事":"，手头的路与账也渐渐熟悉了");
            if(who=="wife")return fact+"。手艺是你自己一步步做出来的；我的针线活也想按自己的节奏安排。";
            if(who=="sister")return fact+"。我想知道这份活要认哪些字、记哪些数，下回也教我一点。";
            return fact+"。能把一行慢慢做熟，母亲替你欢喜，也盼你量着力气接活。";
        }
        static bool Trade(string value){return value=="dock"||value=="shop"||value=="courier";}
        static string HomeMemory(GameState state,string who)
        {
            bool first=state.personalLettersSent==0;
            if(who=="wife"&&HomeChapter.HasMemory(state,"market")) {
                if(state.homeChapter.purchase=="tea")return first
                    ?"那包茶早已喝完，前日又添了一包。收碗时才发觉，仍照那日一家人吃饭的数目，多拿出了一只杯子。"
                    :"那包茶早已喝完，我又去添了一包。回来经过院门，篮子换了只手，想起从前你在这里接过去。";
                return first
                    ?"又去添米油，仍走我们一道提篮子回家的巷子。晚间灯下，阿满练字，我补衣，碗筷收得晚了些。"
                    :"前日又去买米油，走过从前一起提篮子回家的巷子。篮子我提得稳，进门才想起，你从前总会接一接。";
            }
            if(who=="mother"&&HomeChapter.HasMemory(state,"grain"))return first
                ?"你收谷时端过的簸箕，我还用着。你坐过的凳子挪到廊下了，晒谷时正好歇歇脚，不必站一整日。"
                :"前日又收了一簸箕谷子。这回阿满替我扶着瓮口，也学你那样，端着慢慢倒，一粒都舍不得洒。";
            if(who=="sister"&&HomeChapter.HasMemory(state,"lesson")) {
                if(state.homeChapter.word=="return")return first
                    ?"那天你陪我写的“归”，我又描了一遍。石板已经擦干净，好写新的字；擦以前，我先把它抄在了纸上。"
                    :"你陪我写的“归”，如今不用照着描了。我在旁边又写了一个“期”，问嫂嫂是不是等一个日子的意思。";
                return first
                    ?"那天你陪我写的“家”，我已经会自己写了。这封批上的这个字，是我落的笔，嫂嫂说比石板上那回稳。"
                    :"你陪我写的“家”，如今不用照着描了。我又学会了写“人”，想把家里每个人的名字都写在一张纸上。";
            }
            if(HomeChapter.HasMemory(state,"meal"))return who=="wife"
                ?"还记得离乡前一桌人一起吃的那顿饭。如今我先把自己这碗吃热，留给你的话便慢慢写在纸上。"
                :who=="sister"?"我还记得我们一桌吃饭的傍晚。下回阿兄回来，我想坐你旁边，把新学的字都念一遍。"
                :"离乡前那顿饭，大家说的都是家常。如今灯下少了你，却没有少你的地方；饭先各自吃好，团圆的日子慢慢等。";
            return "";
        }
        static string HomeLife(string who,int previousLetters)
        {
            if(who=="wife") {
                if(previousLetters<=0)return "近来邻里托我做两件针线活。我先把布和线的材料开支记下，只接白日里做得完的订单；赚来的钱，想留一份添针线。";
                if(previousLetters==1)return "这阵子的针线订单交了几件。我把材料开支和收来的工钱分两栏记，和阿满每日认几个字。往后想把价目自己写清，不再每回请人代念。";
                return "针线活还在做，材料钱先扣出来，余下由我安排。我已能认常用的账字，打算和邻里的妇人合着接些订单，留出认字和歇息的时间；这也是我自己的计划。";
            }
            if(who=="sister") {
                if(previousLetters<=0)return "这几日我把自己的名字写稳了，也试着认批封上的乡名。有几个字还总写反，嫂嫂让我慢一点。";
                if(previousLetters==1)return "我同嫂嫂一起认她针线账上的字。她教我把材料开支分开记，我教她写常用的数，原来读书也能用在这些小事上。";
                return "我想试着把一页家里的账独自记完，再请母亲和嫂嫂一起核。字认多了，我也能多作一点自己的打算。";
            }
            if(previousLetters<=0)return "家里先把饭食和屋里日常安排着，阿满的笔墨另记一栏。你媳妇也在接自己的针线活，家用不会只等海上这一封。";
            if(previousLetters==1)return "家里的米油开支我记着，你媳妇的针线材料另记，不混着算。阿满每日认几个字，也愿意帮我们核一核数。";
            return "如今家里的日常各有分工：我顾米油，你媳妇安排自己的针线活，阿满练字也练记账。你在外安心一步步做，海这边的事我们也会商量着办。";
        }
        static string Remittance(string who,string intent,int amount)
        {
            if(amount==0)return who=="wife"?"这次虽未附银，信已收到。我的材料开支会自己算清，你把手头的饭钱留好。"
                :who=="sister"?"这次虽未附银，字已经到了。我的练习可以接着做，不必把每封信都写成一笔催款。"
                :"这次虽未附银，字已经到了。知道你的近况，家里便少一层猜想，先顾好自己的饭食。";
            string received="附来的 "+amount+" 份银已收讫，";
            if(intent=="study")return received+(amount==2?"单列为阿满的书资，先添纸笔，旧书仍接着用。":amount==5?"单列为阿满的书资，添纸笔，也留一份找旧书。":"单列为阿满的书资，纸笔与旧书先买齐，余下留作后续读书开支。")+"这笔和米油、针线材料的开支分开记。";
            string use=amount==2?"先添眼下缺的米油，手头紧的事一件件办。":amount==5?"米油先补齐，余下留作接下来几日家用。":"米油先补齐，另留一点修屋檐漏雨的地方，余下存着。";
            if(who=="wife")return received+"已记入家用簿；"+use+"我的针线材料另算，不让两笔钱混在一起。你也留足吃住的钱。";
            if(who=="sister")return received+"我陪母亲记入家用簿；"+use+"阿兄自己也要留足饭钱。";
            return received+"已记入家用簿；"+use+"请先顾好自己。";
        }
    }
}
