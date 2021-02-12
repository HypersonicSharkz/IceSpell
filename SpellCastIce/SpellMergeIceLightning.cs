using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace SpellCastIce
{
    class SpellMergeIceLightning : SpellMergeData
    {
        public float fireRate = 2;
        public float shotSpeed = 5;

        private bool activated;
        private float lastShotTime;

        private ItemData shuriken;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            shuriken = Catalog.GetData<ItemData>("IceShuriken");
        }

        public override void Merge(bool active)
        {
            base.Merge(active);
            if (active)
            {
                activated = true;
            }
            else
            {
                currentCharge = 0f;
                activated = false;
            }
        }

        public override void Update()
        {
            base.Update();
            if (activated && Time.time - lastShotTime > 1/fireRate)
            {
                lastShotTime = Time.time;
                shuriken.SpawnAsync(delegate (Item item)
                {
                    foreach (CollisionHandler collisionHandler in item.collisionHandlers)
                    {
                        collisionHandler.SetPhysicModifier(this, 0, 0f, 1f, -1f, -1f, null);
                    }

                    item.rb.AddForce(item.transform.forward * shotSpeed, ForceMode.Impulse);

                    item.gameObject.AddComponent<ShurikenItem>().item = item;
                    item.IgnoreRagdollCollision(mana.creature.ragdoll);
                    item.Throw(1f, Item.FlyDetection.Forced);

                }, mana.mergePoint.position, Quaternion.LookRotation((mana.casterLeft.magic.transform.up + mana.casterRight.magic.transform.up)));
            }
        }

    }

    public class ShurikenItem : MonoBehaviour
    {
        public Item item;

        void OnCollisionEnter(Collision collision)
        {
            DespawnItem();
            if (collision.gameObject.GetComponentInParent<Creature>())
            {
                Creature creature = collision.gameObject.GetComponentInParent<Creature>();
                creature.brain.TryAction(new ActionShock(1f, 0.2f, Catalog.GetData<EffectData>("ImbueLightningRagdoll", true)));
            }
        }

        void DespawnItem()
        {
            GameObject hitEf = item.GetCustomReference("Hit").gameObject;

            hitEf.transform.parent = null;
            hitEf.GetComponent<ParticleSystem>().Play();

            item.Despawn(0.1f);

            Destroy(hitEf, 1f);
        }
    }
}
