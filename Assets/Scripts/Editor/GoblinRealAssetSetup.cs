using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using MoreMountains.TopDownEngine;
using DungeonCrawl.Gameplay;

namespace DungeonCrawl.EditorTools
{
    /// <summary>One-off editor setup for the purchased "Goblin" Asset Store package (Aleksey Kozhemyakin):
    /// builds a real parameterized Animator Controller (the stock one shipped with the package has zero
    /// parameters and only unconditional transitions) driving its combat_idle/walk/run/attack01-03/get_hit/death
    /// clips, and wires Health + CharacterController + GoblinMeleeAI onto Assets/goblin/Prefabs/goblin/SK_goblin.prefab.</summary>
    public static class GoblinRealAssetSetup
    {
        private const string PrefabPath = "Assets/goblin/Prefabs/goblin/SK_goblin.prefab";
        private const string AnimFolder = "Assets/goblin/Animations/goblin/";
        private const string ControllerPath = "Assets/goblin/Controllers/goblin/GoblinCombat.controller";
        private const string DungeonScenePath = "Assets/Scenes/Dungeon.unity";

        [MenuItem("Dungeon Crawl/Setup Purchased Goblin Asset")]
        public static void SetupPurchasedGoblin()
        {
            var controller = BuildAnimatorController();
            if (controller == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("Could not load goblin prefab at " + PrefabPath);
                return;
            }

            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError(PrefabPath + " has no Animator component - is this the right prefab?");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }
            animator.runtimeAnimatorController = controller;

            var health = root.GetComponent<Health>();
            if (health == null)
            {
                health = root.AddComponent<Health>();
            }
            health.InitialHealth = 40;
            health.MaximumHealth = 40;

            SetupCharacterController(root);

            var ai = root.GetComponent<GoblinMeleeAI>();
            if (ai == null)
            {
                ai = root.AddComponent<GoblinMeleeAI>();
            }
            var aiSo = new SerializedObject(ai);
            aiSo.FindProperty("animator").objectReferenceValue = animator;
            aiSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log("Purchased goblin asset wired up: GoblinCombat.controller assigned, Health/CharacterController/GoblinMeleeAI added to " + PrefabPath + ".");
        }

        /// <summary>Collapses duplicate goblin instances sitting on the same spot (e.g. from the setup command
        /// having been run more than once) down to one per distinct position.</summary>
        [MenuItem("Dungeon Crawl/Remove Duplicate Goblins In Dungeon Scene")]
        public static void RemoveDuplicateGoblins()
        {
            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (newPrefab == null)
            {
                Debug.LogError("Could not load goblin prefab at " + PrefabPath);
                return;
            }

            var scene = EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Single);

