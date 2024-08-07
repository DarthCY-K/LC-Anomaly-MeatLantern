﻿using LCAnomalyLibrary.Comp;
using LCAnomalyLibrary.Util;
using MeatLantern.Job;
using MeatLantern.Utility;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MeatLantern.Comp
{
    public class CompMeatLantern : LC_CompEntity
    {
        #region 变量

        public MeatLanternState meatLanternState;

        /// <summary>
        /// 下一次吞噬的时间
        /// </summary>
        public int nextEat = -99999;

        public new CompProperties_MeatLantern Props => (CompProperties_MeatLantern)props;

        #endregion 变量

        #region 生命周期

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref nextEat, "nextEat", -99999);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            CheckSpawnHostile();
        }

        //public override void CompTickRare()
        //{
        //    base.CompTickRare();

        //    //转移的时候，若被放下就逃脱
        //    if (((Pawn)parent).kindDef == Def.PawnKindDefOf.MeatLanternContained)
        //    {
        //        Notify_Escaped();
        //    }
        //}

        #endregion 生命周期

        #region 触发事件

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            MeatLanternUtility.OnMeatLanternDeath((Pawn)parent, prevMap);
        }

        public override void Notify_Escaped()
        {
            base.Notify_Escaped();

            MeatLanternUtility.OnMeatLanternEscape((Pawn)parent, parent.MapHeld);
        }

        /// <summary>
        /// 绑到收容平台上后的操作
        /// </summary>
        public override void Notify_Holded()
        {
            CheckIsDiscovered();
        }

        #endregion 触发事件

        #region 行为逻辑

        /// <summary>
        /// 吞噬攻击单位
        /// </summary>
        /// <param name="victims">受害者Lsit</param>
        public void Eat(List<Pawn> victims)
        {
            MeatLanternUtility.OnMeatLanternEat((Pawn)parent, victims);

            nextEat = Find.TickManager.TicksGame + Props.eatCooldownTick;
            ((Pawn)parent).mindState.enemyTarget = null;
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="state">状态</param>

        public void SetState(MeatLanternState state)
        {
            meatLanternState = state;
            ((Pawn)parent).jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        /// <summary>
        /// 判断是否应该在生成时改变敌对情况（是逃跑状态则派系变空，否则变实体阵营）
        /// </summary>
        private void CheckSpawnHostile()
        {
            PawnKindDef def = ((Pawn)parent).kindDef;

            if (def == Def.PawnKindDefOf.MeatLanternEscaped)
            {
                if (parent.Faction != null)
                    parent.SetFaction(null);
            }
            else if (def == Def.PawnKindDefOf.MeatLanternContained)
            {
                if (parent.Faction != Faction.OfEntities)
                    parent.SetFaction(Faction.OfEntities);
            }
        }

        #endregion 行为逻辑

        #region 研究与图鉴

        protected override LC_StudyResult CheckFinalStudyQuality(Pawn studier, EAnomalyWorkType workType)
        {
            //每级智力提供5%成功率，10级智力提供50%成功率
            float successRate_Intellectual = studier.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f;
            //叠加基础成功率，此处是50%，叠加完应是100%
            float finalSuccessRate = successRate_Intellectual + Props.studySucessRateBase;

            //本能和沟通+10%成功率，洞察+20%成功率，压迫-10%成功率
            switch (workType)
            {
                case EAnomalyWorkType.Instinct:
                    finalSuccessRate += 0.1f;
                    break;

                case EAnomalyWorkType.Insight:
                    finalSuccessRate += 0.2f;
                    break;

                case EAnomalyWorkType.Attachment:
                    finalSuccessRate += 0.1f;
                    break;

                case EAnomalyWorkType.Repression:
                    finalSuccessRate -= 0.1f;
                    break;
            }

            //成功率不能超过90%
            if (finalSuccessRate >= 1f)
                finalSuccessRate = 0.9f;

            return Rand.Chance(finalSuccessRate) ? LC_StudyResult.Good : LC_StudyResult.Normal;
        }

        public override bool CheckStudierSkillRequire(Pawn studier)
        {
            if (studier.skills.GetSkill(SkillDefOf.Intellectual).Level < 4)
            {
                //Log.Message($"工作：{studier.Name}的技能{SkillDefOf.Intellectual.label.Translate()}等级不足4，工作固定无法成功");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否已在图鉴中被解锁
        /// </summary>
        private void CheckIsDiscovered()
        {
            //Log.Message($"检查图鉴解锁情况，我是 {SelfPawn.def.defName}");

            if (AnomalyUtility.ShouldNotifyCodex((Pawn)parent, EntityDiscoveryType.Unfog, out var entries))
            {
                Find.EntityCodex.SetDiscovered(entries, Def.PawnKindDefOf.MeatLanternContained.race, (Pawn)parent);
                Find.EntityCodex.SetDiscovered(entries, Def.PawnKindDefOf.MeatLanternEscaped.race, (Pawn)parent);
            }
        }

        #endregion 研究与图鉴

        #region UI

        public override string CompInspectStringExtra()
        {
            TaggedString taggedString = "Biosignature".Translate() + ": " + BiosignatureName;
            if (DebugSettings.showHiddenInfo)
            {
                taggedString += "\nState: " + meatLanternState;
                taggedString += "\nEatDuration：" + Props.eatCooldownTick;
                taggedString += "\nReadyToEat：" + (Find.TickManager.TicksGame >= nextEat);
            }

            return taggedString;
        }

        #endregion UI
    }
}