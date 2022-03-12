using BepInEx;
using Nekusoul.ShopsOnly;
using RoR2;
using RoR2.ContentManagement;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NekuSoul.ShopsOnly
{
    [BepInPlugin("de.NekuSoul.ShopsOnly", "ShopsOnly", "3.0.2")]
    public class ShopsOnly : BaseUnityPlugin
    {
        private static readonly string[] ReplacedChoices = { "Chest1", "Chest2", "CategoryChestDamage", "CategoryChestHealing", "CategoryChestUtility", "TripleShopLarge" };

        private readonly ContentPackProvider _content = new ContentPackProvider();

        public void Awake()
        {
            On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
            On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(_content);
        }

        private void MultiShopController_CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(_content.Artifact))
            {
                orig(self);
                return;
            }

            if (self.doEquipmentInstead)
            {
                orig(self);
                return;
            }

            var rng = (Xoroshiro128Plus)typeof(MultiShopController).GetField("rng", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(self);

            if (rng == null)
            {
                orig(self);
                return;
            }
            
            var val = rng.RangeFloat(0, 1);

            if (val > 0.975f)
            {
                self.itemTier = ItemTier.Tier3;
                self.baseCost = 400;
            }
            else if (val > 0.8f)
            {
                self.itemTier = ItemTier.Tier2;
                self.baseCost = 50;
            }
            else
            {
                self.itemTier = ItemTier.Tier1;
                self.baseCost = 25;
            }

            self.Networkcost = Run.instance.GetDifficultyScaledCost(self.baseCost);
            
            orig(self);

            var terminalGameObjects = (GameObject[])typeof(MultiShopController)
                .GetField("_terminalGameObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self);

            foreach (var terminalGameObject in terminalGameObjects)
            {
                var shopTerminalBehavior = terminalGameObject.GetComponent<ShopTerminalBehavior>();
                shopTerminalBehavior.itemTier = self.itemTier;
                shopTerminalBehavior.dropTable = null;
                
                var purchaseInteraction = terminalGameObject.GetComponent<PurchaseInteraction>();
                purchaseInteraction.Networkcost = self.Networkcost;

            }
        }

        private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(_content.Artifact))
            {
                return orig(self);
            }

            var weightedSelection = orig(self);

            var chestChance = 0f;

            for (var i = 0; i < weightedSelection.Count; i++)
            {
                var choiceInfo = weightedSelection.GetChoice(i);
                var prefabName = choiceInfo.value.spawnCard.prefab.name;

                if (!ReplacedChoices.Contains(prefabName))
                    continue;

                chestChance += choiceInfo.weight;
                choiceInfo.weight = 0f;
                weightedSelection.ModifyChoiceWeight(i, 0);
            }

            for (var i = 0; i < weightedSelection.Count; i++)
            {
                var choiceInfo = weightedSelection.GetChoice(i);
                var prefabName = choiceInfo.value.spawnCard.prefab.name;

                if (prefabName != "TripleShop")
                    continue;

                chestChance += choiceInfo.weight;
                choiceInfo.weight += chestChance;
                weightedSelection.ModifyChoiceWeight(i, choiceInfo.weight);
            }

            return weightedSelection;
        }
    }
}