using NUnit.Framework;

/// <summary>
/// 결과 안내 문구. 조용한 실패가 이 기능에서 가장 나쁜 결과였다 — AI 쿼터가 떨어진
/// 날 화면은 아무 말 없이 "연필 막대"를 띄웠고, 플레이어는 자기 그림이 그렇게
/// 해석된 줄로 알았다.
/// </summary>
public sealed class ForgeNoticeTests
{
    private static ForgeResponseDto Result(bool fallback = false, bool imageFailed = false)
    {
        return new ForgeResponseDto { fallback = fallback, imageFailed = imageFailed };
    }

    [Test]
    public void SuccessSaysNothing()
    {
        Assert.That(WeaponForgeController.NoticeFor(2, null, Result()), Is.Empty);
    }

    [Test]
    public void StatsFallbackIsAlwaysAnnounced()
    {
        // 이게 빠져 있었던 버그. 기본 스탯이 들어갔으면 반드시 말해야 한다.
        string notice = WeaponForgeController.NoticeFor(0, null, Result(fallback: true));

        Assert.That(notice, Is.Not.Empty);
        Assert.That(notice, Does.Contain("기본 스탯"));
    }

    [Test]
    public void ImageFailureIsAnnouncedForGeneratedStages()
    {
        string notice = WeaponForgeController.NoticeFor(1, null, Result(imageFailed: true));

        Assert.That(notice, Does.Contain("조금 멋있게"));
    }

    [Test]
    public void StageZeroNeverMentionsImageGeneration()
    {
        // 0단계는 애초에 이미지를 만들지 않는다. 실패했다고 말하면 거짓이다.
        Assert.That(
            WeaponForgeController.NoticeFor(0, null, Result(imageFailed: true)),
            Is.Empty);
    }

    [Test]
    public void BothFailuresAreReportedTogether()
    {
        string notice = WeaponForgeController.NoticeFor(2, null, Result(fallback: true, imageFailed: true));

        Assert.That(notice, Does.Contain("기본 스탯"));
        Assert.That(notice, Does.Contain("완전 멋있게"));
    }

    [Test]
    public void ExplicitFailureWinsOverEverythingElse()
    {
        string notice = WeaponForgeController.NoticeFor(2, "서버에 연결할 수 없습니다", Result(fallback: true));

        Assert.That(notice, Is.EqualTo("서버에 연결할 수 없습니다"));
    }

    [Test]
    public void MissingResponseIsNotAnError()
    {
        Assert.That(WeaponForgeController.NoticeFor(1, null, null), Is.Empty);
    }

    [Test]
    public void OutOfRangeStageDoesNotThrow()
    {
        Assert.That(
            () => WeaponForgeController.NoticeFor(99, null, Result(imageFailed: true)),
            Throws.Nothing);
    }
}
