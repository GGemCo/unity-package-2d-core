using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public class StepCopyDefaultDataAddressable : SetupStepBase
    {
        private const string PathSrc = "Packages/com.ggemco.2d.core/Samples~/DataAddressable";
        private const string PathDist = ConfigDefine.PathGGemCo+"/DataAddressable";
        
        private readonly List<string> _pathSrc = new List<string>
        {
            $"{PathSrc}/Images/Icon/blank.png"
        };

        private readonly List<string> _pathDist = new List<string>
        {
            $"{PathDist}/Images/Icon/blank.png"
        };
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            foreach (var src in _pathSrc)
            {
                // 내용이 비어있는 테이블 파일은 건너띄기
                if (src == "EmptyDataTable") continue;
                
                if (!File.Exists(src))
                {
                    msg = $"소스 파일 없음: {src}";
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
                HelperFile.CopyFile(src, dist);
                HelperLog.Info($"{src} -> {dist} 경로로 복사 완료했습니다.", ctx);
                
                // meta 파일
                var srcMeta = $"{_pathSrc[i]}.meta";
                var distMeta = $"{_pathDist[i]}.meta";
                HelperFile.CopyFile(srcMeta, distMeta);
            }
        }
    }
}