using System;
using UnityEngine;

public static class EnemyFactory
{
    public static EnemyHealth Create(
        EnemyDefinition definition,
        EnemySpawnRequest request)
    {
        Validate(definition, request);

        string instanceName = string.IsNullOrEmpty(request.InstanceName)
            ? definition.DisplayName
            : request.InstanceName;
        if (string.IsNullOrEmpty(instanceName))
        {
            instanceName = "Enemy";
        }

        var root = new GameObject(instanceName);
        root.transform.SetParent(request.Parent, false);
        root.transform.position =
            new Vector3(request.Position.x, request.Position.y, -0.2f);

        CreateVisual(root.transform, definition);
        ConfigurePhysics(root, definition);

        root.AddComponent<EnemyDamageFlash>();
        EnemyHealth health = root.AddComponent<EnemyHealth>();
        health.Configure(definition.MaxHealth);
        health.SurfaceMaterial = definition.SurfaceMaterial;

        switch (definition.BehaviorType)
        {
            case EnemyBehaviorType.ChaseContact:
                ChaseContactEnemyController contactController =
                    root.AddComponent<ChaseContactEnemyController>();
                contactController.Initialize(definition, request.Target);
                break;

            case EnemyBehaviorType.ApproachAndShoot:
                ApproachAndShootEnemyController rangedController =
                    root.AddComponent<ApproachAndShootEnemyController>();
                rangedController.Initialize(definition, request.Target);
                break;

            case EnemyBehaviorType.StationaryFourWayShoot:
                StationaryFourWayShooterController stationaryController =
                    root.AddComponent<StationaryFourWayShooterController>();
                stationaryController.Initialize(
                    definition,
                    request.Target,
                    Time.time);
                break;
        }

        return health;
    }

    private static void Validate(
        EnemyDefinition definition,
        EnemySpawnRequest request)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Target == null)
        {
            throw new ArgumentNullException(
                nameof(request),
                "Enemy spawn request must include a target.");
        }

        if (definition.BodySprite == null)
        {
            throw new ArgumentException(
                "Enemy definition must include a body sprite.",
                nameof(definition));
        }

        bool usesProjectiles =
            definition.BehaviorType == EnemyBehaviorType.ApproachAndShoot ||
            definition.BehaviorType == EnemyBehaviorType.StationaryFourWayShoot;
        if (usesProjectiles &&
            (definition.ProjectileSprite == null ||
             definition.ProjectileSpeed <= 0f ||
             definition.ProjectileLifetime <= 0f ||
             definition.ProjectileRadius <= 0f))
        {
            throw new ArgumentException(
                "Ranged enemy definition must include valid projectile data.",
                nameof(definition));
        }

        if (definition.BehaviorType != EnemyBehaviorType.ChaseContact &&
            definition.BehaviorType != EnemyBehaviorType.ApproachAndShoot &&
            definition.BehaviorType != EnemyBehaviorType.StationaryFourWayShoot)
        {
            throw new ArgumentException(
                "Enemy definition uses an unsupported behavior type.",
                nameof(definition));
        }
    }

    private static void CreateVisual(
        Transform parent,
        EnemyDefinition definition)
    {
        string visualName = string.IsNullOrEmpty(definition.DisplayName)
            ? "Enemy Visual"
            : definition.DisplayName + " Visual";
        var visualObject = new GameObject(visualName);
        visualObject.transform.SetParent(parent, false);

        float visualScale =
            definition.VisualHeight /
            Mathf.Max(definition.BodySprite.bounds.size.y, 0.01f);
        visualObject.transform.localScale =
            new Vector3(visualScale, visualScale, 1f);

        EnemyVisualController visual =
            visualObject.AddComponent<EnemyVisualController>();
        visual.Configure(
            definition.BodySprite,
            definition.AnimatorController);
        visualObject.GetComponent<SpriteRenderer>().sortingOrder = 4;
    }

    private static void ConfigurePhysics(
        GameObject root,
        EnemyDefinition definition)
    {
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = definition.BodyCollisionRadius;
        collider.isTrigger = false;
    }
}
