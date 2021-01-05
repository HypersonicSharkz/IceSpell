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

    public class IceStatsJSON
    {
        public int level { get; set; }
        public int levelPoints { get; set; }
        public float xp { get; set; }

        public List<IceManager.AbilitiesEnum> unlocked { get; set; } = new List<IceManager.AbilitiesEnum>();
    }

    public static class IceManager
    {
        public enum XPGains
        {
            Kill = 10,
            HeadShot = 5,
            Hit = 2,
        }

        public static int level = 0;
        public static int levelPoints = 0;
        public static float xp = 0;


        public enum AbilitiesEnum
        {
            noGravity,
            pickUpIceSpikes,
            iceSpikeAim,
            IceMergeIce,
            IceMergeFire,
            IceMergeGrav,
            IceMergeLightning,
            IceImbue
        }

        public static void LoadFromJSON()
        {

            //Create all the ability structs
            abilities = new List<Ability>
            {
                new Ability(5, "AimButton", "Ice Aim", "Your aim has never been better, ice spikes will now more easily hit the target", AbilitiesEnum.iceSpikeAim),
                new Ability(2, "GrabButton", "Grabable Ice", "Ever just wanted to pick up that ice spike you just launched? Well now you can! Unlocks the ability to wield and manipulate ice spikes.", AbilitiesEnum.pickUpIceSpikes),
                new Ability(3, "NoGravButton", "Zero Gravity Ice", "Makes the ice spikes fly without falling, simply no gravity on spikes", AbilitiesEnum.noGravity),
                new Ability(1, "ImbueButton", "Frost Imbuement", "Allows you to imbue any weapon with frost, enemies hit will be slowed by the power of the imbuement", AbilitiesEnum.IceImbue, delegate 
                {
                    Catalog.GetData<SpellCastCharge>("IceSpell").imbueEnabled = true;
                }),
                new Ability(3, "MergeIceButton", "Ice Imbue", "Merge ice to shoot out a bunch of spikes around you", AbilitiesEnum.IceMergeIce, delegate 
                {
                    Catalog.GetData<ContainerData>("PlayerDefault").content.Add(new ContainerData.Content(Catalog.GetData<ItemData>("SpellIceMergeItem")));
                    //Player.currentCreature.container.content.Add(new ContainerData.Content(Catalog.GetData<ItemPhysic>("SpellIceMergeItem"), 1, new List<Item.SavedValue>()));
                }),
                new Ability(5, "MergeFireButton", "FireIce Beam", "Merge ice and fire to shoot out a burning cold beam of ice", AbilitiesEnum.IceMergeFire, delegate 
                {
                    Catalog.GetData<ContainerData>("PlayerDefault").content.Add(new ContainerData.Content(Catalog.GetData<ItemData>("SpellIceFireMergeItem")));
                }),
                new Ability(5, "MergeGravButton", "Ice stasis dome", "Merge ice and gravity to create a freezing sphere around stopping anyone near you", AbilitiesEnum.IceMergeGrav, delegate
                {
                    Catalog.GetData<ContainerData>("PlayerDefault").content.Add(new ContainerData.Content(Catalog.GetData<ItemData>("SpellIceMergeGravItem")));
                })

                
            };

            EventManager.onCreatureHit += EventManager_onCreatureHit;
            EventManager.onCreatureKill += EventManager_onCreatureKill;

            if (File.Exists(Path.Combine(Application.streamingAssetsPath, "Mods/IceSpell/Saves/IceStatSave.json")))
            {
                string json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Mods/IceSpell/Saves/IceStatSave.json"));
                IceStatsJSON ice = JsonConvert.DeserializeObject<IceStatsJSON>(json);

                level = ice.level;
                levelPoints = ice.levelPoints;
                xp = ice.xp;

                foreach (Ability kvp in abilities)
                {
                    if (ice.unlocked.Contains(kvp.abilityEnum))
                    {
                        kvp.Unlock();
                    }
                }

                chargeSpeed = (float)(1f / Math.Pow(1.1f, level));
                spikeSpeed = (float)(5 + Math.Sqrt(level) / 4 );
            } else
            {
                SaveToJSON();
            }

            
        }

        public static void EventManager_onCreatureKill(Creature creature, Player player, ref CollisionStruct collisionStruct, EventTime eventTime)
        {
            if (collisionStruct.sourceColliderGroup?.collisionHandler?.item?.itemId == "IceSpike")
            {
                GainXP(XPGains.Kill);

                if (collisionStruct.damageStruct.hitRagdollPart.type == RagdollPart.Type.Head)
                {
                    GainXP(XPGains.HeadShot);
                }
            }
        }

        public static void EventManager_onCreatureHit(Creature creature, ref CollisionStruct collisionStruct)
        {
            if (collisionStruct.sourceColliderGroup?.collisionHandler?.item?.itemId == "IceSpike")
            {
                GainXP(XPGains.Hit);

                if (collisionStruct.damageStruct.hitRagdollPart.type == RagdollPart.Type.Head)
                {
                    GainXP(XPGains.HeadShot);
                }

                if (creature != Player.currentCreature && !creature.isKilled)
                {
                    if (creature.animator.speed == 1)
                    {
                        if (!creature.GetComponent<IceSpellMWE>())
                        {
                            creature.gameObject.AddComponent<IceSpellMWE>();
                        }
                        IceSpellMWE scr = creature.GetComponent<IceSpellMWE>();
                        scr.SlowStartCoroutine(creature, 100f, 0f, 0f, 8f);
                    }
                }
            }
        }

        public static void GainXP(XPGains xpAction)
        {
            xp += (int)xpAction;

            if (xp >= XpForNextLevel(level))
            {
                float left = xp - XpForNextLevel(level);
                level += 1;
                levelPoints += 1;
                xp = left;

                //Stats up
                chargeSpeed = (float)(1f / Math.Pow(1.1f, level));
                spikeSpeed = (float)(5 + Math.Sqrt(level) / 4);
            }

            SaveToJSON();
        }

        public static void SaveToJSON()
        {
            IceStatsJSON ice = new IceStatsJSON()
            {
                level = level,
                levelPoints = levelPoints,
                xp = xp
            };

            foreach (KeyValuePair<AbilitiesEnum, Ability> kvp in abilityDict)
            {
                if (kvp.Value.unlocked)
                {
                    ice.unlocked.Add(kvp.Key);
                }
            }

            string json = JsonConvert.SerializeObject(ice, Formatting.Indented);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Mods/IceSpell/Saves/IceStatSave.json"), json);
        }

        public static float XpForNextLevel(int lvl)
        {
            return (float)((5 * Math.Pow((lvl+1),2)) + 50);
        }


        public static float chargeSpeed = 1.0f;
        public static float spikeSpeed = 5f;




        public static readonly Dictionary<AbilitiesEnum, Ability> abilityDict = new Dictionary<AbilitiesEnum, Ability>();

        public static bool IsAbilityUnlocked(AbilitiesEnum ab)
        {
            Ability ability;
            abilityDict.TryGetValue(ab, out ability);

            if (ability.unlocked)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public static bool UnlockAbility(AbilitiesEnum ability)
        {
            Ability abilityToUnlock;
            abilityDict.TryGetValue(ability, out abilityToUnlock);

            if (!abilityToUnlock.unlocked)
            {
                if (levelPoints >= abilityToUnlock.levelPointCost)
                {
                    levelPoints -= abilityToUnlock.levelPointCost;
                    abilityToUnlock.Unlock();

                    SaveToJSON();

                    return true;
                }
            }
            return false;
        }


        public static List<Ability> abilities = new List<Ability>();


        public class Ability
        {

            public UnityEvent onUnlockEvent = new UnityEvent();

            public int levelPointCost;
            public bool unlocked;

            public string customRefName;
            public string uiTitle;
            public string uiDescript;
            public AbilitiesEnum abilityEnum;

            public void Unlock()
            {
                unlocked = true;
                onUnlockEvent.Invoke();
            }

            public Ability(int cost, string customRefName, string uiTitle, string uiDescript, AbilitiesEnum abilityEnum, UnityAction onUnlockAction = null)
            {
                Debug.Log(abilityDict.Count);
                levelPointCost = cost;
                unlocked = false;
                this.customRefName = customRefName;
                this.uiTitle = uiTitle;
                this.uiDescript = uiDescript;
                this.abilityEnum = abilityEnum;

                if (onUnlockAction != null)
                {
                    onUnlockEvent.AddListener(onUnlockAction);
                }

                abilityDict.Add(abilityEnum, this);
            }
        }

        public static class Abilities
        {
            /*
            public static Ability Ability_NoGravity = new Ability(2);
            public static Ability Ability_PickUpIceSpikes = new Ability(2);

            public static Ability Ability_IceSpikeAim = new Ability(5);
            public static float Ability_IceSpikeAimPrecision = 1f;

            public static Ability Ability_IceMergeIce = new Ability(2);
            public static Ability Ability_IceMergeFire = new Ability(2);
            public static Ability Ability_IceMergeGrav = new Ability(3);
            public static Ability Ability_IceMergeLightning = new Ability(3);*/

       
        }
    }
}
