using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 버튼 생성에 필요한 메타데이터.
    /// - filedName : GameObject 이름(패키지 Prefix가 자동으로 붙음)
    /// - text      : 버튼에 표시할 기본 텍스트
    /// - localizationTable / localizationKey : 로컬라이제이션 정보(선택)
    /// </summary>
    public sealed class MetaDataButton
    {
        public readonly string filedName;          // NOTE: 기존 코드 호환을 위해 필드명 유지
        public readonly string text;
        public readonly string localizationTable;
        public readonly string localizationKey;

        public MetaDataButton(
            string filedName,
            string text = "",
            string localizationTable = "",
            string localizationKey = "")
        {
            this.filedName = filedName;
            this.text = text;
            this.localizationTable = localizationTable;
            this.localizationKey = localizationKey;
        }
    }

    /// <summary>
    /// TextMeshProUGUI 생성에 필요한 메타데이터.
    /// - RectTransform 위치/크기/앵커
    /// - 폰트 크기 및 정렬
    /// - 로컬라이제이션 정보
    /// </summary>
    public sealed class MetaDataTextMeshProGUI
    {
        public readonly Vector2 pivot;
        public readonly Vector2 position;
        public readonly AnchorPresets anchorPresets;
        public readonly float width;
        public readonly float height;
        public readonly float fontSize;
        public readonly TextMeshProHelper.HorizontalAlignment horizontalAlignment;
        public readonly TextMeshProHelper.VerticalAlignment verticalAlignment;
        public readonly string localizationTable;
        public readonly string localizationKey;

        public MetaDataTextMeshProGUI(
            Vector2 pivot,
            Vector2 position,
            AnchorPresets anchorPresets,
            float width = 0f,
            float height = 0f,
            float fontSize = 0f,
            TextMeshProHelper.HorizontalAlignment horizontalAlignment = TextMeshProHelper.HorizontalAlignment.Center,
            TextMeshProHelper.VerticalAlignment verticalAlignment = TextMeshProHelper.VerticalAlignment.Middle,
            string localizationTable = "",
            string localizationKey = "")
        {
            this.pivot = pivot;
            this.position = position;
            this.anchorPresets = anchorPresets;
            this.width = width;
            this.height = height;
            this.fontSize = fontSize;
            this.horizontalAlignment = horizontalAlignment;
            this.verticalAlignment = verticalAlignment;
            this.localizationTable = localizationTable;
            this.localizationKey = localizationKey;
        }
    }

    /// <summary>
    /// 에디터에서 공통 UI 컴포넌트(Canvas/Button/TMP 텍스트)를 생성·검색하는 유틸리티.
    /// 모든 메서드는 정적 메서드로 제공되며, 런타임 빌드에는 포함되지 않습니다.
    /// </summary>
    public static class CreateUIComponent
    {
        /// <summary>
        /// 패키지 Prefix를 포함한 오브젝트 이름을 생성합니다.
        /// 이미 Prefix가 붙어있는 경우 중복해서 붙이지 않습니다.
        /// </summary>
        public static string GenerateObjectName(string objectName, ConfigPackageInfo.PackageType packageType)
        {
            string prefix = ConfigPackageInfo.GetPackagePrefix(packageType);
            return objectName.StartsWith($"{prefix}_") ? objectName : $"{prefix}_{objectName}";
        }

        /// <summary>
        /// 패키지 전용 Canvas를 찾거나 생성합니다.
        /// - 이름: {Prefix}_Canvas
        /// - RenderMode: ScreenSpaceOverlay
        /// - 필수 컴포넌트: CanvasScaler, GraphicRaycaster
        /// - 프로젝트에 EventSystem이 없으면 함께 생성합니다.
        /// </summary>
        public static Canvas CreateObjectCanvas(ConfigPackageInfo.PackageType packageType)
        {
            string objectName = GenerateObjectName("Canvas", packageType);

            Canvas canvas = GameObject.Find(objectName)?.GetComponent<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var canvasObj = new GameObject(
                objectName,
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 필요한 경우 EventSystem 생성
            CreateEventSystemIfNotExists();

            return canvas;
        }

        /// <summary>
        /// 현재 씬에 EventSystem이 없는 경우 생성합니다.
        /// - 새 Input System 사용 시: InputSystemUIInputModule
        /// - 기존 Input Manager 사용 시: StandaloneInputModule
        /// </summary>
        private static void CreateEventSystemIfNotExists()
        {
            if (CompatObjectFind.FindFirst<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        /// <summary>
        /// 지정한 이름과 프리팹 경로로 GameObject를 찾거나 생성합니다.
        /// - 이미 씬에 존재하면 부모만 재설정합니다.
        /// - prefabPath가 비어 있으면 빈 GameObject를 생성합니다.
        /// - prefabPath가 유효하면 프리팹을 인스턴스화 후 완전 언팩합니다.
        /// </summary>
        public static GameObject CreateGameObjectByPrefab(
            string objectName,
            ConfigPackageInfo.PackageType packageType,
            Transform parent = null,
            string prefabPath = "")
        {
            objectName = GenerateObjectName(objectName, packageType);

            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject != null)
            {
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent, worldPositionStays: false);
                }

                return gameObject;
            }

            bool usedPrefab = false;

            if (string.IsNullOrEmpty(prefabPath))
            {
                gameObject = new GameObject(objectName);
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
                    return null;
                }

                gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (gameObject == null)
                {
                    Debug.LogError("프리팹 인스턴스 생성 실패");
                    return null;
                }

                usedPrefab = true;
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create UI GameObject");
            gameObject.name = objectName;

            if (parent != null)
            {
                gameObject.transform.SetParent(parent, worldPositionStays: false);
            }

            // 프리팹 인스턴스를 완전히 언팩하여 프로젝트 의존성을 줄입니다.
            if (usedPrefab)
            {
                PrefabUtility.UnpackPrefabInstance(
                    gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.UserAction);
            }

            return gameObject;
        }

        /// <summary>
        /// 기본 UI 버튼을 생성하거나 찾아서 반환합니다.
        /// - 이름: {Prefix}_{filedName}
        /// - 프리팹 경로: ConfigEditor.PathPrefabDefaultUIButton
        /// - TextMeshProUGUI에 로컬라이제이션 및 텍스트 설정
        /// </summary>
        public static Button CreateObjectButton(MetaDataButton metaDataButton, ConfigPackageInfo.PackageType packageType)
        {
            if (metaDataButton == null)
            {
                Debug.LogError("MetaDataButton 이 null 입니다.");
                return null;
            }

            string objectName = GenerateObjectName(metaDataButton.filedName, packageType);
            string text = metaDataButton.text;
            string localizationTable = metaDataButton.localizationTable;
            string localizationKey = metaDataButton.localizationKey;

            // 이미 존재하는 버튼 검색
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                return obj.GetComponentInChildren<Button>();
            }

            // Canvas 확보
            Canvas canvas = CreateObjectCanvas(packageType);

            // 기본 버튼 프리팹 인스턴스 생성
            string prefabPath = ConfigEditor.PathPrefabDefaultUIButton;
            obj = CreateGameObjectByPrefab(objectName, packageType, canvas.transform, prefabPath);
            if (obj == null)
            {
                return null;
            }

            obj.transform.localPosition = Vector3.zero;

            var button = obj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }

            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            AddLocalizeStringEvent(buttonText, localizationTable, localizationKey);

            if (buttonText != null)
            {
                buttonText.text = text;
            }

            return button;
        }

        /// <summary>
        /// TextMeshProUGUI 에 LocalizeStringEvent 를 추가하고,
        /// 테이블/키 설정 및 OnUpdateString 이벤트를 연결합니다.
        /// </summary>
        private static void AddLocalizeStringEvent(
            TextMeshProUGUI objectText,
            string localizationTable,
            string localizationKey)
        {
            if (objectText == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(localizationTable) || string.IsNullOrEmpty(localizationKey))
            {
                return;
            }

            var localizeEvent = objectText.gameObject.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
            {
                localizeEvent = objectText.gameObject.AddComponent<LocalizeStringEvent>();
            }

            // 테이블 및 키 설정
            localizeEvent.SetTable(localizationTable);
            localizeEvent.SetEntry(localizationKey);

#if UNITY_6000_0_OR_NEWER
            // Unity 6 이상: 직접 TextMeshProUGUI.SetText 연결
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                localizeEvent.OnUpdateString,
                objectText.SetText);
