using System.Collections;
using UnityEngine;

/// <summary>
/// 抛射物体理模拟系统（弹壳）
/// 功能：
/// - 施加随机初速度和旋转
/// - 生命周期自动销毁
/// - 淡出消失效果
/// </summary>
public class Shell : MonoBehaviour
{
    /// <summary>
    /// 抛射体刚体组件
    /// </summary>
    public Rigidbody myRigidBody;
    /// <summary>
    /// 最小初始推进力
    /// </summary>
    public float minForce;
    /// <summary>
    /// 最大初始推进力
    /// </summary>
    public float maxForce;

    /// <summary>
    /// 存活时间（秒）后开始淡出
    /// </summary>
    float _lifeTime = 4;
    /// <summary>
    /// 淡出效果持续时间（秒）
    /// </summary>
    float _fadeTime = 2;
    
    
    /// <summary>
    /// 初始化抛射体运动
    /// </summary>
    void Start()
    {
        // 生成随机推进力并应用
        float force = Random.Range(minForce, maxForce);
        myRigidBody.AddForce(transform.forward * force); // 沿物体前方推进
        myRigidBody.AddTorque(Random.insideUnitSphere * force); // 随机旋转扭矩

        // 启动生命周期协程
        StartCoroutine(Fade());
    }

    /// <summary>
    /// 生命周期管理协程
    /// 阶段：
    /// 1. 等待存活时间
    /// 2. 执行材质淡出
    /// 3. 销毁对象
    /// </summary>
    IEnumerator Fade()
    {
        // 第一阶段：等待存活时间
        yield return new WaitForSeconds(_lifeTime);

        // 第二阶段：材质透明度渐变
        float percent = 0;
        float fadeSpeed = 1 / _fadeTime; // 计算淡出速度
        Material material = GetComponent<Renderer>().material;
        Color initialColor = material.color; // 记录初始颜色

        while (percent < 1)
        {
            percent += Time.deltaTime * fadeSpeed; // 基于时间累加进度
            material.color = Color.Lerp(initialColor, Color.clear, percent);
            
            yield return null; // 每帧更新
        }

        // 第三阶段：销毁对象
        Destroy(gameObject);
    }
}
