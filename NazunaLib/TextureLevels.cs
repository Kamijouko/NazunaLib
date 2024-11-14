using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using HugsLib;
using AlienRace;
using AlienRace.ExtendedGraphics;

namespace NareisLib
{
    public class TextureLevels : GraphicData
    {
        //图层的唯一标识符
        public string textureLevelsName = "";

        //子文件夹路径，如果不需要就不用管
        public string subFolderPath = "";

        //贴图名称前缀（不带文件夹路径）此定义出的贴图名为唯一标识符，请不要重复使用相同的贴图名（前缀可以重复）
        public List<string> prefix = new List<string>();

        //渲染的层级信息
        public TextureRenderLayer renderLayer = TextureRenderLayer.None;

        //渲染的父节点指定，优先级高于上方的渲染层级信息
        public PawnRenderNodeTagDef renderParentNodeTagDef = null;

        //可选参数，如果xml里为空将根据MultiTexDef指定的物品种类自行选择默认的原版Worker
        public Type renderWorker = null;

        //可选参数，来自RenderNode，设置子SubWorker
        public List<Type> subworkerClasses = new List<Type>();

        //可选参数,设定该层是否会由于方向改变等原因自动变更为其他层，
        //例：Hair层在渲染北面时会自动变为BottomHair层
        public bool staticLayer = false;

        //可选参数，开启后此图层采用水平翻转绘制
        public bool flipped = false;

        //可选参数，与上选项搭配使用，开启后将交换东西方向的贴图
        public bool switchEastWest = false;

        //可选参数，来自RenderNode，图层翻转时所在的‘层高（特指原版的layer）’也会翻转
        public bool oppositeFacingLayerWhenFlipped = false;

        //*可选参数，打开后一些在不同方向会交换的图层类型不再交换
        public bool donotChangeLayer = false;

        //可选参数，设置当前层存在时是否替换或隐藏某些可能存在的层
        public List<TextureLevelHideOption> hideList = new List<TextureLevelHideOption>();

        //可选参数，来自RenderNode
        //public List<RenderSkipFlagDef> renderSkipFlags = new List<RenderSkipFlagDef>();

        //可选参数，来自RenderNode
        public PawnOverlayDrawer.OverlayLayer overlayLayer;

        //可选参数，来自RenderNode
        public bool? overlayOverApparel = false;

        //可选参数，来自RenderNode
        public RenderSkipFlagDef skipFlag;

        //可选参数，来自RenderNode
        public PawnRenderNodeTagDef tagDef;


        




        //可选参数，链接到身体部位，与下面的label二选一
        public BodyPartDef bodyPart = null;

        //可选参数，链接到身体部位
        public string bodyPartLabel = null;

        //可选参数，设置特殊hediff所对应的名称前缀（根据hediff严重度）
        //与上方两个选项联动
        public List<TextureLevelHediffSet> hediffSets = new List<TextureLevelHediffSet>();

        //是否在无Hediff时渲染
        public bool rendNoHediff = true;

        //可选参数，可系统性的为图层创建Job条件需求，
        //使用此ActionManager会使jobSets失效
        public ActionManager actionManager = new ActionManager();

        //可选参数，设置特殊job所对应的名称前缀，
        //在与actionManager同时设定时优先使用actionManager
        public TextureLevelJobSet jobSets;

        //可选参数，控制该层是否在Pawn无job时渲染
        public bool rendNoJob = true;





        //可选参数，渲染时使用的Mesh类型，有Hair，Head和Body三种（注意大小写）非Humanlike的pawn只有Body
        public string meshType = "Body";

        //可选参数，当前图层是否要使用头部的位置偏移，适用于不使用head图层以及父节点也不为head及其子节点的图层
        public bool offsetToHead = false;

        //可选参数，渲染时使用的Mesh大小，为zero时不会识别，默认为zero
        public Vector2 meshSize = Vector2.zero;

        //可选参数，区分性别，在文件名的“_方向”之前添加“_性别”，目前只支持"_Male"和"_Female"
        public bool hasGender = false;
        public bool renderMale = false;
        public bool renderFemale = false;

