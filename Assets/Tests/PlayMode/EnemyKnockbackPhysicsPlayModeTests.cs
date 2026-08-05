using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 진짜 Rigidbody2D + 진짜 ChaseContactEnemyController.FixedUpdate + 진짜 물리 스텝으로
/// 넉백이 실제 화면상 변위를 만드는지 확인한다. 유닛 테스트는 이 조합을 못 잡는다 —
/// 컨트롤러가 매 물리 스텝 MovePosition으로 자기 위치를 덮어쓰기 때문이다.
/// </summary>
public class EnemyKnockbackPhysicsPlayModeTests
{
    private GameObject enemyObject;
    private GameObject targetObject;
    private EnemyDefinition definition;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(enemyObject);
        Object.Destroy(targetObject);
        if (definition != null)
        {
            Object.Destroy(definition);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator Knockback_MovesARealChaseEnemyAwayFromTheAttacker()
    {
        Vector3 isolatedPosition = new Vector3(4567f, 8901f, 0f);

        // 공격자(플레이어)는 적의 왼쪽에 있다
        targetObject = new GameObject("Knockback Physics Target");
        targetObject.transform.position = isolatedPosition + new Vector3(-5f, 0f, 0f);

        enemyObject = new GameObject("Knockback Physics Enemy");
        enemyObject.transform.position = isolatedPosition;
        enemyObject.AddComponent<CircleCollider2D>();
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        health.Configure(100);

        definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        definition.Configure(
            "test_chaser", "시험 추적자", null, null, EnemyBehaviorType.ChaseContact,
            100, 0f, 0, 0.5f, 1f, 0f, 1f, 0.1f);

        ChaseContactEnemyController controller =
            enemyObject.AddComponent<ChaseContactEnemyController>();

        // 실제 EnemyFactory가 만드는 적과 같은 물리 설정
        Rigidbody2D body = enemyObject.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        controller.Initialize(definition, targetObject.transform);

        // 공격 방향은 공격자 → 적, 즉 오른쪽이다. 적은 공격자 반대인 오른쪽으로
        // 밀려나야 한다(추적 이동은 moveSpeed=0이라 기여분이 없다).
        EnemyKnockback.Apply(health, Vector2.right);

        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        yield return null;

        Assert.That(
            enemyObject.transform.position.x, Is.GreaterThan(isolatedPosition.x + 0.05f),
            "공격 방향이 오른쪽이었으므로 적은 공격자 반대인 오른쪽으로 밀려나야 한다");
    }
}
