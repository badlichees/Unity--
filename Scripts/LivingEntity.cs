using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

/// <summary>
/// 可被攻击的实体基类
/// 功能：生命管理、伤害处理、死亡事件
/// 实现接口：IDamageable（可被伤害接口）
/// </summary>
public class LivingEntity : MonoBehaviour, IDamageable 
{
    #region 变量声明
    
    /// <summary>
    /// 是否已经死亡
    /// </summary>
    protected bool Dead;
    
    /// <summary>
    /// 初始状态的生命值
    /// </summary>
    public float startingHealth;
    /// <summary>
    /// 当前生命值
    /// </summary>
    public float Health { get; protected set; }
    /// <summary>
    /// 死亡事件的委托，采用Action泛型委托
    /// </summary>
    public event System.Action OnDeath;
    
    #endregion
    
    /// <summary>
    /// 用虚方法的形式写一个初始化方法：将初始生命值赋予当前生命值
    /// </summary>
    protected virtual void Start() 
    {
        Health = startingHealth;
    }

    /// <summary>
    /// 受击点处理方法（实现IDamageable接口），子弹击中特定部位时调用受伤方法
    /// </summary>
    /// <param name="damage">伤害数值（需为正数）</param>
    /// <param name="hitPoint"></param>
    /// <param name="hitDirection"></param>
    public virtual void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        TakeDamage (damage);
    }

    /// <summary>
    /// 受伤方法：当前生命值减去伤害值，若是当前生命已小于零且处在非死亡状态，则调用死亡方法
    /// </summary>
    /// <param name="damage">伤害值</param>
    public virtual void TakeDamage(float damage)
    {
        Health -= damage;
		
        if (Health <= 0 && !Dead) 
        {
            Die();
        }
    }

    
    /// <summary>
    /// 死亡方法：1、Dead设为true 2、首先检查死亡事件是否有订阅者，若有才会触发该事件
    /// </summary>
    [ContextMenu("Self Destruct")]
    public virtual void Die()
    {
        Dead = true;
        if (OnDeath != null) 
        {
            OnDeath();
        }
        Destroy (gameObject);
    }
}