        //可选参数，使用相对躯体类型的贴图版本，不可与useHeadType同时使用
        public bool useBodyType = false;

        //可选参数，使用相对头部类型的贴图版本，不可与useBodyType同时使用
        public bool useHeadType = false;

        //可选参数，来自RenderNode
        public PawnRenderNodeProperties.RenderNodePawnType pawnType = PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly;







        //可选参数，当渲染的部位不是AlienRace的BodyAddon时使用，控制是否在倒地时渲染
        public bool renderOnGround = true;

        //可选参数，当渲染的部位不是AlienRace的BodyAddon时使用，控制是否在床上时渲染
        public bool renderInBed = true;

        //可选参数，该贴图是否有正在腐烂时的版本
        public bool hasRotting = false;

        //可选参数，该贴图是否有骨架版本
        public bool hasDessicated = false;

        //可选参数，该贴图是否有断头版本，只可用于Body层
        public bool hasStump = false;

        //可选参数，该层使用固定的颜色，通过color和colorTwo两个属性设置颜色
        public bool useStaticColor = false;

        //可选参数，该层是否强制获取头发的颜色并使用
        public bool useHairColor = false;

        //可选参数，该层是否强制获取身体的颜色并使用
        public bool useBodyColor = false;






        //可选参数，如果该贴图为手部层，则这个选项控制Hand层和HandTwo层是否绘制在shell层下方
        public bool handDrawHigherOfShell = true;

        //可选参数，当前层是否为袖子
        public bool isSleeve = false;

        //可选参数，设置袖子是否在shell层下方
        public bool sleeveDrawHigherOfShell = true;






        //是否使用在整体y轴上调整
        //开启此选项后在各方向上的偏移将反应在整个Pawn上
        public bool usePublicYOffset = false;

        //使各方向设置的偏移为一个固定的y轴高度
        //不建议同时开启usePublicYOffset
        public bool useStaticYOffset = false;

        //可选参数，x、y、z分别表示正面侧面和背面，为1时会被渲染，为0时会被忽略
        public Vector3 renderSwitch = Vector3.one;

        //可选参数，相当于设置RenderNode里的baseLayer
        //用于调整每个方向上的1.5新增的‘层’这个概念
        //每1层影响Y轴高度是0.00038461538f，最底层为-10f
        //可以和各个设置各个方向offset一起使用
        public float baseLayer;

        //可选参数，单独设置每个方向的层
        public NareisLib_DrawData drawData = null;

        //可选参数，来自RenderNode
        public bool rotateIndependently = false;

        //可选参数，来自RenderNode
        public PawnRenderNodeProperties.Side side;







        //可选参数，设定某种前缀prefix的生成权重
        public Dictionary<string, int> weightOfThePrefix = new Dictionary<string, int>();

        //可选参数，针对某张贴图的特定名称设定权重（不带文件格式以及前缀后缀）
        public Dictionary<string, int> weightOfTheName = new Dictionary<string, int>();

        //默认的权重（在未设定权重的情况下所有贴图的权重均为1）常数，无法更改
        public const int normalWeight = 1;



        //xml里无需设定并且设定无效
        public PawnRenderNode node = null;
        //xml里无需设定并且设定无效
        public MultiTexBatch cachedBatch = null;
        //xml里无需设定并且设定无效
        public Dictionary<string, int> preFixWeights = new Dictionary<string, int>();

        //xml里无需设定并且设定无效
        public Dictionary<string, Dictionary<string, int>> texWeights = new Dictionary<string, Dictionary<string, int>>();

        //xml里无需设定并且设定无效
        public Dictionary<string, string[]> preFixToTexName = new Dictionary<string, string[]>();

        //xml里无需设定并且设定无效
        public string keyName = "";

        //xml里无需设定并且设定无效
        public Type originalDefClass = null;

        //xml里无需设定并且设定无效
        public string originalDef = "";

        //xml里无需设定并且设定无效
        public string hediffPrefix = "";

        //xml里无需设定并且设定无效
        public bool hediffEnableWithJob = false;

        //xml里无需设定并且设定无效
        public string jobPrefix = "";

        //xml里无需设定并且设定无效
        public string exPath = "";

