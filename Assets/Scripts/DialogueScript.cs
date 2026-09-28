using System;
using System.Collections.Generic;

namespace Qiaopi
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speakerId, text, clipKey;
    }

    /// <summary>
    /// Short, explicitly attributed spoken lines. SceneData.body remains the full
    /// context. Pending outcomes are intentionally handled by the presentation.
    /// Recorded-letter scenes announce that the family voice comes from a letter.
    /// Clip keys are stable and map to one speaker/text pair across every branch.
    /// </summary>
    public static class DialogueScript
    {
        private static readonly Dictionary<string, DialogueLine> Catalog = new Dictionary<string, DialogueLine>();
        private static readonly List<string> Order = new List<string>();

        static DialogueScript()
        {
            Add("peace_mother_01", "mother", "饭已经好了，今天不必赶路。把手洗净，坐下来吃吧。屋前的谷晒干了，明日咱们再慢慢收。" );
            Add("peace_aman_01", "aman", "哥哥，看我在石板上写的字。这是家，这是人，两个摆在一起，是不是我们一家人？" );
            Add("peace_wensheng_01", "wensheng", "我看得懂。吃过饭再陪你认两个，或者替邻里送一趟米，傍晚还能回来，咱们把灯点起来。" );

            Add("pressure_mother_01", "mother", "屋瓦坏了可以慢慢修，米粮和药却不能等。方才催债的人动了手，你有没有受伤？先让我看看。" );
            Add("pressure_uncle_01", "uncle", "我请邻里来作证，本来欠多少就写多少，不让他们随口添。南洋或能问到工，可离家也不是小事。" );
            Add("pressure_wensheng_01", "wensheng", "我想先把家里护住，再把这笔账理清。若真要走，也请你们照看母亲，让我知道该往哪里寄信。" );

            Add("home_mother_01", "mother", "包袱别再塞了。先去厦门，再过海，路上记得给自己留口热饭，别只惦记家里的屋瓦。");
            Add("home_aman_01", "aman", "哥哥，我把门牌写好了。你带去吧，等我认得更多字，就不用托别人给你写信了。");
            Add("home_wensheng_01", "wensheng", "我都想带上，可这一程只能再添一样。让我好好选，将来走得再远，也认得回家的路。");

            Add("funding_uncle_01", "uncle", "借足二十份，到了那边手头宽些，可借据要认。跟同乡走，钱少一点，有人肯替你作保。");
            Add("funding_mother_01", "mother", "我不替你按手印。家里已经欠着修屋的钱，你要想清楚，怎样借，也要怎样还。");
            Add("funding_wensheng_01", "wensheng", "我听明白了。能带走的盘缠有数，欠下的人情也得记着，不能都让你们留在家里担。");

            Add("farewell_mother_01", "mother", "车夫在等，别误了路。我的腿还是老样子，药铺肯宽限几日，你先把自己安顿好。");
            Add("farewell_aman_01", "aman", "我想去问问教书的人，肯不肯让我跟着认字。不是只为替你写信，我自己也想学。");
            Add("farewell_wensheng_01", "wensheng", "药钱和读书，我都听见了。临走的话不能只说好听的，我得选一个眼下办得到的起头。");

            Add("passage_xusheng_01", "xusheng", "我姓许，也是头一回过海。这船晃得厉害，劳你递一下水，等缓过来，我替你看包袱。");
            Add("passage_wensheng_quilt", "wensheng", "薄被是母亲缝的，还能挪出半边。你先靠稳，水慢慢喝，我们轮着歇一会儿。");
            Add("passage_wensheng_plain", "wensheng", "水在这里，你慢些喝。我也没比你好多少，等船稳一点，咱们再看看怎么安顿。");
            Add("passage_narrator_01", "narrator", "厦门的岸线退到看不见。舱里有人换去宽些的铺位，也有人留下相扶，下一程还没有定数。");

            Add("shore_guide_01", "guide", "文生，这里是新加坡。住处先挤一挤，工有三处可以问，码头、杂货铺，还有收寄侨批的信局。");
            Add("shore_guide_penang", "guide", "文生，这里是槟榔屿。转船的事先放下，住处已问好，码头、杂货铺和侨批信局都能去试工。");
            Add("shore_guide_rangoon", "guide", "文生，船停在仰光了。河边多米粮货栈，话听不熟别着急，我带你问码头、杂货铺和侨批信局。");
            Add("shore_wensheng_01", "wensheng", "我肯做事，只是不熟门路。码头凭力气，店里要算账，信局除了跑腿，还要学些什么？");
            Add("shore_guide_02", "guide", "学把钱和信交到该收的人手里。这里说的批，就是信，背后可常连着一家人的米粮。");

            Add("dock_foreman_01", "foreman", "今天急货多，多搬有加钱。不过老吴伤了脚，缺个人分担；货单上也有两处数目对不上。");
            Add("dock_wensheng_01", "wensheng", "家里正等着用钱，我想多挣一些。可这几样事挤在同一天，总得先认下一件做稳。");
            Add("dock_foreman_02", "foreman", "你自己掂量。这里看肩膀，也看一个人怎样应事，今日留下的印象，往后还会跟着你。");

            Add("shop_shopkeeper_01", "shopkeeper", "货和账核过了？你可以多守晚班领现钱，也可以少领一些，跟我学进货和怎样记赊账。");
            Add("shop_wensheng_01", "wensheng", "我还想问，门外那位乡亲拿着信坐了许久，要是店里不忙，我能不能替他念一遍？");
            Add("shop_shopkeeper_02", "shopkeeper", "能。做生意要听清人家的话，念家信更要听。钱、手艺和人情，你先把时间分好。");

            Add("courier_master_he_01", "master_he", "批件放稳。你先核姓名、村名、门牌和款额，碰到旧称要再问，别凭自己猜着往下抄。");
            Add("courier_wensheng_01", "wensheng", "伙计催我再跑几户，我也想早些挣到家用。若每封都停下来核，今日恐怕收不齐。");
            Add("courier_master_he_02", "master_he", "快有快的难处，慢也让人着急。可留给后来人查问的记录，不能因为你忙就省下。");

            Add("first_pay_xusheng_01", "xusheng", "我如今也住这条街，抬头总能碰见乡人。工钱到手了，你是先留家用，还是再接一轮活？");
            Add("first_pay_wensheng_tired", "wensheng", "钱倒能数清，肩膀却酸得睡不着。我怕家里等，也怕再这么熬，连写信的手都拿不稳。");
            Add("first_pay_wensheng_well", "wensheng", "我还撑得住，可药钱、屋瓦、笔墨，一样样都在心里，数到最后，总觉得还差一点。");
            Add("first_pay_xusheng_02", "xusheng", "留给自己吃饭歇脚的，也算正经用钱。你先想好，别把一次寄多寄少，算成全部心意。");

            Add("first_letter_clerk_01", "clerk", "泉州府晋江县，榕溪村，收给你母亲，是这样吧？姓名住处再核一遍，家书也一并交来。");
            Add("first_letter_wensheng_01", "wensheng", "地址是这里。称呼写完了，我却不知道该不该说做工的辛苦，怕她们看了反而睡不安稳。");
            Add("first_letter_clerk_02", "clerk", "我只能替你照应寄出的路，信里留什么话，得你自己定。也可以问几件事，让回批有话答。");

            Add("reply_clerk_01", "clerk", "文生，晋江县的回批到了，封口还好好的。我把信交给你，家里的字，你慢慢看。");
            Add("reply_narrator_mother", "narrator", "你展平信纸。以下是母亲托人写下的话，隔着海，像她还坐在家中的桌边。");
            Add("reply_mother_care", "mother", "前批家用收到了，屋瓦补好两行。你写的难处我们知道，家用量力寄，饭一定要吃好。");
            Add("reply_mother_reassure", "mother", "前批家用收到了。你说吃住顺心，我就放心些，家里先把钱用在修屋和药上，不另替你存了。");
            Add("reply_aman_school", "aman", "哥哥，这几个字是我自己写的。我已去问学，还想添一本书，你看，家里人的名字我都会写了。");
            Add("reply_mother_no_school", "mother", "阿满还托我问，你那里夜里是不是也看得见月亮？药铺的钱暂缓几日，你别急着全担下来。");

            Add("records_clerk_01", "clerk", "这一回十份家用，款额已核过。你母亲说修屋时暂住姨家，旧门牌和现住处可得分别记清。");
            Add("records_wensheng_01", "wensheng", "我知道不能只记一个熟悉的称呼。款项交出去以后，该把哪些纸留下，将来才问得明白？");
            Add("records_clerk_02", "clerk", "留底、请熟悉店家代存，或让家里签回日期款额，都有用。要紧的是你留下的记录能接上来路。");

            Add("missing_guide_01", "guide", "家里托我捎句话，新近那笔钱还没有收妥。我只听到这一句，没带来完整回批，你先别急着断定。");
            Add("missing_wensheng_01", "wensheng", "那就是眼下只能说尚未收到，还不能认定丢了。我得拿出底单，把走过的路一段段问。");
            Add("missing_guide_02", "guide", "对。先问信局、去货栈打听，或请家里复核住处，都能起个头。别让猜测跑到事实前面。");

            Add("trace_desk_master_he_01", "master_he", "你看，旧屋的称呼和暂住处没有接全。转交之后的回函还缺着，不能拿去年的收妥结今年的账。");
            Add("trace_desk_wensheng_records", "wensheng", "我留着款额和经手日期，也能对照旧信。请按这些线索发函核认，别让家里再隔着海乱猜。");
            Add("trace_desk_wensheng_thin", "wensheng", "有些细节我当时没记全。可家里眼下要用钱，我还得想，是继续等查，还是先补一笔应急。");
            Add("trace_desk_master_he_02", "master_he", "线索够不够，要逐项看。你也能留下书面催查，先回去做工；只是待查这一栏，还不能销去。");

            Add("trace_harbor_foreman_known", "foreman", "我认得你，先坐下喝水。你以前留下的照应和货单都有人记着，我可以替你引见经手的人。");
            Add("trace_harbor_foreman_new", "foreman", "我管这一带货栈。你要找的转交记录得另问经手的人，先把日期款额说清，我指你一个去处。");
            Add("trace_harbor_wensheng_01", "wensheng", "我不是来指认谁，只想把这一笔送到哪里问明白。若得请人跑腿，也请把各处答复写下来。");
            Add("trace_harbor_foreman_02", "foreman", "记下话，再把它同凭据对上。若你今天实在撑不住，就留准住处，有消息才知道往哪里找。");

            Add("trace_family_clerk_01", "clerk", "家里的补信来了，里头提到旧屋与姨家两种叫法。这是新的线索，我陪你把前后两封摊开。");
            Add("trace_family_narrator_01", "narrator", "你沿着信上的字读下去。阿满的话被记在纸边，和母亲的叮嘱一道越过海来。");
            Add("trace_family_aman_01", "aman", "哥哥，我问过代收的人了。把旧门牌、母亲的身份和那笔款额放在一起写，才好接着查问。");
            Add("trace_family_wensheng_01", "wensheng", "我不能再只说放心。是拿这些细节接着查，先补家用，还是又把难处藏下，都得由我担着。");

            Add("storms_xusheng_01", "xusheng", "这些年工钱也不是日日准，赶上停工，谁都要紧一紧。你那笔迟批后来怎么样，身体还撑得住吗？");
            Add("storms_wensheng_resolved", "wensheng", "款项已经核妥，回批也收好了。总算有一件事能放心翻过去，可日子还有别的账要认真安排。");
            Add("storms_wensheng_pending", "wensheng", "那一笔还在等核认，我把已有的答复留着。眼前的日子仍得过，也不能假装这件事已经了结。");
            Add("storms_wensheng_debt", "wensheng", "当年借的旅费，这些年陆续还过八份，还欠十二份。手里这点余裕，得想好先照应哪一头。");
            Add("storms_wensheng_trust", "wensheng", "当年担保的人情我也记着，家里还缺一笔急用钱。若再做夜工，就得拿身体换，我不能装作没代价。");

            Add("horizon_guide_01", "guide", "有人问你肯不肯去更远的埠头，条件还得细谈。要回乡的船票也能问，你这次想往哪边走？");
            Add("horizon_wensheng_school", "wensheng", "阿满信里有自己的打算，她还想读书。母亲说门槛换过了，量身高的旧痕还留着，我一直想着。");
            Add("horizon_wensheng_home", "wensheng", "母亲说门槛换过了，量身高的旧痕还留着。阿满也长大了，我怕再走远，连回家的日子都说不准。");
            Add("horizon_guide_02", "guide", "那就把路的代价问清，再作决定。留下要有留下的安排，远行也先把回信的去处告诉家里。");

            Add("return_choice_mother_01", "mother", "到了门前，怎么还站着？包袱先放下。回家以后要怎么过，坐下来慢慢说，不急这一刻。");
            Add("return_choice_wensheng_01", "wensheng", "我带回了这些年的信，也带回没说完的话。往后的日子，我想让你们知道我真正能做到多少。");
            Add("return_choice_aman_school", "aman", "哥哥，我已经会把账记清了，也还想继续读书。你若留下，就听我讲一讲自己想走的路。");
            Add("return_choice_aman_home", "aman", "哥哥，家里这几年也变了许多。我想听你讲海那边的日子，你也听我说说我们怎么过的，好吗？");

            Add("stay_choice_xusheng_01", "xusheng", "你把住处安顿好了，接下来总要给家里一句明白话。是守住这里，还是接着往更远处去？");
            Add("stay_choice_wensheng_courier", "wensheng", "信局这一程教了我很多。若要接稳更多人的批，不能只会跑腿，也要认下经手后的查问和等候。");
            Add("stay_choice_wensheng_shop", "wensheng", "铺里的灯我已经点惯了。留在柜台后，也得有能长期撑住的生计，才能把寄批的日子定下来。");
            Add("stay_choice_wensheng_dock", "wensheng", "码头的活我做熟了，也认得自己力气的边界。不能再把每封家信，都写成要用身体去补的保证。");
            Add("stay_choice_xusheng_02", "xusheng", "写你能做到的，也写暂时做不到的。人不一定立刻回去，话先到家里，才好让他们回你。");

            Add("end_home_lamp_mother_01", "mother", "汤还热着，伸手接过去吧。以前我把这些话写在回批里，如今你坐在桌边，就不用再等船了。");
            Add("end_home_lamp_wensheng_01", "wensheng", "旧债与迟批都理清了，往后未必宽裕，却可以同你们一起商量。今晚，我先把这一碗饭吃好。");
            Add("end_home_lamp_aman_01", "aman", "信我按年月放好了，第一封的纸已经黄了。哥哥，等你写信给旧识，也把我们平安的话带上。");

            Add("end_home_scar_mother_01", "mother", "床替你铺好了，累就先歇。回来不是立刻把所有难处办完，你不用再一个人在外头硬撑着。");
            Add("end_home_scar_wensheng_tired", "wensheng", "身体确实欠着休息，我终于敢认了。办得到的事我慢慢做，办不到的也说明白，不拿平安遮过去。");
            Add("end_home_scar_wensheng_other", "wensheng", "有些心事还要慢慢说开。我愿意把以后的打算摊在桌上，也听听你们这些年没有写进信里的话。");
            Add("end_home_scar_aman_01", "aman", "那我们就从眼前开始，吃饭、歇脚，再把要问要办的事记下来。哥哥，你已经坐在家里了。");

            Add("end_school_window_narrator_01", "narrator", "阿满的新信写得整齐。这一次，她把自己的主意写在前面，不再只替家里报告平安。");
            Add("end_school_window_aman_01", "aman", "哥哥，书资收到，我已经记清数目。我还想继续读，也能替母亲认药单、替邻人写信了。");
            Add("end_school_window_wensheng_01", "wensheng", "你想学的，就认真去学。不必把每个字都写成谢我，这些字可以替你走一条和我不同的路。");

            Add("end_shop_bridge_shopkeeper_01", "shopkeeper", "这间铺以后我们一起照应，进货和赊账都要稳。替乡亲读信可以，收寄款项仍让信局经手。");
            Add("end_shop_bridge_wensheng_01", "wensheng", "我明白。我把寄家用的日子安排清楚，也留个干净桌角，陪人把姓名住处核对好再去寄批。");
            Add("end_shop_bridge_shopkeeper_02", "shopkeeper", "去点灯吧。生意未必一夜兴旺，可一天天做稳，这里就是你守得住的门，也能让家里放心。");

            Add("end_trusted_route_master_he_01", "master_he", "这一段往来，你可以独自照应了。记住，谁家迟了回批，不能只拿再等几日作答复。");
            Add("end_trusted_route_wensheng_01", "wensheng", "我等过那样一封信。姓名、款额、旧称和住处，我会问清；经手留下记录，有疑问就继续查。");
            Add("end_trusted_route_master_he_02", "master_he", "肯把批交给你的人，托来的不只是纸。你有自己的家，也要记得，每一封后面都有人在等。");

            Add("end_new_harbor_xusheng_01", "xusheng", "船快开了。旧住处的人知道怎样替你转信吧？新地方落脚后，再寄一封把门牌写全。");
            Add("end_new_harbor_wensheng_01", "wensheng", "都交代好了。这次工钱怎么算、身体要怎样照顾，我也写给家里，不只留下四个一切安好。");
            Add("end_new_harbor_narrator_01", "narrator", "岸线又一次退远。包里收着旧批和凭据，下一处住址还要自己走到，回信的路却已经先安排好。");

            Add("end_unsent_letter_mother_01", "mother", "把纸放在这里，你慢慢说。我不能替你免去该担的事，可你也不必再靠一句没事把话挡回去。");
            Add("end_unsent_letter_wensheng_debt", "wensheng", "旅费还欠十二份，我愿同你们议定怎样还。迟批查到的结果也一并摊开，不让旧账混在沉默里。");
            Add("end_unsent_letter_wensheng_pending", "wensheng", "那一笔还没核清，查到哪里、下一步问谁，我都写下来。这次我把困难说明白，请你们一起看。");
            Add("end_unsent_letter_wensheng_concealed", "wensheng", "从前我怕你们担心，反让你们猜过我的沉默。藏下的实情，我现在从头说，也愿意听你们的委屈。");
            Add("end_unsent_letter_aman_01", "aman", "我们也有自己的记法和想法。把两边的纸放在一起吧，往后给远方的说明，我愿意陪你写。");

            Add("end_silver_thread_xusheng_01", "xusheng", "又到了写信的日子。这个月工钱不比从前多，可你现在会给自己留饭钱和歇脚的余地了。");
            Add("end_silver_thread_wensheng_01", "wensheng", "寄多少就写多少，暂时做不到也写清。我还想听家里的小事，屋前的树、饭后的闲话，都想听。");
            Add("end_silver_thread_narrator_01", "narrator", "没有一夜富贵，也没有从此无忧。银钱与文字一封封过海，平常人的日子，仍在彼此的回声里继续。");
        }

        public static List<DialogueLine> Get(GameState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            switch (state.nodeId)
            {
                case "peace": return Lines("peace_mother_01", "peace_aman_01", "peace_wensheng_01");
                case "pressure": return Lines("pressure_mother_01", "pressure_uncle_01", "pressure_wensheng_01");
                case "home": return Lines("home_mother_01", "home_aman_01", "home_wensheng_01");
                case "funding": return Lines("funding_uncle_01", "funding_mother_01", "funding_wensheng_01");
                case "farewell": return Lines("farewell_mother_01", "farewell_aman_01", "farewell_wensheng_01");
                case "passage": return Lines("passage_xusheng_01", Has(state, "quilt") ? "passage_wensheng_quilt" : "passage_wensheng_plain", "passage_narrator_01");
                case "shore": return Lines(LifeJourney.DestinationId(state) == "penang" ? "shore_guide_penang" : LifeJourney.DestinationId(state) == "rangoon" ? "shore_guide_rangoon" : "shore_guide_01", "shore_wensheng_01", "shore_guide_02");
                case "dock": return Lines("dock_foreman_01", "dock_wensheng_01", "dock_foreman_02");
                case "shop": return Lines("shop_shopkeeper_01", "shop_wensheng_01", "shop_shopkeeper_02");
                case "courier": return Lines("courier_master_he_01", "courier_wensheng_01", "courier_master_he_02");
                case "first_pay": return Lines("first_pay_xusheng_01", state.health < 50 ? "first_pay_wensheng_tired" : "first_pay_wensheng_well", "first_pay_xusheng_02");
                case "first_letter": return Lines("first_letter_clerk_01", "first_letter_wensheng_01", "first_letter_clerk_02");
                case "reply": return Lines("reply_clerk_01", "reply_narrator_mother", Has(state, "reassure") ? "reply_mother_reassure" : "reply_mother_care", School(state) ? "reply_aman_school" : "reply_mother_no_school");
                case "records": return Lines("records_clerk_01", "records_wensheng_01", "records_clerk_02");
                case "missing": return Lines("missing_guide_01", "missing_wensheng_01", "missing_guide_02");
                case "trace_desk": return Lines("trace_desk_master_he_01", Any(state, "ledger", "copies", "verified", "shop_record") ? "trace_desk_wensheng_records" : "trace_desk_wensheng_thin", "trace_desk_master_he_02");
                case "trace_harbor": return Lines(Any(state, "ship_friend", "crew_help", "marked_cargo") ? "trace_harbor_foreman_known" : "trace_harbor_foreman_new", "trace_harbor_wensheng_01", "trace_harbor_foreman_02");
                case "trace_family": return Lines("trace_family_clerk_01", "trace_family_narrator_01", "trace_family_aman_01", "trace_family_wensheng_01");
                case "storms": return Lines("storms_xusheng_01", Has(state, "resolved") ? "storms_wensheng_resolved" : "storms_wensheng_pending", Debt(state) ? "storms_wensheng_debt" : "storms_wensheng_trust");
                case "horizon": return Lines("horizon_guide_01", School(state) ? "horizon_wensheng_school" : "horizon_wensheng_home", "horizon_guide_02");
                case "return_choice": return Lines("return_choice_mother_01", "return_choice_wensheng_01", School(state) ? "return_choice_aman_school" : "return_choice_aman_home");
                case "stay_choice": return Lines("stay_choice_xusheng_01", Has(state, "route_courier") ? "stay_choice_wensheng_courier" : Has(state, "route_shop") ? "stay_choice_wensheng_shop" : "stay_choice_wensheng_dock", "stay_choice_xusheng_02");
                case "end_home_lamp": return Lines("end_home_lamp_mother_01", "end_home_lamp_wensheng_01", "end_home_lamp_aman_01");
                case "end_home_scar": return Lines("end_home_scar_mother_01", state.health < 48 ? "end_home_scar_wensheng_tired" : "end_home_scar_wensheng_other", "end_home_scar_aman_01");
                case "end_school_window": return Lines("end_school_window_narrator_01", "end_school_window_aman_01", "end_school_window_wensheng_01");
                case "end_shop_bridge": return Lines("end_shop_bridge_shopkeeper_01", "end_shop_bridge_wensheng_01", "end_shop_bridge_shopkeeper_02");
                case "end_trusted_route": return Lines("end_trusted_route_master_he_01", "end_trusted_route_wensheng_01", "end_trusted_route_master_he_02");
                case "end_new_harbor": return Lines("end_new_harbor_xusheng_01", "end_new_harbor_wensheng_01", "end_new_harbor_narrator_01");
                case "end_unsent_letter": return Lines("end_unsent_letter_mother_01", Has(state, "concealed") ? "end_unsent_letter_wensheng_concealed" : Debt(state) ? "end_unsent_letter_wensheng_debt" : "end_unsent_letter_wensheng_pending", "end_unsent_letter_aman_01");
                case "end_silver_thread": return Lines("end_silver_thread_xusheng_01", "end_silver_thread_wensheng_01", "end_silver_thread_narrator_01");
                default: throw new ArgumentException("Unknown dialogue node: " + state.nodeId, "state");
            }
        }

        public static List<DialogueLine> AllLines()
        {
            var lines = new List<DialogueLine>(Order.Count);
            foreach (string key in Order) lines.Add(Copy(Catalog[key]));
            return lines;
        }

        private static bool Has(GameState s, string flag) { return s.flags != null && s.flags.Contains(flag); }
        private static bool Any(GameState s, params string[] flags) { foreach (string flag in flags) if (Has(s, flag)) return true; return false; }
        private static bool School(GameState s) { return Any(s, "sister_school", "twin_promise", "ask_school", "school_supported"); }
        private static bool Debt(GameState s) { return Has(s, "debt") && !Has(s, "debt_paid"); }
        private static void Add(string key, string speaker, string text)
        {
            Catalog.Add(key, new DialogueLine { clipKey = key, speakerId = speaker, text = text });
            Order.Add(key);
        }
        private static List<DialogueLine> Lines(params string[] keys)
        {
            var lines = new List<DialogueLine>(keys.Length);
            foreach (string key in keys) lines.Add(Copy(Catalog[key]));
            return lines;
        }
        private static DialogueLine Copy(DialogueLine line) { return new DialogueLine { speakerId = line.speakerId, text = line.text, clipKey = line.clipKey }; }
    }
}
