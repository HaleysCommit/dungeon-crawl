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

            // Body
            var cat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cat.name = "Cat";
            cat.transform.localScale = new Vector3(0.35f, 0.25f, 0.5f);
            cat.transform.position = player.transform.position - player.transform.forward * 1.5f;

            var renderer = cat.GetComponent<Renderer>();
            var catMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = CatColor };
            renderer.sharedMaterial = catMat;

            Object.DestroyImmediate(cat.GetComponent<CapsuleCollider>());

            // placeholder collider sizing - retune once real (uniformly scaled) art replaces this capsule
            var controller = cat.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.3f, 0f);
            controller.radius = 0.3f;
            controller.height = 0.6f;
            controller.skinWidth = 0.02f;

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
            so.ApplyModifiedPropertiesWithoutUndo();

            // Visual elements
            var furMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Cat/CatFur_Mat.mat");
            var faceMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Cat/CatFaceOverlay_Mat.mat");
            var pinkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Cat/CatPink_Mat.mat");
            var whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Cat/CatWhitePaw_Mat.mat");
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cat/CatOverheadIcon.png");

            if (furMat != null) renderer.sharedMaterial = furMat;

            var visuals = new GameObject("CatVisuals");
            visuals.transform.SetParent(cat.transform, false);

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "CatHead";
            head.transform.SetParent(visuals.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.35f, 0.28f);
            head.transform.localScale = new Vector3(0.38f, 0.34f, 0.34f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            if (furMat != null) head.GetComponent<Renderer>().sharedMaterial = furMat;

            // Snout
            var muzzle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzle.name = "Muzzle";
            muzzle.transform.SetParent(head.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, -0.2f, 0.35f);
            muzzle.transform.localScale = new Vector3(0.45f, 0.35f, 0.35f);
            Object.DestroyImmediate(muzzle.GetComponent<Collider>());
            if (whiteMat != null) muzzle.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // Feline Face Overlay
            var faceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            faceQuad.name = "FelineFaceOverlay";
            faceQuad.transform.SetParent(head.transform, false);
            faceQuad.transform.localPosition = new Vector3(0f, 0.05f, 0.48f);
            faceQuad.transform.localRotation = Quaternion.identity;
            faceQuad.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            Object.DestroyImmediate(faceQuad.GetComponent<Collider>());
            if (faceMat != null) faceQuad.GetComponent<Renderer>().sharedMaterial = faceMat;

            // Ears
            var earL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earL.name = "Ear_L";
            earL.transform.SetParent(head.transform, false);
            earL.transform.localPosition = new Vector3(-0.35f, 0.48f, -0.05f);
            earL.transform.localRotation = Quaternion.Euler(15f, 0f, 32f);
            earL.transform.localScale = new Vector3(0.2f, 0.38f, 0.15f);
            Object.DestroyImmediate(earL.GetComponent<Collider>());
            if (furMat != null) earL.GetComponent<Renderer>().sharedMaterial = furMat;

            var innerEarL = GameObject.CreatePrimitive(PrimitiveType.Quad);
            innerEarL.name = "InnerEar_L";
            innerEarL.transform.SetParent(earL.transform, false);
            innerEarL.transform.localPosition = new Vector3(0f, 0f, 0.52f);
            innerEarL.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            Object.DestroyImmediate(innerEarL.GetComponent<Collider>());
            if (pinkMat != null) innerEarL.GetComponent<Renderer>().sharedMaterial = pinkMat;

            var earR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            earR.name = "Ear_R";
            earR.transform.SetParent(head.transform, false);
            earR.transform.localPosition = new Vector3(0.35f, 0.48f, -0.05f);
            earR.transform.localRotation = Quaternion.Euler(15f, 0f, -32f);
            earR.transform.localScale = new Vector3(0.2f, 0.38f, 0.15f);
            Object.DestroyImmediate(earR.GetComponent<Collider>());
            if (furMat != null) earR.GetComponent<Renderer>().sharedMaterial = furMat;

            var innerEarR = GameObject.CreatePrimitive(PrimitiveType.Quad);
            innerEarR.name = "InnerEar_R";
            innerEarR.transform.SetParent(earR.transform, false);
            innerEarR.transform.localPosition = new Vector3(0f, 0f, 0.52f);
            innerEarR.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            Object.DestroyImmediate(innerEarR.GetComponent<Collider>());
            if (pinkMat != null) innerEarR.GetComponent<Renderer>().sharedMaterial = pinkMat;

            // Tail
            var tailRoot = new GameObject("TailRoot");
            tailRoot.transform.SetParent(visuals.transform, false);
            tailRoot.transform.localPosition = new Vector3(0f, 0.1f, -0.42f);
            tailRoot.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tail.name = "TailMesh";
            tail.transform.SetParent(tailRoot.transform, false);
            tail.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            tail.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
            Object.DestroyImmediate(tail.GetComponent<Collider>());
            if (furMat != null) tail.GetComponent<Renderer>().sharedMaterial = furMat;

            // Four legs, each a hip pivot (rotated by CatCompanion's procedural trot gait) with a thin cylinder mesh
            Transform legFrontLeft = BuildLeg(visuals.transform, "Leg_FrontLeft", new Vector3(-0.18f, -0.05f, 0.22f), furMat);
            Transform legFrontRight = BuildLeg(visuals.transform, "Leg_FrontRight", new Vector3(0.18f, -0.05f, 0.22f), furMat);
            Transform legBackLeft = BuildLeg(visuals.transform, "Leg_BackLeft", new Vector3(-0.18f, -0.05f, -0.22f), furMat);
            Transform legBackRight = BuildLeg(visuals.transform, "Leg_BackRight", new Vector3(0.18f, -0.05f, -0.22f), furMat);

            so.FindProperty("frontLeftLeg").objectReferenceValue = legFrontLeft;
            so.FindProperty("frontRightLeg").objectReferenceValue = legFrontRight;
            so.FindProperty("backLeftLeg").objectReferenceValue = legBackLeft;
            so.FindProperty("backRightLeg").objectReferenceValue = legBackRight;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Overhead Badge
            var overheadGO = new GameObject("OverheadFelineBadge");
            overheadGO.transform.SetParent(cat.transform, false);
            overheadGO.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            overheadGO.transform.localScale = Vector3.one * 0.45f;
            var sr = overheadGO.AddComponent<SpriteRenderer>();
            sr.sprite = iconSprite;
            sr.sortingOrder = 5;

            var felineVisuals = cat.AddComponent<CatFelineVisuals>();
            var soVis = new SerializedObject(felineVisuals);
            soVis.FindProperty("overheadIcon").objectReferenceValue = overheadGO.transform;
            soVis.FindProperty("tailTransform").objectReferenceValue = tailRoot.transform;
            soVis.FindProperty("leftEar").objectReferenceValue = earL.transform;
            soVis.FindProperty("rightEar").objectReferenceValue = earR.transform;
            soVis.FindProperty("bodyTransform").objectReferenceValue = cat.transform;
            soVis.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Cat companion added to Dungeon scene, following " + player.name + " with Magic Missile spell ready.");
        }

        /// <summary>Builds a hip pivot + thin cylinder leg mesh; the pivot's local rotation is what CatCompanion's procedural trot gait animates.</summary>
        private static Transform BuildLeg(Transform parent, string name, Vector3 localHipPosition, Material furMat)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localHipPosition;

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "LegMesh";
            mesh.transform.SetParent(pivot.transform, false);
            mesh.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            mesh.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            if (furMat != null) mesh.GetComponent<Renderer>().sharedMaterial = furMat;

            return pivot.transform;
        }
    }
}

