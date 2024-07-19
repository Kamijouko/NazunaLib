﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using AlienRace.ExtendedGraphics;
using System.IO;

namespace NareisLib
{
    public class MultiRenderComp : ThingComp
    {
        //存储当前Pawn身体，发型，服装的层渲染数据，
        //key为pawn需要替换或者添加图像的部件的originalDefName，格式为Type_OriginalDefName，Type为这个Def的类型，
        //value为存储需要渲染的图像的名称列表以及在哪个层渲染的信息
        public Dictionary<string, MultiTexEpoch> storedDataBody, storedDataHair, storedDataApparel = new Dictionary<string, MultiTexEpoch>(); 

        //用于缓存每次处理完的所有层数据,方向为正面，侧面，背面，反侧面，
        //key为TextureRenderLayer转为整数值，
        //value为经y轴排序后的贴图名字列表
        public Dictionary<int, List<MultiTexBatch>> cachedDataSouth, cachedDataEast, cachedDataNorth, cachedDataWest = new Dictionary<int, List<MultiTexBatch>>();

        //用于缓存已经初始化了的身体，头发，服装的TextureLevels，
        //第一个key为Type_OriginalDefName，Type为这个Def的类型，
        //第二个key为从multiTexBatch读取的TextureLevelName，
        //value为其对应的TextureLevels
        public Dictionary<string, Dictionary<string, TextureLevels>> cachedBodyGraphicData, cachedHairGraphicData, cachedApparelGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();

        //用于缓存上面三个dict为一个
        public Dictionary<string, Dictionary<string, TextureLevels>> cachedAllOriginalDefForGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();

        //用于缓存以上三个dict的总和，
        //key为从multiTexBatch读取的TextureLevelName，
        //value为其对应的TextureLevels
        //public Dictionary<string, TextureLevels> cachedAllGraphicData = new Dictionary<string, TextureLevels>();
        public List<TextureLevels> cachedAllOriginalDefForGraphicDataList = new List<TextureLevels>();

        //用于缓存具有随机状态的贴图的当前的pattern，
        //key为此贴图的Type_OriginalDefName_KeyName，
        //value为此贴图目前应使用的pattern序号
        //public Dictionary<string, int> cachedRandomGraphicPattern = new Dictionary<string, int>();

        //用于缓存是否需要覆盖原部位的层名称列表,其中身体使用Body，头部使用Head，头发使用Hair来表示，其余覆盖部位用其defName表示
        public List<string> cachedOverrideBody, cachedOverrideApparel, cachedOverrideHair = new List<string>();

        //用于缓存当前帧需要隐藏或替换层的数据
        public Dictionary<string, TextureLevelHideOption> cachedHideOrReplaceDict = new Dictionary<string, TextureLevelHideOption>();

        //用于缓存当前pawn的race关联的RenderPlanDef
        //public string cachedRenderPlanDefName = "";

        //用于存储当前pawn的手部DefName
        public string storedHandDefName = "";

        //用于缓存当前手对应的持有装备角度
        public float holdEquipmentAngle = 0f;

        ExtendedGraphicsPawnWrapper pawnWarpper = null;

        public string pawnName = "";

        //public int timeTickLineIndex = 0;
        //public TextureLevelRandomPatternSet[] patternLine = new TextureLevelRandomPatternSet[] { };

        public bool PrefixResolved = false;


