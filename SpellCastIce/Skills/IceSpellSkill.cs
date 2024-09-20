using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace SpellCastIce.Skills
{
    internal class IceSpellSkill : SkillData
    {
        public AbilityManager.AbilitiesEnum Ability { get; set; }

        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            AbilityManager.UnlockAbility(Ability);
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            AbilityManager.RemoveAbility(Ability);
        }
    }
}
