using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public class StepCopyAllSampleData : SetupStepBase
    {
        private readonly List<string> _folderName = new List<string>
        {
            "Audio","Data","DataAddressable","Fonts","Localization","UIWindows"
        };
        
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            foreach (var name in _folderName)
            {
                // 내용이 비어있는 테이블 파일은 건너띄기
                if (name == "EmptyDataTable") continue;
                
                var src = $"Packages/com.ggemco.2d.core/Samples~/{name}";
                if (!Directory.Exists(src))
                {
                    msg = $"소스 폴더 없음: {src}";
                    return false;
                }
            }
            msg = null;
            return true;
        }
        public override void Execute(EditorSetupContext ctx)
        {
            foreach (var name in _folderName)
            {
                var sourceFolder = $"Packages/com.ggemco.2d.core/Samples~/{name}";
                var targetFolder = $"{ConfigDefine.PathGGemCo}/{name}";
                bool forceUpdate = name is "DataAddressable" or "UIWindows";
                HelperFile.CopyDirectory(sourceFolder, targetFolder);
            }
            
            HelperLog.Info($"[{nameof(StepCopyAllSampleData)}] {_folderName.ToArray()} 복사 완료.", ctx);
        }
    }
}