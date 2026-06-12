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
        private sealed class RuntimeSfxResource
        {
            public int ResourceUid;
            public string FileName;
            public int MaxPlayCount;
            public float Volume;
            public float PitchMin;
            public float PitchMax;
        }

        private float _preVolume;
        private readonly Transform _owner;
        private readonly AudioMixer _mixer;
        private readonly AudioMixerGroup _group;
        private readonly string _volumeParam;
        private readonly AddressableLoaderSound _loader;

        private readonly Dictionary<int, Queue<GameObject>> _pool = new();
        private readonly Dictionary<int, int> _playCount = new();
        private readonly Dictionary<int, int> _maxCount = new();
        private readonly Dictionary<int, RuntimeSfxResource> _infoCache = new();
        private readonly List<GameObject> _createdAudioSourceObjects = new();

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
        /// <param name="table">신규 sound_sfx 실제 리소스 테이블입니다.</param>
        public void Initialize(TableSoundSfx table)
        {
            if (table == null || table.GetCount() <= 0)
                return;

            foreach (KeyValuePair<int, StruckTableSoundSfx> pair in table.GetDatas())
                RegisterResource(CreateResourceInfo(pair.Value));
        }

        /// <summary>
        /// 기존 SFX 풀을 정리하고 현재 테이블 기준으로 다시 초기화합니다.
        /// 테스트 툴에서 sound_sfx Row를 수정한 뒤 풀에 캐시된 FileName/MaxPlayCount/Volume 정보를 갱신할 때 사용합니다.
        /// </summary>
        /// <param name="table">신규 sound_sfx 실제 리소스 테이블입니다.</param>
        public void Reinitialize(TableSoundSfx table)
        {
            ClearPool();
            Initialize(table);
        }

        /// <summary>
        /// 실제 SFX 리소스 행을 풀 관리 대상으로 등록합니다.
        /// </summary>
        /// <param name="info">등록할 런타임 리소스 정보입니다.</param>
        private void RegisterResource(RuntimeSfxResource info)
        {
            if (info == null || info.ResourceUid <= 0 || string.IsNullOrWhiteSpace(info.FileName))
                return;

            int uid = info.ResourceUid;
            _infoCache[uid] = info;
            _playCount[uid] = 0;
            _maxCount[uid] = Mathf.Max(0, info.MaxPlayCount);

            Queue<GameObject> pool = new();
            for (int i = 0; i < Mathf.Max(0, info.MaxPlayCount); i++)
                pool.Enqueue(CreateAudioSourceObject(uid, info));

            _pool[uid] = pool;
            _autoExpandedCount[uid] = 0;
        }

        /// <summary>
        /// 해석된 효과음 정보를 기준으로 재생합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost)
        {
            Play(resolved, coroutineHost, 0f);
        }

        /// <summary>
        /// 해석된 효과음 정보를 기준으로 재생하며, 루프 효과음의 자동 정리 시간을 지정합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다. 0 이하이면 클립 길이를 사용합니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost, float durationSeconds)
        {
            if (!resolved.ShouldPlay)
                return;

            Play(resolved.ResourceUid, coroutineHost, resolved.Volume, resolved.Pitch, true, resolved.Loop, durationSeconds);
        }

        /// <summary>
        /// 효과음 재생
        /// </summary>
        /// <param name="uid">실제 sound_sfx 리소스 UID입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        public void Play(int uid, MonoBehaviour coroutineHost)
        {
            Play(uid, coroutineHost, 1f, 1f, false, false, 0f);
        }

        /// <summary>
        /// 효과음 재생 요청을 처리하고, 재생 가능한 AudioSource를 확보합니다.
        /// </summary>
        /// <param name="uid">실제 SFX 리소스 UID입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">true면 volume 값을 최종 볼륨으로 사용하고, false면 리소스 기본 볼륨과 곱합니다.</param>
        /// <param name="loop">AudioSource 루프 재생 여부입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다. 0 이하이면 클립 길이를 사용합니다.</param>
        private void Play(int uid, MonoBehaviour coroutineHost, float volume, float pitch, bool useFinalVolume, bool loop, float durationSeconds)
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
            coroutineHost.StartCoroutine(PlayWhenClipReady(uid, obj, volume, pitch, useFinalVolume, loop, durationSeconds));
        }

        /// <summary>
        /// AudioClip이 아직 캐시에 없으면 비동기로 로드한 뒤 효과음을 재생합니다.
        /// </summary>
        /// <param name="uid">재생할 효과음 리소스 UID입니다.</param>
        /// <param name="obj">재생에 사용할 AudioSource 오브젝트입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">true면 volume 값을 최종 볼륨으로 사용하고, false면 리소스 기본 볼륨과 곱합니다.</param>
        /// <param name="loop">AudioSource 루프 재생 여부입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다. 0 이하이면 클립 길이를 사용합니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayWhenClipReady(int uid, GameObject obj, float volume, float pitch, bool useFinalVolume, bool loop, float durationSeconds)
        {
            if (obj == null || !_infoCache.TryGetValue(uid, out RuntimeSfxResource info))
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

            src.volume = useFinalVolume
                ? Mathf.Clamp01(volume <= 0f ? 1f : volume)
                : Mathf.Clamp01((info.Volume <= 0f ? 1f : info.Volume) * Mathf.Clamp01(volume <= 0f ? 1f : volume));
            src.pitch = Mathf.Approximately(pitch, 0f) ? 1f : pitch;
            src.loop = loop;
            obj.SetActive(true);
            src.Play();

            // 루프 요청은 Timeline 이벤트 길이만큼 유지하고, 길이가 없으면 기존 1회 재생 길이로 정리합니다.
            float releaseDelay = loop && durationSeconds > 0f
                ? durationSeconds
                : src.clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch));
            yield return ReleaseAfter(obj, releaseDelay, uid);
        }

        /// <summary>
        /// AudioSource 오브젝트 반환
        /// </summary>
        /// <param name="obj">반환할 AudioSource 오브젝트입니다.</param>
        /// <param name="delay">반환 지연 시간입니다.</param>
        /// <param name="uid">효과음 리소스 UID입니다.</param>
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
            {
                AudioSource source = obj.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.Stop();
                    source.loop = false;
                }

                obj.SetActive(false);
            }

            if (obj != null && _pool.TryGetValue(uid, out Queue<GameObject> queue))
                queue.Enqueue(obj);

            if (_playCount.ContainsKey(uid))
                _playCount[uid] = Mathf.Max(0, _playCount[uid] - 1);
        }

        /// <summary>
        /// 사용 가능한 AudioSource 또는 자동 생성
        /// </summary>
        /// <param name="uid">효과음 리소스 UID입니다.</param>
        /// <returns>사용 가능한 AudioSource GameObject입니다.</returns>
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
        /// <param name="uid">효과음 리소스 UID입니다.</param>
        /// <param name="info">효과음 리소스 정보입니다.</param>
        /// <returns>생성된 AudioSource GameObject입니다.</returns>
        private GameObject CreateAudioSourceObject(int uid, RuntimeSfxResource info)
        {
            GameObject obj = new GameObject($"{uid}_sfx");
            obj.transform.SetParent(_owner);
            _createdAudioSourceObjects.Add(obj);

            var src = obj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = _group;
            src.volume = Mathf.Clamp01(info.Volume <= 0f ? 1f : info.Volume);
            src.pitch = 1f;
            src.loop = false;
            if (_loader != null && _loader.TryGetAudioClip($"{ConfigAddressableGroupName.Sound}_{info.FileName}", out AudioClip cachedClip))
                src.clip = cachedClip;
            obj.SetActive(false);

            return obj;
        }

        /// <summary>
        /// 실제 SFX 테이블 행을 런타임 풀 정보로 변환합니다.
        /// </summary>
        /// <param name="row">SFX 리소스 행입니다.</param>
        /// <returns>런타임 풀 정보입니다.</returns>
        private static RuntimeSfxResource CreateResourceInfo(StruckTableSoundSfx row)
        {
            if (row == null)
                return null;

            return new RuntimeSfxResource
            {
                ResourceUid = row.Uid,
                FileName = row.FileName,
                MaxPlayCount = row.MaxPlayCount,
                Volume = row.Volume,
                PitchMin = row.GetSafePitchMin(),
                PitchMax = row.GetSafePitchMax(),
            };
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        /// <param name="volume">설정할 SFX 볼륨입니다.</param>
        /// <param name="save">저장 여부입니다.</param>
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
            ClearPool();
        }

        /// <summary>
        /// 컨트롤러가 생성한 AudioSource 오브젝트와 풀 캐시를 모두 정리합니다.
        /// </summary>
        private void ClearPool()
        {
            for (int i = 0; i < _createdAudioSourceObjects.Count; i++)
            {
                GameObject obj = _createdAudioSourceObjects[i];
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }

            _createdAudioSourceObjects.Clear();
            _pool.Clear();
            _playCount.Clear();
            _maxCount.Clear();
            _infoCache.Clear();
            _autoExpandedCount.Clear();
        }

        /// <summary>
        /// 지정한 실제 SFX 리소스 UID만 풀링합니다.
        /// 인트로 씬처럼 일부 사운드만 미리 준비해야 할 때 사용합니다.
        /// </summary>
        /// <param name="table">신규 sound_sfx 실제 리소스 테이블입니다.</param>
        /// <param name="targetUids">풀링 대상 실제 리소스 UID 목록입니다.</param>
        public void InitializeSelective(TableSoundSfx table, List<int> targetUids)
        {
            if (table == null || targetUids == null || targetUids.Count == 0)
                return;

            for (int i = 0; i < targetUids.Count; i++)
            {
                int uid = targetUids[i];
                StruckTableSoundSfx info = table.GetDataByUid(uid);
                RuntimeSfxResource resource = CreateResourceInfo(info);
                RegisterResource(resource);
            }
        }

        /// <summary>
        /// 음소거 처리
        /// </summary>
        /// <param name="set">true면 음소거하고, false면 이전 볼륨으로 복원합니다.</param>
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
