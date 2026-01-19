using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 인스턴스(랜덤 옵션 등)를 관리하는 런타임 저장소.
    /// </summary>
    /// <remarks>
    /// - MonoBehaviour/SaveRegistry에 의존하지 않고, <see cref="SaveDataManager"/>의 컨테이너에 포함되어 저장/복원된다.
    /// - 인벤토리/장비/창고 등은 <see cref="SaveDataIcon.InstanceId"/>로 인스턴스를 참조한다.
    /// </remarks>
    public sealed class ItemInstanceStore
    {
        private long _nextId = 1;
        private readonly Dictionary<long, ItemInstanceInfo> _byId = new();

        /// <summary>
        /// 저장 데이터로부터 상태를 복원한다.
        /// </summary>
        public void Restore(ItemInstanceStoreData data)
        {
            _byId.Clear();
            _nextId = 1;

            if (data == null)
                return;

            _nextId = Math.Max(1, data.NextId);
            if (data.Items == null) return;

            for (int i = 0; i < data.Items.Count; i++)
            {
                var it = data.Items[i];
                if (it == null || it.InstanceId <= 0) continue;
                _byId[it.InstanceId] = it;
                _nextId = Math.Max(_nextId, it.InstanceId + 1);
            }
        }

        /// <summary>
        /// 현재 상태를 저장 데이터로 캡처한다.
        /// </summary>
        public ItemInstanceStoreData Capture()
        {
            return new ItemInstanceStoreData
            {
                NextId = _nextId,
                Items = new List<ItemInstanceInfo>(_byId.Values),
            };
        }

        public bool TryGet(long instanceId, out ItemInstanceInfo info)
        {
            if (instanceId <= 0)
            {
                info = null;
                return false;
            }
            return _byId.TryGetValue(instanceId, out info);
        }

        /// <summary>
        /// 새 인스턴스를 등록하고 ID를 부여한다.
        /// </summary>
        public long RegisterNew(ItemInstanceInfo instance)
        {
            if (instance == null) return 0;

            if (instance.InstanceId <= 0)
                instance.InstanceId = _nextId++;
            else
                _nextId = Math.Max(_nextId, instance.InstanceId + 1);

            _byId[instance.InstanceId] = instance;
            return instance.InstanceId;
        }

        public void Remove(long instanceId)
        {
            if (instanceId <= 0) return;
            _byId.Remove(instanceId);
        }
    }
}
