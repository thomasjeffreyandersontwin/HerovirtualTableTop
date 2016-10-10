// HookCostumeDialog.cpp : implementation file
//

#include "stdafx.h"
#include "HookCostume.h"
#include "HookCostumeDialog.h"
#include "afxdialogex.h"
#include <valarray>
#include <iostream>
#define COH_NPCENTTBL 0x012F6C40
int ix = -1;
int oldix = 0;

extern int NPC_no;

HookCostumeDialog*	m_pDlgMgr = NULL;
NPC_Data mz_NpcData;
BOOL blAutoMovCancel = FALSE;
////////////////////////////////////////////////////////////////
IMPLEMENT_DYNAMIC(HookCostumeDialog, CDialogEx)

HookCostumeDialog::HookCostumeDialog(CWnd* pParent /*=NULL*/)
	: CDialogEx(HookCostumeDialog::IDD, pParent)
{

}

HookCostumeDialog::~HookCostumeDialog()
{
}

void HookCostumeDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT_HOVERINFO, m_NPCInfo);
}


BEGIN_MESSAGE_MAP(HookCostumeDialog, CDialogEx)
END_MESSAGE_MAP()

BOOL HookCostumeDialog::OnInitDialog()
{
	CDialogEx::OnInitDialog();
	NPC_no = -1;
	SetTimer(1001, 500, NULL);	//for NPC info mouse hover 

	CRect cRC;	
	GetClientRect(&cRC);
	INT	nSWidth = GetSystemMetrics( SM_CXFULLSCREEN );
	INT	nSHeight= GetSystemMetrics( SM_CYFULLSCREEN );	
	::SetWindowPos(m_hWnd, HWND_TOPMOST, nSWidth - cRC.Width(), 0, cRC.Width(), cRC.Height(), SWP_SHOWWINDOW|SWP_NOSIZE);
	
	return TRUE;  
}

void HookCostumeDialog::OnSysCommand(UINT nID, LPARAM lParam)
{
	if (nID == SC_CLOSE)
	{		
//		KillTimer(101);
/*		DWORD dwPID = GetCurrentProcessId() ;
		HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
		TerminateProcess(m_hTargetProcess, 0);
		OnClose();
*/
	}
	else
	{
	}
	CDialogEx::OnSysCommand(nID, lParam);

}

NPC_Info HookCostumeDialog::getHoverNPCMemInfo(int noIdnx)
{	
	DWORD playerAddress;
	DWORD startAddress = COH_NPCENTTBL;
	DWORD dwPID = GetCurrentProcessId() ;	
	HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
	
	NPC_Info mNpcInfo;
	memset(&mNpcInfo, 0x00, sizeof(NPC_Info));	

	if(m_hTargetProcess == INVALID_HANDLE_VALUE)	return mNpcInfo;	
	
	startAddress += 4 * noIdnx;
	
	ReadProcessMemory(m_hTargetProcess,(void *)startAddress, &playerAddress,4,NULL);
	
	DWORD dwSel = 0;
	if(playerAddress){
		DWORD	playerNameAddr;	
		memset(&mNpcInfo, 0x00, sizeof(NPC_Info));	
		ReadProcessMemory(m_hTargetProcess,(void *)(playerAddress + 4), &mNpcInfo.dwNpcID, 4, NULL);
		ReadProcessMemory(m_hTargetProcess,(void *)(playerAddress + 0x38), &mNpcInfo.m_NpcXYZ, sizeof(NPC_XYZ), NULL);
		ReadProcessMemory(m_hTargetProcess,(void *)playerAddress, &playerNameAddr, 4, NULL);
		ReadProcessMemory(m_hTargetProcess,(void *)playerNameAddr, &mNpcInfo.npcName, MAX_NPC_NAME, NULL);

		mNpcInfo.dwStartAddress = startAddress;
		mNpcInfo.dwNameAddress = playerNameAddr;
		mNpcInfo.dwDataAddress = playerAddress;		
	}
	CloseHandle(m_hTargetProcess);
	return mNpcInfo;
}

void HookCostumeDialog::SetHoveredNPCInfo(int nIdx)
{
	NPC_Info mNpcInfo;
	mNpcInfo = getHoverNPCMemInfo(nIdx);
	
	char buff[1024];
	sprintf(buff,
		"Name: [%s] X:[%1.2f] Y:[%1.2f] Z:[%1.2f]",
		mNpcInfo.npcName, mNpcInfo.m_NpcXYZ.flX, mNpcInfo.m_NpcXYZ.flY, mNpcInfo.m_NpcXYZ.flZ);				
	USES_CONVERSION;	
	m_NPCInfo.SetWindowText(A2W(buff));

}

//NPC information display
void HookCostumeDialog::SetNPCNo(int no)
{
	if(no==0){
		ix=0;
		m_NPCNo.SetWindowText(_T("-1"));
		m_NPCInfo.SetWindowText(_T(""));
	}else{
		SetHoveredNPCInfo(no);
	}
}

void HookCostumeDialog::OnTimer(UINT_PTR nIDEvent)
{
	if (nIDEvent == 1001)
	{
		if (NPC_no != -1){
			SetHoveredNPCInfo(NPC_no);
		}else{
			m_NPCNo.SetWindowText(_T("-1"));
			GetDlgItem(IDC_EDIT_HOVERINFO)->SetWindowText(_T(""));
		}
		NPC_no = -1;
	}

	CDialogEx::OnTimer(nIDEvent);
}

LRESULT HookCostumeDialog::WindowProc(UINT message, WPARAM wParam, LPARAM lParam)
{
	if (message == WM_USER + 101){
		SetNPCNo(wParam);
	}

	return CDialogEx::WindowProc(message, wParam, lParam);
}