            var allGoblins = new System.Collections.Generic.List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == newPrefab)
                    {
                        allGoblins.Add(t.gameObject);
                    }
                }
            }

            var seenPositions = new System.Collections.Generic.List<Vector3>();
            int removed = 0;
            foreach (var goblin in allGoblins)
            {
                Vector3 pos = goblin.transform.position;
                bool isDuplicate = false;
                foreach (var seen in seenPositions)
                {
                    if (Vector3.Distance(seen, pos) < 0.25f)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    Object.DestroyImmediate(goblin);
                    removed++;
                }
                else
                {
                    seenPositions.Add(pos);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Removed " + removed + " duplicate goblin(s), kept " + seenPositions.Count + " at distinct positions.");
        }

        private static void SetupCharacterController(GameObject root)
        {
            var controllerComp = root.GetComponent<CharacterController>();
            if (controllerComp == null)
            {
                controllerComp = root.AddComponent<CharacterController>();
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            float rootY = root.transform.position.y;
            controllerComp.height = Mathf.Max(bounds.size.y, 0.5f);
            controllerComp.center = new Vector3(0f, (bounds.center.y - rootY) , 0f);
            controllerComp.radius = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.z) * 0.25f, 0.15f);
            controllerComp.skinWidth = 0.03f;
        }

        private static AnimatorController BuildAnimatorController()
        {
            var idleClip = LoadClip("goblin@combat_idle.FBX");
            var walkClip = LoadClip("goblin@combat_walk.FBX");
            var runClip = LoadClip("goblin@combat_run.FBX");
            var attack1Clip = LoadClip("goblin@combat_attack01.FBX");
            var attack2Clip = LoadClip("goblin@combat_attack02.FBX");
            var attack3Clip = LoadClip("goblin@combat_attack03.FBX");
            var getHitClip = LoadClip("goblin@combat_get_hit.FBX");
            var deathClip = LoadClip("gobln@combat_death.FBX");

            if (idleClip == null || walkClip == null || runClip == null
                || attack1Clip == null || attack2Clip == null || attack3Clip == null)
            {
                Debug.LogError("Could not find the expected animation clips under " + AnimFolder
                    + ". Make sure the Goblin package's Animations folder imported correctly.");
                return null;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

            var rootSM = controller.layers[0].stateMachine;

            var idle = rootSM.AddState("Idle", new Vector3(0, 0, 0));
            idle.motion = idleClip;
            rootSM.defaultState = idle;

            var walk = rootSM.AddState("Walk", new Vector3(250, 0, 0));
            walk.motion = walkClip;

            var run = rootSM.AddState("Run", new Vector3(500, 0, 0));
            run.motion = runClip;

            AddLocomotionTransition(idle, walk, AnimatorConditionMode.Greater, 0.15f);
            AddLocomotionTransition(walk, idle, AnimatorConditionMode.Less, 0.1f);
            AddLocomotionTransition(walk, run, AnimatorConditionMode.Greater, 3.5f);
            AddLocomotionTransition(run, walk, AnimatorConditionMode.Less, 3.0f);

            var death = rootSM.AddState("Death", new Vector3(250, 300, 0));
            death.motion = deathClip;
            var deathTransition = rootSM.AddAnyStateTransition(death);
            deathTransition.hasExitTime = false;
            deathTransition.duration = 0.1f;
            deathTransition.canTransitionToSelf = false;
            deathTransition.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            var getHit = rootSM.AddState("GetHit", new Vector3(0, 300, 0));
            getHit.motion = getHitClip;
            var hitTransition = rootSM.AddAnyStateTransition(getHit);
            hitTransition.hasExitTime = false;
            hitTransition.duration = 0.05f;
            hitTransition.canTransitionToSelf = false;
            hitTransition.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            AddReturnToIdleTransition(getHit, idle);

            AddAttackState(rootSM, idle, "Attack1", attack1Clip, 0, new Vector3(500, 300, 0));
            AddAttackState(rootSM, idle, "Attack2", attack2Clip, 1, new Vector3(750, 300, 0));
            AddAttackState(rootSM, idle, "Attack3", attack3Clip, 2, new Vector3(1000, 300, 0));

            return controller;
        }

        private static void AddAttackState(AnimatorStateMachine rootSM, AnimatorState idle, string name,
            AnimationClip clip, int attackIndex, Vector3 position)
        {
            var state = rootSM.AddState(name, position);
            state.motion = clip;

            var transition = rootSM.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            transition.AddCondition(AnimatorConditionMode.Equals, attackIndex, "AttackIndex");

            AddReturnToIdleTransition(state, idle);
        }

        private static void AddReturnToIdleTransition(AnimatorState from, AnimatorState idle)
        {
            var transition = from.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 0.9f;
            transition.duration = 0.15f;
        }

        private static void AddLocomotionTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.2f;
            transition.AddCondition(mode, threshold, "Speed");
        }

        private static AnimationClip LoadClip(string fbxFileName)
        {
            string path = AnimFolder + fbxFileName;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    return clip;
                }
            }
            Debug.LogWarning("Could not find an AnimationClip in " + path);
            return null;
        }
    }
}
