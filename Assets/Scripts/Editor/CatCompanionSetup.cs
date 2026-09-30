using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.TopDownEngine;
using DungeonCrawl.Gameplay;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup: adds a Cat companion that follows the player and casts Magic Missiles into Assets/Scenes/Dungeon.unity.</summary>
    public static class CatCompanionSetup
    {
        private const string DungeonScenePath = "Assets/Scenes/Dungeon.unity";
        private const string MagicMissilePrefabPath = "Assets/Prefabs/MagicMissile.prefab";
        private static readonly Color CatColor = new Color(0.85f, 0.55f, 0.25f);

        [MenuItem("Dungeon Crawl/Setup Cat Companion In Dungeon Scene")]
        public static void SetupCatCompanion()
        {
            var scene = EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Single);

            if (GameObject.Find("Cat") != null)
            {
                Debug.Log("Cat companion already present in Dungeon scene, skipping.");
                return;
            }

            Character player = null;
            foreach (var character in Object.FindObjectsByType<Character>(FindObjectsInactive.Exclude))
            {
                if (character.CharacterType == Character.CharacterTypes.Player)
                {
                    player = character;
                    break;
                }
            }

            if (player == null)
            {
                Debug.LogWarning("No Player character found in Dungeon scene. Add the Cat manually once a player character is present.");
                return;
            }

            // Cat root (clean uniform scale, grounded)
            var cat = new GameObject("Cat");
            cat.transform.position = player.transform.position - player.transform.forward * 1.5f;
            cat.transform.localScale = Vector3.one;

            var controller = cat.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.28f, 0f);
            controller.radius = 0.26f;
            controller.height = 0.55f;
            controller.stepOffset = 0.25f;

            var companion = cat.AddComponent<CatCompanion>();
            var so = new SerializedObject(companion);
            so.FindProperty("target").objectReferenceValue = player.transform;

            var missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MagicMissilePrefabPath);
            if (missilePrefab != null)
            {
                so.FindProperty("magicMissilePrefab").objectReferenceValue = missilePrefab;
            }

            var hitAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TopDownEngine/Demos/Loft3D/Sounds/LoftHit1.wav");
            var castAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TopDownEngine/Demos/Koala2D/Sounds/KoalaLaser.wav");
            if (hitAudio != null) so.FindProperty("hitSound").objectReferenceValue = hitAudio;
            if (castAudio != null) so.FindProperty("castSound").objectReferenceValue = castAudio;

            so.FindProperty("targetLayers").intValue = 1 << 13; // Enemies layer
            so.FindProperty("obstacleLayers").intValue = (1 << 8) | (1 << 11);
            so.FindProperty("castRange").floatValue = 14f;
            so.FindProperty("castCooldown").floatValue = 2.2f;
            so.FindProperty("missilesPerSalvo").intValue = 3;
            so.FindProperty("salvoInterval").floatValue = 0.12f;
            so.FindProperty("missileDamage").floatValue = 22f;

            // Load materials and mesh
            const string artDir = "Assets/Art/Cat/";
            var tabbyMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "ChibiCat_Tabby_Mat.mat");
            var faceMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "ChibiCat_Face_Mat.mat");
            var crownMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "PrincessCrown_Mat.mat");
            var rubyMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "CrownRuby_Mat.mat");
            var pawMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "ChibiCat_Paw_Mat.mat");
            var earMat = AssetDatabase.LoadAssetAtPath<Material>(artDir + "ChibiCat_InnerEar_Mat.mat");
            var crownMesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(artDir + "PrincessCrownMesh.asset");
            var missileMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/MagicMissile_Mat.mat");
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artDir + "CatOverheadIcon.png");

            // Build CatVisuals Root
            var visuals = new GameObject("CatVisuals");
            visuals.transform.SetParent(cat.transform, false);

            // BodyRoot
            var bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(visuals.transform, false);
            bodyRoot.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            var torso = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            torso.name = "TorsoMesh";
            torso.transform.SetParent(bodyRoot.transform, false);
            torso.transform.localScale = new Vector3(0.32f, 0.26f, 0.44f);
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            if (tabbyMat != null) torso.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var chestBib = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chestBib.name = "ChestBib";
            chestBib.transform.SetParent(bodyRoot.transform, false);
            chestBib.transform.localPosition = new Vector3(0f, -0.04f, 0.12f);
            chestBib.transform.localScale = new Vector3(0.26f, 0.20f, 0.22f);
            Object.DestroyImmediate(chestBib.GetComponent<Collider>());
            if (pawMat != null) chestBib.GetComponent<Renderer>().sharedMaterial = pawMat;

            // HeadRoot
            var headRoot = new GameObject("HeadRoot");
            headRoot.transform.SetParent(bodyRoot.transform, false);
            headRoot.transform.localPosition = new Vector3(0f, 0.11f, 0.18f);

            var headMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMesh.name = "HeadMesh";
            headMesh.transform.SetParent(headRoot.transform, false);
            headMesh.transform.localScale = new Vector3(0.38f, 0.32f, 0.32f);
            Object.DestroyImmediate(headMesh.GetComponent<Collider>());
            if (tabbyMat != null) headMesh.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var cheekL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cheekL.name = "Cheek_L";
            cheekL.transform.SetParent(headRoot.transform, false);
            cheekL.transform.localPosition = new Vector3(-0.13f, -0.05f, 0.04f);
            cheekL.transform.localScale = new Vector3(0.18f, 0.16f, 0.18f);
            Object.DestroyImmediate(cheekL.GetComponent<Collider>());
            if (tabbyMat != null) cheekL.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var cheekR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cheekR.name = "Cheek_R";
            cheekR.transform.SetParent(headRoot.transform, false);
            cheekR.transform.localPosition = new Vector3(0.13f, -0.05f, 0.04f);
            cheekR.transform.localScale = new Vector3(0.18f, 0.16f, 0.18f);
            Object.DestroyImmediate(cheekR.GetComponent<Collider>());
            if (tabbyMat != null) cheekR.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var muzzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzle.name = "Muzzle";
            muzzle.transform.SetParent(headRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, -0.06f, 0.14f);
            muzzle.transform.localScale = new Vector3(0.18f, 0.13f, 0.13f);
            Object.DestroyImmediate(muzzle.GetComponent<Collider>());
            if (pawMat != null) muzzle.GetComponent<Renderer>().sharedMaterial = pawMat;

            var faceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceQuad.name = "FelineFaceOverlay";
            faceQuad.transform.SetParent(headRoot.transform, false);
            faceQuad.transform.localPosition = new Vector3(0f, 0.01f, 0.185f);
            faceQuad.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
            Object.DestroyImmediate(faceQuad.GetComponent<Collider>());
            if (faceMat != null) faceQuad.GetComponent<Renderer>().sharedMaterial = faceMat;

            var earL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earL.name = "Ear_L";
            earL.transform.SetParent(headRoot.transform, false);
            earL.transform.localPosition = new Vector3(-0.11f, 0.15f, -0.02f);
            earL.transform.localRotation = Quaternion.Euler(12f, 0f, 22f);
            earL.transform.localScale = new Vector3(0.11f, 0.13f, 0.07f);
            Object.DestroyImmediate(earL.GetComponent<Collider>());
            if (tabbyMat != null) earL.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var innerEarL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            innerEarL.name = "InnerEar_L";
            innerEarL.transform.SetParent(earL.transform, false);
            innerEarL.transform.localPosition = new Vector3(0f, 0f, 0.52f);
            innerEarL.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            Object.DestroyImmediate(innerEarL.GetComponent<Collider>());
            if (earMat != null) innerEarL.GetComponent<Renderer>().sharedMaterial = earMat;

            var earR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earR.name = "Ear_R";
            earR.transform.SetParent(headRoot.transform, false);
            earR.transform.localPosition = new Vector3(0.11f, 0.15f, -0.02f);
            earR.transform.localRotation = Quaternion.Euler(12f, 0f, -22f);
            earR.transform.localScale = new Vector3(0.11f, 0.13f, 0.07f);
            Object.DestroyImmediate(earR.GetComponent<Collider>());
            if (tabbyMat != null) earR.GetComponent<Renderer>().sharedMaterial = tabbyMat;

            var innerEarR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            innerEarR.name = "InnerEar_R";
            innerEarR.transform.SetParent(earR.transform, false);
            innerEarR.transform.localPosition = new Vector3(0f, 0f, 0.52f);
            innerEarR.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            Object.DestroyImmediate(innerEarR.GetComponent<Collider>());
            if (earMat != null) innerEarR.GetComponent<Renderer>().sharedMaterial = earMat;

            // Princess Crown
            var crownGO = new GameObject("PrincessCrown");
            crownGO.transform.SetParent(headRoot.transform, false);
            crownGO.transform.localPosition = new Vector3(0f, 0.15f, 0.01f);
            crownGO.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            crownGO.transform.localScale = Vector3.one * 0.95f;
            if (crownMesh != null) crownGO.AddComponent<MeshFilter>().sharedMesh = crownMesh;
            if (crownMat != null) crownGO.AddComponent<MeshRenderer>().sharedMaterial = crownMat;

            var rubyCenter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rubyCenter.name = "CrownRuby_Center";
            rubyCenter.transform.SetParent(crownGO.transform, false);
            rubyCenter.transform.localPosition = new Vector3(0f, 0.12f, 0.165f);
            rubyCenter.transform.localScale = new Vector3(0.045f, 0.06f, 0.035f);
            Object.DestroyImmediate(rubyCenter.GetComponent<Collider>());
            if (rubyMat != null) rubyCenter.GetComponent<Renderer>().sharedMaterial = rubyMat;

            // Orb
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "ArcaneOrb";
            orb.transform.SetParent(crownGO.transform, false);
            orb.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            orb.transform.localScale = Vector3.one * 0.09f;
            Object.DestroyImmediate(orb.GetComponent<Collider>());
            if (missileMat != null) orb.GetComponent<Renderer>().sharedMaterial = missileMat;

            // Quadruped Legs
            Transform legFL = BuildChibiLeg(bodyRoot.transform, "Leg_FL", new Vector3(-0.12f, -0.03f, 0.13f), true, tabbyMat, pawMat);
            Transform legFR = BuildChibiLeg(bodyRoot.transform, "Leg_FR", new Vector3(0.12f, -0.03f, 0.13f), true, tabbyMat, pawMat);
            Transform legBL = BuildChibiLeg(bodyRoot.transform, "Leg_BL", new Vector3(-0.12f, -0.03f, -0.14f), false, tabbyMat, pawMat);
            Transform legBR = BuildChibiLeg(bodyRoot.transform, "Leg_BR", new Vector3(0.12f, -0.03f, -0.14f), false, tabbyMat, pawMat);

            so.FindProperty("frontLeftLeg").objectReferenceValue = legFL;
            so.FindProperty("frontRightLeg").objectReferenceValue = legFR;
            so.FindProperty("backLeftLeg").objectReferenceValue = legBL;
            so.FindProperty("backRightLeg").objectReferenceValue = legBR;
            so.FindProperty("castPoint").objectReferenceValue = orb.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Long Multi-segment Tail
            var tailSegmentsList = new Transform[4];
            Transform parentTail = bodyRoot.transform;
            Vector3 spawnPos = new Vector3(0f, 0.04f, -0.20f);
            Quaternion spawnRot = Quaternion.Euler(38f, 0f, 0f);

            for (int i = 0; i < 4; i++)
            {
                var seg = new GameObject("Tail_Seg" + i);
                seg.transform.SetParent(parentTail, false);
                seg.transform.localPosition = i == 0 ? spawnPos : new Vector3(0f, 0.11f, 0f);
                seg.transform.localRotation = i == 0 ? spawnRot : Quaternion.Euler(6f, 0f, 0f);

                var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mesh.name = "Mesh";
                mesh.transform.SetParent(seg.transform, false);
                mesh.transform.localPosition = new Vector3(0f, 0.055f, 0f);
                float taper = 1f - (i * 0.16f);
                mesh.transform.localScale = new Vector3(0.065f * taper, 0.055f, 0.065f * taper);
                Object.DestroyImmediate(mesh.GetComponent<Collider>());
                if (tabbyMat != null) mesh.GetComponent<Renderer>().sharedMaterial = (i == 3 && pawMat != null) ? pawMat : tabbyMat;

                tailSegmentsList[i] = seg.transform;
                parentTail = seg.transform;
            }

            // Overhead Badge
            var overheadGO = new GameObject("OverheadFelineBadge");
            overheadGO.transform.SetParent(cat.transform, false);
            overheadGO.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            overheadGO.transform.localScale = Vector3.one * 0.45f;
            var sr = overheadGO.AddComponent<SpriteRenderer>();
            sr.sprite = iconSprite;
            sr.sortingOrder = 5;

            var felineVisuals = cat.AddComponent<CatFelineVisuals>();
            var soVis = new SerializedObject(felineVisuals);
            soVis.FindProperty("bodyRoot").objectReferenceValue = bodyRoot.transform;
            soVis.FindProperty("head").objectReferenceValue = headRoot.transform;
            soVis.FindProperty("princessCrown").objectReferenceValue = crownGO.transform;
            soVis.FindProperty("leftEar").objectReferenceValue = earL.transform;
            soVis.FindProperty("rightEar").objectReferenceValue = earR.transform;
            soVis.FindProperty("legFL").objectReferenceValue = legFL;
            soVis.FindProperty("legFR").objectReferenceValue = legFR;
            soVis.FindProperty("legBL").objectReferenceValue = legBL;
            soVis.FindProperty("legBR").objectReferenceValue = legBR;

            var propTail = soVis.FindProperty("tailSegments");
            propTail.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                propTail.GetArrayElementAtIndex(i).objectReferenceValue = tailSegmentsList[i];
            }
            soVis.FindProperty("overheadIcon").objectReferenceValue = overheadGO.transform;
            soVis.ApplyModifiedPropertiesWithoutUndo();
            felineVisuals.CacheRestPoses();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Cat companion added to Dungeon scene, following " + player.name + " with Magic Missile spell ready.");
        }

        /// <summary>Builds a hip pivot + thigh cylinder + cream paw sock resting on the floor.</summary>
        private static Transform BuildChibiLeg(Transform parent, string name, Vector3 hipPos, bool isFront, Material furMat, Material pawMat)
        {
            var hip = new GameObject(name);
            hip.transform.SetParent(parent, false);
            hip.transform.localPosition = hipPos;

            var thigh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thigh.name = "Thigh";
            thigh.transform.SetParent(hip.transform, false);
            thigh.transform.localPosition = new Vector3(0f, -0.09f, 0f);
            thigh.transform.localScale = new Vector3(0.085f, 0.09f, 0.085f);
            Object.DestroyImmediate(thigh.GetComponent<Collider>());
            if (furMat != null) thigh.GetComponent<Renderer>().sharedMaterial = furMat;

            var paw = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            paw.name = "Paw";
            paw.transform.SetParent(hip.transform, false);
            paw.transform.localPosition = new Vector3(0f, -0.22f, isFront ? 0.03f : 0.02f);
            paw.transform.localScale = new Vector3(0.10f, 0.065f, 0.12f);
            Object.DestroyImmediate(paw.GetComponent<Collider>());
            if (pawMat != null) paw.GetComponent<Renderer>().sharedMaterial = pawMat;

            return hip.transform;
        }
    }
}

