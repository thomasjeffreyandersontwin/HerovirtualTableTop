// ConsoleApplicationCOH.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

/*
int main()
{
    return 0;
}
*/
#include "stdafx.h"
#include <iostream>
#include <limits>

////////////////////////////////
void LoadDLL();
void Close_Game();
void ExecuteCommand(char *commandline);
char *GetHoveredNPCInfo();
char *GetMouseXYZInGame();
void SetHWND();

////////////////////////////////

using namespace std;
int main()
{
	cout << "This app will interact with MFICON++. Make sure MFICON++ is running." << endl;
	cout << "This app will interact with MFICON++. Make sure MFICON++ is running." << endl;

	//Load HookCostume.dll and run game
	LoadDLL();

	//Set user hwnd for NPC hovering;
	SetHWND();

	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	cout << "Press Enter to spawn a sample character" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* spawncommand = "/spawn_npc model_Statesman Sample Character";

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);

	cout << "Get mouse hovering or target NPC information 3 time " << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* npcinfo;
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;


	cout << "Get mouse hovering X, Y, Z information 3 time " << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* mouseinfo;
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cout << "Press Enter to loadcostume" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* loadcostumecommand = "/load_costume spy";
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	/*HERE GOES YOUR CALL TO SEND "spawncommand" TO THE GAME*/

	cout << "Check if the characer is there. Press Enter to delete it" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* clearcommand = "/clear_npc";
	ExecuteCommand(clearcommand);

	/*HERE GOES YOUR CALL TO SEND "clearcommand" TO THE GAME*/

	cout << "Check if the characer is gone. Press Enter to terminate..." << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	Close_Game();

	return 0;
}

//this function is do load HookCostume.dll

#include <windows.h>

//Export function from HookCostume.dll
HMODULE dllhandle = NULL;
typedef BOOL InitGame(HWND hWnd);
typedef BOOL CloseGame(HWND hWnd);
typedef BOOL SetUserHWND(HWND hWnd);
typedef int  Execute_CMD(char *commandline);
typedef char *TGetHoveredNPCInfo();
typedef char *TGetMouseXYZInGame();

//////////////////////////////////////
void LoadDLL()
{
	dllhandle=::LoadLibraryA("HookCostume.dll");

	InitGame *Func_InitGame;
	if (dllhandle != NULL) {
		Func_InitGame = (InitGame *)GetProcAddress(dllhandle, "InitGame");
		if (Func_InitGame != NULL) {
			(Func_InitGame)(NULL);
		}
	}

}

void Close_Game()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	CloseGame *Func_CloseGame;
	if (dllhandle != NULL) {
		Func_CloseGame = (InitGame *)GetProcAddress(dllhandle, "CloseGame");
		if (Func_CloseGame != NULL) {
			(Func_CloseGame)(NULL);
		}
	}
//	FreeLibrary(dllhandle);
}

void ExecuteCommand(char *commandline)
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	Execute_CMD *Func_Execute_CMD;
	if (dllhandle != NULL) {
		Func_Execute_CMD = (Execute_CMD *)GetProcAddress(dllhandle, "ExecuteCommand");
		if (Func_Execute_CMD != NULL) {
			(Func_Execute_CMD)(commandline);
		}
	}

}

void SetHWND()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	SetUserHWND *Func_SetUserHWND;
	if (dllhandle != NULL) {
		Func_SetUserHWND = (SetUserHWND *)GetProcAddress(dllhandle, "SetUserHWND");
		if (Func_SetUserHWND != NULL) {
			(Func_SetUserHWND)(NULL);
		}
	}

}
char *GetHoveredNPCInfo()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	TGetHoveredNPCInfo *Func_GetHoveredNPCInfo;
	if (dllhandle != NULL) {
		Func_GetHoveredNPCInfo = (TGetHoveredNPCInfo *)GetProcAddress(dllhandle, "GetHoveredNPCInfo");
		if (Func_GetHoveredNPCInfo != NULL) {
			return (Func_GetHoveredNPCInfo)();
		}
	}
	return "";
}

char *GetMouseXYZInGame()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	TGetMouseXYZInGame *Func_GetMouseXYZInGame;
	if (dllhandle != NULL) {
		Func_GetMouseXYZInGame = (TGetHoveredNPCInfo *)GetProcAddress(dllhandle, "GetMouseXYZInGame");
		if (Func_GetMouseXYZInGame != NULL) {
			return (Func_GetMouseXYZInGame)();
		}
	}
	return "";
}
