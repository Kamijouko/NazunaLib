using AlienRace.ExtendedGraphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace NareisLib
{
    public class ActionManager
    {
        //使用的行为树，填入defName
        public ActionDef def = null;
        public PawnRenderNode node = null;

        private string valuePath = "";
        private string fullPath = "";
        //private string curKeyName = "";
        private JobDef curJob = null;
        private Behavior curBehavior = null;
        private Action behaviorAction = null;
        private Action loopAction = null;


        private int valueIndex = -1;
        private int tickDelay = 10;
        private Action tickAction = null;
        private int goalTick = -1;
        private bool shouldUpdateAction = false;
        public string GetCurPath
        {
            get
            {
                return valuePath;
            }
        }

        public string GetFullPath
        {
            get
            {
                return fullPath;
            }
        }

        public ActionManager()
        {

        }

        public void ActionOnTick()
        {
            if (def == null || curBehavior == null || curBehavior.linkedWithAction || behaviorAction == null)
                return;
            if (shouldUpdateAction)
            {
                behaviorAction();
                goalTick = GetGoalTick(curBehavior.useRandomTimeDelta);
                shouldUpdateAction = false;
            }
            if (Find.TickManager.TicksGame >= goalTick)
            {
                behaviorAction();
                goalTick = GetGoalTick(curBehavior.useRandomTimeDelta);
            }
            
        }

        public int GetGoalTick(bool useRandomTimeDelta)
        {
            if (useRandomTimeDelta)
                return Find.TickManager.TicksGame + curBehavior.actionStaticChangeSeconds.SecondsToTicks();
            else
                return Find.TickManager.TicksGame + curBehavior.GetActionRandomChangeTick();
        }

        /// <summary>
        /// 最终确认是否渲染
        /// </summary>
        /// <param name="pawn">包装后的Pawn信息获取器</param>
        /// <returns></returns>
        public bool IsApplicable(ExtendedGraphicsPawnWrapper pawn)
        {
            return curBehavior == null || ((curBehavior.rendMoving && pawn.Moving) || (curBehavior.rendUnMoving && !pawn.Moving));
        }

        public void TraverseNode(ExtendedGraphicsPawnWrapper pawn, Pawn obj, JobDef job, TextureLevels textureLevels, string actionValuePath)
        {
            MultiRenderComp comp = obj.GetComp<MultiRenderComp>();
            if (comp == null)
                return;
            //Log.Message($"list count :{comp.cachedLinkedActionManagerList.Count}");
            foreach (TextureLevels level in comp.cachedLinkedActionManagerList)
            {
                level.actionManager.StateUpdate(comp, pawn, obj, job, textureLevels, actionValuePath);
            }
        }

        /// <summary>
        /// 更新ActionManager的CurBehavior的状态
        /// </summary>
        /// <param name="comp">传入的已经验证的Comp</param>
        /// <param name="pawn">包装后的Pawn信息获取器</param>
        /// <param name="obj">Pawn本身</param>
        /// <param name="job">Pawn当前的Job</param>
        /// <param name="key">TextureLevels此时为此Pawn随机到的贴图名称</param>
        /// <param name="actionValuePath">传入的链接目标的子路径</param>
        public void StateUpdate(MultiRenderComp comp, ExtendedGraphicsPawnWrapper pawn, Pawn obj, JobDef job, TextureLevels level, string actionValuePath)
        {
            if (job == null)
                return;
            curJob = job;
            if (def.behaviors.TryGetValue(job, out curBehavior) && curBehavior.textures.Contains((node as TextureLevelsToNode).textureLevels.keyName))
            {
                //Log.Message(curBehavior.type_originalDefName);
                //Log.Message($"{level.originalDefClass}_{level.originalDef}");
                //当使用链接至其他ActionManager时
                if (curBehavior.linkedWithAction
                    && curBehavior.type_originalDefName == $"{level.originalDefClass}_{level.originalDef}"
                    && curBehavior.textureLevelsName == level.textureLevelsName)
                {
                    fullPath = Path.Combine(curBehavior.exPath, valuePath = actionValuePath);
                    if (node != null)
                        node.requestRecache = true;
                    //Log.Message($"linked:{valuePath}");
                    return;
                }
            }
            else
                fullPath = "";
        }

        /// <summary>
        /// 更新ActionManager的CurBehavior的状态
        /// </summary>
        /// <param name="pawn">包装后的Pawn信息获取器</param>
        /// <param name="obj">Pawn本身</param>
        /// <param name="job">Pawn当前的Job</param>
        /// <param name="key">TextureLevels此时为此Pawn随机到的贴图名称</param>
        public void StateUpdate(ExtendedGraphicsPawnWrapper pawn, Pawn obj, JobDef job, string key, TextureLevels level)
        {
            if (job == null || key == null || key == "")
                return;

            if (curJob == null || curJob.defName != job.defName || curBehavior == null)
            {
                curJob = job;
                Reset();
                if (def.behaviors.TryGetValue(job, out curBehavior) && curBehavior.textures.Contains(key))
                {
                    //只有不链接至其他ActionManager时
                    if (!curBehavior.linkedWithAction)
                    {
                        //在正常状态下的逻辑
                        List<string> list;
                        if (curBehavior.pathDict.TryGetValue(pawn.GetPosture(), out list) && !list.NullOrEmpty())
                        {
                            fullPath = Path.Combine(curBehavior.exPath, valuePath = curBehavior.randomFirst ? list.RandomElement() : list[0]);
                            //Log.Message($"origin:{valuePath}");
                            TraverseNode(pawn, obj, job, level, valuePath);
                            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(obj);
                            if (node != null)
                                node.requestRecache = true;
                        }
                        if (curBehavior.randomChange)
                        {
                            behaviorAction = () =>
                            {
                                valuePath = "";
                                fullPath = "";
                                List<string> paths;
                                if (curBehavior.pathDict.TryGetValue(pawn.GetPosture(), out paths) && !paths.NullOrEmpty())
                                {
                                    fullPath = Path.Combine(curBehavior.exPath, valuePath = curBehavior.loopChange ? paths[GetNextIndex(paths)] : paths.RandomElement());
                                    //Log.Message($"origin:{valuePath}");
                                    TraverseNode(pawn, obj, job, level, valuePath);
                                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(obj);
                                    if (node != null)
                                        node.requestRecache = true;
                                }
                            };
                            shouldUpdateAction = true;
                            //RegisterBehavior(obj);
                        }
                        return;
                    }
                }
                else
                {
                    fullPath = "";
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(obj);
                    if (node != null)
                        node.requestRecache = true;
                }
            }
        }

        /// <summary>
        /// 获取轮转变换的下一个索引
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private int GetNextIndex(List<string> list)
        {
            if (valueIndex >= list.Count() - 1)
                return valueIndex = 0;
            else
                return valueIndex++;
        }

        /// <summary>
        /// 注册随机行为以及随机时间
        /// </summary>
        /*private void RegisterBehavior(Thing pawn)
        {
            if (!curBehavior.useRandomTimeDelta)
            {
                //behaviorAction = () => curPrefix = curBehavior.prefixDict.RandomElement();
                HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(behaviorAction, curBehavior.actionStaticChangeSeconds.SecondsToTicks(), pawn, true);
            }
            else
            {
                loopAction = behaviorAction;
                behaviorAction = () =>
                {
                    loopAction();
                    HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(behaviorAction, curBehavior.GetActionRandomChangeTick(), pawn, false);
                    //curPrefix = curBehavior.prefixDict.RandomElement();
                };
                HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(behaviorAction, curBehavior.GetActionRandomChangeTick(), pawn, false);
            }
        }*/

        /// <summary>
        /// 取消注册当前行为
        /// </summary>
        /*private void UnregisterBehavior()
        {
            HugsLibController.Instance.TickDelayScheduler.TryUnscheduleCallback(behaviorAction);
        }*/

        public ActionManager Clone()
        {
            ActionManager result = new ActionManager();
            result.def = def;
            result.node = node;
            result.fullPath = fullPath;
            //result.curKeyName = curKeyName;
            result.curJob = curJob;
            result.curBehavior = curBehavior;
            result.behaviorAction = behaviorAction;
            result.tickDelay = tickDelay;
            result.tickAction = tickAction;
            return result;
        }

        private void Reset()
        {
            valueIndex = -1;
            valuePath = "";
            fullPath = "";
            if (behaviorAction != null)
            {
                //UnregisterBehavior();
                behaviorAction = null;
            }
        }

        public void Destory()
        {
            curJob = null;
            Reset();
        }
    }
}
