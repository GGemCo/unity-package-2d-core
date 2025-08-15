using System.Collections;

namespace GGemCo2DCore
{
    public sealed class SaveDataLoadStep : GameLoadStepBase
    {
        private readonly SaveDataLoader _saveDataLoader;

        public SaveDataLoadStep(int order,
            string localizedKey,
            SaveDataLoader saveDataLoader)
            : base("core.savedata", order, localizedKey)
        {
            _saveDataLoader = saveDataLoader;
        }

        public override IEnumerator Run()
        {
            yield return _saveDataLoader.LoadData(p =>
            {
                // p: 0~1
                progress = p;
            });
            progress = 1f;
        }
    }
}