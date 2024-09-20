using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
//using WeaponEffects;

namespace SpellCastIce
{

    public class SpellCastIce : SpellCastCharge
    {
        [ModOption(category = "Icicle", name = "Throw Velocity", tooltip = "The velocity of ice spikes when thrown")]
        [ModOptionFloatValues(0,10,0.1f)]
        public static float throwForce = 5;

        [ModOption(category = "Aim Assist", name = "Aim Precision", tooltip = "How precise your initial hit must be to activate Aim Assist, higher will make it easier to hit further away targets")]
        [ModOptionFloatValues(0, 5, 0.1f)]
        public static float aimPrecision = 0.2f;

        [ModOption(category = "Double Shot", name = "Angle", tooltip = "How far the ice spikes will be spread")]
        [ModOptionFloatValues(0, 45, 1f)]
        public static float multishotAngle = 25f;

        public bool useLevelSystem = false;

        public override void Init()
        {
            EventManager.onPossess += EventManager_onPossess;
            EventManager.onCreatureHit += EventManager_onCreatureHit;
            //IceManager.LoadFromJSON();

            base.Init();
        }

        private void EventManager_onCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart && collisionInstance.sourceColliderGroup?.collisionHandler?.item?.itemId == "IceSpike")
            {
                if (collisionInstance.damageStruct.damageType != DamageType.Pierce)
                    return;

                if (creature != Player.currentCreature && !creature.isKilled)
                {
                    creature.Inflict("Freezing", this, float.PositiveInfinity, AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.IcicleFreeze) ? 100f : 20f);
                }
            }
        }

        private void EventManager_onPossess(Creature creature, EventTime eventTime)
        {
            //IceManager.LoadFromSave(creature);
        }

        /*
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
        }*/

        public override void Throw(Vector3 velocity)
        {
            base.Throw(velocity);

            Catalog.GetData<ItemData>("IceSpike", true).SpawnAsync(delegate (Item iceSpike) {

                //iceSpike.IgnoreObjectCollision(shooterItem);
                iceSpike.IgnoreRagdollCollision(spellCaster.mana.creature.ragdoll);


                if (!AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.iceSpikeAim))
                {
                    iceSpike.physicBody.velocity = velocity * throwForce;
                    iceSpike.transform.rotation = Quaternion.LookRotation(velocity.normalized);
                    
                } else
                {
                    iceSpike.physicBody.drag = 0;
                    Vector3 aimDir = AimAssist(iceSpike.transform.position, velocity.normalized, aimPrecision, 0.01f, velocity.magnitude * throwForce);

                    iceSpike.physicBody.velocity = aimDir;
                    iceSpike.transform.rotation = Quaternion.LookRotation(aimDir);
                }

                iceSpike.Throw(1f, Item.FlyDetection.Forced);

                int extraShots = 0;

                if (AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.TripleShot))
                    extraShots += 2;

                if (AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.DoubleShot))
                    extraShots += 2;

                if (extraShots > 0)
                {
                    List<Item> iceSpikes = new List<Item>
                    {
                        iceSpike
                    };


                    float angleBetween = multishotAngle / extraShots;

                    for (int i = 0; i < extraShots; i++)
                    {
                        int index = i; 
                        Catalog.GetData<ItemData>("IceSpike", true).SpawnAsync(delegate (Item extraShot)
                        {
                            
                            float angle = multishotAngle / 2f - (index * angleBetween);

                            if (angle <= 0)
                                angle -= angleBetween;

                            extraShot.transform.rotation = iceSpike.transform.rotation * Quaternion.Euler(Vector3.up * angle);

                            extraShot.physicBody.velocity = extraShot.transform.forward * iceSpike.physicBody.velocity.magnitude;
                            extraShot.Throw(1f, Item.FlyDetection.Forced);

                            foreach (Item item in iceSpikes)
                            {
                                extraShot.IgnoreItemCollision(item, true);
                            }

                            extraShot.IgnoreRagdollCollision(spellCaster.mana.creature.ragdoll);

                            iceSpikes.Add(extraShot);

                        }, spellCaster.magicSource.position - velocity.normalized, null, null, false, null);
                    }
                }

            }, spellCaster.magicSource.position - velocity.normalized, null, null, false, null);
        }

        private Vector3 AimAssist(Vector3 ownPosition, Vector3 ownDirection, float aimPrecision, float randomness, float startVelocity)
        {
            Transform toHit = null;
            float closest = float.MaxValue;

            List<Transform> transformsToCheck = new List<Transform>();


            //Add creatures to the list of targets
            foreach (Creature creature in Creature.allActive)
            {
                if (creature != Player.currentCreature && !creature.isKilled)
                {
                    transformsToCheck.Add(creature.ragdoll.GetPart(RagdollPart.Type.Head).transform);
                }
            }

            //Add golem crystals to list of targets
            if (Golem.local != null)
            {
                foreach (GolemCrystal crystal in Golem.local.crystals)
                {
                    transformsToCheck.Add(crystal.transform);
                }
            }

            foreach (Transform transform in transformsToCheck)
            {
                Vector3 creaturePos = transform.position;
                Vector3 toCreature = (creaturePos - ownPosition).normalized;

                //Must be in front
                if (Vector3.Dot(ownDirection, toCreature) > 0)
                {
                    float perpendicularDistance = Vector3.Cross(ownDirection, toCreature).magnitude;

                    if (perpendicularDistance > aimPrecision)
                        continue;

                    if (perpendicularDistance > closest)
                        continue;

                    closest = perpendicularDistance;
                    toHit = transform;
                }
            }

            if (toHit != null)
            {
                if (AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.noGravity))
                    return (toHit.position - ownPosition).normalized * startVelocity;

                return CalculateLaunchDirection(toHit.position, ownPosition, startVelocity);
            }
            else
            {
                return ownDirection * startVelocity;
            }
        }

        public static Vector3 CalculateLaunchDirection(Vector3 targetPosition, Vector3 ballPosition, float initialSpeed)
        {
            // Constants
            float gravity = Physics.gravity.y;

            // Calculate distance components
            Vector3 distance = targetPosition - ballPosition;
            float horizontalDistance = new Vector3(distance.x, 0, distance.z).magnitude;
            float verticalDistance = distance.y;

            // Calculate initial velocity components
            float speedSquared = initialSpeed * initialSpeed;
            float speedQuad = speedSquared * speedSquared;
            float g = Mathf.Abs(gravity);

            // Calculate the angle required to hit the target using the quadratic formula
            // θ = 0.5 * arcsin((g * x^2) / (v^2 * (v^2 - 2 * g * y)))
            float term1 = speedSquared * speedSquared - g * (g * horizontalDistance * horizontalDistance + 2 * verticalDistance * speedSquared);

            if (term1 < 0)
            {
                // No solution exists if term1 is negative
                return Vector3.zero;
            }

            float angle1 = Mathf.Atan((speedSquared + Mathf.Sqrt(term1)) / (g * horizontalDistance));
            float angle2 = Mathf.Atan((speedSquared - Mathf.Sqrt(term1)) / (g * horizontalDistance));

            // Select the smaller angle that hits the target
            float angle = angle1 < angle2 ? angle1 : angle2;

            // Calculate the initial velocity components
            float vx = initialSpeed * Mathf.Cos(angle);
            float vy = initialSpeed * Mathf.Sin(angle);

            // Create the velocity vector
            Vector3 launchVelocity = new Vector3(distance.x, 0, distance.z).normalized * vx;
            launchVelocity.y = vy;

            return launchVelocity;
        }

        public override bool OnImbueCollisionStart(CollisionInstance collisionInstance)
        {
            base.OnImbueCollisionStart(collisionInstance);

            if (collisionInstance.damageStruct.hitRagdollPart)
            {
                if (collisionInstance.damageStruct.damage > 0)
                {
                    Creature creature = collisionInstance.targetCollider.GetComponentInParent<Creature>();
                    if (creature != Player.currentCreature && !creature.isKilled)
                    {
                        Imbue imbue = collisionInstance.sourceColliderGroup.imbue;
                        float cold = 20 * collisionInstance.intensity * imbue.EnergyRatio * base.GetModifier(Modifier.Intensity);

                        creature.Inflict("Freezing", this, float.PositiveInfinity, cold);
                    }
                }
            }

            return true;
        }
    }
}
