using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 환경음 컨트롤러입니다.
    /// 여러 Ambient 리소스를 동시에 루프 재생할 수 있도록 실제 리소스 UID별 AudioSource를 관리합니다.
    /// </summary>
    public sealed class SoundControllerAmbient
    {
        private readonly GameObject _owner;
        private readonly AudioMixerGroup _group;
        private readonly AddressableLoaderSound _loader;
        private readonly Dictionary<int, AudioSource> _sourcesByResourceUid = new Dictionary<int, AudioSource>();

        public SoundControllerAmbient(GameObject owner, AudioMixerGroup group, AddressableLoaderSound loader)
        {
            _owner = owner;
            _group = group;
            _loader = loader;
        }

        /// <summary>
        /// 해석된 Ambient 사운드를 재생합니다.
        /// 이미 같은 리소스가 재생 중이면 볼륨/피치만 갱신합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 Ambient 정보입니다.</param>
        /// <param name="coroutineHost">비동기 로드 코루틴 실행자입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost)
        {
            if (!resolved.ShouldPlay || coroutineHost == null || resolved.ResourceUid <= 0)
                return;

            coroutineHost.StartCoroutine(PlayRoutine(resolved));
        }

        /// <summary>
        /// 특정 Ambient 리소스를 정지합니다.
        /// </summary>
        /// <param name="resourceUid">정지할 실제 Ambient 리소스 UID입니다.</param>
        public void Stop(int resourceUid)
        {
            if (!_sourcesByResourceUid.TryGetValue(resourceUid, out AudioSource source) || source == null)
                return;

            source.Stop();
        }

        /// <summary>
        /// 현재 재생 중인 모든 Ambient 리소스를 정지합니다.
        /// </summary>
        public void StopAll()
        {
            foreach (KeyValuePair<int, AudioSource> pair in _sourcesByResourceUid)
                pair.Value?.Stop();
        }

        /// <summary>
        /// Ambient 클립을 필요 시점에 비동기로 로드한 뒤 재생합니다.
        /// </summary>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayRoutine(ResolvedSound resolved)
        {
            AudioSource source = GetOrCreateSource(resolved.ResourceUid);
            if (source == null)
                yield break;

            if (source.clip == null)
            {
                string key = $"{ConfigAddressableGroupName.Sound}_{resolved.FileName}";
                Task<AudioClip> task = _loader?.LoadAudioClipAsync(key);
                if (task == null)
                    yield break;

                while (!task.IsCompleted)
                    yield return null;

                if (task.IsCanceled || task.IsFaulted || task.Result == null)
                {
                    GcLogger.LogWarning($"Ambient 클립을 로드하지 못했습니다. resourceUid={resolved.ResourceUid}");
                    yield break;
                }

                source.clip = task.Result;
            }

            source.volume = Mathf.Clamp01(resolved.Volume);
            source.pitch = Mathf.Approximately(resolved.Pitch, 0f) ? 1f : resolved.Pitch;
            source.loop = true;

            if (!source.isPlaying)
                source.Play();
        }

        /// <summary>
        /// 실제 Ambient 리소스 UID에 대응하는 AudioSource를 가져오거나 생성합니다.
        /// </summary>
        /// <param name="resourceUid">실제 Ambient 리소스 UID입니다.</param>
        /// <returns>사용할 AudioSource입니다.</returns>
        private AudioSource GetOrCreateSource(int resourceUid)
        {
            if (_sourcesByResourceUid.TryGetValue(resourceUid, out AudioSource source) && source != null)
                return source;

            if (_owner == null)
                return null;

            GameObject obj = new GameObject($"{resourceUid}_ambient");
            obj.transform.SetParent(_owner.transform);
            source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _group;
            source.loop = true;
            _sourcesByResourceUid[resourceUid] = source;
            return source;
        }

        /// <summary>
        /// 컨트롤러가 생성한 Ambient AudioSource를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            _sourcesByResourceUid.Clear();
        }
    }
}
