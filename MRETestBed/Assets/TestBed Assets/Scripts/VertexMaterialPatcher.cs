﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MWAssets = MixedRealityExtension.Assets;
using MixedRealityExtension.Patching.Types;

using MixedRealityExtension.API;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VertexMaterialPatcher : MixedRealityExtension.Factories.DefaultMaterialPatcher
{
	private static int EmissiveColorProp = Shader.PropertyToID("_EmissiveColor");
	private static int EmissiveTexProp = Shader.PropertyToID("_EmissiveTex");

	protected Dictionary<int, Guid> emissiveTextureAssignments = new Dictionary<int, Guid>(20);

	public override void ApplyMaterialPatch(Material material, MWAssets.Material patch)
	{
		base.ApplyMaterialPatch(material, patch);

		if (patch.EmissiveColor != null)
		{
			var color = material.GetColor(EmissiveColorProp);
			color.r = patch.EmissiveColor.R ?? color.r;
			color.g = patch.EmissiveColor.G ?? color.g;
			color.b = patch.EmissiveColor.B ?? color.b;
			color.a = patch.EmissiveColor.A ?? color.a;
			material.SetColor(EmissiveColorProp, color);
		}

		if (patch.EmissiveTextureOffset != null)
		{
			var offset = material.GetTextureOffset(EmissiveTexProp);
			offset.x = patch.EmissiveTextureOffset.X ?? offset.x;
			offset.y = patch.EmissiveTextureOffset.Y ?? offset.y;
			material.SetTextureOffset(EmissiveTexProp, offset);
		}

		if (patch.EmissiveTextureScale != null)
		{
			var scale = material.GetTextureScale(EmissiveTexProp);
			scale.x = patch.EmissiveTextureScale.X ?? scale.x;
			scale.y = patch.EmissiveTextureScale.Y ?? scale.y;
			material.SetTextureScale(EmissiveTexProp, scale);
		}

		if (patch.EmissiveTextureId != null)
		{
			var textureId = patch.EmissiveTextureId.Value;
			emissiveTextureAssignments[material.GetInstanceID()] = textureId;
			if (textureId == Guid.Empty)
			{
				material.SetTexture(EmissiveTexProp, null);
			}
			else
			{
				MREAPI.AppsAPI.AssetCache.OnCached(textureId, tex =>
				{
					if (!material || emissiveTextureAssignments[material.GetInstanceID()] != textureId) return;
					material.SetTexture(EmissiveTexProp, (Texture)tex);
				});
			}
		}

		if (patch.AlphaCutoff != null)
		{
			material.SetFloat("_AlphaCutoff", patch.AlphaCutoff.Value);
		}

		switch (patch.AlphaMode)
		{
			case MWAssets.AlphaMode.Opaque:
				material.renderQueue = (int)RenderQueue.Geometry;
				material.SetOverrideTag("RenderMode", "Opaque");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 0);
				break;
			case MWAssets.AlphaMode.Mask:
				material.renderQueue = (int)RenderQueue.AlphaTest;
				material.SetOverrideTag("RenderMode", "TransparentCutout");
				material.SetInt("_ZWrite", 1);
				material.SetInt("_SrcBlend", (int)BlendMode.One);
				material.SetInt("_DstBlend", (int)BlendMode.Zero);
				material.SetInt("_ShouldCutout", 1);
				break;
			case MWAssets.AlphaMode.Blend:
				material.renderQueue = (int)RenderQueue.Transparent;
				material.SetOverrideTag("RenderMode", "Transparent");
				material.SetInt("_ZWrite", 0);
				material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ShouldCutout", 0);
				break;
			// ignore default case, i.e. null
		}
	}

	public override MWAssets.Material GeneratePatch(Material material)
	{
		var patch = base.GeneratePatch(material);

		var unityColor = material.GetColor(EmissiveColorProp);
		patch.EmissiveColor = new ColorPatch()
		{
			R = unityColor.r,
			G = unityColor.g,
			B = unityColor.b,
			A = unityColor.a
		};

		patch.EmissiveTextureId = MREAPI.AppsAPI.AssetCache.GetId(material.GetTexture(EmissiveTexProp));
		patch.EmissiveTextureOffset = new Vector2Patch(material.GetTextureOffset(EmissiveTexProp));
		patch.EmissiveTextureScale = new Vector2Patch(material.GetTextureScale(EmissiveTexProp));

		patch.AlphaCutoff = material.GetFloat("_AlphaCutoff");
		patch.AlphaMode =
			material.renderQueue == (int)RenderQueue.Transparent ? MWAssets.AlphaMode.Blend :
			material.renderQueue == (int)RenderQueue.AlphaTest ? MWAssets.AlphaMode.Mask :
			MWAssets.AlphaMode.Opaque;

		return patch;
	}
}
