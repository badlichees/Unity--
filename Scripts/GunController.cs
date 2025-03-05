using UnityEngine;
using System.Collections;

public class GunController : MonoBehaviour
{
    /// <summary>
    /// 武器挂载点的空物体（用于确定持枪位置和朝向）
    /// </summary>
    public Transform weaponHold;
    /// <summary>
    /// 所有可装备枪械的预制体数组（索引对应武器编号）
    /// </summary>
    public Gun[] allGuns;
    
    /// <summary>
    /// 当前装备的枪支实例
    /// </summary>
    Gun _equippedGun;
    
    /// <summary>
    /// 通过索引装备武器（常用于武器切换菜单）
    /// </summary>
    /// <param name="weaponIndex">武器在allGuns数组中的索引位置</param>
    public void EquipGun(int weaponIndex)
    {
        // 验证索引有效性（建议在实际调用前添加范围检查）
        EquipGun(allGuns[weaponIndex]);
    }

    /// <summary>
    /// 核心装备武器逻辑：
    /// 1. 销毁当前武器
    /// 2. 实例化新武器预制体
    /// 3. 设置武器父子关系
    /// </summary>
    /// <param name="gunToEquip">需要装备的枪械预制体</param>
    public void EquipGun(Gun gunToEquip) 
    {
        // 卸载现有武器
        if (_equippedGun != null) 
        {
            Destroy(_equippedGun.gameObject);
        }
        
        // 实例化新武器并配置
        _equippedGun = Instantiate(gunToEquip, weaponHold.position, weaponHold.rotation) as Gun;
        _equippedGun.transform.SetParent(weaponHold);  // 保持武器跟随持枪点
        
        // 注意：建议使用对象池优化频繁的Instantiate/Destroy操作
    }

    /// <summary>
    /// 扳机持续按下事件传递（自动连射模式使用）
    /// </summary>
    public void OnTriggerHold() 
    {
        _equippedGun?.OnTriggerHold(); // 空值安全操作符
    }

    /// <summary>
    /// 扳机释放事件传递（单发/连发模式重置使用）
    /// </summary>
    public void OnTriggerRelease()
    {
        _equippedGun?.OnTriggerRelease();
    }

    /// <summary>
    /// 武器高度属性（常用于计算弹道起始点高度）
    /// </summary>
    public float GunHeight
    {
        get { return weaponHold.position.y; }
    }

    /// <summary>
    /// 武器瞄准方向传递（将瞄准点坐标转发给当前武器）
    /// </summary>
    /// <param name="aimPoint">目标点的世界坐标</param>
    public void Aim(Vector3 aimPoint)
    {
        _equippedGun?.Aim(aimPoint);
    }

    /// <summary>
    /// 换弹操作传递（可连接换弹按钮或自动触发）
    /// </summary>
    public void Reload()
    {
        _equippedGun?.Reload();
    }
}