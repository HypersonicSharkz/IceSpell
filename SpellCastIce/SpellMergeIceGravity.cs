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
	class SpellMergeIceGravity : SpellMergeData
	{
		public Trigger captureTrigger;
		public float bubbleMinCharge;
		public float bubbleDuration;
		public string bubbleEffectId;
		public AnimationCurve bubbleScaleCurveOverTime;
		public float bubbleEffectMaxScale;

		protected List<CollisionHandler> capturedObjects = new List<CollisionHandler>();
		protected bool bubbleActive;
		protected EffectData bubbleEffectData;


        public override void OnCatalogRefresh()
        {
            base.OnCatalogRefresh();
			if (bubbleEffectId != null && bubbleEffectId != "")
			{
				bubbleEffectData = Catalog.GetData<EffectData>(bubbleEffectId, true);
				Debug.Log("Got bubble effect DATA");
			}
		}

        public void StartCapture(float radius)
		{
			captureTrigger = new GameObject("IceTrigger").AddComponent<Trigger>();
			captureTrigger.transform.SetParent(this.mana.mergePoint);
			captureTrigger.transform.localPosition = Vector3.zero;
			captureTrigger.transform.localRotation = Quaternion.identity;
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
						if (creature != Player.currentCreature && !creature.isKilled)
						{
							if (creature.ragdoll.state != Ragdoll.State.Frozen)
							{
								if (!creature.GetComponent<IceSpellMWE>())
								{
									creature.gameObject.AddComponent<IceSpellMWE>();
								}
								IceSpellMWE scr = creature.GetComponent<IceSpellMWE>();
								scr.SlowStartCoroutine(creature, 100f, 0f, 0f, 50f);
							}
						}
						capturedObjects.Add(component);
						return;
						
					}
					else
					{
						Creature creature = component.ragdollPart.ragdoll.creature;
						if (creature.GetComponent<IceSpellMWE>())
                        {
							IceSpellMWE scr = creature.GetComponent<IceSpellMWE>();
							scr.UnFreezeCreature(creature);
						}
						capturedObjects.Remove(component);
					}
				}
			}
		}

		public override void Merge(bool active)
		{
			base.Merge(active);
			if (active)
			{
				StartCapture(0f);
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
			StopCapture();
			EffectInstance bubbleEffect = null;
			if (bubbleEffectData != null)
			{
				bubbleEffect = bubbleEffectData.Spawn(captureTrigger.transform, true, null, true, Array.Empty<Type>());
				bubbleEffect.SetIntensity(0f);
				bubbleEffect.Play(0);
			}
			yield return new WaitForFixedUpdate();
			StartCapture(0f);
			captureTrigger.transform.SetParent(null);
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
			for (int i = capturedObjects.Count - 1; i >= 0; i--)
			{
				Creature creature = capturedObjects[i].ragdollPart.ragdoll.creature;
				if (creature.GetComponent<IceSpellMWE>())
				{
					IceSpellMWE scr = creature.GetComponent<IceSpellMWE>();
					scr.UnFreezeCreature(creature);
				}
				capturedObjects.RemoveAt(i);
			}
			UnityEngine.Object.Destroy(captureTrigger.gameObject);
		}
	}
}
