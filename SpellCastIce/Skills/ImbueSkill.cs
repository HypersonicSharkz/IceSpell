using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace SpellCastIce.Skills
{
    internal class ImbueSkill : SkillData
    {
        public override void OnSkillLoaded(SkillData skillData, Creature creature)
        {
            base.OnSkillLoaded(skillData, creature);
            AbilityManager.UnlockAbility(AbilityManager.AbilitiesEnum.IceImbue);

            Catalog.GetData<SpellCastCharge>("IceSpell").imbueEnabled = true;
        }

        public override void OnSkillUnloaded(SkillData skillData, Creature creature)
        {
            base.OnSkillUnloaded(skillData, creature);
            AbilityManager.RemoveAbility(AbilityManager.AbilitiesEnum.IceImbue);

            Catalog.GetData<SpellCastCharge>("IceSpell").imbueEnabled = false;
        }
    }
}