        public MultiRenderCompProperties Props
        {
            get
            {
                return (MultiRenderCompProperties)props;
            }
        }
        public string GetCurHandDefName
        {
            get
            {
                if (storedHandDefName == "" && !Props.handDefNameAndWeights.NullOrEmpty())
                    storedHandDefName = Props.handDefNameAndWeights.Keys.RandomElementByWeight(x => Props.handDefNameAndWeights[x]);
                return storedHandDefName;
            }
            set
            {
                storedHandDefName = value;
            }
        }
        public Dictionary<string, Dictionary<string, TextureLevels>> GetAllOriginalDefForGraphicDataDict
        {
            get
            {
                return cachedAllOriginalDefForGraphicData;
            }
        }
        public List<string> GetAllHideOriginalDefData
        {
            get
            {
                if (cachedOverrideBody != null)
                {
                    if (cachedOverrideHair != null)
                    {
                        if (cachedOverrideApparel != null)
                            return cachedOverrideBody.Concat(cachedOverrideHair).Concat(cachedOverrideApparel).ToList();
                        return cachedOverrideBody.Concat(cachedOverrideHair).ToList();
                    }
                    if (cachedOverrideApparel != null)
                        return cachedOverrideBody.Concat(cachedOverrideApparel).ToList();
                    return cachedOverrideBody;
                }
                return new List<string>();
            }
        }
        public List<MultiTexBatch> GetAllBatch
        {
            get
            {
                return storedDataBody.Values.Concat(storedDataApparel.Values).Concat(storedDataHair.Values).SelectMany(x => x.batches).ToList();
            }
        }
        protected Pawn PawnOwner
        {
            get
            {
                Pawn result;
                if ((result = (parent as Pawn)) != null)
                {
                    return result;
                }
                return null;
            }
        }
        public Texture2D GetGizmoIcon
        {
            get
            {
                Texture2D result = TexCommand.Install;
                if (!ThisModData.RacePlansDatabase[PawnOwner.def.defName].actionSettingGizmo_IconPath.NullOrEmpty())
                {
                    result = ContentFinder<Texture2D>.Get(ThisModData.RacePlansDatabase[PawnOwner.def.defName].actionSettingGizmo_IconPath, true);
                }
                return result;
            }
        }


        public MultiRenderComp()
        {

        }

        public bool TryGetStoredTextureLevels(string type_OriDef, string texLevelsName, out TextureLevels level)
        {
            if (!GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                && GetAllOriginalDefForGraphicDataDict.ContainsKey(type_OriDef)
                && GetAllOriginalDefForGraphicDataDict[type_OriDef].ContainsKey(texLevelsName))
            {
                level = GetAllOriginalDefForGraphicDataDict[type_OriDef][texLevelsName];
                return true;
            }
            level = null;
            return false;
        }


