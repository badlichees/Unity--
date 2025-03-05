using UnityEngine;

/// <summary>
/// 子弹控制系统
/// 功能：
/// - 移动轨迹管理
/// - 碰撞检测与伤害计算
/// - 拖尾视觉效果
/// - 自销毁生命周期控制
/// </summary>
public class Projectile : MonoBehaviour
{
    /// <summary>
    /// 可碰撞的层级（避免与无关层交互）
    /// </summary>
    public LayerMask collisionMask;
    /// <summary>
    /// 拖尾效果颜色（需材质支持_TintColor属性）
    /// </summary>
    public Color trailColor;

    /// <summary>
    /// 投射物移动速度（单位：米/秒）
    /// </summary>
    float _speed = 10;
    /// <summary>
    /// 基础伤害值（可被伤害接口接收）
    /// </summary>
    float _damage = 1;
    /// <summary>
    /// 存活时间（秒）后自动销毁
    /// </summary>
    float _lifetime = 3;
    /// <summary>
    /// 碰撞检测容错间距（防止高速穿透）
    /// </summary>
    float _skinWidth = .1f;

    
    /// <summary>
    /// 初始化逻辑（Unity生命周期方法）
    /// </summary>
    void Start() 
    {
        // 设置自销毁定时器
        Destroy(gameObject, _lifetime);

        // 初始碰撞检测（防止生成时嵌入物体）
        Collider[] initialCollisions = Physics.OverlapSphere(transform.position, 0.1f, collisionMask);
        if (initialCollisions.Length > 0) 
        {
            OnHitObject(initialCollisions[0], transform.position);
        }
        
        // 配置拖尾颜色（_TintColor 是着色器（shader）中的一个属性名）
        GetComponent<TrailRenderer>().material.SetColor("_TintColor", trailColor);
    }

    /// <summary>
    /// 动态修改投射速度（用于武器升级等场景）
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        _speed = newSpeed;
    }
    
    void Update () 
    {
        // 计算本帧应移动距离
        float moveDistance = _speed * Time.deltaTime;
        
        // 先检测碰撞再移动（防止漏检）
        CheckCollisions(moveDistance);
        
        // 执行实际移动
        transform.Translate(Vector3.forward * moveDistance);
    }

    /// <summary>
    /// 碰撞检测核心逻辑
    /// </summary>
    /// <param name="moveDistance">本帧计划移动距离</param>
    void CheckCollisions(float moveDistance) 
    {
        // 创建方向射线（使用物体当前朝向）
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        
        // 执行射线碰撞检测
        if (Physics.Raycast(
                ray, // 射线定义：起点为当前位置，方向为正前方
                out hit, // 输出参数：存储碰撞信息（碰撞体、碰撞点等）
                moveDistance + _skinWidth, // 检测长度：本帧移动距离 + 防穿透容错间距
                collisionMask, // 层级过滤：仅检测指定层（如Enemy/Obstacle）
                QueryTriggerInteraction.Collide // 触发器处理：响应标记为Trigger的碰撞体
            ))
        {
            // 碰撞成功时处理命中逻辑
            OnHitObject(hit.collider, hit.point);
        }
    }

    /// <summary>
    /// 碰撞事件处理
    /// </summary>
    /// <param name="c">碰撞体对象</param>
    /// <param name="hitPoint">碰撞点坐标</param>
    void OnHitObject(Collider c, Vector3 hitPoint) 
    {
        // 尝试获取可受伤接口
        IDamageable damageableObject = c.GetComponent<IDamageable>();
        
        if (damageableObject != null) 
        {
            // 传递伤害数据（含击打方向）
            damageableObject.TakeHit(_damage, hitPoint, transform.forward);
        }
        
        // 无论是否造成伤害都销毁投射物
        Destroy(gameObject);
    }
}