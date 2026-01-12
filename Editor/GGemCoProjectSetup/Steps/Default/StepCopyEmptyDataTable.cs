using System.IO;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Samples~/EmptyDataTable 폴더를 DataAddressable로 복사
    /// </summary>
    public class StepCopyEmptyDataTable : SetupStepBase
    {
        private readonly string _srcTables = ConfigEditor.PathPackageCore+"/Samples~/EmptyDataTable";
        private readonly string _dstTables = ConfigAddressablePath.Root+"/Tables";

        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            var src = _srcTables;
            if (!Directory.Exists(src))
            {
                msg = $"소스 폴더 없음: {src}";
                return false;
            }
            msg = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            HelperFile.CopyDirectory(_srcTables, _dstTables);
            
            HelperLog.Info($"{_srcTables} -> {_dstTables} 경로로 복사 완료했습니다.", ctx);
        }
    }
}
