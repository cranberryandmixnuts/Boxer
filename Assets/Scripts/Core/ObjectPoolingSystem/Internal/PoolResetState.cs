using System;
using UnityEngine;

internal sealed class PoolResetState
{
    private readonly PooledObject owner;
    private readonly bool rootWasActive;
    private readonly TransformState[] transforms;
    private readonly ActiveState[] childActiveStates;
    private readonly LayerState[] layers;
    private readonly RigidbodyState[] rigidbodies;
    private readonly Rigidbody2DState[] rigidbodies2D;
    private readonly AnimatorState[] animators;
    private readonly ColliderState[] colliders;
    private readonly Collider2DState[] colliders2D;
    private readonly RendererState[] renderers;
    private readonly SpriteRendererState[] spriteRenderers;
    private readonly TrailRenderer[] trails;
    private readonly ParticleSystem[] particles;
    private readonly AudioSource[] audioSources;

    internal bool RootWasActive => rootWasActive;

    internal PoolResetState(PooledObject owner, bool rootWasActive)
    {
        this.owner = owner;
        this.rootWasActive = rootWasActive;

        Transform[] foundTransforms = owner.GetComponentsInChildren<Transform>(true);
        transforms = new TransformState[foundTransforms.Length];
        for (int i = 0; i < foundTransforms.Length; i++)
            transforms[i] = new TransformState(foundTransforms[i]);

        childActiveStates = new ActiveState[Mathf.Max(0, foundTransforms.Length - 1)];
        for (int i = 1; i < foundTransforms.Length; i++)
            childActiveStates[i - 1] = new ActiveState(foundTransforms[i].gameObject);

        layers = owner.RestoreGameObjectLayers
            ? CaptureLayers(foundTransforms)
            : Array.Empty<LayerState>();
        rigidbodies = owner.ResetRigidbodies
            ? CaptureRigidbodies(owner)
            : Array.Empty<RigidbodyState>();
        rigidbodies2D = owner.ResetRigidbodies
            ? CaptureRigidbodies2D(owner)
            : Array.Empty<Rigidbody2DState>();
        animators = owner.ResetAnimators
            ? CaptureAnimators(owner)
            : Array.Empty<AnimatorState>();
        colliders = owner.RestoreColliderStates
            ? CaptureColliders(owner)
            : Array.Empty<ColliderState>();
        colliders2D = owner.RestoreColliderStates
            ? CaptureColliders2D(owner)
            : Array.Empty<Collider2DState>();
        renderers = owner.RestoreRendererStates
            ? CaptureRenderers(owner)
            : Array.Empty<RendererState>();
        spriteRenderers = owner.RestoreRendererStates
            ? CaptureSpriteRenderers(owner)
            : Array.Empty<SpriteRendererState>();
        trails = owner.ClearTrailRenderers
            ? owner.GetComponentsInChildren<TrailRenderer>(true)
            : Array.Empty<TrailRenderer>();
        particles = owner.ClearParticleSystems
            ? owner.GetComponentsInChildren<ParticleSystem>(true)
            : Array.Empty<ParticleSystem>();
        audioSources = owner.StopAudioSources
            ? owner.GetComponentsInChildren<AudioSource>(true)
            : Array.Empty<AudioSource>();
    }

    internal void RestoreForSpawn()
    {
        RestoreTransformState();

        if (owner.RestoreChildActiveStates)
        {
            for (int i = 0; i < childActiveStates.Length; i++)
                childActiveStates[i].Restore();
        }

        for (int i = 0; i < layers.Length; i++)
            layers[i].Restore();

        for (int i = 0; i < rigidbodies.Length; i++)
            rigidbodies[i].RestoreConfiguration();

        for (int i = 0; i < rigidbodies2D.Length; i++)
            rigidbodies2D[i].RestoreConfiguration();

        for (int i = 0; i < animators.Length; i++)
            animators[i].RestoreConfiguration();

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].Restore();

