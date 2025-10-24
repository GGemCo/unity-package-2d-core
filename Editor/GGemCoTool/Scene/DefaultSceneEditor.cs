using GGemCo2DCore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GGemCo2DCoreEditor
{
    public class DefaultSceneEditor : DefaultEditorWindow
    {
        private Scene GetActiveSceneInEditor()
        {
            // 현재 에디터에서 활성화된 씬을 가져옴
            return SceneManager.GetActiveScene();
        }
        protected bool CheckCurrentLoadedScene(string sceneName)
        {
            Scene scene = GetActiveSceneInEditor();
            return scene.name == sceneName;
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        protected SoundManager CreateSoundManager(Transform parent = null)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("SoundManager", packageType, parent);
            if (!obj) return null;
            SoundManager soundManager = obj.GetComponent<SoundManager>();
            if (soundManager == null)
            {
                soundManager = obj.AddComponent<SoundManager>();
            }
            return soundManager;
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        protected PopupManager CreatePopupManager(Transform parent = null)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("PopupManager", packageType, parent, ConfigEditor.PathPrefabPopupManager);
            if (!obj) return null;
            PopupManager popupManager = obj.GetComponent<PopupManager>();
            if (popupManager == null)
            {
                obj.AddComponent<PopupManager>();
            }
            
            Transform transform = CreateUIComponent.Find("Canvas", packageType).transform;
            
            popupManager.SetCanvasPopup(transform);
            GameObject[] prefabs = new[] { null, ConfigResources.PopupDefault.Load() };
            popupManager.SetPopupTypePrefabs(prefabs);
            return popupManager;
        }

        protected TextMeshProUGUI CreateLoadingText()
        {
            string fieldName = "textLoadingPercent";
            MetaDataTextMeshProGUI metaDataTextMeshProGUI =
                new MetaDataTextMeshProGUI(new Vector2(1, 0), new Vector2(-100, 100), AnchorPresets.BottomRight, 1000,
                    50, 0, TextMeshProHelper.HorizontalAlignment.Right, TextMeshProHelper.VerticalAlignment.Middle,
                    LocalizationConstants.Tables.Scene,
                    LocalizationConstants.Keys.Loading.TextLoadingPercent());
            TextMeshProUGUI textMeshProUGUI = CreateUIComponent.CreateObjectText(fieldName, packageType, metaDataTextMeshProGUI);
            return textMeshProUGUI;
        }
    }
}