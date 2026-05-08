namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 암호화 적용 방식을 정의합니다.
    /// </summary>
    public enum SaveDataEncryptionMode
    {
        /// <summary>
        /// 저장 데이터를 기존처럼 평문 JSON으로 저장합니다.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// 평문 저장 파일을 읽을 수 있고, 다음 저장 시 암호화 파일로 마이그레이션합니다.
        /// </summary>
        OptionalMigration = 1,

        /// <summary>
        /// 암호화된 저장 파일만 허용합니다.
        /// </summary>
        Required = 2,
    }
}