#else
            // 하위 버전: Proxy 를 통해 SetText 연결
            var proxy = objectText.gameObject.GetComponent<LocalizationTextProxy>();
            if (proxy == null)
            {
                proxy = objectText.gameObject.AddComponent<LocalizationTextProxy>();
                proxy.target = objectText;
            }

            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                localizeEvent.OnUpdateString,
                proxy.SetText);
#endif

            // Editor와 Runtime 모두에서 동작하도록 설정
            for (int i = 0; i < localizeEvent.OnUpdateString.GetPersistentEventCount(); i++)
            {
                localizeEvent.OnUpdateString.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
            }

            localizeEvent.RefreshString();
        }

        /// <summary>
        /// 기본 TextMeshProUGUI 오브젝트를 생성하거나 찾아서 반환합니다.
        /// - 이름: {Prefix}_{objectName}
        /// - 프리팹 경로: ConfigEditor.PathPrefabDefaultUITextMeshProGUI
        /// - metaData 가 주어지면 RectTransform/폰트/정렬/로컬라이제이션까지 설정합니다.
        /// </summary>
        public static TextMeshProUGUI CreateObjectText(
            string objectName,
            ConfigPackageInfo.PackageType packageType,
            MetaDataTextMeshProGUI metaDataTextMeshProGUI = null)
        {
            objectName = GenerateObjectName(objectName, packageType);

            // 이미 존재하는 오브젝트 검색
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
            {
                return obj.GetComponentInChildren<TextMeshProUGUI>();
            }

            // Canvas 확보
            Canvas canvas = CreateObjectCanvas(packageType);

            // 기본 TMP 프리팹 생성
            string prefabPath = ConfigEditor.PathPrefabDefaultUITextMeshProGUI;
            obj = CreateGameObjectByPrefab(objectName, packageType, canvas.transform, prefabPath);
            if (obj == null)
            {
                return null;
            }

            var textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI == null)
            {
                Debug.LogError("TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
                return null;
            }

            if (metaDataTextMeshProGUI == null)
            {
                return textMeshProUGUI;
            }

            // 로컬라이제이션 설정
            AddLocalizeStringEvent(
                textMeshProUGUI,
                metaDataTextMeshProGUI.localizationTable,
                metaDataTextMeshProGUI.localizationKey);

            // RectTransform 설정
            var rect = textMeshProUGUI.rectTransform;
            rect.SetAnchor(metaDataTextMeshProGUI.anchorPresets);
            rect.anchoredPosition = metaDataTextMeshProGUI.position;

            if (metaDataTextMeshProGUI.width > 0f && metaDataTextMeshProGUI.height > 0f)
            {
                rect.sizeDelta = new Vector2(
                    metaDataTextMeshProGUI.width,
                    metaDataTextMeshProGUI.height);
            }

            if (metaDataTextMeshProGUI.fontSize > 0f)
            {
                textMeshProUGUI.fontSize = metaDataTextMeshProGUI.fontSize;
            }

            // 정렬 설정
            TextMeshProHelper.SetAlignment(
                textMeshProUGUI,
                metaDataTextMeshProGUI.horizontalAlignment,
                metaDataTextMeshProGUI.verticalAlignment);

            return textMeshProUGUI;
        }

        /// <summary>
        /// 패키지 Prefix가 적용된 이름으로 GameObject를 찾습니다.
        /// </summary>
        public static GameObject Find(string objectName, ConfigPackageInfo.PackageType packageType)
        {
            return GameObject.Find(GenerateObjectName(objectName, packageType));
        }
    }
}
