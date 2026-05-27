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
        /// <param name="legacyTable">신규 테이블이 비어 있을 때 사용할 레거시 sound 테이블입니다.</param>
        public void Initialize(TableSoundSfx table, TableSound legacyTable = null)
        {
            bool hasResourceRows = table != null && table.GetCount() > 0;
            if (hasResourceRows)
            {
                foreach (KeyValuePair<int, StruckTableSoundSfx> pair in table.GetDatas())
                    RegisterResource(CreateResourceInfo(pair.Value));
            }

            RegisterLegacyFallbackRows(table, legacyTable);
        }


        /// <summary>
        /// 신규 sound_sfx에 아직 이관되지 않은 레거시 SFX 행을 추가 등록합니다.
        /// 부분 마이그레이션 중에도 기존 sound.FileName 행을 계속 재생할 수 있게 합니다.
        /// </summary>
        /// <param name="resourceTable">신규 sound_sfx 테이블입니다.</param>
        /// <param name="legacyTable">레거시 sound 테이블입니다.</param>
        private void RegisterLegacyFallbackRows(TableSoundSfx resourceTable, TableSound legacyTable)
        {
            if (legacyTable == null)
                return;

            foreach (KeyValuePair<int, StruckTableSound> pair in legacyTable.GetDatas())
            {
                StruckTableSound info = pair.Value;
                if (info == null || info.Type != SoundConstants.Type.Sfx || !info.HasLegacyResource())
                    continue;

                if (resourceTable != null && resourceTable.GetFirstBySoundUid(info.Uid) != null)
                    continue;

                RegisterResource(CreateResourceInfo(info));
            }
        }

        /// <summary>
        /// 레거시 sound.txt 기반으로 효과음 pool을 초기화합니다.
        /// 신규 sound_sfx 테이블이 아직 없을 때 이전 데이터와 동작을 유지합니다.
        /// </summary>
        /// <param name="legacyTable">레거시 sound 테이블입니다.</param>
        private void InitializeLegacy(TableSound legacyTable)
        {
            if (legacyTable == null) return;

            foreach (KeyValuePair<int, StruckTableSound> pair in legacyTable.GetDatas())
            {
                StruckTableSound info = pair.Value;
                if (info == null || info.Type != SoundConstants.Type.Sfx || !info.HasLegacyResource())
                    continue;

                RegisterResource(CreateResourceInfo(info));
            }
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
            if (!resolved.ShouldPlay)
                return;

            Play(resolved.ResourceUid, coroutineHost, resolved.Volume, resolved.Pitch, true);
        }

        /// <summary>
        /// 효과음 재생
        /// </summary>
        /// <param name="uid">실제 sound_sfx 리소스 UID 또는 레거시 sound UID입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        public void Play(int uid, MonoBehaviour coroutineHost)
        {
            Play(uid, coroutineHost, 1f, 1f, false);
        }

        /// <summary>
        /// 효과음 재생 요청을 처리하고, 재생 가능한 AudioSource를 확보합니다.
        /// </summary>
        /// <param name="uid">실제 SFX 리소스 UID입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">true면 volume 값을 최종 볼륨으로 사용하고, false면 리소스 기본 볼륨과 곱합니다.</param>
        private void Play(int uid, MonoBehaviour coroutineHost, float volume, float pitch, bool useFinalVolume)
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
            coroutineHost.StartCoroutine(PlayWhenClipReady(uid, obj, volume, pitch, useFinalVolume));
        }

        /// <summary>
        /// AudioClip이 아직 캐시에 없으면 비동기로 로드한 뒤 효과음을 재생합니다.
        /// </summary>
        /// <param name="uid">재생할 효과음 리소스 UID입니다.</param>
        /// <param name="obj">재생에 사용할 AudioSource 오브젝트입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">true면 volume 값을 최종 볼륨으로 사용하고, false면 리소스 기본 볼륨과 곱합니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayWhenClipReady(int uid, GameObject obj, float volume, float pitch, bool useFinalVolume)
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
            obj.SetActive(true);
            src.Play();

            float releaseDelay = src.clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch));
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
                obj.SetActive(false);

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

            var src = obj.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = _group;
            src.volume = Mathf.Clamp01(info.Volume <= 0f ? 1f : info.Volume);
            src.pitch = 1f;
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
        /// 레거시 sound 테이블 행을 런타임 풀 정보로 변환합니다.
        /// </summary>
        /// <param name="row">레거시 sound 행입니다.</param>
        /// <returns>런타임 풀 정보입니다.</returns>
        private static RuntimeSfxResource CreateResourceInfo(StruckTableSound row)
        {
            if (row == null)
                return null;

            return new RuntimeSfxResource
            {
                ResourceUid = row.Uid,
                FileName = row.FileName,
                MaxPlayCount = row.MaxPlayCount,
                Volume = row.Volume,
                PitchMin = 1f,
                PitchMax = 1f,
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
        }

        /// <summary>
        /// 지정한 실제 SFX 리소스 UID만 풀링합니다.
        /// 인트로 씬처럼 일부 사운드만 미리 준비해야 할 때 사용합니다.
        /// </summary>
        /// <param name="table">신규 sound_sfx 실제 리소스 테이블입니다.</param>
        /// <param name="targetUids">풀링 대상 실제 리소스 UID 목록입니다.</param>
        /// <param name="legacyTable">신규 테이블이 비어 있을 때 사용할 레거시 sound 테이블입니다.</param>
        public void InitializeSelective(TableSoundSfx table, List<int> targetUids, TableSound legacyTable = null)
        {
            if (targetUids == null || targetUids.Count == 0) return;

            for (int i = 0; i < targetUids.Count; i++)
            {
                int uid = targetUids[i];
                RuntimeSfxResource resource = null;

                if (table != null)
                {
                    StruckTableSoundSfx info = table.GetDataByUid(uid);
                    resource = CreateResourceInfo(info);
                }

                // 부분 마이그레이션 중에는 신규 sound_sfx와 레거시 sound 행이 함께 존재할 수 있습니다.
                // 신규 테이블에서 UID를 찾지 못하면 레거시 sound UID로 한 번 더 확인합니다.
                if (resource == null && legacyTable != null)
                {
                    StruckTableSound info = legacyTable.GetDataByUid(uid);
                    if (info != null && info.Type == SoundConstants.Type.Sfx && info.HasLegacyResource())
                        resource = CreateResourceInfo(info);
                }

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
