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
            foreach (CollisionHandler collisionHandler in item.collisionHandlers)
            {
                if (AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.noGravity))
                {
                    collisionHandler.SetPhysicModifier(this, 0, 1, 0, 0, -1, null);
                    //item.rb.useGravity = false;
                }
                //collisionHandler.OnCollisionStartEvent += CollisionHandler_OnCollisionStartEvent;
            }
            

            if (!AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.pickUpIceSpikes))
            {
                Handle h = item.handles[0];
                h.enabled = false;
                h.gameObject.SetActive(false);

                item.handles.Clear();
            }

            spawnTime = Time.time;

            item.OnUngrabEvent += Item_OnUngrabEvent1;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent1; ;
        }

        private void Item_OnTelekinesisGrabEvent1(Handle handle, ThunderRoad.Skill.SpellPower.SpellTelekinesis teleGrabber)
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
                    item.GetComponentInChildren<Animation>().Play();
                    despawning = true;
                    item.Despawn(1f);
                }
            } 
        }
    }

    
    public class IceSpellMWE : MonoBehaviour
    {
        Color color = new Color(.49f, .78f, 1);
        public EffectInstance effectInstance;

        private bool playing = false;

        public void TrySlow(Creature targetCreature, float energy, float maxSlow, float minSlow, float duration)
        {
            if (playing)
                return;

            StartCoroutine(SlowCoroutine(targetCreature, energy, maxSlow, minSlow, duration));
        }

        IEnumerator SlowCoroutine(Creature targetCreature, float energy, float maxSlow, float minSlow, float duration)
        {
            EffectData imbueHitRagdollEffectData = Catalog.GetData<EffectData>("ImbueIceRagdoll", true);
            effectInstance = imbueHitRagdollEffectData.Spawn(targetCreature.ragdoll.rootPart.transform);
            effectInstance.SetRenderer(targetCreature.GetRendererForVFX(), false);
            effectInstance.Play(0);
            effectInstance.SetIntensity(1f);
            playing = true;

            float animSpeed = Mathf.Lerp(minSlow, maxSlow, energy / 100);

            if (animSpeed != 0)
            {
                targetCreature.animator.speed *= (animSpeed / 100);
                targetCreature.locomotion.SetSpeedModifier(this, (animSpeed / 100), (animSpeed / 100), (animSpeed / 100), (animSpeed / 100), (animSpeed / 100));
            } else
            {
                targetCreature.ragdoll.SetState(Ragdoll.State.Frozen);
                targetCreature.brain.AddNoStandUpModifier(this);

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
                targetCreature.locomotion.ClearSpeedModifiers();
            } else
            {
                if (!targetCreature.isKilled)
                {
                    targetCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    targetCreature.brain.RemoveNoStandUpModifier(this);

                    targetCreature.brain.Load(targetCreature.brain.instance.id);
                }
            }


            playing = false;
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
                targetCreature.brain.RemoveNoStandUpModifier(this);

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
