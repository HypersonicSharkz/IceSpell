using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace SpellCastIce.Statuss
{
    internal class FreezingStatus : Status
    {
        [ModOption(category = "Freezing Status", name = "Thawing Speed", tooltip = "How fast creatures will thaw after being frozen")]
        [ModOptionFloatValues(0, 100, 1)]
        public static float thawingSpeed = 5;

        [ModOption(category = "Freezing Status", name = "Max Cold", tooltip = "Maxium cold status")]
        [ModOptionFloatValues(100, 400, 1)]
        public static float maxCold = 150;

        [ModOption(category = "Freezing Status", name = "Explosion Force", tooltip = "The explosive force applied when using fire on a frozen enemy")]
        [ModOptionFloatValues(0, 200, 1)]
        public static float frostExplosionForce = 10;

        /*
        [ModOption(category = "Freezing Status", name = "R", tooltip = "R")]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float r = 0.2f;

        [ModOption(category = "Freezing Status", name = "G", tooltip = "G")]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float g = 0.6f;

        [ModOption(category = "Freezing Status", name = "B", tooltip = "B")]
        [ModOptionFloatValues(0, 1, 0.01f)]
        public static float b = 1f;
        */

        protected new StatusDataFreezing data;

        private Color frozenColor = new Color(0.3f, 0.7f, 1f, 1);
        private Color baseSkinColor;

        public bool IsFrozen
        {
            get
            {
                return (entity as Creature).ragdoll.state == Ragdoll.State.Frozen;
            }
        }

        //Should completely freeze at 100%
        public bool ShouldFreeze
        {
            get => Cold >= 100;
        }

        public float Cold
        {
            get
            {
                object value = this.value;
                if (value is float)
                {
                    return (float)value;
                }
                return 0f;
            }
            set
            {
                entity.SetVariable<float>("Cold", value);
                OnValueChange();
            }
        }

        protected override object GetValue()
        {
            return entity.GetVariable<float>("Cold");
        }

        public override void Spawn(StatusData data, ThunderEntity entity)
        {
            base.Spawn(data, entity);
            this.data = (data as StatusDataFreezing);
            if (entity is Creature creature) 
                baseSkinColor = creature.GetColor(Creature.ColorModifier.Skin);
        }

        public override bool AddHandler(object handler, float duration = float.PositiveInfinity, object parameter = null, bool playEffect = true)
        {
            if (parameter is float)
            {
                float cold = (float)parameter;
                AddCold(cold, true);
                duration = float.PositiveInfinity;
                return base.AddHandler(handler, duration, parameter, playEffect);
            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (Cold <= 0)
            {
                Despawn();
                return;
            }

            float speed = Mathf.Clamp(1f - (Cold / 100f), 0, 1);
            
            Golem golem = this.entity as Golem;
            if (golem != null)
            {
                golem.speed.Add(this, speed);
            }

            Creature creature = this.entity as Creature;

            if (creature == null)
                return;

            float heat = entity.GetVariable<float>("Heat");

            if (IsFrozen && !ShouldFreeze)
            {
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.brain.RemoveNoStandUpModifier(this);
            } 
            else if (!IsFrozen && ShouldFreeze)
            {
                creature.ragdoll.SetState(Ragdoll.State.Frozen);
                creature.brain.AddNoStandUpModifier(this);
                AddCold(50, false);
            }

            creature.locomotion.SetAllSpeedModifiers(this, speed);
            creature.animator.speed = speed;
            creature.SetColor(Color.Lerp(baseSkinColor, frozenColor, easeInExpo(speed)), Creature.ColorModifier.Skin, true);
            BrainModuleSpeak module = creature.brain.instance.GetModule<BrainModuleSpeak>(false);
            if (module != null)
            {
                module.SetPitchMultiplier(speed);
            }
            

            //Thawing
            //take burning status into account as well
            //On fire should stop frezzing totally
            AddCold(-thawingSpeed * Time.deltaTime, true);

            if (heat > 20)
            {
                AddCold(-heat * Time.deltaTime, true);
                if (IsFrozen && AbilityManager.IsAbilityUnlocked(AbilityManager.AbilitiesEnum.ExplosiveFrost))
                {
                    ExplodeCreature(creature);
                } 
            }
        }

        public override void FullRemove()
        {
            base.FullRemove();
            Cold = 0;
            Golem golem = this.entity as Golem;
            if (golem != null)
            {
                golem.speed.Remove(this);
            }
            Creature creature = this.entity as Creature;
            if (creature == null || creature.isKilled)
            {
                return;
            }

            if (IsFrozen)
            {
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                creature.brain.RemoveNoStandUpModifier(this);
            }

            creature.locomotion.RemoveSpeedModifier(this);
            creature.animator.speed = 1f;
            BrainModuleSpeak module = creature.brain.instance.GetModule<BrainModuleSpeak>(true);
            if (module != null)
            {
                module.ClearPitchMultiplier();
            }
        }

        public void ExplodeCreature(Creature creature)
        {
            creature.Kill();

            foreach (RagdollPart part in creature.ragdoll.parts)
            {
                if (!part.sliceAllowed)
                    continue;

                part.SafeSlice();
            }
            creature.AddExplosionForce(frostExplosionForce, creature.Center, 1, 1, ForceMode.Impulse);

            Cold = 0;
        }

        public void AddCold(float cold, bool onValueChange = true)
        {
            entity.SetVariable("Cold", (float current) => Mathf.Clamp(current + cold, 0, maxCold));
            if (onValueChange)
            {
                OnValueChange();
            }
        }

        public float easeInExpo(float x) 
        {
            return x == 0 ? 1 : Mathf.Pow(4f, -6f * x);
        }
    }
}
