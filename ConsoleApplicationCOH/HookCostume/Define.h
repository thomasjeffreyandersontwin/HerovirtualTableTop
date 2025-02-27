/************************************************************************/
/*	Define code		Mady by PP	2016.05.10	06:34:PM					*/
/************************************************************************/

#pragma	once

#pragma pack(1)
//////////////////////////////////

enum {
	CMD_COMMON = 1,
	CMD_COSTUME,
	COHVAR_MOV,
	COHVAR_SPAWN
};

enum {
	NPC_NAME = 1,
	NPC_ID,
	NPC_START_ADDRESS,
	NPC_DATA_ADDRESS,
	NPC_NAME_ADDRESS,
	NPC_X,
	NPC_Y,
	NPC_Z,
	NPC_R0,
	//NPC_R1,
	NPC_R2,
	//NPC_R3,
	//NPC_R4,
	//NPC_R5,
	NPC_R6,
	//NPC_R7,
	NPC_R8
};

#define		PI								3.141592653589793238462643383279f
#define		DEG2RAD							PI/180
#define		RAD2DEG							180/PI
#define		MAX_PATH_						1024
#define		MAX_NPC_NAME					128
#define		MAX_NPC_COUNT					1024
#define		MAX_ITEM_COUNT					100000
#define		COSUTUEM_EXTEND					_T("costume")
#define		COMMAND_FILE_COMMON				_T("commandline_common.txt")
#define		COMMAND_FILE_MOV				_T("commandline_mov.txt")
#define		COMMAND_FILE_SPAWN				_T("commandline_spawn.txt")

typedef	struct tagDEF_XYZ
{		
	float flX;
	float flY;
	float flZ;
}DEF_XYZ, *PDEF_XYZ;

typedef	struct tagNPC_XYZ
{		
	float flR[9];
	float flX;
	float flY;
	float flZ;
}NPC_XYZ, *PNPC_XYZ;

typedef	struct tagNPC_Info
{	
	DWORD dwStartAddress;
	DWORD dwNameAddress;
	DWORD dwDataAddress;
	char  npcName[MAX_NPC_NAME];	
	DWORD dwNpcID;
	NPC_XYZ m_NpcXYZ;
}NPC_Info, *PNPC_Info;

typedef	struct tagNPC_Data
{	
	DWORD dwNpcCount;
	NPC_Info m_npcInfo[MAX_NPC_COUNT];
}NPC_Data, *PNPC_Data;

#pragma pack()