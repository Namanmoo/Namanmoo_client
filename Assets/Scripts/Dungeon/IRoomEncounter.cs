using UnityEngine;

namespace NaManMoo.Dungeon
{
    /// <summary>
    /// 방에 무엇이 나올지 정한다. <see cref="DungeonRunner"/>는 적을 직접 만들지 않고
    /// 이 이음새로 넘긴다 — 그래서 방 전환만 따로 테스트할 수 있다.
    ///
    /// 돌려주는 <see cref="Stage1EncounterGate"/>가 적을 추적한다. 전부 죽으면
    /// <c>IsOpen</c>이 서고 런너가 그때 문을 연다. Stage1이 쓰던 것을 그대로 쓴다.
    /// null을 돌려주면 그 방은 싸울 것이 없다는 뜻이다.
    /// </summary>
    public interface IRoomEncounter
    {
        Stage1EncounterGate Spawn(
            Transform roomRoot, Transform player, RoomShape shape, DungeonRoom room);
    }
}
