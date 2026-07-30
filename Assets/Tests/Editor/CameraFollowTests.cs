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
    public void OverscanLetsTheCameraShowPastTheEdge()
    {
        // 벽 선이 화면 끝에 딱 붙으면 갑갑하다. 바깥이 조금 보여야 방 끝이 눈에 들어온다.
        const float Margin = 2.5f;

        float withoutMargin = Clamp(new Vector2(100f, 0f)).x;
        float withMargin = CameraFollow.ClampToBounds(
            new Vector2(100f, 0f), Map, HalfWidth, HalfHeight, Margin).x;

        Assert.That(withMargin, Is.EqualTo(withoutMargin + Margin).Within(0.001f));
    }

    [Test]
    public void OverscanAppliesToEverySide()
    {
        const float Margin = 3f;

        Vector2 lowerLeft = CameraFollow.ClampToBounds(
            new Vector2(-100f, -100f), Map, HalfWidth, HalfHeight, Margin);
        Vector2 upperRight = CameraFollow.ClampToBounds(
            new Vector2(100f, 100f), Map, HalfWidth, HalfHeight, Margin);

        Assert.That(lowerLeft.x, Is.EqualTo(Map.xMin - Margin + HalfWidth).Within(0.001f));
        Assert.That(lowerLeft.y, Is.EqualTo(Map.yMin - Margin + HalfHeight).Within(0.001f));
        Assert.That(upperRight.x, Is.EqualTo(Map.xMax + Margin - HalfWidth).Within(0.001f));
        Assert.That(upperRight.y, Is.EqualTo(Map.yMax + Margin - HalfHeight).Within(0.001f));
    }

    [Test]
    public void OverscanDoesNotMoveTheCameraInTheMiddle()
    {
        // 가운데서는 여백이 아무 영향도 주면 안 된다 — 클램프가 끼어들지 않는 구간이다
        Assert.That(
            CameraFollow.ClampToBounds(new Vector2(3f, -4f), Map, HalfWidth, HalfHeight, 2.5f),
            Is.EqualTo(new Vector2(3f, -4f)));
    }

    [Test]
    public void NegativeOverscanIsTreatedAsNone()
    {
        Assert.That(
            CameraFollow.ClampToBounds(new Vector2(100f, 0f), Map, HalfWidth, HalfHeight, -5f).x,
            Is.EqualTo(Clamp(new Vector2(100f, 0f)).x));
    }

    [Test]
    public void AWideViewStillCentresEvenWithOverscan()
    {
        // 여백을 줘도 뷰가 맵보다 넓으면 자를 여지가 없다. 넓힌 영역의 가운데는 맵 가운데다.
        Vector2 result = CameraFollow.ClampToBounds(
            new Vector2(100f, 100f), Map, halfWidth: 40f, halfHeight: 30f, overscan: 2.5f);

        Assert.That(result, Is.EqualTo(Map.center));
    }

    [Test]
    public void TheComponentDefaultsToShowingALittlePastTheEdge()
    {
        var cameraObject = new GameObject("Overscan Test", typeof(Camera), typeof(CameraFollow));
        try
        {
            CameraFollow follow = cameraObject.GetComponent<CameraFollow>();

            Assert.That(follow.Overscan, Is.GreaterThan(0f), "기본값이 0이면 벽이 화면에 딱 붙는다");

            follow.Overscan = -3f;
            Assert.That(follow.Overscan, Is.Zero, "음수 여백은 경계를 좁혀 카메라가 튄다");
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
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
