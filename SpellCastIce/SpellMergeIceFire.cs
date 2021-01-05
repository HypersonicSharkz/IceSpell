using System;
using System.Collections.Generic;
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


        public override void Merge(bool active)
        {
            base.Merge(active);
            if (active)
            {
                if (effectInstance == null)
                {
                    effectInstance = effectData.Spawn(mana.mergePoint, true, Array.Empty<Type>());
                    foreach (Effect effect in effectInstance.effects)
                    {
                        if (effect.GetComponent<ParticleSystem>())
                        {
                            IceBeamCollision scr = effect.GetComponent<ParticleSystem>().gameObject.AddComponent<IceBeamCollision>();
                            scr.damage = damage;
                            scr.force = force;
                        }
                    }
                }

                playing = false;
            } else
            {
                currentCharge = 0f;
                effectInstance.Stop();
                playing = false;
            }
        }

        public override void Update()
        {
            base.Update();
            if (currentCharge > minCharge)
            {
                if (!playing)
                {
                    effectInstance.Play();
                    effectInstance.SetIntensity(1f);
                    playing = true;
                }
                
            }
        }
    }

    public class IceBeamCollision : MonoBehaviour
    {
        public float damage;
        public float force;

        private ParticleSystem particles;
        private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

        private void Awake()
        {
            particles = gameObject.GetComponent<ParticleSystem>();
        }

        private void OnParticleCollision(GameObject other)
        {
            int numCollisionEvents = particles.GetCollisionEvents(other, collisionEvents);

            foreach (ParticleCollisionEvent pce in collisionEvents)
            {
                Collider collider = pce.colliderComponent as Collider;
                if (collider)
                {
                    if (other.GetComponentInParent<Creature>())
                    {
                        Creature hitCreature = other.GetComponentInParent<Creature>();

                        if (hitCreature != Player.currentCreature)
                        {
                            if (collider.attachedRigidbody)
                            {
                                Rigidbody rb = collider.attachedRigidbody;
                                rb.AddForce(pce.velocity * force, ForceMode.Impulse);
                            }
                        }
                    }
                    else if (other.GetComponentInParent<Item>())
                    {
                        Item hitItem = other.GetComponentInParent<Item>();

                        if (collider.attachedRigidbody)
                        {
                            Rigidbody rb = collider.attachedRigidbody;
                            rb.AddForce(pce.velocity * force, ForceMode.Impulse);
                        }
                    }


                }
            }
        }
    }
}