        //xml里无需设定并且设定无效
        public string genderSuffix = "";

        //xml里无需设定并且设定无效
        public string folder = "";

        //xml里无需设定并且设定无效
        public Graphic cacheGraphic;

        //xml里无需设定并且设定无效
        public Apparel cachedApparel;



        //用于从ThisModData的数据库中克隆
        public TextureLevels Clone()
        {
            TextureLevels result = new TextureLevels();
            result.CopyFrom(this);
            result.textureLevelsName = textureLevelsName;
            result.subFolderPath = subFolderPath;
            result.prefix = prefix;
            result.renderLayer = renderLayer;
            result.staticLayer = staticLayer;            //
            result.flipped = flipped;                   //
            result.switchEastWest = switchEastWest;     //
            result.donotChangeLayer = donotChangeLayer; //
            if (!hideList.NullOrEmpty())
                result.hideList = new List<TextureLevelHideOption>(hideList);//


            if (!hediffSets.NullOrEmpty())
                result.hediffSets = new List<TextureLevelHediffSet>(hediffSets);
            result.actionManager = actionManager.Clone();//
            if (jobSets != null)
                result.jobSets = jobSets.Clone();
            /*if (patternSets != null)
                result.patternSets = patternSets.Clone();*/
            result.rendNoJob = rendNoJob;               //
            result.rendNoHediff = rendNoHediff;         //
            result.meshType = meshType;
            result.meshSize = meshSize;
            result.hasGender = hasGender;
            result.renderMale = renderMale;
            result.renderFemale = renderFemale;
            result.usePublicYOffset = usePublicYOffset;
            result.useStaticYOffset = useStaticYOffset;
            result.bodyPart = bodyPart;
            result.bodyPartLabel = bodyPartLabel;
            result.renderOnGround = renderOnGround;
            result.renderInBed = renderInBed;
            result.hasRotting = hasRotting;
            result.hasDessicated = hasDessicated;
            result.hasStump = hasStump;

            result.useStaticColor = useStaticColor;
            result.useHairColor = useHairColor;
            result.useBodyColor = useBodyColor;

            result.useBodyType = useBodyType;
            result.useHeadType = useHeadType;
            result.handDrawHigherOfShell = handDrawHigherOfShell;
            result.renderSwitch = renderSwitch;
            result.weightOfThePrefix = weightOfThePrefix;
            result.weightOfTheName = weightOfTheName;
            result.isSleeve = isSleeve;
            //result.sleeveTexList = sleeveTexList;
            result.sleeveDrawHigherOfShell = sleeveDrawHigherOfShell;

            result.drawOffsetNorth = drawOffsetNorth;
            result.drawOffsetEast = drawOffsetEast;
            result.drawOffsetSouth = drawOffsetSouth;
            result.drawOffsetWest = drawOffsetWest;

            result.preFixWeights = preFixWeights;
            result.texWeights = texWeights;
            result.preFixToTexName = preFixToTexName;
            result.keyName = keyName;
            result.originalDefClass = originalDefClass;
            result.originalDef = originalDef;
            result.hediffPrefix = hediffPrefix;
            result.jobPrefix = jobPrefix;
            result.exPath = exPath;
            result.genderSuffix = genderSuffix;
            result.folder = folder;



            result.renderParentNodeTagDef = renderParentNodeTagDef;
            result.renderWorker = renderWorker;
            result.subworkerClasses = subworkerClasses;

            result.oppositeFacingLayerWhenFlipped = oppositeFacingLayerWhenFlipped;

            result.overlayLayer = overlayLayer;
            result.overlayOverApparel = overlayOverApparel;
            result.skipFlag = skipFlag;
            result.tagDef = tagDef;

            result.pawnType = pawnType;

            result.baseLayer = baseLayer;
            result.drawData = drawData;
            result.rotateIndependently = rotateIndependently;
            result.side = side;


            result.node = node;
            result.cachedBatch = cachedBatch;
            result.cacheGraphic = cacheGraphic;
            result.cachedApparel = cachedApparel;


            return result;
        }


