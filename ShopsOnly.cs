using System.Linq;
using System.Reflection;
using BepInEx;
using RoR2;
using UnityEngine;

namespace NekuSoul.ShopsOnly
{
	[BepInDependency("com.bepis.r2api")]

	[BepInPlugin("de.NekuSoul.ShopsOnly", "ShopsOnly", "0.3.0")]
	public class ShopsOnly : BaseUnityPlugin
	{
		private static readonly string[] replacedChoices = { "Chest1", "Chest2", "CategoryChestDamage", "CategoryChestHealing", "CategoryChestUtility" };

		public void Awake()
		{
			On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
			On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
		}

		private void MultiShopController_CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController self)
		{
			if (self.doEquipmentInstead)
			{
				orig(self);
				return;
			}

			var rng = (Xoroshiro128Plus)typeof(MultiShopController).GetField("rng", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(self);

			if (rng == null)
				return;

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

			var terminalGameObjects = (GameObject[])typeof(MultiShopController).GetField("terminalGameObjects", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(self);

			if (terminalGameObjects != null)
				foreach (var terminalGameObject in terminalGameObjects)
				{
					var purchaseInteraction = terminalGameObject.GetComponent<PurchaseInteraction>();
					purchaseInteraction.Networkcost = self.Networkcost;
				}

			orig(self);
		}

		private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
		{
			var weightedSelection = orig(self);

			var chestChance = 0f;

			for (var i = 0; i < weightedSelection.Count; i++)
			{
				var choiceInfo = weightedSelection.GetChoice(i);
				var prefabName = choiceInfo.value.spawnCard.prefab.name;

				if (!replacedChoices.Contains(prefabName))
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