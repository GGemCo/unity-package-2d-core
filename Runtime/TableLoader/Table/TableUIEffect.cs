using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 Timeline 테이블 행입니다.
    /// </summary>
    public class StruckTableUIEffect : IUidName
    {
        /// <summary>
        /// UI 효과 고유 UID입니다.
        /// </summary>
        public int Uid { get; set; }

        /// <summary>
        /// 에디터와 디버그 UI에서 표시할 이름입니다.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 제작자가 확인할 작업 메모입니다.
        /// </summary>
        public string Memo;

        /// <summary>
        /// Window, Popup, Hud 등 UI 효과 분류입니다.
        /// </summary>
        public string Category;

        /// <summary>
        /// UIEffectTimelineTargetRegistry에서 기본 타겟을 찾을 때 사용할 키입니다.
        /// </summary>
        public string TargetKey;

        /// <summary>
        /// 로딩 시 RuntimeSequence를 미리 로드할지 여부입니다.
        /// </summary>
        public bool PreLoad;

        /// <summary>
        /// 기본 반복 재생 여부입니다.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// 검증 또는 툴 표시용 기준 길이입니다.
        /// </summary>
        public float DefaultDuration;

        /// <summary>
        /// 해당 UI 효과를 사용할지 여부입니다.
        /// </summary>
        public bool Enabled;
    }

    /// <summary>
    /// ui_effect 테이블을 파싱하는 로더입니다.
    /// </summary>
    public class TableUIEffect : DefaultTable<StruckTableUIEffect>
    {
        /// <summary>
        /// Addressables 테이블 키를 반환합니다.
        /// </summary>
        public override string Key => ConfigAddressableTable.UIEffect;

        /// <summary>
        /// ui_effect 테이블 한 줄을 강타입 Row로 변환합니다.
        /// </summary>
        /// <param name="data">컬럼명과 원본 문자열 값으로 구성된 테이블 행 데이터입니다.</param>
        /// <returns>파싱된 UI 효과 테이블 행입니다.</returns>
        protected override StruckTableUIEffect BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableUIEffect
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                Memo = reader.String("Memo"),
                Category = reader.String("Category", "Common"),
                TargetKey = reader.String("TargetKey"),
                PreLoad = reader.BoolYN("PreLoad"),
                Loop = reader.BoolYN("Loop"),
                DefaultDuration = reader.Float("DefaultDuration"),
                Enabled = reader.BoolYN("Enabled", true),
            };
        }
    }
}
