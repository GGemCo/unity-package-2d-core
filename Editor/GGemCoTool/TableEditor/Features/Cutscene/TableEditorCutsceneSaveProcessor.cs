using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// cutscene 테이블 저장 후 Addressables 동기화를 담당하는 SaveProcessor입니다.
    /// </summary>
    internal sealed class TableEditorCutsceneSaveProcessor : TableEditorSaveProcessorBase
    {
        /// <summary>
        /// 처리 대상 테이블 키입니다.
        /// </summary>
        protected override string TargetTableKey => ConfigAddressableTable.Cutscene;

        /// <summary>
        /// cutscene 후처리는 검증 프로세서 이후에 실행되도록 우선순위를 지정합니다.
        /// </summary>
        public override int Order => 100;

        /// <summary>
        /// cutscene 테이블 저장 완료 후 Addressables cutscene 그룹을 자동 동기화합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public override void AfterSave(TableEditorSaveContext context)
        {
            SettingCutscene.SyncFromTable(new SettingCutsceneOptions
            {
                ShowConfirmDialog = false,
                ShowCompletedDialog = false,
            });
        }
    }
}
