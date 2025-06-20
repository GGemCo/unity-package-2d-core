using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Scripts
{
    public class DefaultAddressableLoader
    {
        // protected async Task<T> LoadByKey<T>(string key)
        // {
        //     try
        //     {
        //     }
        //     catch (Exception ex)
        //     {
        //         GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
        //     }
        // }
        protected async Task LoadByLabel(string label, 
            Action<Dictionary<int, GameObject>> onLoaded)
        {
            try
            {
                var locationHandle = Addressables.LoadResourceLocationsAsync(label);
                await locationHandle.Task;
                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{label} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                // int totalCount = locationHandle.Result.Count;
                // int loadedCount = 0;
                Dictionary<int, GameObject> dict = new Dictionary<int, GameObject>();
                foreach (var location in locationHandle.Result)
                {
                    // addressable 에 등록된 프리팹 하위에 mesh 가 있어서 type 검사 추가
                    if (location.ResourceType != typeof(GameObject)) continue;
                    
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<GameObject>(address);

                    while (!loadHandle.IsDone)
                    {
                        // prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }

                    GameObject prefab = await loadHandle.Task;
                    if (!prefab) continue;
                    
                    dict[int.Parse(address)] = prefab;
                    // loadedCount++;
                }
                onLoaded?.Invoke(dict);
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"설정 로딩 중 오류 발생: {ex.Message}");
            }
        }
    }
}