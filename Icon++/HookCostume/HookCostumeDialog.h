#pragma once
#include "afxwin.h"
#include "afxcmn.h"
#include "Define.h"

// HookCostumeDialog dialog

class HookCostumeDialog : public CDialogEx
{
	DECLARE_DYNAMIC(HookCostumeDialog)

public:
	HookCostumeDialog(CWnd* pParent = NULL);   // standard constructor
	virtual ~HookCostumeDialog();
	int		NPC_no;
// Dialog Data
	enum { IDD = IDD_DIALOGHOOK };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()
public:	
	void SetNPCNo(int no);
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnTimer(UINT_PTR nIDEvent);
		
	NPC_Info getHoverNPCMemInfo(int noIdnx);
	void SetHoveredNPCInfo(int nIdx);
	CEdit m_NPCNo;
	virtual LRESULT WindowProc(UINT message, WPARAM wParam, LPARAM lParam);
	CEdit m_NPCInfo;
};
