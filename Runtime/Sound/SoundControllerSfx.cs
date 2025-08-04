using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 효과음 컨트롤러 (풀 자동 확장 + 무제한 UID 지원)
    /// </summary>
    public class SoundControllerSfx
    {
        private readonly Transform _owner;
        private readonly AudioMixer _mixer;
        private readonly AudioMixerGroup _group;
        private readonly string _volumeParam;
        private readonly AddressableLoaderSound _loader;

        private readonly Dictionary<int, Queue<GameObject>> _pool = new();
        private readonly Dictionary<int, int> _playCount = new();
        private readonly Dictionary<int, int> _maxCount = new();
        private readonly Dictionary<int, StruckTableSound> _infoCache = new();

        private const int MaxAutoExpandCount = 10; // 자동 확장 허용 개수 제한 (0이면 무제한)
        private readonly Dictionary<int, int> _autoExpandedCount = new();

        public SoundControllerSfx(Transform owner, AudioMixer mixer, AudioMixerGroup group, string volumeParam, AddressableLoaderSound loader)
        {
            _owner = owner;
            _mixer = mixer;
            _group = group;
            _volumeParam = volumeParam;
            _loader = loader;
        }
        /// <summary>
        /// 효과음 pool 초기화
        /// </summary>
        public void Initialize(TableSound table)
        {
            if (table == null) return;

            foreach (var uid in table.GetDatas().Keys)
            {
                var info = table.GetDataByUid(uid);
                if (info.Type != SoundConstants.Type.Sfx) continue;

                _infoCache[uid] = info;
                _playCount[uid] = 0;
                _maxCount[uid] = info.MaxPlayCount;

                Queue<GameObject> pool = new();
                for (int i = 0; i < info.MaxPlayCount; i++)
                {
                    pool.Enqueue(CreateAudioSourceObject(uid, info));
                }
                _pool[uid] = pool;
                _autoExpandedCount[uid] = 0;
            }
        }

        /// <summary>
        /// 효과음 재생
        /// </summary>
        public void Play(int uid, MonoBehaviour coroutineHost)
        {
            if (!_pool.ContainsKey(uid)) return;

            bool isUnlimited = !_maxCount.ContainsKey(uid) || _maxCount[uid] == 0;
            bool canPlay = isUnlimited || _playCount[uid] < _maxCount[uid];

            if (!canPlay) return;

            GameObject obj = GetOrCreateAudioSource(uid);
            if (obj == null) return;

            AudioSource src = obj.GetComponent<AudioSource>();
            obj.SetActive(true);
            src.Play();

            _playCount[uid]++;
            coroutineHost.StartCoroutine(ReleaseAfter(obj, src.clip.length, uid));
        }

        /// <summary>
        /// AudioSource 오브젝트 반환
        /// </summary>
        private IEnumerator ReleaseAfter(GameObject obj, float delay, int uid)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
            _pool[uid].Enqueue(obj);
            _playCount[uid] = Mathf.Max(0, _playCount[uid] - 1);
        }

        /// <summary>
        /// 사용 가능한 AudioSource 또는 자동 생성
        /// </summary>
        private GameObject GetOrCreateAudioSource(int uid)
        {
            if (_pool[uid].Count > 0)
                return _pool[uid].Dequeue();

            if (!_infoCache.ContainsKey(uid)) return null;

            // 자동 확장 허용 여부 확인
            if (MaxAutoExpandCount > 0 && _autoExpandedCount[uid] >= MaxAutoExpandCount)
                return null;

            _autoExpandedCount[uid]++;
            return CreateAudioSourceObject(uid, _infoCache[uid]);
        }

        /// <summary>
        /// AudioSource GameObject 생성
        /// </summary>
        private GameObject CreateAudioSourceObject(int uid, StruckTableSound info)
        {
            GameObject obj = new GameObject($"{uid}_sfx");
            obj.transform.SetParent(_owner);

            var src = obj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = _group;
            src.volume = info.Volume;
            src.clip = _loader.GetAudioClip($"{ConfigAddressableGroupName.Sound}_{info.FileName}");
            obj.SetActive(false);

            return obj;
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetVolume(float volume, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            _mixer.SetFloat(_volumeParam, db);
            if (save)
            {
                PlayerPrefsManager.SaveSoundVolumeSfx(volume);
            }
        }
        public void OnDestroy()
        {
        }
    }
}
