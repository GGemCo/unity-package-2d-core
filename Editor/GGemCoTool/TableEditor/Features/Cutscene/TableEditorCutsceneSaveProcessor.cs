using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorCutsceneSaveProcessor : ITableEditorSaveProcessor
    {
        public int Order => 100;

        public bool CanProcess(TableEditorSaveContext context)
        {
            return context != null && context.IsTable(ConfigAddressableTable.Cutscene);
        }

        public void BeforeSave(TableEditorSaveContext context)
        {
        }

        public void AfterSave(TableEditorSaveContext context)
        {
            SettingCutscene.SyncFromTable(new SettingCutsceneOptions
            {
                ShowConfirmDialog = false,
                ShowCompletedDialog = false,
            });
        }
    }
}
