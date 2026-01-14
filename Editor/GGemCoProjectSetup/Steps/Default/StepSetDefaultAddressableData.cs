namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Addressables 기본 데이터(설정/테이블/필수 아이콘 등)를 초기 상태로 구성하는 설정 스텝입니다.
    /// 에디터 전용 AddressableEditor 인스턴스를 생성하여 각 설정 모듈(스크립터블/테이블/아이템)을 순차 적용합니다.
    /// </summary>
    public class StepSetDefaultAddressableData : SetupStepBase
    {
        /// <summary>
        /// Addressables 기본 데이터 설정을 수행하기 전 사전 조건을 검증합니다.
        /// 현재 구현은 항상 통과하며, 필요 시 프로젝트 설정/패키지 존재 여부 등을 여기서 점검할 수 있습니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        /// <param name="msg">검증 실패 시 사용자에게 표시할 메시지</param>
        /// <returns>검증이 통과되면 true, 실패하면 false</returns>
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            msg = null;
            return true;
        }

        /// <summary>
        /// Addressables 기본 데이터 구성을 실행합니다.
        /// 설정 ScriptableObject 구성 → 테이블 그룹 정리/재구성 → 필수 blank 아이콘 등록 순으로 처리합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        public override void Execute(EditorSetupContext ctx)
        {
            // Addressables 편집 작업을 위한 에디터 전용 인스턴스 생성
            var addressableEditor = ctx.addressableEditor;

            // settings 관련 ScriptableObject 구성/초기화
            var settingScriptableObject = new SettingScriptableObject(addressableEditor);
            settingScriptableObject.Setup(ctx);

            // 테이블(그룹/엔트리 등) 구성: 기존 그룹 정리 후 재설정
            var settingTable = new SettingTable(addressableEditor);
            // StepCopyEmptyDataTable 에서 이미 복사된 테이블이 있을 수 있어, 지우고 다시 등록
            settingTable.ClearGroup(ctx);
            settingTable.Setup(ctx);

            // 기본 리소스 등록: blank 아이콘(아이콘만) 추가
            var settingItem = new SettingItem(addressableEditor);
            settingItem.AddBlankIconOnly(ctx, false);
        }
    }
}