        for (int i = 0; i < colliders2D.Length; i++)
            colliders2D[i].Restore();

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].Restore();

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].Restore();

        ResetTransientState();
    }

    internal void ResetForDespawn()
    {
        RestoreTransformState();
        ResetTransientState();
    }

    private void RestoreTransformState()
    {
        if (!owner.RestoreTransformHierarchy)
            return;

        for (int i = 1; i < transforms.Length; i++)
            transforms[i].RestoreParent();

        for (int i = 1; i < transforms.Length; i++)
            transforms[i].RestoreSiblingIndex();

        for (int i = 0; i < transforms.Length; i++)
            transforms[i].RestoreLocalTransform();
    }

    private void ResetTransientState()
    {
        for (int i = 0; i < rigidbodies.Length; i++)
            rigidbodies[i].ResetVelocity();

        for (int i = 0; i < rigidbodies2D.Length; i++)
            rigidbodies2D[i].ResetVelocity();

        for (int i = 0; i < animators.Length; i++)
            animators[i].ResetPlayback();

        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] != null)
                trails[i].Clear();
        }

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
                audioSources[i].Stop();
        }
    }

    private static LayerState[] CaptureLayers(Transform[] transforms)
    {
        var states = new LayerState[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
            states[i] = new LayerState(transforms[i].gameObject);

        return states;
    }

    private static RigidbodyState[] CaptureRigidbodies(PooledObject owner)
    {
        Rigidbody[] components = owner.GetComponentsInChildren<Rigidbody>(true);
        var states = new RigidbodyState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new RigidbodyState(components[i]);

        return states;
    }

    private static Rigidbody2DState[] CaptureRigidbodies2D(PooledObject owner)
    {
        Rigidbody2D[] components = owner.GetComponentsInChildren<Rigidbody2D>(true);
        var states = new Rigidbody2DState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new Rigidbody2DState(components[i]);

        return states;
    }

    private static AnimatorState[] CaptureAnimators(PooledObject owner)
    {
        Animator[] components = owner.GetComponentsInChildren<Animator>(true);
        var states = new AnimatorState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new AnimatorState(components[i]);

        return states;
    }

    private static ColliderState[] CaptureColliders(PooledObject owner)
    {
        Collider[] components = owner.GetComponentsInChildren<Collider>(true);
        var states = new ColliderState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new ColliderState(components[i]);

        return states;
    }

    private static Collider2DState[] CaptureColliders2D(PooledObject owner)
    {
        Collider2D[] components = owner.GetComponentsInChildren<Collider2D>(true);
        var states = new Collider2DState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new Collider2DState(components[i]);

        return states;
    }

    private static RendererState[] CaptureRenderers(PooledObject owner)
    {
        Renderer[] components = owner.GetComponentsInChildren<Renderer>(true);
        var states = new RendererState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new RendererState(components[i]);

        return states;
    }

    private static SpriteRendererState[] CaptureSpriteRenderers(PooledObject owner)
    {
        SpriteRenderer[] components = owner.GetComponentsInChildren<SpriteRenderer>(true);
        var states = new SpriteRendererState[components.Length];
        for (int i = 0; i < components.Length; i++)
            states[i] = new SpriteRendererState(components[i]);

        return states;
    }

    private readonly struct TransformState
    {
        private readonly Transform transform;
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;

        internal TransformState(Transform transform)
        {
            this.transform = transform;
            parent = transform.parent;
            siblingIndex = transform.GetSiblingIndex();
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        internal void RestoreParent()
        {
            if (transform != null && parent != null && transform.parent != parent)
                transform.SetParent(parent, false);
        }

        internal void RestoreSiblingIndex()
        {
            if (transform != null && parent != null && transform.parent == parent)
                transform.SetSiblingIndex(siblingIndex);
        }

        internal void RestoreLocalTransform()
        {
            if (transform == null)
                return;

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }
    }

    private readonly struct LayerState
    {
        private readonly GameObject gameObject;
        private readonly int layer;

        internal LayerState(GameObject gameObject)
        {
            this.gameObject = gameObject;
            layer = gameObject.layer;
        }

        internal void Restore()
        {
            if (gameObject != null)
                gameObject.layer = layer;
        }
    }

    private readonly struct RigidbodyState
    {
        private readonly Rigidbody body;
        private readonly bool isKinematic;
        private readonly bool useGravity;
        private readonly bool detectCollisions;
        private readonly RigidbodyConstraints constraints;

        internal RigidbodyState(Rigidbody body)
        {
            this.body = body;
            isKinematic = body.isKinematic;
            useGravity = body.useGravity;
            detectCollisions = body.detectCollisions;
            constraints = body.constraints;
        }

        internal void RestoreConfiguration()
        {
            if (body == null)
                return;

            body.isKinematic = isKinematic;
            body.useGravity = useGravity;
            body.detectCollisions = detectCollisions;
            body.constraints = constraints;
        }

        internal void ResetVelocity()
        {
            if (body == null || body.isKinematic)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private readonly struct Rigidbody2DState
    {
        private readonly Rigidbody2D body;
        private readonly bool simulated;
        private readonly RigidbodyType2D bodyType;
        private readonly float gravityScale;
        private readonly RigidbodyConstraints2D constraints;

        internal Rigidbody2DState(Rigidbody2D body)
        {
            this.body = body;
            simulated = body.simulated;
            bodyType = body.bodyType;
            gravityScale = body.gravityScale;
            constraints = body.constraints;
        }

        internal void RestoreConfiguration()
        {
            if (body == null)
                return;

            body.bodyType = bodyType;
            body.gravityScale = gravityScale;
            body.constraints = constraints;
            body.simulated = simulated;
        }

        internal void ResetVelocity()
        {
            if (body == null || body.bodyType == RigidbodyType2D.Static)
                return;

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private readonly struct AnimatorState
    {
        private readonly Animator animator;
        private readonly bool enabled;
        private readonly float speed;

        internal AnimatorState(Animator animator)
        {
            this.animator = animator;
            enabled = animator.enabled;
            speed = animator.speed;
        }

        internal void RestoreConfiguration()
        {
            if (animator == null)
                return;

            animator.enabled = enabled;
            animator.speed = speed;
        }

        internal void ResetPlayback()
        {
            if (animator == null)
                return;

            animator.Rebind();
            animator.Update(0f);
        }
    }

    private readonly struct ColliderState
    {
        private readonly Collider collider;
        private readonly bool enabled;
        private readonly bool isTrigger;

        internal ColliderState(Collider collider)
        {
            this.collider = collider;
            enabled = collider.enabled;
            isTrigger = collider.isTrigger;
        }

        internal void Restore()
        {
            if (collider == null)
                return;

            collider.enabled = enabled;
            collider.isTrigger = isTrigger;
        }
    }

    private readonly struct Collider2DState
    {
        private readonly Collider2D collider;
        private readonly bool enabled;
        private readonly bool isTrigger;

        internal Collider2DState(Collider2D collider)
        {
            this.collider = collider;
            enabled = collider.enabled;
            isTrigger = collider.isTrigger;
        }

        internal void Restore()
        {
            if (collider == null)
                return;

            collider.enabled = enabled;
            collider.isTrigger = isTrigger;
        }
    }

    private readonly struct RendererState
    {
        private readonly Renderer renderer;
        private readonly bool enabled;

        internal RendererState(Renderer renderer)
        {
            this.renderer = renderer;
            enabled = renderer.enabled;
        }

        internal void Restore()
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
    }

    private readonly struct SpriteRendererState
    {
        private readonly SpriteRenderer renderer;
        private readonly Sprite sprite;
        private readonly Color color;
        private readonly bool flipX;
        private readonly bool flipY;

        internal SpriteRendererState(SpriteRenderer renderer)
        {
            this.renderer = renderer;
            sprite = renderer.sprite;
            color = renderer.color;
            flipX = renderer.flipX;
            flipY = renderer.flipY;
        }

        internal void Restore()
        {
            if (renderer == null)
                return;

            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = flipX;
            renderer.flipY = flipY;
        }
    }

    private readonly struct ActiveState
    {
        private readonly GameObject gameObject;
        private readonly bool activeSelf;

        internal ActiveState(GameObject gameObject)
        {
            this.gameObject = gameObject;
            activeSelf = gameObject.activeSelf;
        }

        internal void Restore()
        {
            if (gameObject != null && gameObject.activeSelf != activeSelf)
                gameObject.SetActive(activeSelf);
        }
    }
}
