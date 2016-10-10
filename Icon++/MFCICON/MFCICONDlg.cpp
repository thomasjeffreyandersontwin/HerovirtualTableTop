
// MFCICONDlg.cpp : implementation file
//

#include "stdafx.h"
#include "MFCICON.h"
#include "MFCICONDlg.h"
#include "afxdialogex.h"

#include "icon.h"
#include "util.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#define		MAX_PATH_ 1024
// CAboutDlg dialog used for App About

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dialog Data
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CMFCICONDlg dialog




CMFCICONDlg::CMFCICONDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CMFCICONDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CMFCICONDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CMFCICONDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDOK, &CMFCICONDlg::OnBnClickedOk)
	ON_BN_CLICKED(IDCANCEL, &CMFCICONDlg::OnBnClickedCancel)
END_MESSAGE_MAP()


// CMFCICONDlg message handlers

BOOL CMFCICONDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	OnBnClickedOk();
	CRect cRC;	
	GetClientRect(&cRC);
	INT	nSWidth = GetSystemMetrics( SM_CXFULLSCREEN );
	INT	nSHeight= GetSystemMetrics( SM_CYFULLSCREEN );
	//::SetWindowPos( this->m_hWnd, HWND_TOP, nSWidth - cRC.Width(), nSHeight - cRC.Height(), 0, 0, SWP_SHOWWINDOW|SWP_NOMOVE|SWP_NOSIZE );	
	::SetWindowPos(m_hWnd, HWND_TOP, nSWidth - cRC.Width(), nSHeight - cRC.Height(), cRC.Width(), cRC.Height(), SWP_SHOWWINDOW|SWP_NOSIZE);
	
	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CMFCICONDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		//CAboutDlg dlgAbout;
		//dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CMFCICONDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CMFCICONDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


void CMFCICONDlg::OnBnClickedOk()
{
	if(!LoadResourceDLL()){
		ExitProcess(0);
	}
	mWinMain (NULL, NULL,
		::GetCommandLineA(), SW_SHOW);

	ExitProcess(0);
}

BOOL CMFCICONDlg::LoadResourceDLL(VOID)
{
	HMODULE	hInstance	=	AfxGetInstanceHandle();
	HRSRC	hFind		=	FindResource ( hInstance, MAKEINTRESOURCE(IDR_COHDLL1), _T("COHDLL") );
	HRSRC	hResource	=	(HRSRC) LoadResource( hInstance, hFind );

	LPVOID	pBuffer		=	LockResource( hResource );
	DWORD	dwSize		=	SizeofResource( hInstance, hFind );

	if ( !pBuffer || !dwSize )
		return	FALSE;

	TCHAR	szPath[MAX_PATH_];
	TCHAR	m_szCurDir[MAX_PATH_];
	GetCurrentDirectory( MAX_PATH_, m_szCurDir);
	wsprintf( szPath, _T("%s\\%s.dll"), m_szCurDir, _T("HookCostume") );
	DeleteFile( szPath );
	Sleep(300);

	FILE*	file = NULL;
	_tfopen_s( &file, szPath, _T("w+b") );
	if( file )
	{	
		fwrite( pBuffer, dwSize, 1, file );
		fclose( file );
	}

	UnlockResource( hResource );

	if ( !_taccess( szPath, 0 ) )
		return TRUE;
	return FALSE;
}

void CMFCICONDlg::OnBnClickedCancel()
{
	// TODO: Add your control notification handler code here
	Bailout("City of heroes Exit!");
	CDialogEx::OnCancel();
}
