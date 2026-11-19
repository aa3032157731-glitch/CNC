using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.DigitalTwin
{
    [Serializable]
    public class MachineSpecItem
    {
        public string label;
        public string value;
        public string unit;

        public MachineSpecItem() { }
        public MachineSpecItem(string label, string value, string unit = "")
        {
            this.label = label;
            this.value = value;
            this.unit = unit;
        }
    }

    [Serializable]
    public class TeachingQuizItem
    {
        [TextArea(2, 4)] public string question;
        public string[] options;
        public int correctIndex;
        [TextArea(2, 4)] public string explanation;
    }

    [Serializable]
    public class StationInfo
    {
        public int index;
        public string code;
        public string name;
        public string shortName;
        public string englishName;
        public WorkshopHall hall;

        [TextArea(3, 6)] public string processSummary;
        [TextArea(2, 4)] public string teachingGoal;
        public string[] teachingKeyPoints;
        public MachineSpecItem[] specs;
        public TeachingQuizItem quiz;

        public Sprite previewSprite;
        public string previewSpritePath;

        public Vector3 worldPosition;
        public Vector3 focusCameraPos;
        public Vector3 focusLookAtPos;
        public string[] sceneTargetNames;

        [Header("Simulated Telemetry Baselines")]
        public float nominalRpm = 1200f;
        public float nominalTempC = 42.5f;
        public float nominalPowerKw = 14.7f;
        public float nominalLoadPct = 82f;
    }

    [CreateAssetMenu(fileName = "WorkshopCatalog", menuName = "CNC DigitalTwin/Workshop Catalog")]
    public class WorkshopCatalog : ScriptableObject
    {
        public List<StationInfo> stations = new List<StationInfo>();

        public static List<StationInfo> CreateDefaultStations()
        {
            return new List<StationInfo>
            {
                new StationInfo
                {
                    index = 0,
                    code = "ST-01",
                    name = "往复抓棉机（开松混棉）",
                    shortName = "抓棉开松",
                    englishName = "AUTOMATIC BALE OPENER",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "抓棉机作为清花流水线的首道核心装备，通过往复行走机头与高速抓棉打手从多批原棉包中逐层精细抓取棉纤维束，实现初步开松与均匀混棉，并由气力管道输送至后道除杂工序。",
                    teachingGoal = "掌握往复抓棉的“勤抓少抓”工艺原则，理解抓棉打手伸出距、小车行走速度与棉束精细度之间的关系。",
                    teachingKeyPoints = new[]
                    {
                        "1. 勤抓少抓：降低单次抓棉深度，提高棉束开松度与混和均匀性",
                        "2. 气力输送：保持吸棉管道负压稳定，防止棉块堵塞与纤维损伤",
                        "3. 排包混棉：按批次、产地交叉排包，稳定全线成纱强力指标"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("抓棉打手宽度", "2300", "mm"),
                        new MachineSpecItem("额定产量", "≤ 2400", "kg/h"),
                        new MachineSpecItem("安装功率", "14.7", "kW"),
                        new MachineSpecItem("整机尺寸(L×W×H)", "2580×1230×2975", "mm"),
                        new MachineSpecItem("通道长度", "10–50 (2.5m/节)", "m"),
                        new MachineSpecItem("设备净重", "4050", "kg")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "往复抓棉机在清花工序中遵循的核心工艺原则是什么？",
                        options = new[] { "A. 重抓深抓以提高单次抓取量", "B. 勤抓少抓以提升开松与混和均匀度", "C. 高温烘干后再抓取原棉" },
                        correctIndex = 1,
                        explanation = "解析：“勤抓少抓”可减小抓取棉束质量，显著提高开松度，为后续除杂与梳棉打好基础。"
                    },
                    previewSpritePath = "Assets/Fanyi/R-C (2).jpg",
                    worldPosition = new Vector3(172.6f, 6.0f, 118.0f),
                    focusCameraPos = new Vector3(204.0f, 15.0f, 146.0f),
                    focusLookAtPos = new Vector3(168.0f, 5.5f, 114.0f),
                    sceneTargetNames = new[] { "抓棉机 2", "抓棉机" },
                    nominalRpm = 960f,
                    nominalTempC = 41.2f,
                    nominalPowerKw = 14.7f,
                    nominalLoadPct = 84f
                },
                new StationInfo
                {
                    index = 1,
                    code = "ST-02",
                    name = "重杂分离与除杂机",
                    shortName = "清花除杂",
                    englishName = "MULTI-STAGE CLEANER",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "除杂机利用离心分离、气流分选与尘棒打击协同作用，高效清除原棉中夹杂的尘土、籽壳、枝叶、碎屑及重质异物，同时最大限度减少有效棉纤维的落棉损失。",
                    teachingGoal = "理解除杂效率与落棉含纤率之间的平衡机制，掌握尘棒隔距与气流压力的工艺调节方法。",
                    teachingKeyPoints = new[]
                    {
                        "1. 早落少碎：在纤维尚未剧烈打击前优先分离大颗粒杂质",
                        "2. 尘棒倾角：调节排杂间隙，兼顾除杂率与可纺纤维回收率",
                        "3. 异纤检测：联动光电探测去除丙纶丝等有害异物"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("处理能力", "800–1200", "kg/h"),
                        new MachineSpecItem("综合除杂效率", "65–78", "%"),
                        new MachineSpecItem("装机功率", "11.5", "kW"),
                        new MachineSpecItem("工作风压", "-450 ~ -650", "Pa"),
                        new MachineSpecItem("排杂方式", "连续负压吸尘", "")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "清花除杂工序中提倡“早落少碎”的主要目的是？",
                        options = new[] { "A. 防止大杂质被打碎成难以清除的微尘籽屑", "B. 提高车间空气湿度", "C. 增加棉花含水量" },
                        correctIndex = 0,
                        explanation = "解析：大杂质一旦被高速打手击碎，极易粘附于纤维形成疵点，后续梳棉与精梳极难彻底清除。"
                    },
                    previewSpritePath = "Assets/Fanyi/R-C (3).jpg",
                    worldPosition = new Vector3(137.5f, 7.2f, 30.5f),
                    focusCameraPos = new Vector3(165.0f, 15.0f, 54.0f),
                    focusLookAtPos = new Vector3(132.0f, 6.2f, 25.0f),
                    sceneTargetNames = new[] { "除杂机 (1)", "除杂机 (2)" },
                    nominalRpm = 780f,
                    nominalTempC = 39.8f,
                    nominalPowerKw = 11.5f,
                    nominalLoadPct = 79f
                },
                new StationInfo
                {
                    index = 2,
                    code = "ST-03",
                    name = "清梳联 / 梳卷一体机",
                    shortName = "梳棉成条",
                    englishName = "CARDING & LAP SYSTEM",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "梳棉机是纺纱厂的“心脏”。通过刺辊、锡林、盖板与道夫针布之间的高密度分梳作用，将棉块彻底分解为单纤维状态，进一步清除细小杂质与短绒，并凝聚成均匀连续的生条或棉卷。",
                    teachingGoal = "掌握锡林-盖板分梳区的工作原理，理解针布速比、隔距对生条棉结及短绒率的影响。",
                    teachingKeyPoints = new[]
                    {
                        "1. 单纤维分梳：锡林与活动盖板相对运动实现精细梳理",
                        "2. 除微杂棉结：去除清花残余细杂与纤维扭结粒",
                        "3. 均匀成条：自调匀整机构保证输出生条重量偏差 < 1.5%"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("锡林工作宽度", "1000 / 1280", "mm"),
                        new MachineSpecItem("锡林最高转速", "520", "r/min"),
                        new MachineSpecItem("单机产能", "80–120", "kg/h"),
                        new MachineSpecItem("装机功率", "18.5", "kW"),
                        new MachineSpecItem("生条定量范围", "18–26", "g/5m")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "梳棉机被称为“纺纱厂的心脏”，其核心工艺任务是？",
                        options = new[] { "A. 将棉束彻底分解为单纤维并制成均匀生条", "B. 将管纱卷绕成大型筒子纱", "C. 给粗纱施加永久捻度" },
                        correctIndex = 0,
                        explanation = "解析：梳棉工序直接决定了纤维分离度、棉结数量与生条均匀度，对最终成纱质量影响最大。"
                    },
                    previewSpritePath = "Assets/Fanyi/R-C (3).jpg",
                    worldPosition = new Vector3(134.2f, 7.0f, 69.5f),
                    focusCameraPos = new Vector3(166.0f, 16.0f, 96.0f),
                    focusLookAtPos = new Vector3(132.0f, 6.5f, 64.0f),
                    sceneTargetNames = new[] { "梳卷一体（1）", "梳卷一体（1） (1)", "梳卷一体（1） (2)" },
                    nominalRpm = 510f,
                    nominalTempC = 46.5f,
                    nominalPowerKw = 18.5f,
                    nominalLoadPct = 88f
                },
                new StationInfo
                {
                    index = 3,
                    code = "ST-04",
                    name = "末道并条机（并合匀整）",
                    shortName = "末道并条",
                    englishName = "FINISHER DRAW FRAME",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "末道并条机将 6～8 根棉条并合喂入，经高速罗拉牵伸与闭环自调匀整系统处理，显著改善棉条长片段与中短片段重量不匀率，消除纤维弯钩，大幅提升纤维伸直平行度，形成高质量熟条。",
                    teachingGoal = "掌握并合数与牵伸倍数配合原理，理解自调匀整传感器对熟条定量 CV% 的闭环控制逻辑。",
                    teachingKeyPoints = new[]
                    {
                        "1. 多根并合：利用统计学抵消原理降低棉条长片段不匀率",
                        "2. 罗拉牵伸：拉直后弯钩纤维，提升熟条纤维伸直平行度",
                        "3. 自调匀整：实时检测喂入厚度，毫秒级动态修正主牵伸倍数"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("最高出条速度", "800–1000", "m/min"),
                        new MachineSpecItem("喂入并合根数", "6–8", "根"),
                        new MachineSpecItem("牵伸倍数范围", "4.5–11.0", "倍"),
                        new MachineSpecItem("装机功率", "9.2", "kW"),
                        new MachineSpecItem("熟条条干 CV 值", "≤ 2.8", "%")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "末道并条机采用 6～8 根棉条并合喂入并牵伸的主要作用是？",
                        options = new[] { "A. 降低棉条不匀率并提高纤维伸直平行度", "B. 将纤维切断为等长短绒", "C. 对成品纱线进行染色定型" },
                        correctIndex = 0,
                        explanation = "解析：并条工序通过“并合+牵伸+自调匀整”三协同，核心解决棉条均匀度与纤维顺直度问题。"
                    },
                    previewSpritePath = "Assets/Fanyi/c5fa63cfccfce28eff817b3b0561815.jpg",
                    worldPosition = new Vector3(123.3f, 6.0f, 114.0f),
                    focusCameraPos = new Vector3(152.0f, 14.0f, 134.0f),
                    focusLookAtPos = new Vector3(123.3f, 5.2f, 108.0f),
                    sceneTargetNames = new[] { "卷筒储存(1)", "末并" },
                    nominalRpm = 880f,
                    nominalTempC = 43.0f,
                    nominalPowerKw = 9.2f,
                    nominalLoadPct = 81f
                },
                new StationInfo
                {
                    index = 4,
                    code = "ST-05",
                    name = "高速精梳机（精梳提质）",
                    shortName = "精梳工序",
                    englishName = "HIGH-SPEED COMBER",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "精梳机通过锡林梳针与顶梳的周期性精密梳理，彻底排除长度较短的纤维（控制精梳落棉率）以及纤维中的棉结、草屑与籽皮，使纤维获得极高的平行度与长度整齐度，是纺制高支高档纯棉纱的关键工序。",
                    teachingGoal = "掌握精梳工作循环（喂给→钳持梳理→分离接合）的时空配合，理解落棉隔距对成纱强力与成本的权衡。",
                    teachingKeyPoints = new[]
                    {
                        "1. 排除短绒：精准剔除 16mm 以下短纤维，提升纤维长度整齐度",
                        "2. 深度除结：清除微细棉结与杂质，显著改善成纱光洁度",
                        "3. 分离接合：周期性搭接成均匀精梳条，控制搭接波不匀"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("最高钳次", "500–600", "钳次/min"),
                        new MachineSpecItem("眼数配置", "8", "眼"),
                        new MachineSpecItem("精梳落棉率", "12–22", "%"),
                        new MachineSpecItem("装机功率", "11.0", "kW"),
                        new MachineSpecItem("单机产能", "55–75", "kg/h")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "精梳工序相比普梳工序，最显著提升的纱线质量指标是？",
                        options = new[] { "A. 排除短绒与棉结，大幅提高纱线强力与光洁度", "B. 降低纱线支数并增加毛羽", "C. 省略络筒工序" },
                        correctIndex = 0,
                        explanation = "解析：精梳机通过剔除短纤维与棉结，使纤维高度顺直整齐，是生产高支高密面料纱线的必备工艺。"
                    },
                    previewSpritePath = "Assets/Fanyi/e969ddd78eea8dedceb1b70e1724302.jpg",
                    worldPosition = new Vector3(47.3f, 7.0f, 194.0f),
                    focusCameraPos = new Vector3(62.0f, 14.5f, 246.0f),
                    focusLookAtPos = new Vector3(36.0f, 7.0f, 194.0f),
                    sceneTargetNames = new[] { "卷筒储存(1) (1)", "梳卷一体（1） (2)" },
                    nominalRpm = 540f,
                    nominalTempC = 44.6f,
                    nominalPowerKw = 11.0f,
                    nominalLoadPct = 86f
                },
                new StationInfo
                {
                    index = 5,
                    code = "ST-06",
                    name = "悬锭粗纱机（牵伸加捻卷绕）",
                    shortName = "粗纱工序",
                    englishName = "ROVING FRAME",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "粗纱机将熟条进一步牵伸抽细，并施加适度保护性捻度以赋予须条必要强力（防止退绕意外伸长），最终通过锭翼与筒管差速卷绕成形为大卷装粗纱管，为细纱工序连续喂入做好准备。",
                    teachingGoal = "理解粗纱“牵伸—加捻—卷绕”三大核心动作及张力微调原理，掌握粗纱捻系数的选择原则。",
                    teachingKeyPoints = new[]
                    {
                        "1. 四罗拉牵伸：将熟条稳定抽细 5～12 倍，控制粗纱定量",
                        "2. 适度加捻：强力足以承受卷绕与退绕，同时保证细纱能顺利牵伸",
                        "3. 精密卷绕：随卷绕直径增大自动调节筒管转速，保持张力恒定"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("全机锭数", "120–160", "锭"),
                        new MachineSpecItem("锭翼最高转速", "1500", "r/min"),
                        new MachineSpecItem("粗纱定量范围", "3.0–7.5", "g/10m"),
                        new MachineSpecItem("装机功率", "22.0", "kW"),
                        new MachineSpecItem("落纱方式", "自动集体落纱", "")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "粗纱工序给须条施加“保护性捻度”时为什么不能捻度过大？",
                        options = new[] { "A. 捻度过大会导致后道细纱机无法顺利牵伸，易出硬头", "B. 捻度过大会让粗纱变白", "C. 捻度过大无法卷绕到筒管上" },
                        correctIndex = 0,
                        explanation = "解析：粗纱捻度只需满足卷绕、搬运和细纱退绕不断头即可，过大捻度会增加细纱牵伸阻力导致出硬头或条干恶化。"
                    },
                    previewSpritePath = "Assets/Fanyi/b6a4d20db8528f753f21003231b6934.jpg",
                    worldPosition = new Vector3(45.9f, 9.0f, 132.0f),
                    focusCameraPos = new Vector3(84.0f, 16.5f, 162.0f),
                    focusLookAtPos = new Vector3(45.9f, 8.5f, 130.0f),
                    sceneTargetNames = new[] { "产线01", "产线01 (1)" },
                    nominalRpm = 1320f,
                    nominalTempC = 47.2f,
                    nominalPowerKw = 22.0f,
                    nominalLoadPct = 85f
                },
                new StationInfo
                {
                    index = 6,
                    code = "ST-07",
                    name = "环锭细纱长车（成纱核心）",
                    shortName = "细纱成纱",
                    englishName = "RING SPINNING FRAME",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "细纱工序是纺纱生产的最终成纱环节，将粗纱大幅牵伸至设计特数/支数，利用高速钢领-钢丝圈系统完成真捻加捻，赋予纱线所需的强力、弹性、光泽与手感，并按标准管纱成形规律卷绕于细纱管上。",
                    teachingGoal = "掌握环锭细纱机牵伸区、加捻三角区及钢领钢丝圈卷绕力学原理，理解断头率与锭速、温湿度的耦合关系。",
                    teachingKeyPoints = new[]
                    {
                        "1. 大倍数牵伸：将粗纱拉细 20～50 倍至目标成纱线密度",
                        "2. 加捻三角区：控制紧密纺/集聚纺负压，大幅降低成纱毛羽",
                        "3. 钢领钢丝圈：实现每分钟 18,000+ 转的高速加捻与同步卷绕"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("全机锭数", "1008–1200", "锭"),
                        new MachineSpecItem("锭子转速", "16000–22000", "r/min"),
                        new MachineSpecItem("适纺纱支范围", "10–120", "Ne"),
                        new MachineSpecItem("装机功率", "55.0", "kW"),
                        new MachineSpecItem("千锭时断头率", "≤ 8", "根/千锭·h")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "环锭细纱机完成“高速加捻 + 同步卷绕”的核心执行部件组合是？",
                        options = new[] { "A. 锭子、钢领与钢丝圈", "B. 抓棉打手与吸棉管道", "C. 顶梳与分离罗拉" },
                        correctIndex = 0,
                        explanation = "解析：细纱机依靠高速旋转的锭子带动纱线拉动钢丝圈沿钢领滑动，两者转速差实现卷绕，旋转一周加上一个捻回。"
                    },
                    previewSpritePath = "Assets/Fanyi/e969ddd78eea8dedceb1b70e1724302.jpg",
                    worldPosition = new Vector3(-16.2f, 10.0f, 130.0f),
                    focusCameraPos = new Vector3(18.0f, 16.0f, 158.0f),
                    focusLookAtPos = new Vector3(-16.2f, 9.5f, 128.0f),
                    sceneTargetNames = new[] { "洗砂机01", "洗砂机01 (2)" },
                    nominalRpm = 18500f,
                    nominalTempC = 51.4f,
                    nominalPowerKw = 55.0f,
                    nominalLoadPct = 91f
                },
                new StationInfo
                {
                    index = 7,
                    code = "ST-08",
                    name = "全自动络筒机（清纱成形）",
                    shortName = "络筒成形",
                    englishName = "AUTOMATIC WINDING MACHINE",
                    hall = WorkshopHall.SpinningHall,
                    processSummary = "络筒作为纺纱的最后一道工序和织造的首道工序，起着承上启下的“桥梁”作用。全自动络筒机通过电子清纱器切除纱疵、空气捻接器无结接头，将多个细纱管纱高速并卷为大卷装、成形良好的筒子纱。",
                    teachingGoal = "掌握电子清纱器切疵分级标准与空气捻接强力要求，理解防叠成形与张力闭环控制对后道织造效率的意义。",
                    teachingKeyPoints = new[]
                    {
                        "1. 电子清纱：光电/电容双通道实时切除粗节、细节、棉结与异纤",
                        "2. 空气捻接：无结头接头强力达原纱 85%+，显著提升织造效率",
                        "3. 大卷装防叠：精密槽筒防叠卷绕，保证高速退绕不脱圈"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("单机锭数", "60", "锭"),
                        new MachineSpecItem("最高络纱线速度", "1800–2200", "m/min"),
                        new MachineSpecItem("接头方式", "无结空气捻接", ""),
                        new MachineSpecItem("装机功率", "28.5", "kW"),
                        new MachineSpecItem("成筒重量", "1.8–2.5", "kg/筒")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "全自动络筒机在现代纺纱流程中起到“承上启下”桥梁作用的关键功能是？",
                        options = new[] { "A. 电子清纱除疵、空气捻接并卷绕为大卷装筒子纱", "B. 将原棉包开松混合", "C. 对棉条进行并合牵伸" },
                        correctIndex = 0,
                        explanation = "解析：络筒既是纺纱品质的最后把关（电子清纱），又是满足高速整经/织造大卷装连续退绕的基础。"
                    },
                    previewSpritePath = "Assets/Fanyi/1cb8be29d30766661488f22f213bb84.jpg",
                    worldPosition = new Vector3(-22.0f, 9.0f, 240.0f),
                    focusCameraPos = new Vector3(16.0f, 15.5f, 266.0f),
                    focusLookAtPos = new Vector3(-22.0f, 8.5f, 238.0f),
                    sceneTargetNames = new[] { "牢筒机", "牢筒机 (1)" },
                    nominalRpm = 1950f,
                    nominalTempC = 45.8f,
                    nominalPowerKw = 28.5f,
                    nominalLoadPct = 87f
                },
                new StationInfo
                {
                    index = 8,
                    code = "ST-09",
                    name = "智能视觉检测与立体仓储",
                    shortName = "检测与立体库",
                    englishName = "AI INSPECTION & AS/RS WAREHOUSE",
                    hall = WorkshopHall.InspectionWarehouse,
                    processSummary = "成品纱筒经 AGV 自动物流车转运至智能检测与立体仓储车间，由视觉质检流水线与六轴工业机械臂自动判别合格/不合格品并分流码垛，合格成品自动入库至高密度自动化立体货架，实现全流程无人化闭环管理。",
                    teachingGoal = "理解智能工厂“产线—质检—AGV—立体库（WMS/MES）”数字孪生闭环协同逻辑与质量追溯机制。",
                    teachingKeyPoints = new[]
                    {
                        "1. 在线视觉质检：自动检测筒纱成形、色差与污渍，实时分拣不良品",
                        "2. 工业机械臂码垛：精准抓取与多托盘自动码放，节拍稳定可控",
                        "3. 立体库与 AGV 调度：实时映射 16 货位状态，实现按单智能出入库"
                    },
                    specs = new[]
                    {
                        new MachineSpecItem("检测分拣节拍", "1800", "筒/小时"),
                        new MachineSpecItem("视觉检出准确率", "≥ 99.6", "%"),
                        new MachineSpecItem("AGV 编队数量", "6", "台"),
                        new MachineSpecItem("示范区货位监控", "16 (4×4 矩阵)", "位"),
                        new MachineSpecItem("系统集成接口", "MES / WMS / PLC", "")
                    },
                    quiz = new TeachingQuizItem
                    {
                        question = "智能检测与立体仓储车间在棉纺数字孪生体系中的核心价值是？",
                        options = new[] { "A. 实现质检自动分拣、无人搬运入库与全链路数据追溯", "B. 替代抓棉机完成原棉开松", "C. 仅用于人工手动搬运记录" },
                        correctIndex = 0,
                        explanation = "解析：检测分拣、机械臂码垛、AGV 物流与立体仓储共同构成“黑灯工厂”的末端智能物流与质量闭环。"
                    },
                    previewSpritePath = "Assets/Scenes/Play/摄图网_600865141_工业棉纱纺织工厂(仅交流学习使用).jpg",
                    worldPosition = new Vector3(39.5f, 9.0f, 408.0f),
                    focusCameraPos = new Vector3(39.5f, 14.2f, 376.0f),
                    focusLookAtPos = new Vector3(39.5f, 8.8f, 424.0f),
                    sceneTargetNames = new[] { "检测产线", "检测产线 (1)", "立体仓库", "立体仓库 (1)" },
                    nominalRpm = 1440f,
                    nominalTempC = 36.5f,
                    nominalPowerKw = 32.0f,
                    nominalLoadPct = 76f
                }
            };
        }
    }
}
