using System;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Play Mode 테스트에서 사용할 더미 CharacterBase를 생성/재사용하는 공용 팩토리입니다.
    /// </summary>
    public static class CharacterTestDummyFactory
    {
        public sealed class CreateOptions
        {
            public string ToolOwnerKey = string.Empty;
            public string DummyName = "CharacterTest_DummyTarget";
            public bool ReuseExisting = true;
            public bool FocusSelectionAfterCreate = true;
            public bool PingAfterCreate = false;
            public Vector3? SpawnPosition;
            public Vector3 SpawnOffset = new(150f, 0f, 0f);
        }

        public static bool TryCreateOrReuseDummyTarget(
            CharacterBase sourceCharacter,
            CreateOptions options,
            out CharacterBase dummyCharacter,
            out string error)
        {
            dummyCharacter = null;
            error = null;

            if (!Application.isPlaying)
            {
                error = "Play Mode에서만 더미 Target을 생성할 수 있습니다.";
                return false;
            }

            if (sourceCharacter == null)
            {
                error = "원본 CharacterBase가 필요합니다.";
                return false;
            }

            options ??= new CreateOptions();
            options.ToolOwnerKey ??= string.Empty;
            options.DummyName ??= "CharacterTest_DummyTarget";

            if (options.ReuseExisting)
            {
                dummyCharacter = FindReusableDummy(options.ToolOwnerKey, options.DummyName);
                if (dummyCharacter != null)
                {
                    MoveDummy(dummyCharacter, sourceCharacter, options);
                    FocusDummy(dummyCharacter, options);
                    return true;
                }
            }

            var sourceObject = sourceCharacter.gameObject;
            if (sourceObject == null)
            {
                error = "원본 CharacterBase의 GameObject를 찾지 못했습니다.";
                return false;
            }

            var spawnPosition = options.SpawnPosition ?? (sourceCharacter.transform.position + options.SpawnOffset);
            var instance = Object.Instantiate(sourceObject, spawnPosition, sourceCharacter.transform.rotation);
            if (instance == null)
            {
                error = "더미 Target 생성에 실패했습니다.";
                return false;
            }

            instance.name = options.DummyName;

            dummyCharacter = instance.GetComponent<CharacterBase>();
            if (dummyCharacter == null)
                dummyCharacter = instance.GetComponentInChildren<CharacterBase>();

            if (dummyCharacter == null)
            {
                Object.Destroy(instance);
                error = "복제된 오브젝트에서 CharacterBase를 찾지 못했습니다.";
                return false;
            }

            ConfigureDummy(dummyCharacter, sourceCharacter, options);
            FocusDummy(dummyCharacter, options);
            return true;
        }

        private static CharacterBase FindReusableDummy(string toolOwnerKey, string dummyName)
        {
#if UNITY_2023_1_OR_NEWER
            var markers = Object.FindObjectsByType<CharacterTestDummyMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var markers = Object.FindObjectsOfType<CharacterTestDummyMarker>();
#endif
            foreach (var marker in markers)
            {
                if (marker == null || !marker.Matches(toolOwnerKey, dummyName))
                    continue;

                if (!marker.TryGetComponent<CharacterBase>(out var character))
                    character = marker.GetComponentInParent<CharacterBase>();

                if (character != null)
                    return character;
            }

            return null;
        }

        private static void ConfigureDummy(CharacterBase dummyCharacter, CharacterBase sourceCharacter, CreateOptions options)
        {
            if (dummyCharacter == null)
                return;

            var gameObject = dummyCharacter.gameObject;
            gameObject.name = options.DummyName;

            var marker = gameObject.GetComponent<CharacterTestDummyMarker>();
            if (marker == null)
                marker = gameObject.AddComponent<CharacterTestDummyMarker>();
            marker.Bind(options.ToolOwnerKey, options.DummyName, sourceCharacter);

            if (gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
                gameObject.tag = "Untagged";

            DisableKnownRuntimeDrivers(gameObject);
            ResetPhysicsState(gameObject);
            MoveDummy(dummyCharacter, sourceCharacter, options);
        }

        private static void MoveDummy(CharacterBase dummyCharacter, CharacterBase sourceCharacter, CreateOptions options)
        {
            if (dummyCharacter == null || sourceCharacter == null)
                return;

            var position = options.SpawnPosition ?? (sourceCharacter.transform.position + options.SpawnOffset);
            dummyCharacter.transform.position = position;
            dummyCharacter.transform.rotation = sourceCharacter.transform.rotation;
        }

        private static void FocusDummy(CharacterBase dummyCharacter, CreateOptions options)
        {
            if (dummyCharacter == null)
                return;

            if (options.FocusSelectionAfterCreate)
                Selection.activeGameObject = dummyCharacter.gameObject;

            if (options.PingAfterCreate)
                EditorGUIUtility.PingObject(dummyCharacter.gameObject);
        }

        private static void DisableKnownRuntimeDrivers(GameObject target)
        {
            if (target == null)
                return;

            DisableIfExists<MonsterBrainTicker>(target);
            DisableIfExists<MonsterLegacyBrain>(target);
            DisableIfExists<ControllerMonster>(target);
            DisableIfExists<ControllerPlayer>(target);
            DisableIfExists<PlayerAutoMoveController>(target);
            DisableIfExists<EquipController>(target);
            DisableIfExists<ToolController>(target);

            foreach (var monoBehaviour in target.GetComponents<MonoBehaviour>())
            {
                if (monoBehaviour == null)
                    continue;

                var typeName = monoBehaviour.GetType().Name;
                if (string.Equals(typeName, "MonsterBtRunner", StringComparison.Ordinal) ||
                    string.Equals(typeName, "MonsterSkillDriverAdapter", StringComparison.Ordinal))
                {
                    monoBehaviour.enabled = false;
                }
            }
        }

        private static void DisableIfExists<T>(GameObject target) where T : Behaviour
        {
            var component = target.GetComponent<T>();
            if (component != null)
                component.enabled = false;
        }

        private static void ResetPhysicsState(GameObject target)
        {
            if (target == null)
                return;

            foreach (var rigid in target.GetComponentsInChildren<Rigidbody2D>())
            {
                if (rigid == null)
                    continue;

                rigid.linearVelocity = Vector2.zero;
                rigid.angularVelocity = 0f;
            }
        }
    }
}
