using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인스턴스 아이템(랜덤 옵션 등)을 보관/저장하는 중앙 저장소.
    /// - SaveRegistry 확장 섹션으로 저장된다.
    /// </summary>
    public sealed class ItemInstanceDatabase : MonoBehaviour, ISaveContributor
    {
        public const string Section = "core.item_instances";

        public string SectionKey => Section;

        /// <summary>
        /// Save 데이터 우선순위.
        /// Inventory/Equip이 InstanceId를 참조할 수 있도록 기본값보다 약간 먼저 저장/복원한다.
        /// </summary>
        public int Priority => 50;

        private long _nextId = 1;
        private readonly Dictionary<long, ItemInstanceData> _byId = new();

        private void Awake()
        {
            SaveRegistry.Register(this);
        }

        private void OnDestroy()
        {
            SaveRegistry.Unregister(this);
        }

        public bool TryGet(long instanceId, out ItemInstanceData data)
        {
            if (instanceId <= 0)
            {
                data = null;
                return false;
            }
            return _byId.TryGetValue(instanceId, out data);
        }

        /// <summary>
        /// 새 인스턴스를 등록하고 ID를 부여한다.
        /// </summary>
        public long RegisterNew(ItemInstanceData instance)
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

        public void Capture(SaveEnvelope env)
        {
            env.SetSection(SectionKey, new ItemInstanceSaveDto
            {
                NextId = _nextId,
                Items = new List<ItemInstanceData>(_byId.Values),
            });
        }

        public void Restore(SaveEnvelope env)
        {
            _byId.Clear();
            _nextId = 1;

            if (env == null) return;
            if (!env.TryGetSection<ItemInstanceSaveDto>(SectionKey, out var dto) || dto == null)
                return;

            _nextId = Math.Max(1, dto.NextId);
            if (dto.Items == null) return;

            for (int i = 0; i < dto.Items.Count; i++)
            {
                var it = dto.Items[i];
                if (it == null || it.InstanceId <= 0) continue;
                _byId[it.InstanceId] = it;
                _nextId = Math.Max(_nextId, it.InstanceId + 1);
            }
        }

        [Serializable]
        private sealed class ItemInstanceSaveDto
        {
            public long NextId;
            public List<ItemInstanceData> Items;
        }
    }
}
