#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 간단한 생성 허브: 버튼 클릭으로 각 팩토리 호출
    /// - 필요에 따라 검색/필터/아이콘 추가 확장
    /// </summary>
    internal class CreatorHubWindow : EditorWindow
    {
        [MenuItem(ConfigEditor.NameToolCreateHubWindow, false, (int)ConfigEditor.ToolOrdering.CreateHubWindow)]
        public static void Open() => GetWindow<CreatorHubWindow>("GGemCo Creator");

        private Vector2 _scroll;

        private struct Item
        {
            public string label;
            public Action onClick;
        }

        private static List<Item> _items;

        private void OnEnable()
        {
            // 등록 (필요시 리플렉션으로 자동 수집 가능)
            _items = new List<Item>
            {
                new Item{ label="Trap/Default Trap", onClick=() =>
                    TrapFactory.CreateFixed(new MenuCommand(Selection.activeGameObject))},
                new Item{ label="Trap/Spike (Static)", onClick=() =>
                    TrapFactory.CreateTimer(new MenuCommand(Selection.activeGameObject))},
                // new Item{ label="Projectile/Default", onClick=() =>
                //     ProjectileFactory.CreateDefault(new MenuCommand(Selection.activeGameObject))},
                // new Item{ label="Effect/Default", onClick=() =>
                //     EffectFactory.CreateDefault(new MenuCommand(Selection.activeGameObject))}
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("선택한 게임오브젝트의 자식으로 생성됩니다(Hierarchy 우클릭 메뉴와 동일).",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var it in _items)
            {
                if (GUILayout.Button(it.label, GUILayout.Height(26)))
                {
                    it.onClick?.Invoke();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