        //对当前Level应该渲染的贴图路径进行处理，并且将每个路径都对应一个GraphicData
        public void GetAllGraphicDatas(MultiTexDef def)
        {
            if (def == null)
                return;
            if (graphicClass == null)
                graphicClass = typeof(Graphic_Multi);
            if (drawOffsetSouth == null)
                drawOffsetSouth = Vector3.zero;
            if (drawOffsetEast == null)
                drawOffsetEast = Vector3.zero;
            if (drawOffsetNorth == null)
                drawOffsetNorth = Vector3.zero;
            if (drawOffsetWest == null)
                drawOffsetWest = Vector3.zero;
            folder = def.path;
            originalDef = def.originalDef;
            originalDefClass = def.originalDefClass;
            string type_originalDefName = originalDefClass.ToStringSafe() + "_" + def.originalDef;

            //处理当prefix列表为空的时候，就使用原GraphicData的texPath路径
            if (prefix.NullOrEmpty())
            {
                string tmpKeyName = "singleLevel" + ThisModData.TmpLevelID;
                if (texPath == null)
                {
                    texPath = tmpKeyName;
                    ThisModData.TmpLevelID++;
                }   

                if (!ThisModData.TexLevelsDatabase[type_originalDefName].ContainsKey(textureLevelsName))
                {
                    //preFixToTexName["NullPrefix"] = new string[] { tmpKeyName };
                    //ResolveSingleTexWeights(tmpKeyName);
                    //keyName = tmpKeyName;
                    ThisModData.TexLevelsDatabase[type_originalDefName][textureLevelsName] = this;
                }
                return;
            }

            //组合指定的文件夹路径
            string[] folderAbsDir = LoadedModManager.RunningModsListForReading.Select(x => Path.Combine(x.RootDir, "Textures", folder, subFolderPath)).Where(x => Directory.Exists(x)).ToArray();

            //处理prefix对应的图层的全名并且赋予它prefix预设权重列表所对应的权重
            if (!prefix.NullOrEmpty())
            {
                foreach (string pre in prefix)
                {
                    string[] fullName = folderAbsDir.SelectMany(x => Directory.GetFiles(x, pre + "*")).ToArray();
                    
                    if (fullName.Length > 0)
                    {
                        //Log.Warning(fullName[0]);
                        string[] resolvedName = fullName.Select(x => ResolveKeyName(Path.GetFileNameWithoutExtension(x))).Distinct().ToArray();
                        preFixToTexName[pre] = resolvedName;;
                        ResolvePrefixWeights(pre);
                    }
                }
            }
            

            //处理路径全名为不带后缀的名称，并且如果有针对特定名称列表的话就给特定名称赋予权重
            if (!preFixToTexName.NullOrEmpty())
            {
                if (!ThisModData.TexLevelsDatabase[type_originalDefName].ContainsKey(textureLevelsName))
                {
                    foreach (string pre in preFixToTexName.Keys)
                    {
                        if (preFixToTexName[pre].NullOrEmpty())
                            continue;
                        foreach (string name in preFixToTexName[pre])
                        {
                            ResolveSingleTexWeights(pre, name);
                        }
                    }
                    ThisModData.TexLevelsDatabase[type_originalDefName][textureLevelsName] = this;
                }
                
            }
        } 

        //赋予特定名称权重
        public void ResolveSingleTexWeights(string pre, string keyName)
        {
            if (!texWeights.ContainsKey(pre))
                texWeights[pre] = new Dictionary<string, int>();
            if (!weightOfTheName.NullOrEmpty())
            {
                if (weightOfTheName.ContainsKey(keyName))
                    texWeights[pre][keyName] = weightOfTheName[keyName];
                else
                    texWeights[pre][keyName] = normalWeight;
            }
            else
                texWeights[pre][keyName] = normalWeight;
        }

        //赋予prefix权重
        public void ResolvePrefixWeights(string pre)
        {
            if (!weightOfThePrefix.NullOrEmpty())
            {
                if (weightOfThePrefix.ContainsKey(pre))
                    preFixWeights[pre] = weightOfThePrefix[pre];
                else
                    preFixWeights[pre] = normalWeight;
            }
            else
                preFixWeights[pre] = normalWeight;
        }

