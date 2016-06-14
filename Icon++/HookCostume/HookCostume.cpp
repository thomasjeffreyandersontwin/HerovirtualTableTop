// HookCostume.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "HookCostume.h"
#include "power.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CHookCostumeApp

BEGIN_MESSAGE_MAP(CHookCostumeApp, CWinApp)
END_MESSAGE_MAP()


// CHookCostumeApp construction

CHookCostumeApp::CHookCostumeApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}


// The one and only CHookCostumeApp object

CHookCostumeApp theApp;

#include "HookCostumeDialog.h"
HANDLE	m_HookDLGThread;
DWORD	m_HookDLGThreadID;
HookCostumeDialog m_HookDLG;
int HookDLGThread()
{
	Sleep(10000);
	m_HookDLG.DoModal();
	return 0;
}
BOOL PowerHook();
//////////////////////////////////////
BOOL CHookCostumeApp::InitInstance()
{
	CWinApp::InitInstance();

	m_HookDLGThread	= ::CreateThread( NULL, 0, (LPTHREAD_START_ROUTINE)HookDLGThread, (LPVOID)0, 0, &m_HookDLGThreadID );
	
	PowerHook();// &m_HookDLG);

	return TRUE;
}


int CHookCostumeApp::ExitInstance()
{
	// TODO: Add your specialized code here and/or call the base class
	TerminateThread(m_HookDLGThread, 0);
	return CWinApp::ExitInstance();
}
