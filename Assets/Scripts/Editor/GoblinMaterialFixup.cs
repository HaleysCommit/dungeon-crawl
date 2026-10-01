using UnityEngine;
using UnityEditor;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off fix: the purchased goblin asset's material uses the Built-in Render Pipeline's
    /// Standard shader, which renders as magenta/pink in this URP project (URP doesn't recognize Built-in
    /// shaders). Converts it to Universal Render Pipeline/Lit, remapping the texture slots that changed names.</summary>
    public static class GoblinMaterialFixup
    {
        private const string MaterialPath = "Assets/goblin/Materials/goblin/M_goblin.mat";

        [MenuItem("Dungeon Crawl/Fix Goblin Material For URP")]
        public static void FixMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError("Could not find material at " + MaterialPath);
                return;
            }

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("Could not find the Universal Render Pipeline/Lit shader - is URP installed in this project?");
                return;
            }

            // Built-in Standard property names differ from URP/Lit's (_MainTex -> _BaseMap, _Color -> _BaseColor);
            // _BumpMap/_MetallicGlossMap/_OcclusionMap keep the same names in both.
            var baseMap = material.GetTexture("_MainTex");
            var baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var bumpMap = material.GetTexture("_BumpMap");
            var metallicGlossMap = material.GetTexture("_MetallicGlossMap");
            var occlusionMap = material.GetTexture("_OcclusionMap");

            material.shader = urpLit;

            if (baseMap != null)
            {
                material.SetTexture("_BaseMap", baseMap);
            }
            material.SetColor("_BaseColor", baseColor);

            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
                material.EnableKeyword("_NORMALMAP");
            }
            if (metallicGlossMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGlossMap);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Smoothness", 0.4f);
            }
            if (occlusionMap != null)
            {
                material.SetTexture("_OcclusionMap", occlusionMap);
                material.EnableKeyword("_OCCLUSIONMAP");
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("Converted " + MaterialPath + " from Built-in Standard to Universal Render Pipeline/Lit.");
        }
    }
}
