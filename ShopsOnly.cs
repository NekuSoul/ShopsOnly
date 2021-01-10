using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace NekuSoul.ShopsOnly
{
	[BepInDependency("com.bepis.r2api")]
	[BepInPlugin("de.NekuSoul.ShopsOnly", "ShopsOnly", "0.4.1")]
	[R2APISubmoduleDependency("LoadoutAPI", "ResourcesAPI")]
	public class ShopsOnly : BaseUnityPlugin
	{
		private static readonly string[] ReplacedChoices = { "Chest1", "Chest2", "CategoryChestDamage", "CategoryChestHealing", "CategoryChestUtility" };

		private readonly ArtifactDef _artifact = ScriptableObject.CreateInstance<ArtifactDef>();

		private readonly Sprite _enabledTexture = LoadSprite(Nekusoul.ShopsOnly.Properties.Resources.enabled);
		private readonly Sprite _disabledTexture = LoadSprite(Nekusoul.ShopsOnly.Properties.Resources.disabled);

		public void Awake()
		{
			On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
			On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;

			_artifact.nameToken = "Artifact of Multishop Terminals";
			_artifact.descriptionToken = "Regular chests are replaced with Multishop Terminals.";
			_artifact.smallIconDeselectedSprite = _disabledTexture;
			_artifact.smallIconSelectedSprite = _enabledTexture;
			ArtifactCatalog.getAdditionalEntries += list => list.Add(_artifact);
		}

		private void MultiShopController_CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController self)
		{
			if (!RunArtifactManager.instance.IsArtifactEnabled(_artifact))
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
			if (!RunArtifactManager.instance.IsArtifactEnabled(_artifact))
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

		static Sprite LoadSprite(byte[] resourceBytes)
		{
			//Check to make sure that the byte array supplied is not null, and throw an appropriate exception if they are.
			if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));

			//Create a temporary texture, then load the texture onto it.
			var tempTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
			tempTex.LoadImage(resourceBytes, false);

			return Sprite.Create(tempTex, new Rect(0, 0, tempTex.width, tempTex.height), new Vector2(0.5f, 0.5f)); ;
		}
	}
}