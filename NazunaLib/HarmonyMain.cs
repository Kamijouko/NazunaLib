﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using AlienRace;
using HugsLib;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using Verse.AI.Group;
using System.Reflection;
using Verse.AI;
using RimWorld.Planet;
using UnityEngine.UIElements;
using static HarmonyLib.Code;


namespace NareisLib
{
    [StaticConstructorOnStartup]
    public class HarmonyMain
    {
        static HarmonyMain()
        {
            var harmonyInstance = new Harmony("NareisLib.kamijouko.nazunarei");

            //harmonyInstance.Patch(AccessTools.Method(typeof(ThingWithComps), "PostMake", null, null), null, new HarmonyMethod(typeof(PawnRenderPatchs), "PostMakeAddCompPostfix", null), null, null);
            //harmonyInstance.Patch(AccessTools.Method(typeof(ThingWithComps), "ExposeData", null, null), null, new HarmonyMethod(typeof(PawnRenderPatchs), "ExposeDataAddCompPostfix", null), null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(ThingWithComps), "InitializeComps", null, null), new HarmonyMethod(typeof(PawnRenderPatchs), "InitializeCompsAddCompPrefix", null), null, null, null);
            
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnGraphicSet), "ResolveAllGraphics", null, null), null, null, null, new HarmonyMethod(typeof(PawnRenderPatchs), "ResolveAllGraphicsFinalizer", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnGraphicSet), "ResolveApparelGraphics", null, null), null, new HarmonyMethod(typeof(PawnRenderPatchs), "ResolveHairGraphicsPostfix", null), null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnGraphicSet), "ResolveApparelGraphics", null, null), null, new HarmonyMethod(typeof(PawnRenderPatchs), "ResolveApparelGraphicsPostfix", null), null, null);

            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawPawnBody", null, null), new HarmonyMethod(typeof(PawnRenderPatchs), "DrawPawnBodyPrefix", null), null, null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawPawnBody", null, null), null, null, null, new HarmonyMethod(typeof(PawnRenderPatchs), "DrawPawnBodyFinalizer", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawBodyApparel", null, null), new HarmonyMethod(typeof(PawnRenderPatchs), "DrawBodyApparelPrefix", null), null, null, null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", null, null), null, null, new HarmonyMethod(typeof(PawnRenderPatchs), "RenderPawnInternalHeadPatchTranspiler", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawHeadHair", null, null), null, null, new HarmonyMethod(typeof(PawnRenderPatchs), "DrawHeadHairPatchTranspiler", null), null);
            harmonyInstance.Patch(AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", null, null), null, new HarmonyMethod(typeof(PawnRenderPatchs), "RenderPawnInternalPostfix", null), null, null);

        }
    }



    //初始化后加载资源全靠它！！！（已停用）
    //[HarmonyPatch(typeof(UIRoot_Entry))]
    //[HarmonyPatch("DoMainMenu")]
    public class InitialModPatch
    {
        /*static bool Prefix(UIRoot_Entry __instance)
        {
            if (!ModStaticMethod.AllLevelsLoaded)
            {
                LoadAndResolveAllPlanDefs();
                ModStaticMethod.AllLevelsLoaded = true;
            }
            return true;
        }

        public static void LoadAndResolveAllPlanDefs()
        {
            List<RenderPlanDef> list = DefDatabase<RenderPlanDef>.AllDefsListForReading;
            if (list.NullOrEmpty())
                return;
            foreach (RenderPlanDef plan in list)
            {
                if (plan.plans.NullOrEmpty())
                    continue;
                string planDef = plan.defName;
                Dictionary<string, MultiTexDef> data = new Dictionary<string, MultiTexDef>();
                foreach (MultiTexDef def in plan.plans)
                {
                    if (def.levels.NullOrEmpty() || data.ContainsKey(def.originalDef))
                        continue;
                    foreach (TextureLevels level in def.levels)
                    {
                        level.GetAllGraphicDatas(def.path);
                    }
                    data[def.originalDef] = def;
                }
                ThisModData.DefAndKeyDatabase[planDef] = data;
            }
        }*/
    }



    public class PawnRenderPatchs
    {
        //给所有Pawn添加多层渲染Comp，CompTick有触发条件所以不存在性能问题
        public static bool InitializeCompsAddCompPrefix(ThingWithComps __instance/*, List<ThingComp> ___comps*/)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn == null || pawn.def.comps.Exists(x => x.GetType() == typeof(MultiRenderCompProperties)))
                return true;
            pawn.def.comps.Add(new MultiRenderCompProperties());
            /*ThingComp comp = null;
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                //Log.Warning("Add new Comp");
                comp = (ThingComp)Activator.CreateInstance(typeof(MultiRenderComp));
                comp.parent = pawn;
                //Traverse p = Traverse.Create(pawn);
                //List<ThingComp> list = (List<ThingComp>)p.Field("comps").GetValue();
                ___comps.Add(comp);
                //p.Field("comps").SetValue(list);
                comp.Initialize(new MultiRenderCompProperties());
            }*/
            return true;
        }

        public static void PostMakeAddCompPostfix(ThingWithComps __instance, List<ThingComp> ___comps)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn == null)
                return;
            ThingComp comp = null;
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                comp = (ThingComp)Activator.CreateInstance(typeof(MultiRenderComp));
                comp.parent = pawn;
                ___comps.Add(comp);
                comp.Initialize(pawn.def.comps.Exists(x => x.GetType() == typeof(MultiRenderCompProperties))
                                ? pawn.def.comps.First(x => x.GetType() == typeof(MultiRenderCompProperties))
                                : new MultiRenderCompProperties());
            }
            if (comp != null)
                comp.PostPostMake();
        }


        //处理defName所指定的MultiTexDef，
        //对其属性levels里所存储的所有TextureLevels都根据指定的权重随机一个贴图的名称，
        //并将名称记录进一个从其属性cacheOfLevels得来的MultiTexEpoch中所对应渲染图层的MultiTexBatch的名称列表里，
        //最终返回这个MultiTexEpoch
        public static MultiTexEpoch ResolveMultiTexDef(MultiTexDef def, out Dictionary<string, TextureLevels> data)
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

                    if (!batches.Exists(x => x.textureLevelsName == level.textureLevelsName))
                        batches.Add(new MultiTexBatch(def.originalDefClass, def.originalDef, def.defName, keyName, level.textureLevelsName, level.renderLayer, level.renderSwitch));

                    Log.Warning("render switch : " + level.renderSwitch.ToStringSafe());
                    Log.Warning("render layer : " + level.renderLayer.ToStringSafe());

                    string type_defName = def.originalDefClass.ToStringSafe() + "_" + def.originalDef;
                    if (ThisModData.TexLevelsDatabase.ContainsKey(type_defName) && ThisModData.TexLevelsDatabase[type_defName].ContainsKey(level.textureLevelsName))
                    {
                        TextureLevels textureLevels = ThisModData.TexLevelsDatabase[type_defName][level.textureLevelsName].Clone();
                        textureLevels.keyName = keyName;
                        if (textureLevels.patternSets != null)
                            textureLevels.patternSets.typeOriginalDefNameKeyName = textureLevels.originalDefClass.ToStringSafe() + "_" + textureLevels.originalDef + "_" + keyName;
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


        //从comp的storedData里获取TextureLevels数据，用于处理读取存档时从已有的storedData字典中得到的epoch
        public static Dictionary<string, TextureLevels> GetLevelsDictFromEpoch(MultiTexEpoch epoch)
        {
            return !epoch.batches.NullOrEmpty() ? epoch.batches.ToDictionary(k => k.textureLevelsName, v => ResolveKeyNameForLevel(ThisModData.TexLevelsDatabase[v.originalDefClass.ToStringSafe() + "_" + v.originalDefName][v.textureLevelsName].Clone(), v.keyName)) : new Dictionary<string, TextureLevels>();
        }
        //上方法的子方法，为获取到的TextureLevels进行赋值操作
        public static TextureLevels ResolveKeyNameForLevel(TextureLevels level, string key)
        {
            level.keyName = key;
            if (level.patternSets != null)
                level.patternSets.typeOriginalDefNameKeyName = level.originalDefClass.ToStringSafe() + "_" + level.originalDef + "_" + key;
            return level;
        }


        //预处理Pawn身体的多层渲染数据，只会在pawn出现或生成时执行一次，在整体预处理方法中最后执行（因为原方法的顺序）
        static void ResolveAllGraphicsFinalizer(PawnGraphicSet __instance)
        {
            if (!ModStaticMethod.AllLevelsLoaded || ThisModData.DefAndKeyDatabase.NullOrEmpty())
                return;
            Pawn pawn = __instance.pawn;
            string race = pawn.def.defName;
            if (!ThisModData.RacePlansDatabase.ContainsKey(race))
                return;
            RenderPlanDef def = ThisModData.RacePlansDatabase[race];
            string plan = def.defName;

            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;//AddComp(ref comp, ref pawn);
            //comp.cachedRenderPlanDefName = plan;

            List<string> cachedOverride = new List<string>();
            Dictionary<string, Dictionary<string, TextureLevels>> cachedGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();
            Dictionary<string, MultiTexEpoch> data = new Dictionary<string, MultiTexEpoch>();
            HeadTypeDef head = pawn.story.headType;
            string headName = head != null ? head.defName : "";
            string fullOriginalDefName = typeof(HeadTypeDef).ToStringSafe() + "_" + headName;
            if (ThisModData.DefAndKeyDatabase.ContainsKey(plan))
            {
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
                    if (!multidef.renderOriginTex)
                        cachedOverride.Add("Head");
                }
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
                    if (!multidef.renderOriginTex)
                        cachedOverride.Add("Body");
                }
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
            }
            comp.cachedOverrideBody = cachedOverride;
            comp.cachedBodyGraphicData = cachedGraphicData;
            comp.storedDataBody = data;
            comp.ResolveAllLayerBatch();
            comp.PrefixResolved = true;
            comp.pawnName = pawn.Name.ToStringFull;
        }

        //预处理Pawn的头发，在发型变换时会执行
        static void ResolveHairGraphicsPostfix(PawnGraphicSet __instance)
        {
            if (!ModStaticMethod.AllLevelsLoaded || ThisModData.DefAndKeyDatabase.NullOrEmpty())
                return;
            Pawn pawn = __instance.pawn;
            string race = pawn.def.defName;
            if (!ThisModData.RacePlansDatabase.ContainsKey(race))
                return;
            RenderPlanDef def = ThisModData.RacePlansDatabase[race];
            string plan = def.defName;
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;//AddComp(ref comp, ref pawn);

            List<string> cachedOverride = new List<string>();
            Dictionary<string, Dictionary<string, TextureLevels>> cachedGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();
            Dictionary<string, MultiTexEpoch> data = new Dictionary<string, MultiTexEpoch>();
            HairDef hair = pawn.story.hairDef;
            string keyName = hair != null ? hair.defName : "";
            string fullOriginalDefName = typeof(HairDef).ToStringSafe() + "_" + keyName;
            if (hair != null && ThisModData.DefAndKeyDatabase.ContainsKey(plan) && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
            {
                
                MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                if (comp.storedDataHair.NullOrEmpty() || !comp.storedDataHair.ContainsKey(fullOriginalDefName))
                {
                    Log.Warning("Hair Respawning Hair Respawning Hair Respawning");
                    Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                    data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                    cachedGraphicData[fullOriginalDefName] = cachedData;
                }
                else
                {
                    //Log.Warning("Hair Loading Hair Loading Hair Loading");
                    //Log.Warning("TexLevelsDatabase : " + ThisModData.TexLevelsDatabase.Count);
                    data[fullOriginalDefName] = comp.storedDataHair[fullOriginalDefName];
                    //Log.Warning("first key : " + batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName);
                    //Log.Warning("TexLevelsDatabase has first key : " + ThisModData.TexLevelsDatabase.ContainsKey(batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName).ToStringSafe());
                    //Log.Warning("second key : " + batch.textureLevelsName);
                    //Log.Warning("TexLevelsDatabase has second key : " + ThisModData.TexLevelsDatabase[batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName].ContainsKey(batch.textureLevelsName).ToStringSafe());
                    
                    cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(comp.storedDataHair[fullOriginalDefName]);
                }
                if (!multidef.renderOriginTex)
                    cachedOverride.Add("Hair");
            }
            comp.cachedOverrideHair = cachedOverride;
            comp.cachedHairGraphicData = cachedGraphicData;
            comp.storedDataHair = data;
            if (comp.PrefixResolved)
                comp.ResolveAllLayerBatch();
        }

        //预处理Pawn穿着的多层渲染服装，在服装变换时会执行
        static void ResolveApparelGraphicsPostfix(PawnGraphicSet __instance)
        {
            if (!ModStaticMethod.AllLevelsLoaded || ThisModData.DefAndKeyDatabase.NullOrEmpty())
                return;
            Pawn pawn = __instance.pawn;
            string race = pawn.def.defName;
            if (!ThisModData.RacePlansDatabase.ContainsKey(race))
                return;
            RenderPlanDef def = ThisModData.RacePlansDatabase[race];
            string plan = def.defName;
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;//AddComp(ref comp, ref pawn);

            List<string> cachedOverride = new List<string>();
            Dictionary<string, Dictionary<string, TextureLevels>> cachedGraphicData = new Dictionary<string, Dictionary<string, TextureLevels>>();
            Dictionary<string, MultiTexEpoch> data = new Dictionary<string, MultiTexEpoch>();
            using (List<Apparel>.Enumerator enumerator = __instance.pawn.apparel.WornApparel.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    string keyName = enumerator.Current.def.defName;
                    string fullOriginalDefName = typeof(ThingDef).ToStringSafe() + "_" + keyName;
                    if (ThisModData.DefAndKeyDatabase.ContainsKey(plan) && ThisModData.DefAndKeyDatabase[plan].ContainsKey(fullOriginalDefName))
                    {
                        MultiTexDef multidef = ThisModData.DefAndKeyDatabase[plan][fullOriginalDefName];
                        if (comp.storedDataApparel.NullOrEmpty() || !comp.storedDataApparel.ContainsKey(fullOriginalDefName))
                        {
                            Dictionary<string, TextureLevels> cachedData = new Dictionary<string, TextureLevels>();
                            data[fullOriginalDefName] = ResolveMultiTexDef(multidef, out cachedData);
                            cachedGraphicData[fullOriginalDefName] = cachedData;
                        }
                        else
                        {
                            data[fullOriginalDefName] = comp.storedDataApparel[fullOriginalDefName];
                            cachedGraphicData[fullOriginalDefName] = GetLevelsDictFromEpoch(data[fullOriginalDefName]);
                        }
                        if (!multidef.renderOriginTex)
                            cachedOverride.Add(keyName);
                    }
                }
            }
            comp.cachedOverrideApparel = cachedOverride;
            comp.cachedApparelGraphicData = cachedGraphicData;
            comp.storedDataApparel = data;
            if (comp.PrefixResolved)
                comp.ResolveAllLayerBatch();
        }


        //BottomOverlay BottomHair BodyPrefix
        static bool DrawPawnBodyPrefix(PawnRenderer __instance, Pawn ___pawn, ref bool __state, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            //Log.Warning(___pawn.Name + " flags: DrawNow = " + flags.FlagSet(PawnRenderFlags.DrawNow).ToStringSafe());
            __state = false;
            MultiRenderComp comp = ___pawn.GetComp<MultiRenderComp>();
            AlienPartGenerator.AlienComp alienComp = ___pawn.GetComp<AlienPartGenerator.AlienComp>();
            if (comp == null)
                return __state = true;
            if (!comp.PrefixResolved)
                __instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp.GetDataOfDirection(facing);
            if (curDirection.NullOrEmpty())
                return __state = true;

            //Log.Warning("curDirection curDirection curDirection : " + curDirection.Count);
            //Log.Warning("curDirection curDirection curDirection : " + curDirection.ContainsKey((int)TextureRenderLayer.BottomHair).ToStringSafe());
            //Log.Warning("curDirection curDirection curDirection : " + curDirection[(int)TextureRenderLayer.BottomHair].Count);
            //Log.Warning("curDirection curDirection curDirection : " + curDirection[(int)TextureRenderLayer.BottomHair].First().textureLevelsName);

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 vector = rootLoc;
            vector.y += 0.004687258f;/*原身体为0.008687258f，反映精度为0.0003f*/
            Mesh bodyMesh = null;
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (___pawn.RaceProps.Humanlike)
            {
                bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(___pawn).MeshAt(facing);
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(___pawn).MeshAt(facing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(___pawn).MeshAt(facing);/*__instance.graphics.HairMeshSet.MeshAt(facing);*/
            }
            else
                bodyMesh = __instance.graphics.nakedGraphic.MeshAt(facing);

            List<int> renderLayers = new List<int>() { (int)TextureRenderLayer.BottomOverlay, (int)TextureRenderLayer.BottomHair }; 
            
            foreach (int level in renderLayers)
            {
                if (curDirection.ContainsKey(level))
                {
                    Color colorTwo = alienComp != null ? alienComp.GetChannel("hair").second : Color.white;
                    foreach (MultiTexBatch batch in curDirection[level])
                    {
                        string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                        string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                        //Log.Warning("MultiTexBatch MultiTexBatch MultiTexBatch : " + typeOriginalDefName);

                        //Log.Warning("GraphicData GraphicData GraphicData : " + comp.GetAllOriginalDefForGraphicDataDict.Count);
                        //Log.Warning("GraphicData GraphicData GraphicData : " + comp.GetAllOriginalDefForGraphicDataDict.First().Key);
                        //Log.Warning("GraphicData GraphicData GraphicData : " + comp.GetAllOriginalDefForGraphicDataDict.First().Value.First().Key);

                        if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                            || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                            continue;

                        TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                        Color colorOne = ___pawn.story.HairColor;
                        Mesh mesh = null;
                        Vector3 offset = Vector3.zero;
                        if (data.meshSize != Vector2.zero)
                        {
                            mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                            switch (data.meshType)
                            {
                                case "Head": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                                case "Hair": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                            }
                        }
                        else
                        {
                            if (___pawn.RaceProps.Humanlike)
                            {
                                switch (data.meshType)
                                {
                                    case "Body": mesh = bodyMesh; break;
                                    case "Head":
                                        mesh = headMesh;
                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                        break;
                                    case "Hair":
                                        mesh = hairMesh;
                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                        break;
                                }
                            }
                            else
                                mesh = bodyMesh;
                        }
                        int pattern = 0;
                        if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                            pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                        string condition = "";
                        if (data.hasRotting && bodyDrawType == RotDrawMode.Rotting)
                            condition = "Rotting";
                        if (data.hasDessicated && bodyDrawType == RotDrawMode.Dessicated)
                            condition = "Dessicated";
                        Vector3 dataOffset = data.DrawOffsetForRot(facing);
                        dataOffset.y *= 0.0001f;
                        Vector3 pos = vector + offset/* + dataOffset*/;
                        Material mat = data.GetGraphic(batch.keyName, colorOne, colorTwo, pattern, condition).MatAt(facing, null);
                        GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }
                }
            }

            if (!curDirection.ContainsKey((int)TextureRenderLayer.Body) && !curDirection.ContainsKey((int)TextureRenderLayer.Apparel))
                __state = true;

            return __state;
        }


        //下面补丁的子方法
        public static Material OverrideMaterialIfNeeded(PawnRenderer instance, Material original, Pawn pawn, bool portrait = false)
        {
            Material baseMat = (!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original;
            return instance.graphics.flasher.GetDamagedMat(baseMat);
        }

        public static Material OverrideMaterialIfNeeded(Material original, Pawn pawn, PawnRenderer render, bool portrait = false)
        {
            Material baseMat = (!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original;
            return render.graphics.flasher.GetDamagedMat(baseMat);
        }


        //Body HandOne Hand HandTwo Apparel(除了shell层) BottomShell DrawPawnBodyFinalizer
        static void DrawPawnBodyFinalizer(PawnRenderer __instance, Pawn ___pawn, bool __state, Vector3 rootLoc, float angle, Rot4 facing, RotDrawMode bodyDrawType, PawnRenderFlags flags, out Mesh bodyMesh)
        {
            bodyMesh = null;
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (___pawn.RaceProps.Humanlike)
            {
                bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(___pawn).MeshAt(facing);
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(___pawn).MeshAt(facing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(___pawn).MeshAt(facing);
            }
            else
                bodyMesh = __instance.graphics.nakedGraphic.MeshAt(facing);

            MultiRenderComp comp = ___pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;
            if (!comp.PrefixResolved)
                __instance.graphics.ResolveAllGraphics();

            ThingDef_AlienRace thingDef_AlienRace = ___pawn.def as ThingDef_AlienRace;
            AlienPartGenerator alienPartGenerator = null;
            if (thingDef_AlienRace != null)
                alienPartGenerator = thingDef_AlienRace.alienRace.generalSettings.alienPartGenerator;

            Dictionary<int, List<MultiTexBatch>> curDirection = comp.GetDataOfDirection(facing);
            if (curDirection.NullOrEmpty())
                return;

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 vector = rootLoc;
            vector.y += 0.008687258f;
            Vector3 loc = vector;
            loc.y += 0.0014478763f;
            

            //如果原方法未执行且并不具有多层身体或者不隐藏原身体
            if (!__state
                && (!curDirection.ContainsKey((int)TextureRenderLayer.Body) 
                    || (!comp.GetAllHideOriginalDefData.NullOrEmpty() 
                        && !comp.GetAllHideOriginalDefData.Contains("Body"))))
            {
                Material bodyMat;
                PawnGraphicSet pawnSet = __instance.graphics;
                if (bodyDrawType == RotDrawMode.Fresh)
                {
                    if (___pawn.Dead && pawnSet.corpseGraphic != null)
                        bodyMat = pawnSet.corpseGraphic.MatAt(facing, null);
                    else
                        bodyMat = pawnSet.nakedGraphic.MatAt(facing, null);
                }
                else if (bodyDrawType == RotDrawMode.Rotting || pawnSet.dessicatedGraphic == null)
                    bodyMat = pawnSet.rottingGraphic.MatAt(facing, null);
                else
                    bodyMat = pawnSet.dessicatedGraphic.MatAt(facing, null);
                Material material = (___pawn.RaceProps.IsMechanoid && ___pawn.Faction != null && ___pawn.Faction != Faction.OfMechanoids) 
                    ? __instance.graphics.GetOverlayMat(bodyMat, ___pawn.Faction.MechColor) 
                    : bodyMat;
                Material mat = flags.FlagSet(PawnRenderFlags.Cache) ? material : OverrideMaterialIfNeeded(material, ___pawn, __instance, flags.FlagSet(PawnRenderFlags.Portrait));
                GenDraw.DrawMeshNowOrLater(bodyMesh, vector, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
            }

            List<int> renderLayers;
            if (__state)
                renderLayers = new List<int>() { (int)TextureRenderLayer.HandOne, (int)TextureRenderLayer.Hand, (int)TextureRenderLayer.HandTwo };
            else
                renderLayers = new List<int>() { (int)TextureRenderLayer.Body, (int)TextureRenderLayer.HandOne, (int)TextureRenderLayer.Hand, (int)TextureRenderLayer.HandTwo };
            foreach (int level in renderLayers)
            {
                if (curDirection.ContainsKey(level))
                {
                    Color colorOne = ___pawn.story.SkinColor;
                    Color colorTwo = alienPartGenerator != null ? alienPartGenerator.SkinColor(___pawn, false) : Color.white;
                    foreach (MultiTexBatch batch in curDirection[level])
                    {
                        string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                        string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                        if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() 
                            || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                            continue;

                        TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                        Mesh mesh = null;
                        Vector3 offset = Vector3.zero;
                        if (data.meshSize != Vector2.zero)
                        {
                            mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                            switch (data.meshType)
                            {
                                case "Head": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                                case "Hair": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                            }
                        }
                        else
                        {
                            if (___pawn.RaceProps.Humanlike)
                            {
                                switch (data.meshType)
                                {
                                    case "Body": mesh = bodyMesh; break;
                                    case "Head":
                                        mesh = headMesh;
                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                        break;
                                    case "Hair":
                                        mesh = hairMesh;
                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                        break;
                                }
                            }
                            else
                                mesh = bodyMesh;
                        }
                        int pattern = 0;
                        if (data.isSleeve)
                        {
                            string handPrefix = typeof(HandTypeDef).ToStringSafe() + "_" + comp.GetCurHandDefName + "_";
                            string handKeyName = data.sleeveTexList.FirstOrDefault(x => comp.cachedRandomGraphicPattern.Keys.Contains(handPrefix + x));
                            if (handKeyName != null)
                                pattern = comp.cachedRandomGraphicPattern[handPrefix + handKeyName];
                            else
                                continue;
                        }  
                        else if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                            pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                        string condition = "";
                        if (data.hasRotting && bodyDrawType == RotDrawMode.Rotting)
                            condition = "Rotting";
                        if (data.hasDessicated && bodyDrawType == RotDrawMode.Dessicated)
                            condition = "Dessicated";
                        Vector3 handOffset = Vector3.zero;
                        if (data.handDrawBehindShell && !data.isSleeve)
                        {
                            if (level == (int)TextureRenderLayer.Hand || level == (int)TextureRenderLayer.HandTwo)
                                handOffset.y = (__instance.graphics.apparelGraphics.Count + 1) * 0.0028957527f;
                        }
                        else if (data.sleeveDrawBehindShell && data.isSleeve)
                        {
                            handOffset.y = (__instance.graphics.apparelGraphics.Count + 2) * 0.0028957527f;
                        }
                        else
                        {
                            if (facing != Rot4.North)
                                handOffset.y = 0.014478763f;
                            else
                                handOffset.y = 0.011583012f;
                        }
                        Vector3 dataOffset = data.DrawOffsetForRot(facing);
                        dataOffset.y *= 0.0001f;
                        Vector3 pos = vector + offset + handOffset/* + dataOffset*/;
                        Material material = data.GetGraphic(batch.keyName, colorOne, colorTwo, pattern, condition).MatAt(facing, null);
                        Material mat = (___pawn.RaceProps.IsMechanoid 
                            && ___pawn.Faction != null 
                            && ___pawn.Faction != Faction.OfMechanoids) ? __instance.graphics.GetOverlayMat(material, ___pawn.Faction.MechColor) : material;
                        GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }
                }
            }
            vector.y += 0.0014478763f;
            //vector.y += 0.0028957527f;

            if (!__state && flags.FlagSet(PawnRenderFlags.Clothes) && curDirection.ContainsKey((int)TextureRenderLayer.Apparel))
            {
                for (int i = 0; i < __instance.graphics.apparelGraphics.Count; i++)
                {
                    ApparelGraphicRecord apparel = __instance.graphics.apparelGraphics[i];
                    if ((apparel.sourceApparel.def.apparel.shellRenderedBehindHead || apparel.sourceApparel.def.apparel.LastLayer != ApparelLayerDefOf.Shell)
                        && !PawnRenderer.RenderAsPack(apparel.sourceApparel)
                        && apparel.sourceApparel.def.apparel.LastLayer != ApparelLayerDefOf.Overhead
                        && apparel.sourceApparel.def.apparel.LastLayer != ApparelLayerDefOf.EyeCover)
                    {
                        //如果当前服装的def没在需要被隐藏的列表里的话
                        if (!comp.GetAllHideOriginalDefData.NullOrEmpty() && !comp.GetAllHideOriginalDefData.Contains(apparel.sourceApparel.def.defName))
                        {
                            //先画此服装
                            Material apparelMAt = (___pawn.RaceProps.IsMechanoid && ___pawn.Faction != null && ___pawn.Faction != Faction.OfMechanoids)
                                ? __instance.graphics.GetOverlayMat(apparel.graphic.MatAt(facing, null), ___pawn.Faction.MechColor)
                                : apparel.graphic.MatAt(facing, null);
                            Material material = flags.FlagSet(PawnRenderFlags.Cache)
                                ? apparelMAt
                                : OverrideMaterialIfNeeded(__instance, apparelMAt, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait));

                            GenDraw.DrawMeshNowOrLater(bodyMesh, vector, quat, material, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }

                        //如果是需要多层的服装的话
                        string apparelTypeOriginalDefName = apparel.sourceApparel.def.GetType().ToStringSafe() + "_" + apparel.sourceApparel.def.defName;
                        if (!comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() && comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(apparelTypeOriginalDefName))
                        {
                            Color apparelColor = apparel.sourceApparel.DrawColor;
                            List<int> layers = new List<int>() { (int)TextureRenderLayer.Apparel };
                            if (curDirection.ContainsKey((int)TextureRenderLayer.BottomShell))
                                layers = new List<int>() { (int)TextureRenderLayer.BottomShell, (int)TextureRenderLayer.Apparel };
                            foreach (int layer in layers)
                            {
                                Vector3 local = vector;
                                if (layer == (int)TextureRenderLayer.BottomShell)
                                    local.y = 0.006687258f;
                                foreach (MultiTexBatch batch in curDirection[layer])
                                {
                                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                                    if (typeOriginalDefName == apparelTypeOriginalDefName
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                                    {
                                        TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                                        Mesh mesh = null;
                                        Vector3 offset = Vector3.zero;
                                        if (data.meshSize != Vector2.zero)
                                        {
                                            mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                                            switch (data.meshType)
                                            {
                                                case "Head": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                                                case "Hair": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                                            }
                                        }
                                        else
                                        {
                                            if (___pawn.RaceProps.Humanlike)
                                            {
                                                switch (data.meshType)
                                                {
                                                    case "Body": mesh = bodyMesh; break;
                                                    case "Head":
                                                        mesh = headMesh;
                                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                                        break;
                                                    case "Hair":
                                                        mesh = hairMesh;
                                                        offset = quat * __instance.BaseHeadOffsetAt(facing);
                                                        break;
                                                }
                                            }
                                            else
                                                mesh = bodyMesh;
                                        }
                                        int pattern = 0;
                                        if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                                            pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                                        string condition = "";
                                        if (data.hasRotting && bodyDrawType == RotDrawMode.Rotting)
                                            condition = "Rotting";
                                        if (data.hasDessicated && bodyDrawType == RotDrawMode.Dessicated)
                                            condition = "Dessicated";
                                        Vector3 dataOffset = data.DrawOffsetForRot(facing);
                                        dataOffset.y *= 0.0001f;
                                        Vector3 pos = local + offset/* + dataOffset*/;
                                        Material material = data.GetGraphic(batch.keyName, apparelColor, Color.white, pattern, condition).MatAt(facing, null);
                                        Material mat = (___pawn.RaceProps.IsMechanoid && ___pawn.Faction != null && ___pawn.Faction != Faction.OfMechanoids)
                                            ? __instance.graphics.GetOverlayMat(material, ___pawn.Faction.MechColor)
                                            : material;
                                        Material apparelMat = flags.FlagSet(PawnRenderFlags.Cache)
                                            ? mat
                                            : OverrideMaterialIfNeeded(__instance, mat, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                                        GenDraw.DrawMeshNowOrLater(mesh, pos, quat, apparelMat, flags.FlagSet(PawnRenderFlags.DrawNow));
                                    }
                                }
                            }
                        }
                        vector.y += 0.0014478763f;
                        //vector.y += 0.0028957527f;
                    }
                }
            }
            if (ModsConfig.IdeologyActive && __instance.graphics.bodyTattooGraphic != null && bodyDrawType != RotDrawMode.Dessicated && (facing != Rot4.North || ___pawn.style.BodyTattoo.visibleNorth))
            {
                GenDraw.DrawMeshNowOrLater(__instance.GetBodyOverlayMeshSet().MeshAt(facing), loc, quat, __instance.graphics.bodyTattooGraphic.MatAt(facing, null), flags.FlagSet(PawnRenderFlags.DrawNow));
            }
        }


        //Apparel(Shell层) BottomShell DrawBodyApparelPrefix 
        static bool DrawBodyApparelPrefix(PawnRenderer __instance, Pawn ___pawn, Vector3 shellLoc, Vector3 utilityLoc, Mesh bodyMesh, float angle, Rot4 bodyFacing, PawnRenderFlags flags)
        {
            bool patchResult = true;
            
            MultiRenderComp comp = ___pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return true;
            if (!comp.PrefixResolved)
                __instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp.GetDataOfDirection(bodyFacing);
            if (curDirection.NullOrEmpty())
                return true;

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (___pawn.RaceProps.Humanlike)
            {
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(___pawn).MeshAt(bodyFacing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(___pawn).MeshAt(bodyFacing);
            }


            List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;

            bool hasMultiTexApparel = !comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() 
                && apparelGraphics.Exists(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell 
                    && !x.sourceApparel.def.apparel.shellRenderedBehindHead 
                    && comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(x.sourceApparel.def.GetType().ToStringSafe() + "_" + x.sourceApparel.def.defName));

            if (curDirection.ContainsKey((int)TextureRenderLayer.Apparel) && hasMultiTexApparel)
            {
                patchResult = false;

                for (int i = 0; i < apparelGraphics.Count; i++)
                {
                    ApparelGraphicRecord apparel = apparelGraphics[i];
                    //如果是shell服装的话
                    if (apparel.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell && !apparel.sourceApparel.def.apparel.shellRenderedBehindHead)
                    {
                        //如果当前服装的def没在需要被隐藏的列表里的话
                        if (!comp.GetAllHideOriginalDefData.NullOrEmpty() && !comp.GetAllHideOriginalDefData.Contains(apparel.sourceApparel.def.defName))
                        {
                            //先画此服装
                            Material apparelMat = apparel.graphic.MatAt(bodyFacing, null);
                            Material material = flags.FlagSet(PawnRenderFlags.Cache)
                                ? apparelMat
                                : OverrideMaterialIfNeeded(__instance, apparelMat, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                            Vector3 loc = shellLoc;
                            if (apparel.sourceApparel.def.apparel.shellCoversHead)
                                loc.y += 0.0014478763f;
                                //loc.y += 0.0028957527f;
                            GenDraw.DrawMeshNowOrLater(bodyMesh, loc, quat, material, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }

                        //如果是多层服装的话
                        string apparelTypeOriginalDefName = apparel.sourceApparel.def.GetType().ToStringSafe() + "_" + apparel.sourceApparel.def.defName;
                        if (!comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() 
                            && comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(apparelTypeOriginalDefName) 
                            && curDirection.ContainsKey((int)TextureRenderLayer.Apparel))
                        {
                            Vector3 loc = shellLoc;
                            if (apparel.sourceApparel.def.apparel.shellCoversHead)
                                loc.y += 0.0014478763f;
                                //loc.y += 0.0028957527f;
                            Color apparelColor = apparel.sourceApparel.DrawColor;

                            List<int> layers = new List<int>() { (int)TextureRenderLayer.Apparel };
                            if (curDirection.ContainsKey((int)TextureRenderLayer.BottomShell))
                                layers = new List<int>() { (int)TextureRenderLayer.BottomShell, (int)TextureRenderLayer.Apparel };
                            
                            foreach (int layer in layers)
                            {
                                Vector3 local = loc;
                                if (layer == (int)TextureRenderLayer.BottomShell)
                                    local.y = 0.005687258f;
                                foreach (MultiTexBatch batch in curDirection[layer])
                                {
                                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                                    if (typeOriginalDefName == apparelTypeOriginalDefName
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                                    {
                                        TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                                        Mesh mesh = null;

                                        Vector3 offset = Vector3.zero;
                                        if (data.meshSize != Vector2.zero)
                                        {
                                            mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(bodyFacing);
                                            switch (data.meshType)
                                            {
                                                case "Head": offset = quat * __instance.BaseHeadOffsetAt(bodyFacing); break;
                                                case "Hair": offset = quat * __instance.BaseHeadOffsetAt(bodyFacing); break;
                                            }
                                        }
                                        else
                                        {
                                            if (___pawn.RaceProps.Humanlike)
                                            {
                                                switch (data.meshType)
                                                {
                                                    case "Body": mesh = bodyMesh; break;
                                                    case "Head":
                                                        mesh = headMesh;
                                                        offset = quat * __instance.BaseHeadOffsetAt(bodyFacing);
                                                        break;
                                                    case "Hair":
                                                        mesh = hairMesh;
                                                        offset = quat * __instance.BaseHeadOffsetAt(bodyFacing);
                                                        break;
                                                }
                                            }
                                            else
                                                mesh = bodyMesh;
                                        }
                                        //Log.Warning("has mesh mesh mesh : " + (bodyMesh != null).ToStringSafe());
                                        int pattern = 0;
                                        if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                                            pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                                        Vector3 dataOffset = data.DrawOffsetForRot(bodyFacing);
                                        dataOffset.y *= 0.0001f;
                                        Vector3 pos = local + offset/* + dataOffset*/;
                                        Material material = data.GetGraphic(batch.keyName, apparelColor, Color.white, pattern).MatAt(bodyFacing, null);
                                        Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                                            ? material
                                            : OverrideMaterialIfNeeded(__instance, material, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                                        GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                                    }
                                }
                            }
                        }
                    }

                    //渲染背包/工具层
                    if (PawnRenderer.RenderAsPack(apparel.sourceApparel) && !comp.GetAllHideOriginalDefData.NullOrEmpty() && !comp.GetAllHideOriginalDefData.Contains(apparel.sourceApparel.def.defName))
                    {
                        Material material2 = apparel.graphic.MatAt(bodyFacing, null);
                        material2 = (flags.FlagSet(PawnRenderFlags.Cache) ? material2 : OverrideMaterialIfNeeded(material2, ___pawn, __instance, flags.FlagSet(PawnRenderFlags.Portrait)));
                        if (apparel.sourceApparel.def.apparel.wornGraphicData != null)
                        {
                            Vector2 vector = apparel.sourceApparel.def.apparel.wornGraphicData.BeltOffsetAt(bodyFacing, ___pawn.story.bodyType);
                            Vector2 vector2 = apparel.sourceApparel.def.apparel.wornGraphicData.BeltScaleAt(bodyFacing, ___pawn.story.bodyType);
                            Matrix4x4 matrix = Matrix4x4.Translate(utilityLoc) * Matrix4x4.Rotate(quat) * Matrix4x4.Translate(new Vector3(vector.x, 0f, vector.y)) * Matrix4x4.Scale(new Vector3(vector2.x, 1f, vector2.y));
                            GenDraw.DrawMeshNowOrLater(bodyMesh, matrix, material2, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                        else
                        {
                            GenDraw.DrawMeshNowOrLater(bodyMesh, shellLoc, quat, material2, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                    }
                }
            }

            if (curDirection.ContainsKey((int)TextureRenderLayer.FrontShell))
            {
                Vector3 loc = shellLoc;
                loc.y += 0.0304054035f;

                foreach (MultiTexBatch batch in curDirection[(int)TextureRenderLayer.FrontShell])
                {
                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                    if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                            || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                            || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                        continue;

                    TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                    Apparel apparel = apparelGraphics.FirstOrDefault(x => x.sourceApparel.def.defName == batch.originalDefName).sourceApparel;
                    Color apparelColor = apparel == null ? data.color : apparel.DrawColor;
                    Mesh mesh = null;

                    Vector3 offset = Vector3.zero;
                    if (data.meshSize != Vector2.zero)
                    {
                        mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(bodyFacing);
                        switch (data.meshType)
                        {
                            case "Head": offset = quat * __instance.BaseHeadOffsetAt(bodyFacing); break;
                            case "Hair": offset = quat * __instance.BaseHeadOffsetAt(bodyFacing); break;
                        }
                    }
                    else
                    {
                        if (___pawn.RaceProps.Humanlike)
                        {
                            switch (data.meshType)
                            {
                                case "Body": mesh = bodyMesh; break;
                                case "Head":
                                    mesh = headMesh;
                                    offset = quat * __instance.BaseHeadOffsetAt(bodyFacing);
                                    break;
                                case "Hair":
                                    mesh = hairMesh;
                                    offset = quat * __instance.BaseHeadOffsetAt(bodyFacing);
                                    break;
                            }
                        }
                        else
                            mesh = bodyMesh;
                    }
                    //Log.Warning("has mesh mesh mesh : " + (bodyMesh != null).ToStringSafe());
                    int pattern = 0;
                    if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                        pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                    Vector3 dataOffset = data.DrawOffsetForRot(bodyFacing);
                    dataOffset.y *= 0.0001f;
                    Vector3 pos = loc + offset/* + dataOffset*/;
                    Material material = data.GetGraphic(batch.keyName, apparelColor, Color.white, pattern).MatAt(bodyFacing, null);
                    Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                        ? material
                        : OverrideMaterialIfNeeded(__instance, material, ___pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                    GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                }
            }
            return patchResult;
        }



        //下面转释器方法的子方法，用于覆盖隐形贴图和受伤贴图等，特定用于头部
        public static Material GetHeadOverrideMat(Material mat, PawnRenderer instance, bool portrait = false, bool allowOverride = true)
        {
            Material material = mat;
            if (material != null && allowOverride)
            {
                if (!portrait && instance.graphics.pawn.IsInvisible())
                {
                    material = InvisibilityMatPool.GetInvisibleMat(material);
                }
                material = instance.graphics.flasher.GetDamagedMat(material);
            }
            return material;
        }




        //Head RenderPawnInternalTranspiler
        public static IEnumerable<CodeInstruction> RenderPawnInternalHeadPatchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) }, null);
            FieldInfo pawn = AccessTools.Field(typeof(PawnRenderer), "pawn");
            MethodInfo renderPawnInternalHeadTranPatch = AccessTools.Method(typeof(PawnRenderPatchs), "RenderPawnInternalHeadTranPatch", null, null);
            List<CodeInstruction> instructionList = instructions.ToList<CodeInstruction>();
            int num;
            for (int i = 0; i < instructionList.Count; i = num + 1)
            {
                CodeInstruction instruction = instructionList[i];
                //将其插入到绘制头部贴图的DrawMeshNowOrLater前一行，并跳过原方法
                if (instruction.OperandIs(drawMeshNowOrLater))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_3);//headYOffset

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);//bodyDrawType

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);//facing

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 6);//flags

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//this.

                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);//pawn

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//PawnRenderer instance

                    yield return new CodeInstruction(OpCodes.Call, renderPawnInternalHeadTranPatch);

                    i++;
                }
                yield return instructionList[i];
                num = i;
            }
            yield break;
        }

        public static void RenderPawnInternalHeadTranPatch(Mesh headMesh, Vector3 loc, Quaternion quat, Material headMat, bool drawNow, Vector3 headYOffset, RotDrawMode bodyDrawType, Rot4 facing, PawnRenderFlags flags, Pawn pawn, PawnRenderer instance)
        {
            //Log.Warning("run head patch");
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();

            if (comp != null && !comp.PrefixResolved)
                instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp != null ? comp.GetDataOfDirection(facing) : new Dictionary<int, List<MultiTexBatch>>();

            int layer = (int)TextureRenderLayer.Head;

            //是否绘制原head贴图
            if (comp == null
                || curDirection.NullOrEmpty()
                || !curDirection.ContainsKey(layer) 
                || comp.GetAllHideOriginalDefData.NullOrEmpty() 
                || !comp.GetAllHideOriginalDefData.Contains("Head"))
            {
                GenDraw.DrawMeshNowOrLater(headMesh, loc, quat, headMat, drawNow);
            }

            if (comp == null)
                return;

            ThingDef_AlienRace thingDef_AlienRace = pawn.def as ThingDef_AlienRace;
            AlienPartGenerator alienPartGenerator = null;
            if (thingDef_AlienRace != null)
                alienPartGenerator = thingDef_AlienRace.alienRace.generalSettings.alienPartGenerator;

            //绘制多层贴图
            if (!curDirection.NullOrEmpty() && curDirection.ContainsKey(layer))
            {
                Mesh bodyMesh = null;
                Mesh hairMesh = null;
                if (pawn.RaceProps.Humanlike)
                {
                    bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn).MeshAt(facing);
                    hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(pawn).MeshAt(facing);
                }
                else
                    bodyMesh = instance.graphics.nakedGraphic.MeshAt(facing);
                Color colorOne = pawn.story.SkinColor;
                Color colorTwo = alienPartGenerator != null ? alienPartGenerator.SkinColor(pawn, false) : Color.white;

                foreach (MultiTexBatch batch in curDirection[layer])
                {
                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                    if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                        || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(pawn, batch.keyName))
                        continue;

                    TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                    Mesh mesh = null;
                    Vector3 offset = Vector3.zero;
                    if (data.meshSize != Vector2.zero)
                    {
                        mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                        switch (data.meshType)
                        {
                            case "Head": offset = quat * instance.BaseHeadOffsetAt(facing); break;
                            case "Hair": offset = quat * instance.BaseHeadOffsetAt(facing); break;
                        }
                    }
                    else
                    {
                        if (pawn.RaceProps.Humanlike)
                        {
                            switch (data.meshType)
                            {
                                case "Body": mesh = bodyMesh; break;
                                case "Head":
                                    mesh = headMesh;
                                    offset = quat * instance.BaseHeadOffsetAt(facing);
                                    break;
                                case "Hair":
                                    mesh = hairMesh;
                                    offset = quat * instance.BaseHeadOffsetAt(facing);
                                    break;
                            }
                        }
                        else
                            mesh = bodyMesh;
                    }
                    int pattern = 0;
                    if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                        pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                    string condition = "";
                    if (flags.FlagSet(PawnRenderFlags.HeadStump))
                    {
                        if (data.hasStump)
                            condition = "Stump";
                        else
                            continue;
                    }
                    if (data.hasRotting && bodyDrawType == RotDrawMode.Rotting)
                        condition = "Rotting";
                    if (data.hasDessicated && bodyDrawType == RotDrawMode.Dessicated)
                        condition = "Dessicated";
                    Vector3 dataOffset = data.DrawOffsetForRot(facing);
                    dataOffset.y *= 0.0001f;
                    Vector3 pos = headYOffset + offset/* + dataOffset*/;
                    Material material = data.GetGraphic(batch.keyName, colorOne, colorTwo, pattern, condition).MatAt(facing, null);
                    Material mat = GetHeadOverrideMat(material, instance, flags.FlagSet(PawnRenderFlags.Portrait), !flags.FlagSet(PawnRenderFlags.Cache));
                    GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, drawNow);
                }
            }
        }




        //下面的转释器方法的子方法，用于覆盖隐形贴图和效果贴图等，特定用于头发
        public static Material GetHairOverrideMat(Material mat, PawnRenderer instance, bool portrait = false, bool cached = true)
        {
            Material material = mat;
            if (!portrait && instance.graphics.pawn.IsInvisible())
            {
                material = InvisibilityMatPool.GetInvisibleMat(material);
            }
            if (!cached)
            {
                return instance.graphics.flasher.GetDamagedMat(material);
            }
            return material;
        }



        //FaceMask Hair HeadMask Hat DrawHeadHairTranspiler
        public static IEnumerable<CodeInstruction> DrawHeadHairPatchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) }, null);
            FieldInfo pawn = AccessTools.Field(typeof(PawnRenderer), "pawn");
            MethodInfo drawHeadHairHeadTranPatch = AccessTools.Method(typeof(PawnRenderPatchs), "DrawHeadHairHairTranPatch", null, null);
            MethodInfo drawHeadHairFaceMaskTranPatch = AccessTools.Method(typeof(PawnRenderPatchs), "DrawHeadHairFaceMaskTranPatch", null, null);
            MethodInfo drawHeadHairHeadMaskTranPatch = AccessTools.Method(typeof(PawnRenderPatchs), "DrawHeadHairHeadMaskTranPatch", null, null);
            List<CodeInstruction> instructionList = instructions.ToList<CodeInstruction>();
            int num;
            for (int i = 0; i < instructionList.Count; i = num + 1)
            {
                CodeInstruction instruction = instructionList[i];
                if (i > 10 && instructionList[i - 2].opcode == OpCodes.Ldloc_S && instructionList[i - 2].OperandIs(6) && instructionList[i - 3].OperandIs(drawMeshNowOrLater))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_3);//angle

                    yield return new CodeInstruction(OpCodes.Ldarg_1);//rootLoc vector

                    yield return new CodeInstruction(OpCodes.Ldarg_2);//headOffset

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);//headfacing

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 7);//flags

                    yield return new CodeInstruction(OpCodes.Ldloc_1);//apparelGraphics

                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);//shouldDraw

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//this.

                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);//pawn

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//PawnRenderer instance

                    yield return new CodeInstruction(OpCodes.Call, drawHeadHairFaceMaskTranPatch);

                    i += 34;
                }


                if (instruction.OperandIs(drawMeshNowOrLater) && instructionList[i - 37].opcode == OpCodes.Ldloc_2/* && instructionList[i + 4].OperandIs(6)*/)
                {
                    //Log.Warning("RunPatched");
                    yield return new CodeInstruction(OpCodes.Ldarg_1);//rootLoc vector

                    yield return new CodeInstruction(OpCodes.Ldarg_2);//headOffset

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);//headfacing

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 7);//flags

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//this.

                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);//pawn

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//PawnRenderer instance

                    yield return new CodeInstruction(OpCodes.Call, drawHeadHairHeadTranPatch);

                    i++;
                }

                if (i > 10 && instructionList[i - 2].opcode == OpCodes.Ldloc_S && instructionList[i - 2].OperandIs(6) && instructionList[i - 6].OperandIs(drawMeshNowOrLater))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_3);//angle

                    yield return new CodeInstruction(OpCodes.Ldarg_1);//rootLoc vector

                    yield return new CodeInstruction(OpCodes.Ldarg_2);//headOffset

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 5);//headfacing

                    yield return new CodeInstruction(OpCodes.Ldarg_S, 7);//flags

                    yield return new CodeInstruction(OpCodes.Ldloc_1);//apparelGraphics

                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);//shouldDraw

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//this.

                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);//pawn

                    yield return new CodeInstruction(OpCodes.Ldarg_0);//PawnRenderer instance

                    yield return new CodeInstruction(OpCodes.Call, drawHeadHairHeadMaskTranPatch);

                    i += 52;
                }
                yield return instructionList[i];
                num = i;
            }
            yield break;
        }

        public static void DrawHeadHairFaceMaskTranPatch(float angle, Vector3 vector, Vector3 headOffset, Rot4 facing, PawnRenderFlags flags, List<ApparelGraphicRecord> apparelGraphics, bool shouldDraw, Pawn pawn, PawnRenderer instance)
        {
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            
            if (comp != null && !comp.PrefixResolved)
                instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp != null ? comp.GetDataOfDirection(facing) : new Dictionary<int, List<MultiTexBatch>>();

            Mesh bodyMesh = null;
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (pawn.RaceProps.Humanlike)
            {
                bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn).MeshAt(facing);
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(facing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(pawn).MeshAt(facing);
            }
            else
                bodyMesh = instance.graphics.nakedGraphic.MeshAt(facing);

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 hairYOffset = vector + headOffset;
            hairYOffset.y += 0.028957527f;
            int layer = (int)TextureRenderLayer.FaceMask;

            for (int index = 0; index < apparelGraphics.Count; index++)
            {
                ApparelGraphicRecord apparel = apparelGraphics[index];
                if ((!shouldDraw || apparel.sourceApparel.def.apparel.hatRenderedFrontOfFace) && apparel.sourceApparel.def.apparel.forceRenderUnderHair)
                {
                    Vector3 loc = hairYOffset;
                    if (apparel.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                    {
                        loc = vector + headOffset;
                        if (apparel.sourceApparel.def.apparel.hatRenderedBehindHead)
                            loc.y += 0.02216602f;
                        else
                            loc.y += !(facing == Rot4.North) || apparel.sourceApparel.def.apparel.hatRenderedAboveBody ? 0.03185328f : 0.002895753f;
                    }
                    //是否绘制原装备的贴图
                    if (comp == null
                        || comp.GetAllHideOriginalDefData.NullOrEmpty() 
                        || !comp.GetAllHideOriginalDefData.Contains(apparel.sourceApparel.def.defName))
                    {
                        Material original = apparel.graphic.MatAt(facing, null);
                        Material mat = flags.FlagSet(PawnRenderFlags.Cache) ? original : OverrideMaterialIfNeeded(original, pawn, instance, flags.FlagSet(PawnRenderFlags.Portrait));
                        GenDraw.DrawMeshNowOrLater(hairMesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }

                    if (comp == null)
                        continue;

                    //如果是多层服装的话
                    string apparelTypeOriginalDefName = apparel.sourceApparel.def.GetType().ToStringSafe() + "_" + apparel.sourceApparel.def.defName;
                    if (!curDirection.NullOrEmpty() && curDirection.ContainsKey(layer) 
                        && !comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() 
                        && comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(apparelTypeOriginalDefName))
                    {
                        Color apparelColor = apparel.sourceApparel.DrawColor;
                        foreach (MultiTexBatch batch in curDirection[layer])
                        {
                            string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                            string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                            if (typeOriginalDefName == apparelTypeOriginalDefName 
                                && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                                && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(pawn, batch.keyName))
                            {
                                TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                                Mesh mesh = null;
                                if (data.meshSize != Vector2.zero)
                                {
                                    mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                                    if (data.meshType == "Body")
                                    {
                                        loc.x = vector.x;
                                        loc.z = vector.z;
                                    }
                                }
                                else
                                {
                                    if (pawn.RaceProps.Humanlike)
                                    {
                                        switch (data.meshType)
                                        {
                                            case "Body": 
                                                mesh = bodyMesh;
                                                loc.x = vector.x;
                                                loc.z = vector.z;
                                                break;
                                            case "Head": mesh = headMesh; break;
                                            case "Hair": mesh = hairMesh; break;
                                        }
                                    }
                                    else
                                    {
                                        mesh = bodyMesh;
                                        loc.x = vector.x;
                                        loc.z = vector.z;
                                    }
                                }                                        
                                int pattern = 0;
                                if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                                    pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                                Vector3 dataOffset = data.DrawOffsetForRot(facing);
                                dataOffset.y *= 0.0001f;
                                Vector3 pos = loc/* + dataOffset*/;
                                Material material = data.GetGraphic(batch.keyName, apparelColor, Color.white, pattern).MatAt(facing, null);
                                Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                                    ? material
                                    : OverrideMaterialIfNeeded(instance, material, pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                                GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                            }
                        }
                    }
                }
            }
        }

        public static void DrawHeadHairHairTranPatch(Mesh hairMesh, Vector3 loc, Quaternion quat, Material hairMat, bool drawNow, Vector3 vector, Vector3 headOffset, Rot4 facing, PawnRenderFlags flags, Pawn pawn, PawnRenderer instance)//
        {
            //Log.Warning("run hair patch");
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            AlienPartGenerator.AlienComp alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>();

            if (comp != null && !comp.PrefixResolved)
                instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp != null ? comp.GetDataOfDirection(facing) : new Dictionary<int, List<MultiTexBatch>>();

            int layer = (int)TextureRenderLayer.Hair;
            //是否绘制原头发贴图
            if (comp == null
                || curDirection.NullOrEmpty()
                || !curDirection.ContainsKey(layer) 
                || comp.GetAllHideOriginalDefData.NullOrEmpty() 
                || !comp.GetAllHideOriginalDefData.Contains("Hair"))
            {
                GenDraw.DrawMeshNowOrLater(hairMesh, loc, quat, hairMat, drawNow);
            }

            if (comp == null)
                return;

            //绘制多层头发贴图
            if (!curDirection.NullOrEmpty() && curDirection.ContainsKey(layer))
            {
                //Log.Error(facing.AsInt + ": " + curDirection[layer].Select(x => x.textureLevelsName).ToList().Aggregate((x, y) => x + ", " + y));
                
                Vector3 hairYOffset = vector + headOffset;
                hairYOffset.y += 0.028957527f;
                Mesh bodyMesh = null;
                Mesh headMesh = null;
                if (pawn.RaceProps.Humanlike)
                {
                    bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn).MeshAt(facing);
                    headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(facing);
                }
                else
                    bodyMesh = instance.graphics.nakedGraphic.MeshAt(facing);

                Color colorOne = pawn.story.HairColor;
                Color colorTwo = alienComp != null ? alienComp.GetChannel("hair").second : Color.white;

                //Log.Warning("Hair hair hair : " + curDirection.Count);

                foreach (MultiTexBatch batch in curDirection[layer])
                {
                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                    if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                        || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(pawn, batch.keyName))
                        continue;

                    TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                    Mesh mesh = null;
                    Vector3 hairPos = hairYOffset;
                    if (data.meshSize != Vector2.zero)
                    {
                        mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                        if (data.meshType == "Body")
                        {
                            hairPos.x = vector.x;
                            hairPos.z = vector.z;
                        }
                    }
                    else
                    {
                        if (pawn.RaceProps.Humanlike)
                        {
                            switch (data.meshType)
                            {
                                case "Body":
                                    mesh = bodyMesh;
                                    hairPos.x = vector.x;
                                    hairPos.z = vector.z;
                                    break;
                                case "Head": mesh = headMesh; break;
                                case "Hair": mesh = hairMesh; break;
                            }
                        }
                        else
                        {
                            mesh = bodyMesh;
                            hairPos.x = vector.x;
                            hairPos.z = vector.z;
                        } 
                    }
                    int pattern = 0;
                    if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                        pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                    Vector3 dataOffset = data.DrawOffsetForRot(facing);
                    dataOffset.y *= 0.0001f;
                    Vector3 pos = hairPos/* + dataOffset*/;
                    Material material = data.GetGraphic(batch.keyName, colorOne, colorTwo, pattern).MatAt(facing, null);
                    Material mat = GetHairOverrideMat(material, instance, flags.FlagSet(PawnRenderFlags.Portrait), !flags.FlagSet(PawnRenderFlags.Cache));
                    GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, drawNow);
                }
            }
        }

        public static void DrawHeadHairHeadMaskTranPatch(float angle, Vector3 vector, Vector3 headOffset, Rot4 facing, PawnRenderFlags flags, List<ApparelGraphicRecord> apparelGraphics, bool shouldDraw, Pawn pawn, PawnRenderer instance)
        {
            MultiRenderComp comp = pawn.GetComp<MultiRenderComp>();
            
            if (comp != null && !comp.PrefixResolved)
                instance.graphics.ResolveAllGraphics();

            Dictionary<int, List<MultiTexBatch>> curDirection = comp != null ? comp.GetDataOfDirection(facing) : new Dictionary<int, List<MultiTexBatch>>();

            Mesh bodyMesh = null;
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (pawn.RaceProps.Humanlike)
            {
                bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn).MeshAt(facing);
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(facing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(pawn).MeshAt(facing);
            }
            else
                bodyMesh = instance.graphics.nakedGraphic.MeshAt(facing);

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 hairYOffset = vector + headOffset;
            hairYOffset.y += 0.028957527f;

            for (int index = 0; index < apparelGraphics.Count; index++)
            {
                ApparelGraphicRecord apparel = apparelGraphics[index];
                if ((!shouldDraw || apparel.sourceApparel.def.apparel.hatRenderedFrontOfFace) 
                    && (apparel.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead || apparel.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.EyeCover) 
                    && !apparel.sourceApparel.def.apparel.forceRenderUnderHair)
                {
                    Vector3 loc = hairYOffset;
                    if (apparel.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                    {
                        loc = vector + headOffset;
                        if (apparel.sourceApparel.def.apparel.hatRenderedBehindHead)
                            loc.y += 0.02216602f;
                        else
                            loc.y += !(facing == Rot4.North) || apparel.sourceApparel.def.apparel.hatRenderedAboveBody ? 0.03185328f : 0.002895753f;
                    }
                    //是否绘制原装备的贴图
                    if (comp == null
                        || comp.GetAllHideOriginalDefData.NullOrEmpty() 
                        || !comp.GetAllHideOriginalDefData.Contains(apparel.sourceApparel.def.defName))
                    {
                        Material original = apparel.graphic.MatAt(facing, null);
                        Material mat = flags.FlagSet(PawnRenderFlags.Cache) ? original : OverrideMaterialIfNeeded(original, pawn, instance, flags.FlagSet(PawnRenderFlags.Portrait));
                        GenDraw.DrawMeshNowOrLater(hairMesh, loc, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                    }

                    if (comp == null)
                        continue;

                    //如果是多层服装的话
                    string apparelTypeOriginalDefName = apparel.sourceApparel.def.GetType().ToStringSafe() + "_" + apparel.sourceApparel.def.defName;
                    if (!curDirection.NullOrEmpty()
                        && !comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty() 
                        && comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(apparelTypeOriginalDefName))
                    {
                        List<int> renderLayers = new List<int>() { (int)TextureRenderLayer.HeadMask, (int)TextureRenderLayer.Hat };
                        foreach (int layer in renderLayers)
                        {
                            if (curDirection.ContainsKey(layer))
                            {
                                Color apparelColor = apparel.sourceApparel.DrawColor;
                                foreach (MultiTexBatch batch in curDirection[layer])
                                {
                                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                                    if (typeOriginalDefName == apparelTypeOriginalDefName 
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                                        && comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(pawn, batch.keyName))
                                    {
                                        TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                                        Mesh mesh = null;
                                        if (data.meshSize != Vector2.zero)
                                        {
                                            mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                                            if (data.meshType == "Body")
                                            {
                                                loc.x = vector.x;
                                                loc.z = vector.z;
                                            }
                                        }
                                        else
                                        {
                                            if (pawn.RaceProps.Humanlike)
                                            {
                                                switch (data.meshType)
                                                {
                                                    case "Body":
                                                        mesh = bodyMesh;
                                                        loc.x = vector.x;
                                                        loc.z = vector.z;
                                                        break;
                                                    case "Head": mesh = headMesh; break;
                                                    case "Hair": mesh = hairMesh; break;
                                                }
                                            }
                                            else
                                            {
                                                mesh = bodyMesh;
                                                loc.x = vector.x;
                                                loc.z = vector.z;
                                            }
                                        }
                                        int pattern = 0;
                                        if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                                            pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                                        Vector3 dataOffset = data.DrawOffsetForRot(facing);
                                        dataOffset.y *= 0.0001f;
                                        Vector3 pos = loc/* + dataOffset*/;
                                        Material material = data.GetGraphic(batch.keyName, apparelColor, Color.white, pattern).MatAt(facing, null);
                                        Material mat = flags.FlagSet(PawnRenderFlags.Cache)
                                            ? material
                                            : OverrideMaterialIfNeeded(instance, material, pawn, flags.FlagSet(PawnRenderFlags.Portrait));
                                        GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //Overlay RenderPawnInternalPostfix
        static void RenderPawnInternalPostfix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            MultiRenderComp comp = ___pawn.GetComp<MultiRenderComp>();
            if (comp == null)
                return;
            if (!comp.PrefixResolved)
                __instance.graphics.ResolveAllGraphics();
            Rot4 facing = bodyFacing;
            Dictionary<int, List<MultiTexBatch>> curDirection = comp.GetDataOfDirection(facing);
            if (curDirection.NullOrEmpty())
                return;

            Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
            Mesh bodyMesh = null;
            Mesh hairMesh = null;
            Mesh headMesh = null;
            if (___pawn.RaceProps.Humanlike)
            {
                bodyMesh = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(___pawn).MeshAt(facing);
                headMesh = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(___pawn).MeshAt(facing);
                hairMesh = HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn(___pawn).MeshAt(facing);
            }
            else
                bodyMesh = __instance.graphics.nakedGraphic.MeshAt(facing);

            if (curDirection.ContainsKey((int)TextureRenderLayer.Overlay))
            {
                Vector3 bodyLoc = rootLoc;
                bodyLoc.y += 0.037644785f;

                foreach (MultiTexBatch batch in curDirection[(int)TextureRenderLayer.Overlay])
                {
                    string typeOriginalDefName = batch.originalDefClass.ToStringSafe() + "_" + batch.originalDefName;
                    string typeOtiginalDefNameKeyName = typeOriginalDefName + "_" + batch.keyName;

                    if (comp.GetAllOriginalDefForGraphicDataDict.NullOrEmpty()
                        || !comp.GetAllOriginalDefForGraphicDataDict.ContainsKey(typeOriginalDefName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName].ContainsKey(batch.textureLevelsName)
                        || !comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName].CanRender(___pawn, batch.keyName))
                        continue;

                    TextureLevels data = comp.GetAllOriginalDefForGraphicDataDict[typeOriginalDefName][batch.textureLevelsName];
                    Color colorOne = ((ThingDef)GenDefDatabase.GetDef(data.originalDefClass, data.originalDef)).graphicData != null ? 
                                     ((ThingDef)GenDefDatabase.GetDef(data.originalDefClass, data.originalDef)).graphicData.color
                                     : Color.white;
                    Mesh mesh = null;
                    Vector3 offset = Vector3.zero;
                    if (data.meshSize != Vector2.zero)
                    {
                        mesh = MeshPool.GetMeshSetForWidth(data.meshSize.x, data.meshSize.y).MeshAt(facing);
                        switch (data.meshType)
                        {
                            case "Head": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                            case "Hair": offset = quat * __instance.BaseHeadOffsetAt(facing); break;
                        }
                    }
                    else
                    {
                        if (___pawn.RaceProps.Humanlike)
                        {
                            switch (data.meshType)
                            {
                                case "Body": mesh = bodyMesh; break;
                                case "Head":
                                    mesh = headMesh;
                                    offset = quat * __instance.BaseHeadOffsetAt(facing);
                                    break;
                                case "Hair":
                                    mesh = hairMesh;
                                    offset = quat * __instance.BaseHeadOffsetAt(facing);
                                    break;
                            }
                        }
                        else
                            mesh = bodyMesh;
                    }
                    int pattern = 0;
                    if (!comp.cachedRandomGraphicPattern.NullOrEmpty() && comp.cachedRandomGraphicPattern.ContainsKey(typeOtiginalDefNameKeyName))
                        pattern = comp.cachedRandomGraphicPattern[typeOtiginalDefNameKeyName];
                    string condition = "";
                    if (data.hasRotting && bodyDrawType == RotDrawMode.Rotting)
                        condition = "Rotting";
                    if (data.hasDessicated && bodyDrawType == RotDrawMode.Dessicated)
                        condition = "Dessicated";
                    Vector3 dataOffset = data.DrawOffsetForRot(facing);
                    dataOffset.y *= 0.0001f;
                    Vector3 pos = bodyLoc + offset/* + dataOffset*/;
                    Material mat = data.GetGraphic(batch.keyName, colorOne, Color.white, pattern, condition).MatAt(facing, null);
                    GenDraw.DrawMeshNowOrLater(mesh, pos, quat, mat, flags.FlagSet(PawnRenderFlags.DrawNow));
                }
            }
        }



        //备用随机算法
        public static string GetRandom(System.Random rand, Dictionary<string, int> list)
        {
            int i = rand.Next(list.Values.Max() + 1);
            List<string> result = list.Keys.Where(x => list[x] >= i).ToList();
            return result.RandomElement();
        }



    }



    //初始化AB包加载（暂时停用）
    //[HarmonyPatch(typeof(Root))]
    //[HarmonyPatch("CheckGlobalInit")]
    public class DAL_GameObjectPrefabLoadPatch
    {
        static void Postfix()
        {
            if (DAL_DynamicGameObjectPrefabManager.Initialized)
            {
                return;
            }
            DAL_DynamicGameObjectPrefabManager.InitGameObjectToList();
        }
    }



    //DAL_WorldCurrent构建
    [HarmonyPatch(typeof(World))]
    [HarmonyPatch("ConstructComponents")]
    public class DAL_WorldCurrentConstructPatch
    {
        static void Postfix()
        {
            DAL_WorldCurrent.GOM = new DAL_DynamicGameObjectManager();
        }
    }

}
