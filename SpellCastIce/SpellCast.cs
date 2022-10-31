using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using IngameDebugConsole;
//using WeaponEffects;

namespace SpellCastIce
{

    public class SpellCastIce : SpellCastCharge
    {
        public bool useLevelSystem = false;

        public override void Init()
        {
            Debug.LogError("Initted");
            EventManager.onPossess += EventManager_onPossess;
            IceManager.LoadFromJSON();

            base.Init();
        }

        private void EventManager_onPossess(Creature creature, EventTime eventTime)
        {
            if (creature.container.contents.Where(c => c.itemData.id == "SpellIceItem").Count() <= 0)
                creature.container.AddContent(Catalog.GetData<ItemData>("SpellIceItem"));

            IceManager.LoadFromSave(creature);
        }

        public static void IceStats()
        {
            Debug.Log("Level: " + IceManager.level);
            Debug.Log("XP: " + IceManager.xp + " / " + IceManager.XpForNextLevel(IceManager.level));
            Debug.Log("Points to spend: " + IceManager.levelPoints);

            foreach (KeyValuePair<IceManager.AbilitiesEnum, IceManager.Ability> kvp in IceManager.abilityDict)
            {
                Debug.Log(kvp.Key.ToString() + "Unlocked: " + kvp.Value.unlocked);
            }

            Debug.Log("ChargeSpeed: " + IceManager.chargeSpeed);
            Debug.Log("SpikeSpeed: " + IceManager.spikeSpeed);
        }

        public override void Throw(Vector3 velocity)
        {
            base.Throw(velocity);
            if (this.spellCaster.ragdollHand.playerHand)
            {
                PlayerControl.GetHand(this.spellCaster.ragdollHand.side).HapticPlayClip(Catalog.gameData.haptics.telekinesisThrow, 1f);
            }
            Catalog.GetData<ItemData>("IceSpike", true).SpawnAsync(delegate (Item iceSpike) {

                //iceSpike.IgnoreObjectCollision(shooterItem);
                iceSpike.IgnoreRagdollCollision(spellCaster.mana.creature.ragdoll);


                if (!IceManager.IsAbilityUnlocked(IceManager.AbilitiesEnum.iceSpikeAim))
                {
                    iceSpike.rb.AddForce(velocity * IceManager.spikeSpeed, ForceMode.Impulse);
                    iceSpike.transform.rotation = Quaternion.LookRotation(velocity.normalized);
                } else
                {
                    Vector3 aimDir = AimAssist(iceSpike.transform.position, velocity.normalized, 0.7f, 0.01f);

                    iceSpike.rb.AddForce(aimDir * velocity.magnitude * IceManager.spikeSpeed, ForceMode.Impulse);
                    iceSpike.transform.rotation = Quaternion.LookRotation(aimDir);
                }
                
                iceSpike.Throw(1f, Item.FlyDetection.Forced);
            }, spellCaster.magic.position - (spellCaster.magic.forward * 0.5f), null, null, false, null);
        }

        private Vector3 AimAssist(Vector3 ownPosition, Vector3 ownDirection, float aimPrecision, float randomness)
        {
            Creature toHit = null;
            float closest = -1;
            Vector3 dirS = Vector3.zero;

            foreach (Creature creature in Creature.all)
            {
                if (creature != Player.currentCreature && !creature.isKilled)
                {
                    Vector3 dir = (creature.ragdoll.GetPart(RagdollPart.Type.Head).transform.position - ownPosition).normalized;
                    if (Vector3.Dot(ownDirection, dir) > aimPrecision)
                    {
                        if (Vector3.Dot(ownDirection, dir) > closest)
                        {
                            closest = Vector3.Dot(ownDirection, dir);
                            toHit = creature;
                            dirS = dir;
                        }
                    }
                }
            }

            if (toHit != null)
            {
                Vector3 rand = UnityEngine.Random.insideUnitSphere * randomness;

                return (dirS + rand).normalized;
            } else
            {
                return ownDirection;
            }

            
        }

        public override bool OnImbueCollisionStart(CollisionInstance collisionInstance)
        {
            base.OnImbueCollisionStart(collisionInstance);

            if (collisionInstance.damageStruct.hitRagdollPart)
            {
                if (collisionInstance.damageStruct.damage > 1)
                {
                    Creature creature = collisionInstance.targetCollider.GetComponentInParent<Creature>();
                    if (creature != Player.currentCreature && !creature.isKilled)
                    {
                        if (creature.animator.speed == 1)
                        {
                            if (!creature.GetComponent<IceSpellMWE>())
                            {
                                creature.gameObject.AddComponent<IceSpellMWE>();
                            }
                            IceSpellMWE scr = creature.GetComponent<IceSpellMWE>();
                            scr.SlowStartCoroutine(creature, collisionInstance.sourceColliderGroup.imbue.energy, 50f, 80f, 5f);
                        }
                    }
                }
            }

            return true;
        }
    }
}
