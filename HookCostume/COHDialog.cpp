// OHDialog.cpp : implementation file
//
#include "stdafx.h"
#include "HookCostume.h"
#include "COHDialog.h"
#include "afxdialogex.h"


// COHDialog dialog

IMPLEMENT_DYNAMIC(COHDialog, CDialogEx)

COHDialog::COHDialog(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_DIALOG1, pParent)
{

}

COHDialog::~COHDialog()
{
}

void COHDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT1, m_NPCINFO);
	DDX_Control(pDX, IDC_EDIT2, m_command);
	DDX_Control(pDX, IDC_EDIT3, m_mousepos);
	DDX_Control(pDX, IDC_EDIT4, m_stagepos);
}


BEGIN_MESSAGE_MAP(COHDialog, CDialogEx)
	ON_BN_CLICKED(IDOK, &COHDialog::OnBnClickedOk)
	ON_WM_TIMER()
END_MESSAGE_MAP()

// COHDialog message handlers
__declspec(dllexport) int __cdecl ExecuteCommand(char *cmdstring);

char strcommand[1024];
void COHDialog::OnBnClickedOk()
{
	TCHAR buff[1024];
	m_command.GetWindowTextW(buff, 1024);

	USES_CONVERSION;
	sprintf_s(strcommand, "%s", W2A(buff));

	ExecuteCommand(strcommand);

	//CDialogEx::OnOK();
}


BOOL COHDialog::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	CRect cRC;
	GetClientRect(&cRC);
	INT	nSWidth = GetSystemMetrics(SM_CXFULLSCREEN);
	INT	nSHeight = GetSystemMetrics(SM_CYFULLSCREEN);
	::SetWindowPos(m_hWnd, HWND_TOPMOST, nSWidth - cRC.Width(), 0, cRC.Width(), cRC.Height(), SWP_SHOWWINDOW | SWP_NOSIZE);

	m_command.SetWindowText(_T("/loadcostume Agents of Orisha 1\\Agents of Orisha 1_BeamRifle_PenetratingRay.fx x=0 y=0.5 z=10"));

	SetTimer(1, 500, NULL); //timer for NPC hovering

	SetTimer(2, 50, NULL);	//timer for mouse x,y,z

//	char buff[1024];
//	sprintf_s(buff, "%x", gamePID);
//	USES_CONVERSION;
//	SetWindowText(A2W(buff));
//	ManagerWND = this.m_hWnd;
	return TRUE;  // return TRUE unless you set the focus to a control
				  // EXCEPTION: OCX Property Pages should return FALSE
}

void SetNPC();
void SetXYZ();
LRESULT COHDialog::WindowProc(UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message) {
		case WM_USER+101:
//			KillTimer(1);
			//NPC hovering display
//			SetNPCNo(wParam);
//			SetTimer(1, 100, NULL);
			break;
		case WM_USER + 102:
			//Mouse Pos X,Y,Z
			SetXYZ();// wParam, lParam);
			break;
	}

	return CDialogEx::WindowProc(message, wParam, lParam);
}


void COHDialog::OnTimer(UINT_PTR nIDEvent)
{
	switch(nIDEvent){
		case 1:
			//NPC hovering display
			SetNPC();
			break;
		case 2:
			//Mouse Pos X,Y,Z
			SetXYZ();// wParam, lParam);
			break;

	}

	CDialogEx::OnTimer(nIDEvent);
}
