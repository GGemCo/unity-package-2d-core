using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// sound 대표 테이블 저장 후처리 프로세서입니다.
    /// </summary>
    /// <remarks>
    /// sound 대표 테이블의 레거시 FileName/리소스 컬럼 경로가 제거되어
    /// Addressables 파일 검증과 증분 동기화는 sound_* 리소스 테이블 프로세서에서만 수행합니다.
    /// </remarks>
    internal sealed class TableEditorSoundSaveProcessor : TableEditorSaveProcessorBase
    {
        /// <summary>
        /// 처리 대상 테이블 키입니다.
        /// </summary>
        protected override string TargetTableKey => ConfigAddressableTable.Sound;

        /// <summary>
        /// 실행 우선순위입니다.
        /// </summary>
        public override int Order => 10;
    }
}