        //去掉贴图名称中的额外信息
        public static string ResolveKeyName(string key)
        {
            //此处的顺序很重要
            key = key.Replace("_northm", "");
            key = key.Replace("_north", ""); 
            key = key.Replace("_southm", "");
            key = key.Replace("_south", "");
            key = key.Replace("_eastm", "");
            key = key.Replace("_east", "");
            key = key.Replace("_westm", "");
            key = key.Replace("_west", "");
            key = key.Replace("_Male", "");
            key = key.Replace("_Female", "");
            key = key.Replace("_Rotting", "");
            key = key.Replace("_Dessicated", "");
            key = key.Replace("_Stump", "");
            //List<string> list = DefDatabase<BodyTypeDef>.AllDefsListForReading.Select(x => x.defName).Concat(DefDatabase<HeadTypeDef>.AllDefsListForReading.Select(x => x.defName)).ToList();
            foreach (string surffix in ThisModData.SuffixList)
                key = key.Replace("_" + surffix + "_", "_");
            return key;
        }

        

        //检查关联的bodyPart是否还存在
        public bool RequiredBodyPartStatusFor(ExtendedGraphicsPawnWrapper pawn)
        {
            if (pawn.HasNamedBodyPart(this.bodyPart, this.bodyPartLabel))
            {
                return true;
            }
            return hediffSets.Any(x => x.hediff == HediffDefOf.MissingBodyPart);
        }

        //检查是否在地上或床上渲染
        public bool VisibleForPostureOf(ExtendedGraphicsPawnWrapper pawn)
        {
            return (pawn.GetPosture() == PawnPosture.Standing || this.renderOnGround) && (pawn.VisibleInBed() || this.renderInBed);
        }

        //检查当前Pawn的job是否符合并对jobPrefix赋值
        public bool ResolvePrefixForJob(ExtendedGraphicsPawnWrapper pawn, Pawn obj, string keyName)
        {
            if (jobSets == null && actionManager.def == null)
                return true;
            if (pawn.CurJob != null)
            {
                if (actionManager.def == null)
                {
                    TextureLevelJobDataSet data;
                    return !jobSets.JobMap.TryGetValue(pawn.CurJob.def, out data) || (data.texList.Contains(keyName) && data.IsApplicable(pawn, out jobPrefix));
                }
                else
                {
                    jobPrefix = "";
                    actionManager.StateUpdate(pawn, obj, pawn.CurJob.def, keyName, this);
                    exPath = actionManager.GetFullPath;
                    return actionManager.IsApplicable(pawn);
                }
            }
            exPath = "";
            jobPrefix = "";
            return rendNoJob;
        }

        //检查当前Pawn当前部位的hediff是否符合并对hediffPrefix赋值
        public bool ResolvePrefixForHediff(ExtendedGraphicsPawnWrapper pawn, string keyName)
        {
            if (hediffSets.NullOrEmpty() || (bodyPart == null && bodyPartLabel == ""))
                return true;
            int priority = 0;
            hediffPrefix = "";
            foreach (TextureLevelHediffSet set in hediffSets)
            {
                if (!set.texList.Contains(keyName))
                    continue;
                if (set.priority >= priority && set.GetCurHediffPrefix(pawn, bodyPart, bodyPartLabel, ref hediffPrefix))
                {
                    priority = set.priority;
                    hediffEnableWithJob = set.enableWithJob;
                }
            }
            if (hediffPrefix == "")
                return rendNoHediff;
            return true;
        }

        //检查当前Pawn的性别并对genderSuffix赋值
        public bool ResolveSuffixForGenderOf(ExtendedGraphicsPawnWrapper pawn)
        {
            if (!hasGender)
                return true;
            if (pawn.GetGender() != Gender.Female)
            {
                genderSuffix = "_Male";
                return renderMale;
            }
            genderSuffix = "_Female";
            return renderFemale;
        }


        //最终判定以及处理该graphicData
        public bool CanRender(Pawn pawn, string keyName)
        {
            ExtendedGraphicsPawnWrapper obj = new ExtendedGraphicsPawnWrapper(pawn);
            return RequiredBodyPartStatusFor(obj) && VisibleForPostureOf(obj) && ResolvePrefixForJob(obj, pawn, keyName) && ResolvePrefixForHediff(obj, keyName) && ResolveSuffixForGenderOf(obj);
        }

