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
        public float damage;
        public float force;

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
            effectInstance = effectData.Spawn(mana.mergePoint.position, mana.mergePoint.rotation);
            effectInstance.Play();

            await Task.Delay(2300);

            Collider[] hit = Physics.OverlapCapsule(mana.mergePoint.position, mana.mergePoint.position + (mana.mergePoint.forward * 8f), 0.7f);
            
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
                                creature.ragdoll.SetState(Ragdoll.State.Frozen);
                                collider.attachedRigidbody.AddForce(mana.mergePoint.forward * 30, ForceMode.Impulse);

                                CollisionInstance l_Dmg = new CollisionInstance(new DamageStruct(DamageType.Energy, damage));
                                creature.Damage(l_Dmg);
                            }
                        }

                    }

                }
            }

            await Task.Delay(2300);

            effectInstance.Stop();
            playing = false;
        }
    }
}
