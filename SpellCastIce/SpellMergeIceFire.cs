using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using System.Collections;

namespace SpellCastIce
{
    class SpellMergeIceFire : SpellMergeData
    {
        public float minCharge;
        public string beamEffectId;
        [ModOption(category = "Firery Ice Beam", name = "Beam Damage", tooltip = "Damage dealt to enemies hit by the beam")]
        [ModOptionFloatValues(0,100, 5f)]
        public static float damage = 5f;

        [ModOption(category = "Firery Ice Beam", name = "Beam Force", tooltip = "Force added to enemies hit by the beam")]
        [ModOptionFloatValues(0, 500, 5f)]
        public static float force = 200f;

        private EffectData effectData;
        private EffectInstance effectInstance;

        public bool playing;


        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();

            if (beamEffectId != null && beamEffectId != "")
            {
                effectData = Catalog.GetData<EffectData>(beamEffectId, true);
            }
        }

        public override void Update()
        {
            base.Update();
            if (currentCharge > minCharge && !playing)
            {
                currentCharge = 0;
                Fire();

            } else if (playing)
            {
                currentCharge = 0;
            }
        }

        public async void Fire()
        {
            playing = true;
            Vector3 pos = mana.mergePoint.position;
            Vector3 frwd = mana.mergePoint.forward;
            effectInstance = effectData.Spawn(pos, mana.mergePoint.rotation);
            
            effectInstance.Play();

            await Task.Delay(2000);

            Collider[] hit = Physics.OverlapCapsule(pos, pos + (frwd * 10f), 1.5f);

            HashSet<Creature> hitCreatures = new HashSet<Creature>();

            foreach (Collider collider in hit)
            {
                if (collider.attachedRigidbody)
                {
                    if (collider.GetComponentInParent<Creature>())
                    {
                        Creature creature = collider.GetComponentInParent<Creature>();

                        if (!creature.isPlayer)
                        {
                            if (!creature.isKilled)
                            {
                                hitCreatures.Add(creature);
                            }
                        }

                    }

                }
            }

            foreach (Creature creature in hitCreatures)
            {
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.brain.AddNoStandUpModifier(this);
                creature.ragdoll.rootPart.physicBody.AddForce(frwd * force, ForceMode.VelocityChange);

                creature.Inflict("Freezing", this, float.PositiveInfinity, 200f);

                CollisionInstance l_Dmg = new CollisionInstance(new DamageStruct(DamageType.Energy, damage));
                creature.Damage(l_Dmg);
            }

            await Task.Delay(2300);

            foreach (Creature creature in hitCreatures)
            {
                creature.brain.RemoveNoStandUpModifier(this);
            }

            effectInstance.Stop();
            playing = false;
        }
    }
}
