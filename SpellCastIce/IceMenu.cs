using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

namespace SpellCastIce
{
    class IceMenuModule : MenuModule
    {
        private Text levelTxt;
        private Text xpTxt;
        private Text pointsTxt;

        private Button unlockBTN;

        private Text abilityTitle;
        private Text abilityDescription;
        private Text cost;

        private IceManager.AbilitiesEnum selectedAbility;

        private Menu menu;

        public override void Init(MenuData menuData, Menu menu)
        {
            base.Init(menuData, menu);
            levelTxt = menu.GetCustomReference("Level").GetComponent<Text>();
            xpTxt = menu.GetCustomReference("XP").GetComponent<Text>();
            pointsTxt = menu.GetCustomReference("Points").GetComponent<Text>();
            
            unlockBTN = menu.GetCustomReference("AbilityUnlock").GetComponent<Button>();

            abilityTitle = menu.GetCustomReference("AbilityTitle").GetComponent<Text>();
            abilityDescription = menu.GetCustomReference("AbilityDescription").GetComponent<Text>();
            cost = menu.GetCustomReference("Cost").GetComponent<Text>();

            this.menu = menu;

            SetUpSkillButton(IceManager.AbilitiesEnum.iceSpikeAim);
            SetUpSkillButton(IceManager.AbilitiesEnum.pickUpIceSpikes);
            SetUpSkillButton(IceManager.AbilitiesEnum.noGravity);
            SetUpSkillButton(IceManager.AbilitiesEnum.IceImbue);
            SetUpSkillButton(IceManager.AbilitiesEnum.IceMergeIce);
            SetUpSkillButton(IceManager.AbilitiesEnum.IceMergeFire);

            unlockBTN.onClick.AddListener(delegate
            {
                if (IceManager.UnlockAbility(selectedAbility))
                {
                    ReloadUnlocks();
                } 
            });
        }

        public override void OnShow(bool show)
        {
            base.OnShow(show);

            menuData.page2.gameObject.SetActive(false);


            ReloadUnlocks();
            
        }

        private void ReloadUnlocks()
        {
            levelTxt.text = IceManager.level.ToString();
            xpTxt.text = IceManager.xp.ToString("0.0") + " / " + IceManager.XpForNextLevel(IceManager.level).ToString();
            pointsTxt.text = IceManager.levelPoints.ToString();

            foreach (KeyValuePair<IceManager.AbilitiesEnum, IceManager.Ability> kvp in IceManager.abilityDict)
            {
                SetUnlockedState(kvp.Value.customRefName, kvp.Key);
            }
        }

        public void SetUnlockedState(string customRefName, IceManager.AbilitiesEnum ab)
        {
            Image uImg = menu.GetCustomReference(customRefName).GetComponentInChildren<Image>();

            IceManager.Ability ability;

            IceManager.abilityDict.TryGetValue(ab, out ability);
            if (ability.unlocked)
            {
                uImg.color = Color.green;
            } else
            {
                uImg.color = Color.red;
            }
        }

        private void SetUpSkillButton(IceManager.AbilitiesEnum abilitiesEnum)
        {
            IceManager.Ability ability;
            IceManager.abilityDict.TryGetValue(abilitiesEnum, out ability);

            menu.GetCustomReference(ability.customRefName).GetComponentInChildren<Button>().onClick.AddListener(delegate 
            {
                LoadAbilityPage(ability.uiTitle, ability.uiDescript, ability.levelPointCost, abilitiesEnum);
            });
        }

        private void LoadAbilityPage(string abilityName, string abilityDsc, int cost, IceManager.AbilitiesEnum abilitiesEnum)
        {
            menuData.page2.gameObject.SetActive(true);

            abilityTitle.text = abilityName;
            abilityDescription.text = abilityDsc;
            this.cost.text = cost.ToString();

            if (IceManager.IsAbilityUnlocked(abilitiesEnum))
            {
                this.unlockBTN.interactable = false;
                this.unlockBTN.GetComponentInChildren<Text>().text = "Owned";
            }
            else
            {
                this.unlockBTN.interactable = true;
                this.unlockBTN.GetComponentInChildren<Text>().text = "Unlock";
            }
                

            selectedAbility = abilitiesEnum;
        }
    }
}
