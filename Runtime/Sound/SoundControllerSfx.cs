using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 효과음 컨트롤러 (풀 자동 확장 + 무제한 UID 지원)
    /// </summary>
    public class SoundControllerSfx
    {
        private float _preVolume;
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
            _preVolume = 0;
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
            if (coroutineHost == null)
                return;

            if (!_pool.ContainsKey(uid))
            {
                GcLogger.LogError($"sfx sound pool is null. Uid: {uid}");
                return;
            }

            bool isUnlimited = !_maxCount.ContainsKey(uid) || _maxCount[uid] == 0;
            bool canPlay = isUnlimited || _playCount[uid] < _maxCount[uid];

            if (!canPlay) return;

            GameObject obj = GetOrCreateAudioSource(uid);
            if (obj == null) return;

            _playCount[uid]++;
            coroutineHost.StartCoroutine(PlayWhenClipReady(uid, obj));
        }

        /// <summary>
        /// AudioClip이 아직 캐시에 없으면 비동기로 로드한 뒤 효과음을 재생합니다.
        /// </summary>
        /// <param name="uid">재생할 효과음 UID입니다.</param>
        /// <param name="obj">재생에 사용할 AudioSource 오브젝트입니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayWhenClipReady(int uid, GameObject obj)
        {
            if (obj == null || !_infoCache.TryGetValue(uid, out StruckTableSound info))
            {
                ReturnToPool(uid, obj);
                yield break;
            }

            AudioSource src = obj.GetComponent<AudioSource>();
            if (src == null)
            {
                ReturnToPool(uid, obj);
                yield break;
            }

            if (src.clip == null)
            {
                string key = $"{ConfigAddressableGroupName.Sound}_{info.FileName}";
                Task<AudioClip> task = _loader?.LoadAudioClipAsync(key);
                if (task == null)
                {
                    ReturnToPool(uid, obj);
                    yield break;
                }

                while (!task.IsCompleted)
                    yield return null;

                if (task.IsCanceled || task.IsFaulted || task.Result == null)
                {
                    GcLogger.LogWarning($"SFX 클립을 로드하지 못했습니다. uid={uid}");
                    ReturnToPool(uid, obj);
                    yield break;
                }

                src.clip = task.Result;
            }

            obj.SetActive(true);
            src.Play();

            yield return ReleaseAfter(obj, src.clip.length, uid);
        }

        /// <summary>
        /// AudioSource 오브젝트 반환
        /// </summary>
        private IEnumerator ReleaseAfter(GameObject obj, float delay, int uid)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(uid, obj);
        }

        /// <summary>
        /// 사용이 끝난 AudioSource 오브젝트를 풀에 되돌립니다.
        /// </summary>
        /// <param name="uid">효과음 UID입니다.</param>
        /// <param name="obj">반환할 AudioSource 오브젝트입니다.</param>
        private void ReturnToPool(int uid, GameObject obj)
        {
            if (obj != null)
                obj.SetActive(false);

            if (obj != null && _pool.TryGetValue(uid, out Queue<GameObject> queue))
                queue.Enqueue(obj);

            if (_playCount.ContainsKey(uid))
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
            if (_loader != null && _loader.TryGetAudioClip($"{ConfigAddressableGroupName.Sound}_{info.FileName}", out AudioClip cachedClip))
                src.clip = cachedClip;
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

        public void InitializeSelective(TableSound table, List<int> targetUids)
        {
            if (table == null || targetUids == null || targetUids.Count == 0) return;

            foreach (var uid in targetUids)
            {
                var info = table.GetDataByUid(uid);
                if (info == null || info.Type != SoundConstants.Type.Sfx) continue;

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
        /// 음소거 처리
        /// </summary>
        /// <param name="set"></param>
        public void Mute(bool set)
        {
            if (set)
            {
                _preVolume = PlayerPrefsManager.LoadSoundVolumeSfx();
                SetVolume(0);
            }
            else
            {
                SetVolume(_preVolume);
                _preVolume = 0;
            }
        }
    }
}
