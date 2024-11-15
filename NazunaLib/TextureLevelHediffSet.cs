using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using AlienRace.ExtendedGraphics;

namespace NareisLib
{
    public class TextureLevelHediffSet
    {
        //填入Hediff的defName
        public HediffDef hediff;

        //填入Hediff的hediffClass，请不要与上方参数同时使用
        public Type hediffClass;

        //控制此Hediff效果是否有对应Job时的效果
        //如果开启的话必须为当前的Job效果也准备一份Hediff效果贴图
        public bool enableWithJob = false;

        //此Hediff的优先级，当这些Hediff可以同时存在于某个部位时才须设置，优先渲染数字大的效果贴图
        public int priority = 0;

        //当当前贴图属于以下列表中时才具有此hediff特殊效果
        public List<string> texList = new List<string>();

        //严重度对应的贴图名称前缀列表
        public List<TextureLevelHediffSeveritySet> severity = new List<TextureLevelHediffSeveritySet>();

        //是否使用严重度，适用于不具有严重度的Hediff
        public bool useSeverity = true;

        //不使用严重度时应用的前缀
        public string noSeverityPrefix = "";

        public bool GetCurHediffPrefix(ExtendedGraphicsPawnWrapper pawn, BodyPartDef part, string partLabel, ref string prefix)
        {
            if (hediffClass != null)
            {
                if (useSeverity)
                {
                    float tmp = 0f;
                    float maxSeverityOfHediff = SeverityOfHediffsClassOnPart(pawn, hediffClass, part, partLabel).Max();
                    foreach (TextureLevelHediffSeveritySet set in severity)
                    {
                        if (maxSeverityOfHediff >= set.severity && set.severity >= tmp)
                        {
                            tmp = set.severity;
                            prefix = set.prefix;
                            return true;
                        }
                    }
                }
                else if (HasHediffOfClassAndPart(pawn, hediffClass, part, partLabel))
                {
                    prefix = noSeverityPrefix;
                    return true;
                }
            }
            if (hediff != null)
            {
                if (useSeverity)
                {
                    float tmp = 0f;
                    float maxSeverityOfHediff = pawn.SeverityOfHediffsOnPart(hediff, part, partLabel).Max();
                    foreach (TextureLevelHediffSeveritySet set in severity)
                    {
                        if (maxSeverityOfHediff >= set.severity && set.severity >= tmp)
                        {
                            tmp = set.severity;
                            prefix = set.prefix;
                            return true;
                        }
                    }
                }
                else if (pawn.HasHediffOfDefAndPart(hediff, part, partLabel) != null)
                {
                    prefix = noSeverityPrefix;
                    return true;
                }
            }
            return false;
        }


        public virtual IEnumerable<float> SeverityOfHediffsClassOnPart(ExtendedGraphicsPawnWrapper pawn, Type hediffClass, BodyPartDef part, string partLabel)
        {
            return from h in pawn.GetHediffList()
                   where IsHediffOfClassAndPart(pawn, h, hediffClass, part, partLabel)
                   select h.Severity;
        }

        private bool HasHediffOfClassAndPart(ExtendedGraphicsPawnWrapper pawn, Type hediffClass, BodyPartDef part, string partLabel)
        {
            bool result = pawn.GetHediffList().Any((Hediff h) => IsHediffOfClassAndPart(pawn, h, hediffClass, part, partLabel));
            //Log.Message(result.ToString());
            return result;
        }

        private bool IsHediffOfClassAndPart(ExtendedGraphicsPawnWrapper pawn, Hediff hediff, Type hediffClass, BodyPartDef part, string partLabel)
        {
            return hediff.def.hediffClass == hediffClass && ((hediff.Part == null && hediff.Part.untranslatedCustomLabel == partLabel) || pawn.IsBodyPart(hediff.Part, part, partLabel));
        }
    }
}
