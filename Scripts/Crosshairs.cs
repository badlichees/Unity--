using UnityEngine;

/// <summary>
/// 准星控制系统
/// 功能：动态旋转准星、目标检测反馈、光标隐藏控制
/// </summary>
public class Crosshairs : MonoBehaviour
{
    /// <summary>
    /// 射线检测的目标层级
    /// </summary>
    public LayerMask targetMask;
    /// <summary>
    /// 准星中心点与目标重合时的颜色
    /// </summary>
    public Color dotHighlightColor = Color.red;
    /// <summary>
    /// 准星中心点精灵渲染组件
    /// </summary>
    public SpriteRenderer dot;

    /// <summary>
    /// 准星默认颜色缓存
    /// </summary>
    Color _originalDotColor;

    void Start()
    {
        // 隐藏系统光标
        Cursor.visible = false;
        // 保存初始准星颜色
        _originalDotColor = dot.color;
    }

    void Update()
    {
        // 每帧逆时针旋转准星（40度/秒）
        // 使用Time.deltaTime保证不同帧率下的旋转速度一致
        transform.Rotate(Vector3.forward * -40 * Time.deltaTime);
    }

    /// <summary>
    /// 目标检测方法（应由射击/瞄准系统调用）
    /// </summary>
    /// <param name="ray">检测射线（通常来自摄像机视口中心）</param>
    public void DetectTargets(Ray ray)
    {
        // 发射100单位长度的射线检测目标
        if (Physics.Raycast(ray, 100, targetMask))
        {
            // 命中目标：显示高亮颜色
            dot.color = dotHighlightColor;
        }
        else
        {
            // 未命中目标：恢复默认颜色
            dot.color = _originalDotColor;
        }
    }
}