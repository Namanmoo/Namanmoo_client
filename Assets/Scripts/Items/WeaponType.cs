/// <summary>
/// 무기 종류 — 궤도와 1:1이다 (검=swing, 도끼=spin, 창=thrust,
/// 유도탄=homing, 투사체=straight, 부메랑=boomerang).
///
/// 순서를 바꾸면 안 된다 — 씬·에셋에 int로 직렬화돼 있다.
/// 2026-08 개명: Missile 자리는 옛 Projectile(유도), Projectile 자리는 옛 Gun(직선).
/// 서버·저장 무기 JSON의 문자열도 같은 개명을 따라야 한다.
/// </summary>
public enum WeaponType
{
    Spear,
    Sword,
    Axe,
    Missile,
    Projectile,
    Boomerang
}
