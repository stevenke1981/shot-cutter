using System.ComponentModel;

namespace ShotCutter.Core.Models;

public enum CaptureMode
{
    [Description("等間隔截圖")]
    Interval,

    [Description("指定時間點")]
    TimePoint,

    [Description("關鍵幀")]
    KeyFrame,

    [Description("場景變化偵測")]
    SceneChange,

    [Description("第一/最後幀")]
    FirstLastFrame,

    [Description("智慧場景分析")]
    SmartScene,
}
