using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.Skill.SpellMerge.SpellMergeGravity;

namespace SpellCastIce
{
	class SpellMergeIceGravity : SpellMergeData
	{
		public Trigger captureTrigger;
		public float bubbleMinCharge;

        [ModOption(category = "Ice Dome", name = "Duration", tooltip = "How long the dome will last in seconds")]
        [ModOptionFloatValues(0, 60, 1f)]
        public static float bubbleDuration = 15;

		public string bubbleEffectId;
		public AnimationCurve bubbleScaleCurveOverTime;
		public float bubbleEffectMaxScale;

		protected List<Creature> capturedCreatures = new List<Creature>();
		protected bool bubbleActive;
		protected EffectData bubbleEffectData;


        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
			if (bubbleEffectId != null && bubbleEffectId != "")
			{
				bubbleEffectData = Catalog.GetData<EffectData>(bubbleEffectId, true);
			}
		}

        public void StartCapture(float radius)
		{
			captureTrigger = new GameObject("IceTrigger").AddComponent<Trigger>();
			captureTrigger.transform.position = this.mana.mergePoint.position;
			captureTrigger.transform.rotation = this.mana.mergePoint.rotation;
            captureTrigger.SetCallback(new Trigger.CallBack(this.OnTrigger));
			captureTrigger.SetLayer(GameManager.GetLayer(LayerName.MovingItem));
			captureTrigger.SetRadius(radius);
			captureTrigger.SetActive(true);
		}

		protected void OnTrigger(Collider other, bool enter)
		{
			if (other.attachedRigidbody && !other.attachedRigidbody.isKinematic)
			{
				CollisionHandler component = other.attachedRigidbody.GetComponent<CollisionHandler>();
				if (component && component.ragdollPart && component.ragdollPart.ragdoll != mana.creature.ragdoll && bubbleActive)
				{
					if (enter)
					{
						Creature creature = component.ragdollPart.ragdoll.creature;
						if (!capturedCreatures.Contains(creature) && creature != Player.currentCreature && !creature.isKilled)
						{
                            creature.Inflict("Freezing", this, float.PositiveInfinity, 200f);
                            capturedCreatures.Add(creature);
                        }
						return;
					}
				}
			}
		}

		public override void Merge(bool active)
		{
			base.Merge(active);
			if (active || bubbleActive)
			{
				return;
			}
			Vector3 from = Player.local.transform.rotation * PlayerControl.GetHand(Side.Left).GetHandVelocity();
			Vector3 from2 = Player.local.transform.rotation * PlayerControl.GetHand(Side.Right).GetHandVelocity();
			if (from.magnitude > SpellCaster.throwMinHandVelocity && from2.magnitude > SpellCaster.throwMinHandVelocity)
			{
				if (Vector3.Angle(from, mana.casterLeft.magicSource.position - mana.mergePoint.position) < 45f || Vector3.Angle(from2, mana.casterRight.magicSource.position - mana.mergePoint.position) < 45f)
				{
					if (currentCharge > bubbleMinCharge)
					{
						mana.StopCoroutine("BubbleCoroutine");
						mana.StartCoroutine(BubbleCoroutine(bubbleDuration));
					}
				}
			}
		}

		protected IEnumerator BubbleCoroutine(float duration)
		{
			bubbleActive = true;
			capturedCreatures.Clear();

			StartCapture(0f);
			captureTrigger.transform.SetParent(null);

			EffectInstance bubbleEffect = null;
			if (bubbleEffectData != null)
			{
				bubbleEffect = bubbleEffectData.Spawn(captureTrigger.transform);
				bubbleEffect.SetIntensity(0f);
				bubbleEffect.Play(0);
			}
			yield return new WaitForFixedUpdate();
			
			float startTime = Time.time;
			while (Time.time - startTime < duration)
			{
				if (!captureTrigger)
				{
					yield break;
				}
                float num = bubbleScaleCurveOverTime.Evaluate((Time.time - startTime) / duration);
                captureTrigger.SetRadius(num * bubbleEffectMaxScale * 0.5f);
				if (bubbleEffect != null)
				{
					bubbleEffect.SetIntensity(num);
				}
				yield return null;
			}
			if (bubbleEffect != null)
			{
				bubbleEffect.End(false, -1f);
			}
			StopCapture();
			bubbleActive = false;
			yield break;
		}

		public void StopCapture()
		{
			captureTrigger.SetActive(false);
			UnityEngine.Object.Destroy(captureTrigger.gameObject);
		}
	}
}
