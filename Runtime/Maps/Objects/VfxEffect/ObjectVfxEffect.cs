using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.U2D;

namespace GGemCo2DCore
{
    public class ObjectVfxEffect : DefaultMapObject
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)] 
        [Tooltip("자동 재생 여부")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("재생 속도")]
        [SerializeField] private float timeScale = 1f;
        [Tooltip("크기")]
        [SerializeField] private Vector3 scale = Vector3.one;
        [Tooltip("재생 클립 이름")]
        [SerializeField] private string animationName;
        
        private Animator _animator;
        
        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponent<Animator>();
            InitializeScale();
        }
        
        private void Start()
        {
            if (GcLogger.IsNull(_animator, nameof(Animator))) return;
            
            _animator.speed = timeScale;
            _animator.Play(animationName, 0, 0);
            _animator.Update(0);
        }
        
        private void InitializeScale()
        {
            if (scale == Vector3.zero || scale == Vector3.one) return;
            transform.localScale = scale;
        }
    }
}