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
        [ModOption(category = "Ice Shurikens", name = "Fire Rate", tooltip = "Shurikens per second")]
        [ModOptionFloatValues(0, 50, 1f)]
        public static float fireRate = 5;

        [ModOption(category = "Ice Shurikens", name = "Shuriken Speed", tooltip = "How fast the shurikens move")]
        [ModOptionFloatValues(0, 100, 1f)]
        public static float shotSpeed = 25;

        private bool activated;
        private float lastShotTime;

        private ItemData shuriken;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            shuriken = Catalog.GetData<ItemData>("IceShuriken");
        }

        public override IEnumerator OnCatalogRefreshCoroutine()
        {
            return base.OnCatalogRefreshCoroutine();
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
            if (currentCharge < minCharge)
                return;

            if (activated && Time.time - lastShotTime > 1/fireRate)
            {
                lastShotTime = Time.time;
                shuriken.SpawnAsync(delegate (Item item)
                {
                    foreach (CollisionHandler collisionHandler in item.collisionHandlers)
                    {
                        collisionHandler.SetPhysicModifier(this, 0, 1);
                    }

                    item.physicBody.useGravity = false;
                    item.physicBody.AddForce(item.transform.forward * shotSpeed, ForceMode.Impulse);

                    item.gameObject.AddComponent<ShurikenItem>().item = item;
                    item.IgnoreRagdollCollision(mana.creature.ragdoll);
                    item.Throw(1f, Item.FlyDetection.Forced);

                }, mana.mergePoint.position, Quaternion.LookRotation((mana.casterLeft.magicSource.transform.up + mana.casterRight.magicSource.transform.up)));
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
                creature.TryElectrocute(1f, 0.2f, true, false, Catalog.GetData<EffectData>("ImbueLightningRagdoll", true));
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
