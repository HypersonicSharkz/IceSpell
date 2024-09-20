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
    class SpellMergeIce : SpellMergeData
    {
        [ModOption(category = "Ice Nova", name = "Min Spike Count", tooltip = "The amount of spikes to throw when casting Ice Nova at minimum charge")]
        [ModOptionIntValues(1, 100, 1)]
        public static int minSpawnAmount = 4;

        [ModOption(category = "Ice Nova", name = "Max Spike Count", tooltip = "The amount of spikes to throw when casting Ice Nova at full charge")]
        [ModOptionIntValues(1, 100, 1)]
        public static int maxSpawnAmount = 16;

        [ModOption(category = "Ice Nova", name = "Min Charge", tooltip = "The minimum charge for casting Ice Nova")]
        [ModOptionFloatValues(0, 1, 0.1f)]
        public static float shotMinCharge = 0.2f;

        [ModOption(category = "Ice Nova", name = "Spike Force", tooltip = "The speed of the spikes thrown by casting Ice Nova")]
        [ModOptionFloatValues(0, 200, 1f)]
        public static float novaForce = 35f;

        public string shotEffectId;

        protected EffectData shotEffectData;

        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
            shotEffectData = Catalog.GetData<EffectData>(shotEffectId, true);
        }

        public override void Merge(bool active)
        {
            base.Merge(active);
            if (!active)
            {
                Vector3 leftVel = Player.local.transform.rotation * PlayerControl.GetHand(Side.Left).GetHandVelocity();
                Vector3 rightVel = Player.local.transform.rotation * PlayerControl.GetHand(Side.Right).GetHandVelocity();
                if (leftVel.magnitude > SpellCaster.throwMinHandVelocity && rightVel.magnitude > SpellCaster.throwMinHandVelocity)
                {
                    if (Vector3.Angle(leftVel, mana.casterLeft.magicSource.position - mana.mergePoint.position) < 45f || Vector3.Angle(rightVel, mana.casterRight.magicSource.position - mana.mergePoint.position) < 45f)
                    {
                        if (currentCharge >= shotMinCharge)
                        {
                            FireSpikes();
                        }
                    }
                }
            }
        }

        private void FireSpikes()
        {
            EffectInstance effectInstance;
            effectInstance = shotEffectData.Spawn(mana.mergePoint, true);

            effectInstance.SetIntensity(0f);
            effectInstance.Play(0);

            int spikeAmount = Mathf.RoundToInt(Mathf.Clamp(maxSpawnAmount * currentCharge, minSpawnAmount, maxSpawnAmount));

            List<Item> spikes = new List<Item>();

            for (int i = 1; i <= spikeAmount; i++)
            {
                Catalog.GetData<ItemData>("IceSpike", true).SpawnAsync(delegate (Item iceSpike){
                    iceSpike.transform.position = mana.mergePoint.position;

                    iceSpike.transform.rotation = Quaternion.Euler(0, (360 / spikeAmount) * spikes.Count, 0);

                    foreach (Item item in spikes)
                    {
                        item.IgnoreObjectCollision(iceSpike);
                    }
                    iceSpike.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                    iceSpike.physicBody.AddForce(iceSpike.transform.forward * novaForce, ForceMode.Impulse);
                    iceSpike.Throw(1f, Item.FlyDetection.Forced);

                    spikes.Add(iceSpike);
                }, null, null, null, false, null);
            }

            currentCharge = 0;
        }
    }
}
