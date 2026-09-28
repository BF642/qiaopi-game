using System;

namespace Qiaopi
{
    [Serializable]
    public sealed class InspectionCase
    {
        public string title;
        public string leftTitle;
        public string leftText;
        public string rightTitle;
        public string rightText;
        public string question;
        public string[] answers;
        public int correct;
        public string success;
        public string failure;
    }

    /// <summary>
    /// Evidence-based interaction data only. Correct answers are zero-based;
    /// the caller awards credit and records completion. No narrative state changes.
    /// Amounts are the story's fictional game units, never historical currency rates.
    /// Each call returns a fresh case, including a fresh answers array.
    /// </summary>
    public static class InspectionCases
    {
        public static InspectionCase Get(string nodeId, int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException("index", "Inspection index must be 0, 1, or 2.");
            switch (nodeId)
            {
                case "shop":
                    if (index == 0)
                        return Case("货架上少了哪一袋", "进货与晨盘", "陈记杂货铺 · 1906年\n开门时：米 8 袋\n午后新到：米 3 袋", "出货与现存", "今日已交给买家：4 袋\n伙计数过货架：6 袋\n另有一张货栈提货单未核",
                            "收铺前，该怎样记这次盘点？", 1,
                            "记现存 6 袋，直接照货架数结账", "应存 7 袋；先查少的一袋和提货单", "新到货后共 11 袋，都记作现存",
                            "8＋3－4＝7。货架只有6袋，差一袋；这是一处待核的差额，还不能直接断定遗失。",
                            "现存数不能替代出入账：8袋原存，加3袋入货，减4袋出货，应剩7袋。直接结账或漏减出货，都会把差额藏起来。");
                    if (index == 1)
                        return Case("一笔买卖两种货", "柜台售货单", "盐：3 包，每包 2 份\n灯油：1 壶，计 3 份\n以上均为游戏记账单位", "收款小笺", "买家交来：7 份\n同意把不足部分记赊账\n伙计尚未填本次欠额",
                            "这笔买卖还应记赊账多少？", 2,
                            "记赊账 5 份", "已经付清，不记赊账", "记赊账 2 份",
                            "盐钱为3×2＝6份，加灯油3份，共9份；已收7份，本次还欠2份。",
                            "先按数量算货价：盐共6份、油3份，应收9份。7份只是已交的钱，并非整笔货价，差额是2份。");
                    return Case("旧账和新账要分清", "熟客的账页", "前回尚欠：4 份\n今天另赊米钱：3 份\n旧账未注明结清", "今日还款条", "同一熟客今日还款：5 份\n注明：抵扣前后欠款\n掌柜已确认实收",
                        "收下这5份后，账页还应结存多少欠款？", 0,
                        "仍欠 2 份，并记清本次还款", "旧账已还，倒找给熟客 1 份", "仍欠 7 份，把还款另放一旁",
                        "旧欠4份加新欠3份，共7份；收到5份后，余欠2份。还款记在同一人的账下，才不会重复追讨。",
                        "这5份抵扣前后欠款，不能只减旧欠4份，也不能漏掉它。4＋3－5＝2，结存的才是下一回要接着算的数。");

                case "records":
                    if (index == 0)
                        return Case("家还在，住处暂变", "1908年待寄批封", "新加坡 → 泉州府晋江县\n村名：榕溪村\n住处：陈家旧屋", "前一封回批摘句", "母亲：修屋这阵暂住姨家\n具体门牌：未写明\n家里以为村中熟人认得",
                            "批封和回批对照后，哪一处必须继续核问？", 1,
                            "村里人认得，只留榕溪村即可", "保留旧址，注记暂住并请补齐姨家门牌", "把村名改为姨家，删去原来的住处",
                            "村名没有变，但实际暂住处未写全。你找出了缺漏；还须让家里补清门牌，不能把“熟人认得”当作完整地址。",
                            "“姨家”不是完整门牌，暂住也不等于原村名失效。保留已知旧址、注明待补信息，才能让两端继续核对。");
                    if (index == 1)
                        return Case("谁收款，谁协助", "文生的寄款交代", "寄件人：陈文生\n收款人：文生的母亲\n阿满：妹妹，协助读信", "伙计的草录", "收款人栏：阿满\n亲属栏：妹妹\n母亲的身份说明尚未抄入",
                            "这张草录应怎样处理？", 0,
                            "请伙计核清母亲身份，阿满另记为协助者", "阿满是家人，直接把她当指定收款人", "只把阿满改成陈家，收款人不必再问",
                            "寄款交代指定母亲收款，阿满协助读信不等于自动获得收款授权。人物关系要分别记清，不能拿一家人代替核认。",
                            "同住一户并不能替代指定收款人的核认。草录把协助者写进收款人栏，需要更正；“陈家”也不足以辨明具体的人。");
                    return Case("十份家用，不能多一笔", "文生的交款记录", "1908年初 · 第二笔家用\n实际交寄：十份\n收款人：陈母", "待复核的寄件底单", "同一日期、同一收款人\n抄录款额：十一份\n伙计请寄件人复核",
                        "两张记录只差一份，该怎样办？", 2,
                        "按十一份寄出，以底单为准", "不必更正，反正姓名日期都对", "请伙计复核实收，将底单订正为十份",
                        "这次实际交寄的是十份。姓名日期一致也不能抵消款额差错，订正后再留底，才能查明以后所问的是哪一笔钱。",
                        "底单不是额外一份钱的来源，款额也不是可以略过的小处。这笔实收十份，应核对订正，不能让两张记录各写一个数。");

                case "trace_desk":
                    if (index == 0)
                        return Case("两个住处的线索", "迟批寄件底单", "1908年初：家用十份\n地址：榕溪村陈家旧屋\n收款人：陈母", "家中补信", "修屋期间暂住姨家\n乡里还沿用旧屋的叫法\n此笔新款：尚未确认收到",
                            "目前能提出哪一步查问？", 2,
                            "旧屋有人认识，认定银钱已经领走", "姓名相同，认定家里记错了款项", "请核认新旧住处与代收情况，暂不判定送达",
                            "两张纸提供的是地址核认线索，不是领款证明。下一步要把新旧门牌、本人及可能的代收情况问清，才能继续追查。",
                            "熟悉门牌或姓名相同都不能证明领款，补信也没有确认收妥。眼下应核问住处与代收，不应把猜测写成查明的结果。");
                    if (index == 1)
                        return Case("哪一段还没有答复", "起程经手记录", "本案经手日期（虚构）\n二月初二：新加坡交件\n二月初六：厦门接件", "后续转交记录", "二月初九：注明转往泉州\n泉州具体转交记录：待回函\n收款人签记：未见",
                            "按照已经留下的日期，应该先追问哪里？", 0,
                            "向泉州经手处问后续转交与收款记录", "认定二月初九就是陈母收款日期", "抹掉已有记录，从新加坡重新猜起",
                            "现有记录止于转往泉州，后续仍待回函。经手日期证明走到哪一段，不能自动当作收款日期；从断点查起才有依据。",
                            "“转往泉州”不等于“交到陈母手中”。初二、初六的记录也不能随意抹去；应接着问缺失的泉州转交与收款环节。");
                    return Case("一封收妥，未必是这一笔", "正在追查的底单", "年份：1908年\n第二笔家用：十份\n这笔对应的回批：尚待核认", "匣中一封旧回批", "落款：1907年春\n正文：前批家用已收到\n未提1908年这笔十份款",
                        "能用这封旧回批，把眼前的查案结清吗？", 1,
                        "能，家人写过收到，就证明两笔都到", "不能，须核对1908年本笔款的收款答复", "不能，只要回批迟就能认定钱丢了",
                        "旧回批证明的是此前那一笔，落款年份与本案不符。你找到的是不可混用的证据，仍须等待或追问1908年这笔款的答复。",
                        "回批要与具体款项对应，不能把1907年的收妥沿用到1908年。尚未收到对应答复，也不足以断言银钱遗失。");
                default:
                    throw new ArgumentException("No inspection cases for story node: " + nodeId, "nodeId");
            }
        }

        private static InspectionCase Case(string title, string leftTitle, string leftText,
            string rightTitle, string rightText, string question, int correct,
            string first, string second, string third, string success, string failure)
        {
            return new InspectionCase {
                title = title, leftTitle = leftTitle, leftText = leftText,
                rightTitle = rightTitle, rightText = rightText, question = question,
                answers = new[] { first, second, third }, correct = correct,
                success = success, failure = failure
            };
        }
    }
}
