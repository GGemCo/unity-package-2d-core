using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 지정된 두 캐릭터 사이의 Collider2D 충돌을 일시적으로 무시하고, 종료 시 원복하는 범위 객체입니다.
    /// </summary>
    internal sealed class MotionCollisionIgnoreScope2D : IDisposable
    {
        private struct ColliderPair
        {
            public Collider2D Source;
            public Collider2D Target;
        }

        private readonly List<ColliderPair> _pairs = new(8);
        private bool _disposed;

        public static MotionCollisionIgnoreScope2D Create(GameObject sourceRoot, GameObject targetRoot)
        {
            if (sourceRoot == null || targetRoot == null)
                return null;

            var sourceColliders = CollectCharacterColliders(sourceRoot);
            var targetColliders = CollectCharacterColliders(targetRoot);
            if (sourceColliders.Count == 0 || targetColliders.Count == 0)
                return null;

            var scope = new MotionCollisionIgnoreScope2D();
            for (int i = 0; i < sourceColliders.Count; i++)
            {
                Collider2D source = sourceColliders[i];
                if (source == null)
                    continue;

                for (int j = 0; j < targetColliders.Count; j++)
                {
                    Collider2D target = targetColliders[j];
                    if (target == null || ReferenceEquals(source, target))
                        continue;

                    Physics2D.IgnoreCollision(source, target, true);
                    scope._pairs.Add(new ColliderPair
                    {
                        Source = source,
                        Target = target,
                    });
                }
            }

            return scope._pairs.Count > 0 ? scope : null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            for (int i = 0; i < _pairs.Count; i++)
            {
                ColliderPair pair = _pairs[i];
                if (pair.Source == null || pair.Target == null)
                    continue;

                Physics2D.IgnoreCollision(pair.Source, pair.Target, false);
            }

            _pairs.Clear();
        }

        private static List<Collider2D> CollectCharacterColliders(GameObject root)
        {
            var result = new List<Collider2D>(4);
            var unique = new HashSet<Collider2D>();
            CharacterBase character = root.GetComponent<CharacterBase>();
            if (character == null)
                character = root.GetComponentInParent<CharacterBase>();

            if (character != null)
            {
                AddIfValid(character.colliderMapObject, unique, result);
                AddIfValid(character.colliderHitArea, unique, result);
            }

            if (result.Count > 0)
                return result;

            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(includeInactive: false);
            for (int i = 0; i < colliders.Length; i++)
            {
                AddIfValid(colliders[i], unique, result);
            }

            return result;
        }

        private static void AddIfValid(Collider2D collider, HashSet<Collider2D> unique, List<Collider2D> result)
        {
            if (collider == null)
                return;

            if (!unique.Add(collider))
                return;

            result.Add(collider);
        }
    }
}