        //初始化，与基类的Init方法相同但显式
        public void Initialization(Color one, Color two)
        {
            if (graphicClass == null)
            {
                cacheGraphic = null;
                return;
            }
            ShaderTypeDef shaderType = this.shaderType;
            if (shaderType == null)
            {
                shaderType = ShaderTypeDefOf.CutoutComplex;
            }
            Shader shader = shaderType.Shader;
            color = color != Color.white ? color : one;
            colorTwo = colorTwo != Color.white ? colorTwo : two;
            cacheGraphic = GraphicDatabase.Get(this.graphicClass, this.texPath, shader, this.drawSize, color, colorTwo, this, this.shaderParameters, this.maskPath);
            if (onGroundRandomRotateAngle > 0.01f)
            {
                cacheGraphic = new Graphic_RandomRotated(cacheGraphic, this.onGroundRandomRotateAngle);
            }
            if (this.Linked)
            {
                cacheGraphic = GraphicUtility.WrapLinked(cacheGraphic, this.linkType);
            }
        }

        //取得完整的贴图名称，前缀顺序为pattern，job，hediff
        public string GetFullKeyName(string keyName, string condition = "", string bodyType = "", string headType = "")
        {            
            /*string patternPrefix = "";
            if (pattern > 0)
                patternPrefix = "Pattern" + pattern.ToString() + "_";*/
            if (jobPrefix != "")
                jobPrefix = jobPrefix + "_";
            if (hediffPrefix != "")
                hediffPrefix = hediffPrefix + "_";
            if (bodyType != "" && headType == "")
                bodyType = "_" + bodyType;
            if (headType != "" && bodyType == "")
                headType = "_" + headType;
            if (condition != "")
                condition = "_" + condition;
            string result = new StringBuilder().Append(new string[] {jobPrefix, keyName, genderSuffix, bodyType, headType, condition }.SelectMany(x => x).ToArray()).ToString();
            return result;
        }

        //取得graphic，修改了基类的属性Graphic，参数为完全处理完毕后多层渲染comp里记录的keyName（列表在MultiTexBatch里）
        public Graphic GetGraphic(string keyName, Color color, Color colorTwo, string condition = "", string bodyType = "", string headType = "")
        {
            string path = (exPath == "" || exPath == null)
                ? ((hediffPrefix == "" || hediffPrefix == null)
                   ? Path.Combine(folder, subFolderPath, GetFullKeyName(keyName, condition, bodyType, headType))
                   : Path.Combine(folder, subFolderPath, hediffPrefix, GetFullKeyName(keyName, condition, bodyType, headType)))
                : ((hediffPrefix == "" || hediffPrefix == null)
                   ? Path.Combine(exPath, GetFullKeyName(keyName, condition, bodyType, headType))
                   : (hediffEnableWithJob
                      ? Path.Combine(exPath, hediffPrefix, GetFullKeyName(keyName, condition, bodyType, headType))
                      : Path.Combine(folder, subFolderPath, hediffPrefix, GetFullKeyName(keyName, condition, bodyType, headType))));
            if (texPath != path || cacheGraphic == null)
            {
                texPath = path;
                Initialization(color, colorTwo);
            }
            return cacheGraphic;
        }

        public TextureLevelsToNode GetPawnRenderNode(MultiRenderComp renderComp, Pawn pawn)
        {
            TextureLevelsToNodeProperties prop = MakeProperties();
            prop.InitProperties(this, cachedBatch);
            TextureLevelsToNode result = (TextureLevelsToNode)Activator.CreateInstance(prop.nodeClass, new object[]
            {
                pawn,
                prop,
                pawn.Drawer.renderer.renderTree,
                this,
                cachedBatch,
                renderComp,
                cachedApparel
            });
            result.textureLevels.node = result;
            result.textureLevels.actionManager.node = result;
            return result;
        }

        TextureLevelsToNodeProperties MakeProperties()
        {
            return new TextureLevelsToNodeProperties();
        }
    }
}
