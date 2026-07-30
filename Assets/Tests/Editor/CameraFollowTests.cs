using NUnit.Framework;
using UnityEngine;

public sealed class CameraFollowTests
{
    // 맵보다 좁은 뷰 — Stage1의 실제 상황 (맵 45x40, 뷰 약 35.6x20)
    private static readonly Rect Map = Rect.MinMaxRect(-22.5f, -20f, 22.5f, 20f);
    private const float HalfWidth = 17.8f;
    private const float HalfHeight = 10f;

    private static Vector2 Clamp(Vector2 desired)
    {
        return CameraFollow.ClampToBounds(desired, Map, HalfWidth, HalfHeight);
    }

    [Test]
    public void CameraFollowsFreelyInTheMiddle()
    {
        Assert.That(Clamp(new Vector2(3f, -4f)), Is.EqualTo(new Vector2(3f, -4f)));
    }

    [Test]
    public void CameraStopsBeforeShowingPastTheMapEdges()
    {
        // 플레이어가 맵 끝으로 가도 카메라는 맵 밖 여백을 보여주지 않는다
        Assert.That(Clamp(new Vector2(-100f, 0f)).x, Is.EqualTo(Map.xMin + HalfWidth));
        Assert.That(Clamp(new Vector2(100f, 0f)).x, Is.EqualTo(Map.xMax - HalfWidth));
        Assert.That(Clamp(new Vector2(0f, -100f)).y, Is.EqualTo(Map.yMin + HalfHeight));
        Assert.That(Clamp(new Vector2(0f, 100f)).y, Is.EqualTo(Map.yMax - HalfHeight));
    }

    [Test]
    public void AxesAreClampedIndependently()
    {
        // 가로만 끝에 닿았으면 세로는 자유롭게 따라가야 한다
        Vector2 result = Clamp(new Vector2(100f, 5f));

        Assert.That(result.x, Is.EqualTo(Map.xMax - HalfWidth));
        Assert.That(result.y, Is.EqualTo(5f));
    }

    [Test]
    public void AnAxisWiderThanTheMapCentresInstead()
    {
        // 창을 아주 넓게 늘이면 뷰가 맵보다 넓어진다. 자를 여지가 없으니 가운데로 둔다
        // (자르려 하면 min > max가 되어 카메라가 튄다)
        Vector2 result = CameraFollow.ClampToBounds(
            new Vector2(100f, 100f), Map, halfWidth: 40f, halfHeight: 30f);

        Assert.That(result, Is.EqualTo(Map.center));
    }

    [Test]
    public void ViewExactlyMatchingTheMapCentres()
    {
        Vector2 result = CameraFollow.ClampToBounds(
            new Vector2(10f, 10f), Map, Map.width * 0.5f, Map.height * 0.5f);

        Assert.That(result, Is.EqualTo(Map.center));
    }

    [Test]
    public void MapBoundsMatchTheGeneratedOutline()
    {
        // 카메라가 쓰는 경계가 실제로 그려지는 맵과 어긋나면 빈 공간이 보인다
        Rect bounds = Stage1MapDefinition.Bounds;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 point in Stage1MapDefinition.Outline)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        Assert.That(bounds.xMin, Is.EqualTo(minX).Within(0.001f));
        Assert.That(bounds.xMax, Is.EqualTo(maxX).Within(0.001f));
        Assert.That(bounds.yMin, Is.EqualTo(minY).Within(0.001f));
        Assert.That(bounds.yMax, Is.EqualTo(maxY).Within(0.001f));
        Assert.That(bounds.width, Is.GreaterThan(0f));
    }

    [Test]
    public void FollowComponentKeepsTheCameraDepth()
    {
        var cameraObject = new GameObject("Follow Test", typeof(Camera), typeof(CameraFollow));
        try
        {
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var target = new GameObject("Target");
            target.transform.position = new Vector3(6f, 3f, 5f);

            CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
            // 경계를 아주 넓게 둬 클램프가 끼어들지 않게 한다 — 여기서 보려는 건 z다.
            // (테스트 카메라의 종횡비는 배치 모드에서 정해지므로 통제할 수 없다)
            follow.Bounds = Rect.MinMaxRect(-10000f, -10000f, 10000f, 10000f);
            follow.Target = target.transform;
            follow.SnapToTarget();

            // z를 따라가면 직교 카메라의 깊이가 바뀌어 스프라이트가 잘린다
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(-10f));
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(6f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(3f));

            Object.DestroyImmediate(target);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
