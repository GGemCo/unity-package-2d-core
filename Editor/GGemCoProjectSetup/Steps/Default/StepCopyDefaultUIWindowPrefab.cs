using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public class StepCopyDefaultUIWindowPrefab : SetupStepBase
    {
        private const string PathSrc = "Packages/com.ggemco.2d.core/Samples~/UIWindows";
        private const string PathDist = ConfigDefine.PathGGemCo+"/UIWindows";
        
        private readonly List<string> _pathSrc = new List<string>
        {
            $"{PathSrc}/Option"
        };

        private readonly List<string> _pathDist = new List<string>
        {
            $"{PathDist}/Option"
        };
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            foreach (var src in _pathSrc)
            {
                // 내용이 비어있는 테이블 파일은 건너띄기
                if (src == "EmptyDataTable") continue;
                
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
            for (int i = 0; i < _pathSrc.Count; ++i)
            {
                var src = _pathSrc[i];
                var dist = _pathDist[i];
                HelperFile.CopyDirectory(src, dist);
            }
            
            AssetDatabase.Refresh();
        }
    }
}