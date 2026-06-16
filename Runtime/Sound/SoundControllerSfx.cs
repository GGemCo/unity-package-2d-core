using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 효과음 AudioSource 풀과 재생 중 AudioClip 참조 수명을 관리합니다.
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

        private sealed class ActiveSfxPlayback
        {
            public int Uid;
            public GameObject Object;
            public MonoBehaviour CoroutineHost;
            public Coroutine Routine;
            public SoundPlaybackLease ClipLease;
            public bool IsReturned;
        }

        private float _preVolume;
        private readonly Transform _owner;
        private readonly AudioMixer _mixer;
        private readonly AudioMixerGroup _group;
        private readonly string _volumeParam;
        private readonly AddressableLoaderSound _loader;

        private readonly Dictionary<int, Queue<GameObject>> _pool = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, int> _playCount = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _maxCount = new Dictionary<int, int>();
        private readonly Dictionary<int, RuntimeSfxResource> _infoCache = new Dictionary<int, RuntimeSfxResource>();
        private readonly Dictionary<int, int> _createdCount = new Dictionary<int, int>();
        private readonly HashSet<ActiveSfxPlayback> _activePlaybacks = new HashSet<ActiveSfxPlayback>();
        private readonly List<GameObject> _createdAudioSourceObjects = new List<GameObject>();

        private const int MaxUnlimitedAudioSourceCount = 10;
        private bool _isDestroyed;

        /// <summary>
        /// SFX 풀에 사용할 소유 Transform, 믹서, 사운드 로더를 설정합니다.
        /// </summary>
        /// <param name="owner">생성한 AudioSource 오브젝트의 부모입니다.</param>
        /// <param name="mixer">SFX 볼륨을 제어할 AudioMixer입니다.</param>
        /// <param name="group">SFX 출력 AudioMixerGroup입니다.</param>
        /// <param name="volumeParam">SFX 볼륨 Exposed Parameter 이름입니다.</param>
        /// <param name="loader">AudioClip 재생 참조를 관리할 로더입니다.</param>
        public SoundControllerSfx(
            Transform owner,
            AudioMixer mixer,
            AudioMixerGroup group,
            string volumeParam,
            AddressableLoaderSound loader)
        {
            _preVolume = 0f;
            _owner = owner;
            _mixer = mixer;
            _group = group;
            _volumeParam = volumeParam;
            _loader = loader;
        }

        /// <summary>
        /// 효과음 리소스 메타데이터와 빈 풀을 초기화합니다.
        /// AudioSource는 시작 시 일괄 생성하지 않고 최초 재생 요청 시 필요한 수만 생성합니다.
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
            _maxCount[uid] = Mathf.Max(0, info.MaxPlayCount);

            if (!_pool.ContainsKey(uid))
                _pool[uid] = new Queue<GameObject>();

            if (!_playCount.ContainsKey(uid))
                _playCount[uid] = 0;

            if (!_createdCount.ContainsKey(uid))
                _createdCount[uid] = 0;
        }

        /// <summary>
        /// 해석된 효과음 정보를 기준으로 재생합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴 실행자입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost)
        {
            Play(resolved, coroutineHost, 0f);
        }

        /// <summary>
        /// 해석된 효과음 정보를 기준으로 재생하며 루프 효과음의 자동 정리 시간을 지정합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴 실행자입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost, float durationSeconds)
        {
            PlayWithHandle(resolved, coroutineHost, durationSeconds, SoundPlaybackStopPolicy.Auto);
        }

        /// <summary>
        /// 해석된 효과음을 재생하고 외부에서 정지할 수 있는 핸들을 반환합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴 실행자입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        /// <returns>재생 정지 핸들입니다. 재생할 수 없으면 null입니다.</returns>
        public SoundPlaybackHandle PlayWithHandle(
            ResolvedSound resolved,
            MonoBehaviour coroutineHost,
            float durationSeconds,
            SoundPlaybackStopPolicy stopPolicy)
        {
            return PlayWithHandle(resolved, coroutineHost, durationSeconds, stopPolicy, 1f);
        }

        /// <summary>
        /// 해석된 효과음을 요청 단위 재생 속도 배율과 함께 재생하고, 호출자가 정지할 수 있는 핸들을 반환합니다.
        /// 테이블에 정의된 pitch에 요청 배율을 곱해 최종 AudioSource pitch로 사용합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 효과음 정보입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴을 실행할 호스트입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        /// <param name="pitchMultiplier">요청 단위 재생 속도 배율입니다. 0 이하이면 1로 보정합니다.</param>
        /// <returns>재생 정지 핸들입니다. 재생하지 못하면 null을 반환합니다.</returns>
        public SoundPlaybackHandle PlayWithHandle(
            ResolvedSound resolved,
            MonoBehaviour coroutineHost,
            float durationSeconds,
            SoundPlaybackStopPolicy stopPolicy,
            float pitchMultiplier)
        {
            if (!resolved.ShouldPlay)
                return null;

            float safePitchMultiplier = pitchMultiplier > 0f ? pitchMultiplier : 1f;
            return Play(
                resolved.ResourceUid,
                coroutineHost,
                resolved.Volume,
                resolved.Pitch * safePitchMultiplier,
                true,
                resolved.Loop,
                durationSeconds,
                stopPolicy);
        }

        /// <summary>
        /// 실제 SFX 리소스 UID를 재생합니다.
        /// </summary>
        /// <param name="uid">실제 sound_sfx 리소스 UID입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴 실행자입니다.</param>
        public void Play(int uid, MonoBehaviour coroutineHost)
        {
            Play(uid, coroutineHost, 1f, 1f, false, false, 0f, SoundPlaybackStopPolicy.Auto);
        }

        /// <summary>
        /// 효과음 재생 요청을 처리하고 재생 가능한 AudioSource를 확보합니다.
        /// </summary>
        /// <param name="uid">실제 SFX 리소스 UID입니다.</param>
        /// <param name="coroutineHost">자동 반환 코루틴 실행자입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">volume을 최종 볼륨으로 사용할지 여부입니다.</param>
        /// <param name="loop">AudioSource 루프 재생 여부입니다.</param>
        /// <param name="durationSeconds">루프 효과음을 유지할 시간입니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        /// <returns>재생 정지 핸들입니다.</returns>
        private SoundPlaybackHandle Play(
            int uid,
            MonoBehaviour coroutineHost,
            float volume,
            float pitch,
            bool useFinalVolume,
            bool loop,
            float durationSeconds,
            SoundPlaybackStopPolicy stopPolicy)
        {
            if (_isDestroyed || coroutineHost == null)
                return null;

            if (!_pool.ContainsKey(uid))
            {
                GcLogger.LogError($"sfx sound pool is null. Uid: {uid}");
                return null;
            }

            bool isUnlimited = !_maxCount.ContainsKey(uid) || _maxCount[uid] == 0;
            bool canPlay = isUnlimited || _playCount[uid] < _maxCount[uid];
            if (!canPlay)
                return null;

            GameObject obj = GetOrCreateAudioSource(uid);
            if (obj == null)
                return null;

            _playCount[uid]++;
            ActiveSfxPlayback playback = new ActiveSfxPlayback
            {
                Uid = uid,
                Object = obj,
                CoroutineHost = coroutineHost,
            };
            _activePlaybacks.Add(playback);

            SoundPlaybackHandle handle = new SoundPlaybackHandle(() => StopPlayback(playback));
            BeginPlaybackAsync(playback, volume, pitch, useFinalVolume, loop, durationSeconds, stopPolicy);
            return handle;
        }

        /// <summary>
        /// AudioClip 재생 참조를 비동기로 획득한 뒤 AudioSource 재생을 시작합니다.
        /// 로드 중 정지되면 획득된 참조만 해제하고 풀 오브젝트를 다시 건드리지 않습니다.
        /// </summary>
        /// <param name="playback">재생 상태입니다.</param>
        /// <param name="volume">요청별 볼륨 값입니다.</param>
        /// <param name="pitch">요청별 피치입니다.</param>
        /// <param name="useFinalVolume">volume을 최종 볼륨으로 사용할지 여부입니다.</param>
        /// <param name="loop">루프 재생 여부입니다.</param>
        /// <param name="durationSeconds">자동 정리 시간입니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        private async void BeginPlaybackAsync(
            ActiveSfxPlayback playback,
            float volume,
            float pitch,
            bool useFinalVolume,
            bool loop,
            float durationSeconds,
            SoundPlaybackStopPolicy stopPolicy)
        {
            if (playback == null || !_infoCache.TryGetValue(playback.Uid, out RuntimeSfxResource info))
            {
                ReturnToPool(playback);
                return;
            }

            string key = $"{ConfigAddressableGroupName.Sound}_{info.FileName}";
            SoundPlaybackLease lease = null;
            try
            {
                lease = await _loader.AcquirePlaybackAsync(key);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"SFX 클립 로드 중 예외가 발생했습니다. uid={playback.Uid}, error={ex.Message}");
            }

            if (_isDestroyed || playback.IsReturned || playback.CoroutineHost == null || lease == null)
            {
                lease?.Dispose();
                if (!playback.IsReturned)
                    ReturnToPool(playback);
                return;
            }

            AudioSource source = playback.Object != null
                ? playback.Object.GetComponent<AudioSource>()
                : null;
            if (source == null)
            {
                lease.Dispose();
                ReturnToPool(playback);
                return;
            }

            playback.ClipLease = lease;
            source.clip = lease.Clip;
            source.volume = useFinalVolume
                ? Mathf.Clamp01(volume <= 0f ? 1f : volume)
                : Mathf.Clamp01((info.Volume <= 0f ? 1f : info.Volume) * Mathf.Clamp01(volume <= 0f ? 1f : volume));
            source.pitch = Mathf.Approximately(pitch, 0f) ? 1f : pitch;
            source.loop = loop;
            playback.Object.SetActive(true);
            source.Play();

            if (stopPolicy == SoundPlaybackStopPolicy.ByHandle)
                return;

            float releaseDelay = (stopPolicy == SoundPlaybackStopPolicy.ByDuration || loop) && durationSeconds > 0f
                ? durationSeconds
                : source.clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            playback.Routine = playback.CoroutineHost.StartCoroutine(ReleaseAfter(playback, releaseDelay));
        }

        /// <summary>
        /// 지정한 시간 후 효과음 재생 상태를 풀에 반환합니다.
        /// </summary>
        /// <param name="playback">반환할 SFX 재생 상태입니다.</param>
        /// <param name="delay">반환 지연 시간입니다.</param>
        /// <returns>지연 실행 열거자입니다.</returns>
        private IEnumerator ReleaseAfter(ActiveSfxPlayback playback, float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            ReturnToPool(playback);
        }

        /// <summary>
        /// 외부 핸들에서 요청한 SFX 정지를 처리합니다.
        /// </summary>
        /// <param name="playback">정지할 SFX 재생 상태입니다.</param>
        private void StopPlayback(ActiveSfxPlayback playback)
        {
            if (playback == null || playback.IsReturned)
                return;

            if (playback.CoroutineHost != null && playback.Routine != null)
            {
                playback.CoroutineHost.StopCoroutine(playback.Routine);
                playback.Routine = null;
            }

            ReturnToPool(playback);
        }

        /// <summary>
        /// 사용이 끝난 AudioSource를 정지하고 Clip 참조를 제거한 뒤 풀에 되돌립니다.
        /// </summary>
        /// <param name="playback">반환할 SFX 재생 상태입니다.</param>
        private void ReturnToPool(ActiveSfxPlayback playback)
        {
            if (playback == null || playback.IsReturned)
                return;

            playback.IsReturned = true;
            _activePlaybacks.Remove(playback);

            int uid = playback.Uid;
            GameObject obj = playback.Object;
            if (obj != null)
            {
                AudioSource source = obj.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.Stop();
                    source.loop = false;
                    source.clip = null;
                }

                obj.SetActive(false);
            }

            playback.ClipLease?.Dispose();
            playback.ClipLease = null;
            playback.Routine = null;

            if (!_isDestroyed && obj != null && _pool.TryGetValue(uid, out Queue<GameObject> queue))
                queue.Enqueue(obj);

            if (_playCount.ContainsKey(uid))
                _playCount[uid] = Mathf.Max(0, _playCount[uid] - 1);
        }

        /// <summary>
        /// 풀에서 사용 가능한 AudioSource를 가져오거나 최초 사용 시 새로 생성합니다.
        /// </summary>
        /// <param name="uid">효과음 리소스 UID입니다.</param>
        /// <returns>사용 가능한 AudioSource GameObject입니다.</returns>
        private GameObject GetOrCreateAudioSource(int uid)
        {
            if (!_pool.TryGetValue(uid, out Queue<GameObject> pool))
                return null;

            if (pool.Count > 0)
                return pool.Dequeue();

            if (!_infoCache.TryGetValue(uid, out RuntimeSfxResource info))
                return null;

            int createdCount = _createdCount.TryGetValue(uid, out int count) ? count : 0;
            int maxPlayCount = _maxCount.TryGetValue(uid, out int maxCount) ? maxCount : 0;
            int creationLimit = maxPlayCount > 0 ? maxPlayCount : MaxUnlimitedAudioSourceCount;
            if (creationLimit > 0 && createdCount >= creationLimit)
                return null;

            GameObject audioSourceObject = CreateAudioSourceObject(uid, info);
            if (audioSourceObject != null)
                _createdCount[uid] = createdCount + 1;

            return audioSourceObject;
        }

        /// <summary>
        /// 효과음 재생용 AudioSource GameObject를 생성합니다.
        /// Clip은 재생 참조 임대 객체를 획득한 시점에만 연결합니다.
        /// </summary>
        /// <param name="uid">효과음 리소스 UID입니다.</param>
        /// <param name="info">효과음 리소스 정보입니다.</param>
        /// <returns>생성된 AudioSource GameObject입니다.</returns>
        private GameObject CreateAudioSourceObject(int uid, RuntimeSfxResource info)
        {
            GameObject obj = new GameObject($"{uid}_sfx");
            obj.transform.SetParent(_owner);
            _createdAudioSourceObjects.Add(obj);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _group;
            source.volume = Mathf.Clamp01(info.Volume <= 0f ? 1f : info.Volume);
            source.pitch = 1f;
            source.loop = false;
            source.clip = null;
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
        /// SFX 믹서 볼륨을 변경합니다.
        /// </summary>
        /// <param name="volume">0~1 범위의 볼륨입니다.</param>
        /// <param name="save">PlayerPrefs에 저장할지 여부입니다.</param>
        public void SetVolume(float volume, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            _mixer.SetFloat(_volumeParam, db);
            if (save)
                PlayerPrefsManager.SaveSoundVolumeSfx(volume);
        }

        /// <summary>
        /// 컨트롤러가 생성한 AudioSource 풀과 재생 참조를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            if (_isDestroyed)
                return;

            _isDestroyed = true;
            ClearPool();
        }

        /// <summary>
        /// 활성 재생을 모두 정지한 뒤 생성한 AudioSource와 풀 캐시를 정리합니다.
        /// </summary>
        private void ClearPool()
        {
            if (_activePlaybacks.Count > 0)
            {
                List<ActiveSfxPlayback> activePlaybacks = new List<ActiveSfxPlayback>(_activePlaybacks);
                for (int i = 0; i < activePlaybacks.Count; i++)
                    StopPlayback(activePlaybacks[i]);
            }

            for (int i = 0; i < _createdAudioSourceObjects.Count; i++)
            {
                GameObject obj = _createdAudioSourceObjects[i];
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }

            _activePlaybacks.Clear();
            _createdAudioSourceObjects.Clear();
            _pool.Clear();
            _playCount.Clear();
            _maxCount.Clear();
            _infoCache.Clear();
            _createdCount.Clear();
        }

        /// <summary>
        /// 지정한 실제 SFX 리소스 UID만 풀 관리 대상으로 등록합니다.
        /// </summary>
        /// <param name="table">신규 sound_sfx 실제 리소스 테이블입니다.</param>
        /// <param name="targetUids">풀 관리 대상으로 등록할 실제 리소스 UID 목록입니다.</param>
        public void InitializeSelective(TableSoundSfx table, List<int> targetUids)
        {
            if (table == null || targetUids == null || targetUids.Count == 0)
                return;

            for (int i = 0; i < targetUids.Count; i++)
            {
                int uid = targetUids[i];
                StruckTableSoundSfx info = table.GetDataByUid(uid);
                RegisterResource(CreateResourceInfo(info));
            }
        }

        /// <summary>
        /// 효과음 음소거 상태를 변경합니다.
        /// </summary>
        /// <param name="set">true면 음소거하고 false면 이전 볼륨으로 복원합니다.</param>
        public void Mute(bool set)
        {
            if (set)
            {
                _preVolume = PlayerPrefsManager.LoadSoundVolumeSfx();
                SetVolume(0f);
            }
            else
            {
                SetVolume(_preVolume);
                _preVolume = 0f;
            }
        }
    }
}
