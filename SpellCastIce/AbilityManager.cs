using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;
using ThunderRoad;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using System.Collections;

namespace SpellCastIce
{

    public static class AbilityManager
    {
        public enum AbilitiesEnum
        {
            None = 0,
            noGravity = 1,
            pickUpIceSpikes = 2,
            iceSpikeAim = 4,
            IceImbue = 8,
            DoubleShot = 16,
            TripleShot = 32,
            IcicleFreeze = 64,
            ExplosiveFrost = 128,
        }

        private static AbilitiesEnum unlockedAbilities;

        public static bool IsAbilityUnlocked(AbilitiesEnum ab)
        {
            return unlockedAbilities.HasFlag(ab);
        }

        public static void UnlockAbility(AbilitiesEnum ability)
        {
            unlockedAbilities |= ability;
        }

        public static void RemoveAbility(AbilitiesEnum ability)
        {
            unlockedAbilities &= ~ability;
        }
    }
}