        public override void CompTick()
        {
            base.CompTick();

            /*if (parent as Pawn != null && (parent as Pawn).Faction != null && (parent as Pawn).Faction.IsPlayer)
                Log.Warning("手臂：" + GetCurHandDefName);*/
            if (!parent.Spawned)
                return;
            if (pawnWarpper == null)
            {
                if (parent as Pawn != null && (parent as Pawn).Faction != null && (parent as Pawn).Faction.IsPlayer)
                    pawnWarpper = new ExtendedGraphicsPawnWrapper((Pawn)parent);
            }
            else if (ModStaticMethod.ThisMod.pawnCurJobDisplayToggle)
            {
                if (pawnWarpper.CurJob != null)
                    Log.Warning(pawnName + "当前工作：" + pawnWarpper.CurJob.def.defName);
                else
                    Log.Warning(pawnName + "当前工作：Null");
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            foreach (TextureLevels t in cachedAllOriginalDefForGraphicDataList)
            {
                if (t.actionManager.def != null)
                    t.actionManager.Destory();
            }
        }


        //根据方向获取comp存储的keyName的Dict
        public Dictionary<int, List<MultiTexBatch>> GetDataOfDirection(Rot4 facing)
        {
            switch (facing.AsInt)
            {
                case 2: return cachedDataSouth;
                case 1: return cachedDataEast;
                case 3: return cachedDataWest;
                case 0: return cachedDataNorth;
                default: return null;
            }
        }


        //对MultiTexEpoch所有的MultiTexBatch的Layer，针对每个方向进行分类和排序，并记入缓存
        public void ResolveAllLayerBatch()
        {
            List<MultiTexBatch> list = GetAllBatch;
            if (ModStaticMethod.ThisMod.debugToggle)
                Log.Warning("batch:" + list.Count().ToString());
            /*if (list.NullOrEmpty())
                return;

            Dictionary<int, List<MultiTexBatch>> dataSouth = new Dictionary<int, List<MultiTexBatch>>();
            Dictionary<int, List<MultiTexBatch>> dataEast = new Dictionary<int, List<MultiTexBatch>>();
            Dictionary<int, List<MultiTexBatch>> dataWest = new Dictionary<int, List<MultiTexBatch>>();
            Dictionary<int, List<MultiTexBatch>> dataNorth = new Dictionary<int, List<MultiTexBatch>>();
            foreach (MultiTexBatch batch in list)
            {
                if (batch.renderSwitch.x != 0)
                {
                    if (dataSouth.NullOrEmpty() || !dataSouth.ContainsKey((int)batch.layer))
                        dataSouth[(int)batch.layer] = new List<MultiTexBatch>();
                    dataSouth[(int)batch.layer].Add(batch.Clone());
                }
                if (batch.renderSwitch.y != 0)
                {
                    TextureRenderLayer layer = batch.layer;
                    if (!batch.staticLayer && !batch.donotChangeLayer && batch.layer == TextureRenderLayer.BottomHair)
                        layer = TextureRenderLayer.Hair;
                    if (dataEast.NullOrEmpty() || !dataEast.ContainsKey((int)layer))
                        dataEast[(int)layer] = new List<MultiTexBatch>();
                    if (dataWest.NullOrEmpty() || !dataWest.ContainsKey((int)layer))
                        dataWest[(int)layer] = new List<MultiTexBatch>();
                    dataEast[(int)layer].Add(batch.Clone());
                    dataWest[(int)layer].Add(batch.Clone());
                }
                if (batch.renderSwitch.z != 0)
                {
                    TextureRenderLayer layer = batch.layer;
                    if (!batch.staticLayer && !batch.donotChangeLayer)
                    {
                        if (batch.layer == TextureRenderLayer.BottomHair)
                            layer = TextureRenderLayer.Hair;
                        else if (batch.layer == TextureRenderLayer.BottomShell)
                            layer = TextureRenderLayer.FrontShell;
                        else if (batch.layer == TextureRenderLayer.FrontShell)
                            layer = TextureRenderLayer.BottomShell;
                        else if (batch.layer == TextureRenderLayer.Hair)
                            layer = TextureRenderLayer.BottomHair;
                    }
                    if (dataNorth.NullOrEmpty() || !dataNorth.ContainsKey((int)layer))
                        dataNorth[(int)layer] = new List<MultiTexBatch>();
                    dataNorth[(int)layer].Add(batch.Clone());
                }
            }
            cachedDataSouth = dataSouth;
            cachedDataEast = dataEast;
            cachedDataWest = dataWest;
            cachedDataNorth = dataNorth;

            
            if (!cachedDataSouth.NullOrEmpty())
            {
                foreach (int t in cachedDataSouth.Keys)
                {
                    if (cachedDataSouth[t].Count() > 1)
                        cachedDataSouth[t].Sort((i, j) => ThisModData.TexLevelsDatabase[i.originalDefClass.ToStringSafe() + "_" + i.originalDefName][i.textureLevelsName].drawOffsetSouth.Value.y.CompareTo(ThisModData.TexLevelsDatabase[j.originalDefClass.ToStringSafe() + "_" + j.originalDefName][j.textureLevelsName].drawOffsetSouth.Value.y));
                }
            }
            if (!cachedDataEast.NullOrEmpty())
            {
                foreach (int t in cachedDataEast.Keys)
                {
                    if (cachedDataEast[t].Count() > 1)
                        cachedDataEast[t].Sort((i, j) => ThisModData.TexLevelsDatabase[i.originalDefClass.ToStringSafe() + "_" + i.originalDefName][i.textureLevelsName].drawOffsetEast.Value.y.CompareTo(ThisModData.TexLevelsDatabase[j.originalDefClass.ToStringSafe() + "_" + j.originalDefName][j.textureLevelsName].drawOffsetEast.Value.y));
                }
            }
            if (!cachedDataWest.NullOrEmpty())
            {
                foreach (int t in cachedDataWest.Keys)
                {
                    if (cachedDataWest[t].Count() > 1)
                        cachedDataWest[t].Sort((i, j) => ThisModData.TexLevelsDatabase[i.originalDefClass.ToStringSafe() + "_" + i.originalDefName][i.textureLevelsName].drawOffsetWest.Value.y.CompareTo(ThisModData.TexLevelsDatabase[j.originalDefClass.ToStringSafe() + "_" + j.originalDefName][j.textureLevelsName].drawOffsetWest.Value.y));
                }
            }
            if (!cachedDataNorth.NullOrEmpty())
            {
                foreach (int t in cachedDataNorth.Keys)
                {
                    if (cachedDataNorth[t].Count() > 1)
                        cachedDataNorth[t].Sort((i, j) => ThisModData.TexLevelsDatabase[i.originalDefClass.ToStringSafe() + "_" + i.originalDefName][i.textureLevelsName].drawOffsetNorth.Value.y.CompareTo(ThisModData.TexLevelsDatabase[j.originalDefClass.ToStringSafe() + "_" + j.originalDefName][j.textureLevelsName].drawOffsetNorth.Value.y));
                }
            }*/
            
            
            

            cachedAllOriginalDefForGraphicData = cachedBodyGraphicData.Concat(cachedHairGraphicData).Concat(cachedApparelGraphicData).ToDictionary(k => k.Key, v => v.Value);
            cachedAllOriginalDefForGraphicDataList = cachedAllOriginalDefForGraphicData.Values.SelectMany(x => x.Values).ToList();
            //cachedAllGraphicData = cachedAllOriginalDefForGraphicData.SelectMany(x => x.Value).ToDictionary(k => k.Key, v => v.Value);

            //获取临时的所有应该隐藏某个图层的列表
            cachedHideOrReplaceDict = cachedAllOriginalDefForGraphicDataList.Where(x => !x.hideList.NullOrEmpty()).SelectMany(x => x.hideList).ToLookup(k => k.defLevelName).ToDictionary(g => g.Key, g => g.First());


            //初始化randomPattern队列
            /*patternLine = cachedAllOriginalDefForGraphicData.SelectMany(x => x.Value.Values).Where(x => x.patternSets != null && x.patternSets.texList.Contains(x.keyName)).Select(x => x.patternSets).ToArray();
            if (patternLine.Length > 1)
                patternLine.SortStable((i, j) => i.RandomNextIntervalAndPattern().CompareTo(j.RandomNextIntervalAndPattern()));
            cachedRandomGraphicPattern.Clear();
            timeTickLineIndex = 0;*/

            if (ModStaticMethod.ThisMod.debugToggle)
            {
                Log.Warning("south:" + cachedDataSouth.SelectMany(x => x.Value).Count().ToString());
                Log.Warning("east:" + cachedDataEast.SelectMany(x => x.Value).Count().ToString());
                Log.Warning("north:" + cachedDataNorth.SelectMany(x => x.Value).Count().ToString());
                Log.Warning("AllGraphicData:" + GetAllOriginalDefForGraphicDataDict.Values.SelectMany(x => x.Values).Count().ToString());
                Log.Warning("levels:" + ThisModData.TexLevelsDatabase.Values.SelectMany(x => x.Values).Count().ToString());
                Log.Warning("plans:" + ThisModData.DefAndKeyDatabase.Values.SelectMany(x => x.Values).Count().ToString());
            }
        }



        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (PawnOwner != null && PawnOwner.Faction == Faction.OfPlayer && Find.Selector.SingleSelectedThing == PawnOwner && ThisModData.RacePlansDatabase.ContainsKey(PawnOwner.def.defName))
            {
                List<TextureLevels> levels = cachedAllOriginalDefForGraphicDataList.Where(x => x.actionManager.def != null).ToList();

                foreach (TextureLevels level in levels)
                {
                    
                }
                yield return new Command_Action
                {
                    defaultLabel = ThisModData.RacePlansDatabase[PawnOwner.def.defName].actionSettingGizmo_Label,
                    defaultDesc = ThisModData.RacePlansDatabase[PawnOwner.def.defName].actionSettingGizmo_Desc,
                    icon = GetGizmoIcon,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Page_Setting_CurBehavior(this));
                    }
                };
            }
            yield break;
        }


        //下方法的子方法，为获取到的TextureLevels进行赋值操作
        public static TextureLevels ResolveKeyNameForLevel(TextureLevels level, string key, MultiTexBatch batch)
        {
            level.keyName = key;
            level.cachedBatch = batch;
            /*if (level.patternSets != null)
                level.patternSets.typeOriginalDefNameKeyName = level.originalDefClass.ToStringSafe() + "_" + level.originalDef + "_" + key;*/
            return level;
        }

        //从comp的storedData里获取TextureLevels数据，用于处理读取存档时从已有的storedData字典中得到的epoch
        public static Dictionary<string, TextureLevels> GetLevelsDictFromEpoch(MultiTexEpoch epoch)
        {
            return !epoch.batches.NullOrEmpty() ? epoch.batches.ToDictionary(k => k.textureLevelsName, v => ResolveKeyNameForLevel(ThisModData.TexLevelsDatabase[v.originalDefClass.ToStringSafe() + "_" + v.originalDefName][v.textureLevelsName].Clone(), v.keyName, v)) : new Dictionary<string, TextureLevels>();
        }

        //处理defName所指定的MultiTexDef，
        //对其属性levels里所存储的所有TextureLevels都根据指定的权重随机一个贴图的名称，
        //并将名称记录进一个从其属性cacheOfLevels得来的MultiTexEpoch中所对应渲染图层的MultiTexBatch的名称列表里，
        //最终返回这个MultiTexEpoch
        public static MultiTexEpoch ResolveMultiTexDef(MultiTexDef def, out Dictionary<string, TextureLevels> data, Apparel apparel = null)
        {
            MultiTexEpoch epoch = new MultiTexEpoch(def.originalDefClass.ToStringSafe() + "_" + def.originalDef);
            List<MultiTexBatch> batches = new List<MultiTexBatch>();
            data = new Dictionary<string, TextureLevels>();
            try
            {
                foreach (TextureLevels level in def.levels)
                {
                    string pre = "";
                    string keyName = "";
                    if (level.prefix.NullOrEmpty() && level.texPath != null)
                    {
                        keyName = TextureLevels.ResolveKeyName(Path.GetFileNameWithoutExtension(level.texPath));
                    }
                    else if (!level.prefix.NullOrEmpty())
                    {
                        pre = level.prefix.RandomElementByWeight(x => level.preFixWeights.ContainsKey(x) ? level.preFixWeights[x] : 1);
                        keyName = level.preFixToTexName[pre].RandomElementByWeight(x => level.texWeights[pre].ContainsKey(x) ? level.texWeights[pre][x] : 1);
                    }

                    MultiTexBatch batch = new MultiTexBatch(def.originalDefClass, def.originalDef, def.defName, keyName, level.textureLevelsName, level.renderLayer, level.renderSwitch, level.staticLayer, level.donotChangeLayer);
                    if (!batches.Exists(x => x.textureLevelsName == level.textureLevelsName))
                        batches.Add(batch);

                    if (ModStaticMethod.ThisMod.debugToggle)
                    {
                        Log.Warning("render switch : " + level.renderSwitch.ToStringSafe());
                        Log.Warning("render layer : " + level.renderLayer.ToStringSafe());
                    }

                    string type_defName = def.originalDefClass.ToStringSafe() + "_" + def.originalDef;
                    if (ThisModData.TexLevelsDatabase.ContainsKey(type_defName) && ThisModData.TexLevelsDatabase[type_defName].ContainsKey(level.textureLevelsName))
                    {
                        TextureLevels textureLevels = ThisModData.TexLevelsDatabase[type_defName][level.textureLevelsName].Clone();
                        textureLevels.keyName = keyName;
                        textureLevels.cachedBatch = batch;
                        textureLevels.cachedApparel = apparel;
                        /*if (textureLevels.patternSets != null)
                            textureLevels.patternSets.typeOriginalDefNameKeyName = textureLevels.originalDefClass.ToStringSafe() + "_" + textureLevels.originalDef + "_" + keyName;*/
                        if (!data.ContainsKey(textureLevels.textureLevelsName))
                            data[textureLevels.textureLevelsName] = textureLevels;
                    }
                }
                epoch.batches = batches;
            }
            catch (Exception ex)
            {
                throw new Exception("一个MultiTexDef:" + def.defName + "出错了, 这很有可能是其levels中的某个textureLevelsName配置不符合规范, 或者是对应的贴图及其路径内包含错误(请尝试检查标点符号以及贴图和路径是否存在等等)", ex);
            }

            return epoch;
        }
        

        public void PreResolveAllLayerBatch()
        {
            if (!ModStaticMethod.AllLevelsLoaded || ThisModData.DefAndKeyDatabase.NullOrEmpty())
                return;
            if (!(parent is Pawn))
                return;
            Pawn pawn = (Pawn)parent;
            string race = pawn.def.defName;
            if (!ThisModData.RacePlansDatabase.ContainsKey(race))
                return;
            RenderPlanDef def = ThisModData.RacePlansDatabase[race];
            string plan = def.defName;
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;//AddComp(ref comp, ref pawn);
            Dictionary<string, Dictionary<string, TextureLevels>> cachedGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();
            Dictionary<string, MultiTexEpoch> data = new Dictionary<string, MultiTexEpoch>();
            if (ThisModData.DefAndKeyDatabase.ContainsKey(plan))
            {
                //头部
                HeadTypeDef head = pawn.story.headType;
                string headName = head != null ? head.defName : "";
                string fullOriginalDefName = typeof(HeadTypeDef).ToStringSafe() + "_" + headName;
                if (head != null && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                {
                    MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                    if (comp.storedDataBody.NullOrEmpty() || !comp.storedDataBody.ContainsKey(fullOriginalDefName))
                    {
                        Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                        data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                        cachedGraphicData[fullOriginalDefName] = cachedData;
                    }
                    else
                    {
                        data[fullOriginalDefName] = comp.storedDataBody[fullOriginalDefName];
                        cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(data[fullOriginalDefName]);
                    }
                }
                //身体
                BodyTypeDef body = pawn.story.bodyType;
                string bodyName = body != null ? body.defName : "";
                fullOriginalDefName = typeof(BodyTypeDef).ToStringSafe() + "_" + bodyName;
                if (body != null && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                {
                    MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                    if (comp.storedDataBody.NullOrEmpty() || !comp.storedDataBody.ContainsKey(fullOriginalDefName))
                    {
                        Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                        data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                        cachedGraphicData[fullOriginalDefName] = cachedData;
                    }
                    else
                    {
                        data[fullOriginalDefName] = comp.storedDataBody[fullOriginalDefName];
                        cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(data[fullOriginalDefName]);
                    }
                }
                //手部
                string hand = comp.GetCurHandDefName;
                fullOriginalDefName = typeof(HandTypeDef).ToStringSafe() + "_" + hand;
                if (hand != "" && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                {
                    MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                    if (comp.storedDataBody.NullOrEmpty() || !comp.storedDataBody.ContainsKey(fullOriginalDefName))
                    {
                        Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                        data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                        cachedGraphicData[fullOriginalDefName] = cachedData;
                    }
                    else
                    {
                        data[fullOriginalDefName] = comp.storedDataBody[fullOriginalDefName];
                        cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(data[fullOriginalDefName]);
                    }
                }
                comp.cachedBodyGraphicData = cachedGraphicData;
                comp.storedDataBody = data;
                cachedGraphicData.Clear();
                data.Clear();
                //头发
                HairDef hair = pawn.story.hairDef;
                string keyName = hair != null ? hair.defName : "";
                fullOriginalDefName = typeof(HairDef).ToStringSafe() + "_" + keyName;
                if (hair != null && ThisModData.DefAndKeyDatabase.ContainsKey(plan) && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                {

                    MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                    if (comp.storedDataHair.NullOrEmpty() || !comp.storedDataHair.ContainsKey(fullOriginalDefName))
                    {
                        Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                        data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                        cachedGraphicData[fullOriginalDefName] = cachedData;
                    }
                    else
                    {
                        data[fullOriginalDefName] = comp.storedDataHair[fullOriginalDefName];

                        cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(comp.storedDataHair[fullOriginalDefName]);
                    }
                }
                comp.cachedHairGraphicData = cachedGraphicData;
                comp.storedDataHair = data;
                cachedGraphicData.Clear();
                data.Clear();
                //衣服
                using (List<Apparel>.Enumerator enumerator = pawn.apparel.WornApparel.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        string appKeyName = enumerator.Current.def.defName;
                        string appFullOriginalDefName = typeof(ThingDef).ToStringSafe() + "_" + appKeyName;
                        if (ThisModData.DefAndKeyDatabase.ContainsKey(plan) && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                        {
                            MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][appFullOriginalDefName];
                            if (comp.storedDataApparel.NullOrEmpty() || !comp.storedDataApparel.ContainsKey(appFullOriginalDefName))
                            {
                                Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                                data[appFullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                                cachedGraphicData[appFullOriginalDefName] = cachedData;
                            }
                            else
                            {
                                data[appFullOriginalDefName] = comp.storedDataApparel[appFullOriginalDefName];
                                cachedGraphicData[appFullOriginalDefName] = GetLevelsDictFromEpoch(data[appFullOriginalDefName]);
                            }
                        }
                    }

                }
                comp.cachedApparelGraphicData = cachedGraphicData;
                comp.storedDataApparel = data;
                cachedGraphicData.Clear();
                data.Clear();
                comp.ResolveAllLayerBatch();
                comp.PrefixResolved = true;
                comp.pawnName = pawn.Name.ToStringFull;
            }
        }


        /// <summary>
        /// 将身体每个部分的图层转换为原版的Node，1.5专用
        /// </summary>
        /// <returns></returns>
        public override List<PawnRenderNode> CompRenderNodes()
        {
            PreResolveAllLayerBatch();





            return base.CompRenderNodes();
        }

        





        public override void PostExposeData()
        {
            base.PostExposeData();
            //Log.Warning("Comp Loading");
            //GetAllBatch.Where(x => x.layer == TextureRenderLayer.Apparel).SelectMany(x => x.keyList).Distinct().ToList().Sort((x, y) => ThisModData.TexLevelsDatabase[x].DrawOffsetForRot(Rot4.South).y.CompareTo(ThisModData.TexLevelsDatabase[y].DrawOffsetForRot(Rot4.South).y));
            Scribe_Values.Look<string>(ref pawnName, "pawnName", null, false);
            Scribe_Values.Look<string>(ref storedHandDefName, "storedHandDefName", null, false);
            Scribe_Collections.Look<string, MultiTexEpoch>(ref storedDataBody, "storedDataBody", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look<string, MultiTexEpoch>(ref storedDataHair, "storedDataHair", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look<string, MultiTexEpoch>(ref storedDataApparel, "storedDataApparel", LookMode.Value, LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (storedDataBody == null)
                    storedDataBody = new Dictionary<string, MultiTexEpoch>();
                if (storedDataHair == null)
                    storedDataHair = new Dictionary<string, MultiTexEpoch>();
                if (storedDataApparel == null)
                    storedDataApparel = new Dictionary<string, MultiTexEpoch>();
                //Log.Warning("Comp Load data Body : " + storedDataBody.Count);
                //Log.Warning("Comp Load data Hair : " + storedDataHair.Count);
                //Log.Warning("Comp Load data Apparel : " + storedDataApparel.Count);
            }
        }
    }
}
