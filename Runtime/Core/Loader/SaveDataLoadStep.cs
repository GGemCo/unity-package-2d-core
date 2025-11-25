using System.Collections;

namespace GGemCo2DCore
{
    public sealed class SaveDataLoadStep : GameLoadStepBase
    {
        private readonly SaveDataLoaderBase _saveDataLoaderBase;

        public SaveDataLoadStep(string id, int order,
            string localizedKey,
            SaveDataLoaderBase saveDataLoader)
            : base(id, order, localizedKey)
        {
            _saveDataLoaderBase = saveDataLoader;
        }

        public override IEnumerator Run()
        {
            yield return _saveDataLoaderBase.LoadData(p =>
            {
                // p: 0~1
                progress = p;
            });
            progress = 1f;
        }
    }
}