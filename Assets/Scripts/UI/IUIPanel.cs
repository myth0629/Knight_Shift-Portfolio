using UnityEngine;

/// <summary>
/// UI 패널이 구현해야 하는 인터페이스
/// UIPanelManager에 등록되어 ESC 키로 관리될 수 있습니다.
/// </summary>
public interface IUIPanel
{
    /// <summary>
    /// 패널이 현재 열려있는지 확인
    /// </summary>
    bool IsOpen();

    /// <summary>
    /// 패널 닫기
    /// </summary>
    void Close();
}
