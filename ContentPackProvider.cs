using System;
using System.Collections;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace Nekusoul.ShopsOnly
{
	class ContentPackProvider : IContentPackProvider
	{
		public string identifier => "de.NekuSoul.ShopsOnly";

		public ArtifactDef Artifact { get; private set; }
		public Sprite EnabledTexture { get; private set; }
		public Sprite DisabledTexture { get; private set; }

		public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
		{
			EnabledTexture = LoadSprite(Properties.Resources.enabled);
			args.ReportProgress(0.5f);
			yield return null;

			DisabledTexture = LoadSprite(Properties.Resources.disabled);
			args.ReportProgress(1f);
		}

		public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
		{
			Artifact = ScriptableObject.CreateInstance<ArtifactDef>();
			Artifact.nameToken = "Artifact of Options";
			Artifact.descriptionToken = "Regular chests are replaced with Multishop Terminals.";
			Artifact.smallIconDeselectedSprite = DisabledTexture;
			Artifact.smallIconSelectedSprite = EnabledTexture;

			args.output.artifactDefs.Add(new[] { Artifact });
			args.ReportProgress(1f);
			yield break;
		}

		public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
		{
			args.ReportProgress(1f);
			yield break;
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
