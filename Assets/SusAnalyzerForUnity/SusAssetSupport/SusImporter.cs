#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tea.Safu
{
	[ScriptedImporter(1, "sus")]
	public class SusImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			SusAsset sus = ScriptableObject.CreateInstance<SusAsset>();
			Texture2D icon = Resources.Load("Icons/SusAsset_Icon") as Texture2D;

			ctx.AddObjectToAsset("SusAsset", sus, icon);
			ctx.SetMainObject(sus);

			try
			{
				sus.RawText = File.ReadAllText(ctx.assetPath);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message, sus);
			}
		}
	}
}
#endif