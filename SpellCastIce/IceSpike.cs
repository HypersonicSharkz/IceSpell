using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace SpellCastIce
{
    public class IceSpikeModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            Debug.Log("Pls ffs");
            IceSpikeItem scr = item.gameObject.AddComponent<IceSpikeItem>();
            scr.item = item;
            scr.module = this;
            scr.Initialize();
        } 
    }

    public class IceSpikeItem : MonoBehaviour
    {
        public Item item;
        public IceSpikeModule module;

        private float spawnTime;
        private bool despawning;

        public void Initialize()
        {
            Debug.Log("Spike init");

            foreach (CollisionHandler collisionHandler in item.collisionHandlers)
            {
                if (IceManager.IsAbilityUnlocked(IceManager.AbilitiesEnum.noGravity))
                {
                    collisionHandler.SetPhysicModifier(this, 0, 0f, 1f, -1f, -1f, null);
                }
                //collisionHandler.OnCollisionStartEvent += CollisionHandler_OnCollisionStartEvent;
            }
            

            if (!IceManager.IsAbilityUnlocked(IceManager.AbilitiesEnum.pickUpIceSpikes))
            {
                Handle h = item.handles[0];
                h.enabled = false;
                h.gameObject.SetActive(false);

                item.handles.Clear();
            }

            spawnTime = Time.time;

            item.OnUngrabEvent += Item_OnUngrabEvent1;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;
        }

        private void Item_OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            spawnTime = Time.time;
        }

        private void Item_OnUngrabEvent1(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - spawnTime > 5f && !despawning)
            {
                if (!item.IsHanded() && !item.isTelekinesisGrabbed)
                {
                    despawning = true;
                    item.Despawn();
                }
            } 
        }

        /*
        public static void CollisionHandler_OnCollisionStartEvent(ref CollisionStruct collisionInstance)
        {
            if (collisionInstance.damageStruct.hitRagdollPart)
            {
                if (!collisionInstance.damageStruct.hitRagdollPart.ragdoll.creature.isKilled)
                {
                    IceManager.GainXP(IceManager.XPGains.Hit);

                    if (collisionInstance.damageStruct.hitRagdollPart.type == RagdollPart.Type.Head)
                    {
                        IceManager.GainXP(IceManager.XPGains.HeadShot);
                    }

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
                                scr.SlowStartCoroutine(creature, 100f, 0f, 0f, 8f);
                            }
                        }

                        if (creature.currentHealth - collisionInstance.damageStruct.damage <= 0)
                        {
                            IceManager.GainXP(IceManager.XPGains.Kill);
                        }
                    }
                }
            }

        }*/
    }

    public class IceSpellMWE : MonoBehaviour
    {
        Color color = new Color(.49f, .78f, 1);
        public EffectInstance effectInstance;

        public void SlowStartCoroutine(Creature targetCreature, float energy, float maxSlow, float minSlow, float duration)
        {
            StartCoroutine(SlowCoroutine(targetCreature, energy, maxSlow, minSlow, duration));
        }

        IEnumerator SlowCoroutine(Creature targetCreature, float energy, float maxSlow, float minSlow, float duration)
        {

            EffectData imbueHitRagdollEffectData = Catalog.GetData<EffectData>("ImbueIceRagdoll", true);
            effectInstance = imbueHitRagdollEffectData.Spawn(targetCreature.ragdoll.rootPart.transform, true, Array.Empty<Type>());
            effectInstance.SetRenderer(targetCreature.GetRendererForVFX(), false);
            effectInstance.Play(0);
            effectInstance.SetIntensity(1f);

            float animSpeed = Mathf.Lerp(minSlow, maxSlow, energy / 100);

            if (animSpeed != 0)
            {
                targetCreature.animator.speed *= (animSpeed / 100);
                targetCreature.locomotion.speed *= (animSpeed / 100);
            } else
            {
                targetCreature.ragdoll.SetState(Ragdoll.State.Frozen);
                targetCreature.ragdoll.AddNoStandUpModifier(this);

                targetCreature.brain.Stop();
            }
            

            /*
            targetCreature.animator.speed *= (animSpeed / 100);
            targetCreature.locomotion.speed *= (animSpeed / 100);
            */



            yield return new WaitForSeconds(duration);

            /*
            targetCreature.animator.speed = 1;
            targetCreature.locomotion.speed = targetCreature.data.locomotionSpeed;



            if (!targetCreature.brain.instance.isActive)
            {
                targetCreature.brain.instance.Start();
            }*/


            if (animSpeed != 0)
            {
                targetCreature.animator.speed = 1;
                targetCreature.locomotion.speed = targetCreature.data.locomotionSpeed;
            } else
            {
                if (!targetCreature.isKilled)
                {
                    targetCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    targetCreature.ragdoll.RemoveNoStandUpModifier(this);

                    targetCreature.brain.Load(targetCreature.brain.instance.id);
                }
            }



            effectInstance.Despawn();

            /*
            targetCreature.umaCharacter.umaDCS.SetColor("Skin", defaultSkinColor, default(Color), 0, true);
            */


        }

        public void UnFreezeCreature(Creature targetCreature)
        {

            if (!targetCreature.isKilled)
            {
                targetCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                targetCreature.ragdoll.RemoveNoStandUpModifier(this);

                targetCreature.brain.Load(targetCreature.brain.instance.id);
            }
            

            effectInstance.Despawn();
        }

        IEnumerator SetColorOfMat(Material material, float duration)
        {
            Color defaultColor = material.color;

            material.SetColor("_BaseColor", color);
            yield return new WaitForSeconds(duration);
            material.SetColor("_BaseColor", defaultColor);
        }
    }
}
