using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 아이템 테이블을 기반으로 아이템 관련 이미지(드랍/아이콘/장비 파츠)를 Addressables에 등록하고,
    /// 각 카테고리별 SpriteAtlas를 생성/갱신하여 패킹(Pack)까지 수행하는 설정 모듈입니다.
    /// </summary>
    /// <remarks>
    /// 구성 요소:
    /// - Addressables 그룹: DropImage / IconImage / EquipImage
    /// - SpriteAtlas: ItemDropAtlas / ItemIconAtlas / ItemEquipAtlas
    /// 동작 정책:
    /// - 대상 그룹 엔트리를 초기화(Clear)한 뒤, 테이블 데이터를 바탕으로 엔트리를 재등록합니다.
    /// - 장비(Equip) 아틀라스는 Spine 등에서 사용하기 위해 Read/Write를 활성화합니다.
    /// </remarks>
    public class SettingItem : DefaultAddressable
    {
        private const string Title = "아이템 아이콘, 드랍 이미지 추가하기";

        /// <summary>이 모듈이 UI/테이블/공용 설정에 접근하기 위해 참조하는 AddressableEditor 인스턴스입니다.</summary>
        private readonly AddressableEditor _addressableEditor;

        /// <summary>아이템 아이콘 이미지가 등록될 Addressables 그룹 이름입니다.</summary>
        private readonly string _groupNameIconImage;

        /// <summary>아이템 장비(Equip) 파츠 이미지가 등록될 Addressables 그룹 이름입니다.</summary>
        private readonly string _groupNameEquipImage;

        /// <summary>
        /// 아이템(Addressables) 설정 모듈을 생성합니다.
        /// </summary>
        /// <param name="addressableEditorWindow">테이블/레이아웃 등 공용 정보를 제공하는 에디터 윈도우</param>
        public SettingItem(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;

            // 기본 대상 그룹(드랍 이미지)
            targetGroupName = ConfigAddressableGroupName.ItemGroup.DropImage;

            // 추가 그룹(아이콘/장비)
            _groupNameIconImage = ConfigAddressableGroupName.ItemGroup.IconImage;
            _groupNameEquipImage = ConfigAddressableGroupName.ItemGroup.EquipImage;
        }

        /// <summary>
        /// SettingItem 전용 UI를 렌더링합니다.
        /// 테이블이 없으면 안내 메시지를 표시하고, 버튼 클릭 시 Setup을 실행합니다.
        /// </summary>
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (TableLoaderManager.LoadItemTable() == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Item} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(
                        Title,
                        GUILayout.Width(_addressableEditor.buttonWidth),
                        GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(
                            Title,
                            "아이템 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.",
                            "OK");
                    }
                }
            }
        }

        /// <summary>
        /// 아이템 관련 이미지(Addressables 엔트리)와 SpriteAtlas를 생성/갱신하고 저장합니다.
        /// </summary>
        /// <param name="ctx">
        /// 파이프라인/배치 실행 컨텍스트(로그/정책 공유)입니다.
        /// null이면 사용자 확인 다이얼로그를 띄운 뒤 진행합니다.
        /// </param>
        /// <remarks>
        /// 부작용(Side effects):
        /// - Addressables 그룹 엔트리 초기화 및 재등록
        /// - SpriteAtlas 파일 생성/수정 및 강제 Pack
        /// - 에셋 저장(AssetDatabase.SaveAssets)
        /// - 완료 후 테이블 리로드(_addressableEditor.LoadTables)
        /// </remarks>
        public void Setup(EditorSetupContext ctx = null)
        {
            if (ctx == null)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result) return;
            }

            // 아이템 테이블 데이터(UID 기반)를 읽어 Addressables 엔트리를 구성합니다.
            Dictionary<int, StruckTableItem> dictionary = TableLoaderManager.LoadItemTable().GetDatas();

            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // 그룹(드랍/장비/아이콘) 가져오기 또는 생성
            AddressableAssetGroup groupDropImage = GetOrCreateGroup(settings, targetGroupName);
            AddressableAssetGroup groupEquipImage = GetOrCreateGroup(settings, _groupNameEquipImage);
            AddressableAssetGroup groupIconImage = GetOrCreateGroup(settings, _groupNameIconImage);

            // 매 실행 시 그룹 엔트리를 초기화하고 테이블 기반으로 재구성합니다.
            ClearGroupEntries(settings, groupDropImage);
            ClearGroupEntries(settings, groupEquipImage);
            ClearGroupEntries(settings, groupIconImage);

            // SpriteAtlas 생성/갱신 준비
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);

            var atlasDrop = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemDropAtlas.spriteatlas");
            var atlasIcon = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemIconAtlas.spriteatlas");
            var pathEquipAtlas = $"{atlasFolderPath}/ItemEquipAtlas.spriteatlas";
            SpriteAtlas atlasEquip = GetOrCreateSpriteAtlas(pathEquipAtlas);

            // Atlas에 포함시킬 원본 에셋 목록
            List<Object> assetsDrop = new();
            List<Object> assetsIcon = new();
            List<Object> assetsEquip = new();

            // 테이블 기반 등록
            foreach (KeyValuePair<int, StruckTableItem> outerPair in dictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;

                // Drop 이미지(UID별 개별 엔트리 + Atlas 후보 등록)
                string dropPath = $"{ConfigAddressablePath.Root}/{info.ImageItemPath}.png";
                Add(settings, groupDropImage, $"{ConfigAddressableLabel.ImageItemDrop}_{info.Uid}", dropPath);
                AddToListIfExists(assetsDrop, dropPath);

                // Icon 이미지(UID별 개별 엔트리 + Atlas 후보 등록)
                string iconPath = $"{ConfigAddressablePath.Root}/{info.ImagePath}.png";
                Add(settings, groupIconImage, $"{ConfigAddressableLabel.ImageItemIcon}_{info.Uid}", iconPath);
                AddToListIfExists(assetsIcon, iconPath);

                // 장비가 아니면 Equip 이미지는 생략
                if (info.Type != ItemConstants.Type.Equip) continue;

                // Equip 이미지(파츠/슬롯 조합으로 여러 파일이 존재할 수 있음)
                string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{info.Uid}";
                List<string> slotNames = ItemConstants.SlotNameByPartsType.GetValueOrDefault(info.PartsID);
                if (slotNames != null)
                {
                    foreach (string slotName in slotNames)
                    {
                        if (string.IsNullOrEmpty(slotName)) continue;

                        string equipPath = $"{ConfigAddressablePath.Root}/{info.PartsImagePath}_{slotName}.png";
                        Add(settings, groupEquipImage, baseKey, equipPath);
                        AddToListIfExists(assetsEquip, equipPath);
                    }
                }
            }

            // 기본 장비 이미지(테이블에 없어도 사용될 수 있는 디폴트 파츠) 등록
            foreach (var data in ItemConstants.FolderNameByPartsType)
            {
                var partType = data.Key;
                if (partType == ItemConstants.PartsType.None) continue;

                string folderName = data.Value;
                if (string.IsNullOrEmpty(folderName)) continue;

                List<string> slotNames = ItemConstants.SlotNameByPartsType.GetValueOrDefault(partType);
                if (slotNames == null) continue;

                foreach (var slotName in slotNames)
                {
                    if (string.IsNullOrEmpty(slotName)) continue;

                    string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{folderName}_{slotName}";
                    string equipPath = $"{ConfigAddressablePath.Images.Parts}/{folderName}/{slotName}.png";
                    Add(settings, groupEquipImage, baseKey, equipPath);
                    AddToListIfExists(assetsEquip, equipPath);
                }
            }

            // blank 아이콘(개별 등록 + atlas 포함)
            AddBlankIconToLists(settings, groupIconImage, assetsIcon);

            // Atlas 구성(기존 packables 정리 후 새 목록 반영)
            ClearAndAddToAtlas(atlasDrop, assetsDrop);
            ClearAndAddToAtlas(atlasIcon, assetsIcon);
            ClearAndAddToAtlas(atlasEquip, assetsEquip);

            // Atlas 자체를 Addressables에 등록(라벨 포함)
            if (assetsDrop.Count > 0)
                Add(settings, groupDropImage, ConfigAddressableLabel.ImageItemDrop, AssetDatabase.GetAssetPath(atlasDrop), ConfigAddressableLabel.ImageItemDrop);

            if (assetsIcon.Count > 0)
                Add(settings, groupIconImage, ConfigAddressableLabel.ImageItemIcon, AssetDatabase.GetAssetPath(atlasIcon), ConfigAddressableLabel.ImageItemIcon);

            if (assetsEquip.Count > 0)
                Add(settings, groupEquipImage, ConfigAddressableLabel.ImageItemEquip, AssetDatabase.GetAssetPath(atlasEquip), ConfigAddressableLabel.ImageItemEquip);

            // 강제로 pack 시키기(Atlas가 변경되었을 때 즉시 결과를 반영하기 위함)
            if (assetsDrop.Count > 0 || assetsIcon.Count > 0 || assetsEquip.Count > 0)
                SpriteAtlasUtility.PackAtlases(new[] { atlasDrop, atlasIcon, atlasEquip }, EditorUserBuildSettings.activeBuildTarget, false);
            
            // 장비 아틀라스는 Spine 등에서 사용하기 위해 Read/Write를 활성화합니다.
            AssetDatabase.ImportAsset(pathEquipAtlas,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var equipAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(pathEquipAtlas);
            if (equipAtlas)
            {
                SpriteAtlasTextureSettings spriteAtlasTextureSettings = new SpriteAtlasTextureSettings
                {
                    anisoLevel = 1,
                    readable = true,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear
                };
                equipAtlas.SetTextureSettings(spriteAtlasTextureSettings);
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 아이템 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] 아이템 설정 완료", "OK");
            }
        }

        /// <summary>
        /// blank 아이콘을 아이콘 그룹과 Atlas 후보 목록에 추가합니다.
        /// </summary>
        /// <param name="settings">Addressable 설정 객체</param>
        /// <param name="groupIconImage">아이콘 이미지 그룹</param>
        /// <param name="assetsIcon">아이콘 Atlas에 포함될 에셋 목록</param>
        private void AddBlankIconToLists(AddressableAssetSettings settings, AddressableAssetGroup groupIconImage, List<Object> assetsIcon)
        {
            string key = $"{ConfigAddressableLabel.ImageItemIcon}_blank";
            string path = GetBlankIconPath();

            Add(settings, groupIconImage, key, path);
            AddToListIfExists(assetsIcon, path);
        }

        /// <summary>
        /// 아이콘 그룹에 blank 이미지를 Addressables 및 SpriteAtlas에 등록합니다.
        /// </summary>
        /// <remarks>
        /// - Addressables 그룹/라벨 규칙은 SettingItem의 설정을 그대로 따릅니다.
        /// - 아이콘 Atlas(ItemIconAtlas)가 없으면 생성됩니다.
        /// - 외부 Step/툴에서도 호출할 수 있도록 public 으로 제공합니다.
        /// </remarks>
        /// <param name="ctx">배치 실행 컨텍스트(로그 출력에 사용)</param>
        /// <param name="saveAssets">true면 SaveAssets/Refresh까지 수행합니다.</param>
        /// <param name="packAtlas">true면 Atlas를 강제 Pack 합니다.</param>
        public void AddBlankIconOnly(EditorSetupContext ctx = null, bool saveAssets = true, bool packAtlas = true)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // Icon 그룹 가져오기 또는 생성
            AddressableAssetGroup groupIconImage = GetOrCreateGroup(settings, _groupNameIconImage);

            // Icon Atlas 가져오기 또는 생성
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);

            var atlasIcon = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemIconAtlas.spriteatlas");

            // blank 정보
            string key = $"{ConfigAddressableLabel.ImageItemIcon}_blank";
            string path = GetBlankIconPath();

            // 1) blank 이미지 Addressables 등록(개별 엔트리)
            Add(settings, groupIconImage, key, path);

            // 2) Atlas에 blank 추가(존재하면 중복 추가 방지)
            AddSingleToAtlas(atlasIcon, path);

            // 3) Atlas 자체를 Addressables에 등록(라벨 포함)
            AddOrUpdateAtlasAddressableEntry(
                settings,
                groupIconImage,
                atlasIcon,
                ConfigAddressableLabel.ImageItemIcon,
                ConfigAddressableLabel.ImageItemIcon);

            // 4) 선택적으로 강제 Pack
            if (packAtlas)
            {
                SpriteAtlasUtility.PackAtlases(
                    new[] { atlasIcon },
                    EditorUserBuildSettings.activeBuildTarget,
                    false);
            }

            // 5) 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (ctx != null)
                HelperLog.Info("[Addressable] blank 아이콘 등록 완료", ctx);
        }

        /// <summary>
        /// blank 아이콘의 Asset 경로를 반환합니다.
        /// </summary>
        /// <returns>blank.png의 프로젝트 내 상대 경로</returns>
        public static string GetBlankIconPath()
        {
            return $"{ConfigAddressablePath.Root}/Images/Icon/blank.png";
        }

        /// <summary>
        /// Atlas에 단일 asset을 추가합니다(이미 포함되어 있으면 추가하지 않음).
        /// </summary>
        /// <param name="atlas">대상 SpriteAtlas</param>
        /// <param name="assetPath">추가할 에셋 경로</param>
        private static void AddSingleToAtlas(SpriteAtlas atlas, string assetPath)
        {
            if (atlas == null || string.IsNullOrEmpty(assetPath))
                return;

            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj == null)
                return;

            // 이미 포함되어 있는지 확인
            var packables = atlas.GetPackables();
            if (packables != null)
            {
                foreach (var p in packables)
                {
                    if (p == obj)
                        return;
                }
            }

            atlas.Add(new[] { obj });
            EditorUtility.SetDirty(atlas);
        }

        /// <summary>
        /// Atlas를 Addressables 그룹에 등록하거나, 이미 등록되어 있으면 라벨을 보장합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정</param>
        /// <param name="group">등록 대상 그룹</param>
        /// <param name="atlas">등록할 Atlas 에셋</param>
        /// <param name="entryKey">Addressables 엔트리 키</param>
        /// <param name="label">부여할 라벨</param>
        private void AddOrUpdateAtlasAddressableEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            SpriteAtlas atlas,
            string entryKey,
            string label)
        {
            if (settings == null || group == null || atlas == null)
                return;

            string atlasPath = AssetDatabase.GetAssetPath(atlas);
            if (string.IsNullOrEmpty(atlasPath))
                return;

            // Add(...)가 내부에서 엔트리 생성/갱신을 처리한다고 가정(기존 코드와 동일 사용)
            Add(settings, group, entryKey, atlasPath, label);
        }
    }
}
